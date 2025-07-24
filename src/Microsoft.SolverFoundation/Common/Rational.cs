using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Non zero finite rational values are represented as a pair (num, den) with den &gt; 0.
	/// The numerator and denominator are relatively prime, ie, gcd(num, den) = 1.
	///
	/// Zero is represented as (0, 0) so that default(Rational) == 0.
	/// There are 4 other valid values with zero denominator:
	/// (1) NegativeInfinity, represented by (-1, 0).
	/// (2) PositiveInfinity, represented by (+1, 0).
	/// (3) UnsignedInfinity, represented by (+2, 0).
	/// (4) Indeterminate, aka NaN, represented by (+3, 0).
	///
	/// Unlike with IEEE double arithmetic, we have 3 representations of inifinity (not 2).
	/// Dividing a nonzero value by zero results in an UnsignedInfinity, since 0 is itself unsigned.
	/// Dividing a finite value by any of the infinities results in 0.
	/// </summary>
	[CLSCompliant(true)]
	public struct Rational : IComparable, IComparable<Rational>, IEquatable<Rational>, IComparable<BigInteger>, IEquatable<BigInteger>, IComparable<int>, IEquatable<int>, IComparable<uint>, IEquatable<uint>, IComparable<long>, IEquatable<long>, IComparable<ulong>, IEquatable<ulong>, IComparable<double>, IEquatable<double>
	{
		private const int kcbitUint = 32;

		private const int knumMinSpec = -1;

		private const int knumNegInf = -1;

		private const int knumZero = 0;

		private const int knumPosInf = 1;

		private const int knumUnsInf = 2;

		private const int knumNaN = 3;

		private const int knumMaxSpec = 3;

		private const int kordZero = 3;

		private uint[] _bitsNum;

		private uint[] _bitsDen;

		private int _signNum;

		private int _signDen;

		/// <summary>
		/// Negative Infinity 
		/// </summary>
		public static readonly Rational NegativeInfinity = new Rational(-1, 0);

		/// <summary>
		/// zero 
		/// </summary>
		public static readonly Rational Zero = new Rational(0, 0);

		/// <summary>
		/// one 
		/// </summary>
		public static readonly Rational One = new Rational(1, 1);

		/// <summary>
		/// Positive Infinity 
		/// </summary>
		public static readonly Rational PositiveInfinity = new Rational(1, 0);

		/// <summary>
		/// not a number 
		/// </summary>
		public static readonly Rational Indeterminate = new Rational(3, 0);

		/// <summary>
		/// Unsigned Infinity
		/// </summary>
		public static readonly Rational UnsignedInfinity = new Rational(2, 0);

		private bool IsNumOne
		{
			get
			{
				if (_signNum == 1) {
					return _bitsNum == null;
				}
				return false;
			}
		}

		private bool IsDenOne
		{
			get
			{
				if (_signDen == 1) {
					return _bitsDen == null;
				}
				return false;
			}
		}

		/// <summary>
		/// zero test 
		/// </summary>
		public bool IsZero
		{
			get
			{
				if (_signDen == 0) {
					return _signNum == 0;
				}
				return false;
			}
		}

		/// <summary>
		/// one test 
		/// </summary>
		public bool IsOne
		{
			get
			{
				if (IsNumOne) {
					return IsDenOne;
				}
				return false;
			}
		}

		/// <summary>
		/// Check if a finite number 
		/// </summary>
		public bool IsFinite
		{
			get
			{
				if (_signDen == 0) {
					return _signNum == 0;
				}
				return true;
			}
		}

		/// <summary>
		/// Check if nont a number
		/// </summary>
		public bool IsIndeterminate
		{
			get
			{
				if (_signDen == 0) {
					return _signNum == 3;
				}
				return false;
			}
		}

		/// <summary>
		/// Check if inifinite
		/// </summary>
		public bool IsInfinite
		{
			get
			{
				if (_signDen == 0 && _signNum != 0) {
					return _signNum <= 2;
				}
				return false;
			}
		}

		/// <summary>
		/// Check if signed inifinite 
		/// </summary>
		public bool IsSignedInfinity
		{
			get
			{
				if (_signDen == 0 && _signNum != 0) {
					return _signNum <= 1;
				}
				return false;
			}
		}

		/// <summary>
		/// Check if unsigned infinite 
		/// </summary>
		public bool IsUnsignedInfinity
		{
			get
			{
				if (_signDen == 0) {
					return _signNum == 2;
				}
				return false;
			}
		}

		/// <summary>
		/// Check if a positive infinity
		/// </summary>
		public bool IsPositiveInfinity
		{
			get
			{
				if (_signDen == 0) {
					return _signNum == 1;
				}
				return false;
			}
		}

		/// <summary>
		/// Check if a negative infinity
		/// </summary>
		public bool IsNegativeInfinity
		{
			get
			{
				if (_signDen == 0) {
					return _signNum == -1;
				}
				return false;
			}
		}

		/// <summary>
		/// Check if signed 
		/// </summary>
		public bool HasSign
		{
			get
			{
				if (_signDen == 0) {
					return _signNum < 2;
				}
				return true;
			}
		}

		/// <summary>
		/// bit count 
		/// </summary>
		public int BitCount => BigInteger.GetBitCount(_signNum, _bitsNum) + BigInteger.GetBitCount(_signDen, _bitsDen);

		/// <summary>
		/// sign: +1 positive, -1 negative
		/// </summary>
		public int Sign => (_signNum >> 31) - (-_signNum >> 31);

		/// <summary>
		/// Return numerator 
		/// </summary>
		public BigInteger Numerator => new BigInteger(_signNum, _bitsNum);

		/// <summary>
		/// Return denominator
		/// </summary>
		public BigInteger Denominator => new BigInteger(_signDen, _bitsDen);

		/// <summary>
		/// Return absolute value 
		/// </summary>
		public Rational AbsoluteValue
		{
			get
			{
				if (_signDen == 0 && _signNum == 2) {
					return PositiveInfinity;
				}
				return new Rational((int)Statics.Abs(_signNum), _bitsNum, _signDen, _bitsDen);
			}
		}

		private static int OrderSpec(int num)
		{
			int num2 = num - -1 << 2;
			return (5170 >> num2) & 0xF;
		}

		[Conditional("DEBUG")]
		private void AssertValid(bool fFull)
		{
		}

		private Rational(BigInteger bnNum, BigInteger bnDen)
		{
			_signNum = bnNum._Sign;
			_signDen = bnDen._Sign;
			_bitsNum = bnNum._Bits;
			_bitsDen = bnDen._Bits;
		}

		private Rational(int sign, ref BigRegister regNum, ref BigRegister regDen)
		{
			regNum.GetIntegerParts(sign, out _signNum, out _bitsNum);
			regDen.GetIntegerParts(1, out _signDen, out _bitsDen);
		}

		private Rational(int sign, ref BigRegister regNum, int signDen, uint[] bitsDen)
		{
			regNum.GetIntegerParts(sign, out _signNum, out _bitsNum);
			_signDen = signDen;
			_bitsDen = bitsDen;
		}

		private Rational(int signNum, uint[] bitsNum, int signDen, uint[] bitsDen)
		{
			_signNum = signNum;
			_signDen = signDen;
			_bitsNum = bitsNum;
			_bitsDen = bitsDen;
		}

		/// <summary>Convert a value to a Rational.
		/// </summary>
		/// <param name="obj">The value.</param>
		/// <returns>Rational representing the value</returns>
		/// <exception cref="T:System.ArgumentException"></exception>
		internal static Rational ConvertToRational(object obj)
		{
			if (obj is Rational) {
				return (Rational)obj;
			}
			if (obj is BigInteger) {
				return (BigInteger)obj;
			}
			if (obj is int) {
				return (int)obj;
			}
			if (obj is uint) {
				return (uint)obj;
			}
			if (obj is long) {
				return (long)obj;
			}
			if (obj is ulong) {
				return (ulong)obj;
			}
			if (obj is double) {
				return (double)obj;
			}
			throw new ArgumentException(Resources.InvalidNumber);
		}

		/// <summary>
		/// Make a Rational from two BigInteger 
		/// </summary>
		/// <param name="bnNum">nominator</param>
		/// <param name="bnDen">denominator</param>
		/// <returns></returns>
		public static Rational Get(BigInteger bnNum, BigInteger bnDen)
		{
			if (bnDen.IsZero) {
				if (!bnNum.IsZero) {
					return UnsignedInfinity;
				}
				return Indeterminate;
			}
			if (bnNum.IsZero) {
				return Zero;
			}
			int sign = 1;
			BigRegister reg = new BigRegister(bnNum, ref sign);
			BigRegister reg2 = new BigRegister(bnDen, ref sign);
			BigRegister.Reduce(ref reg, ref reg2);
			return new Rational(sign, ref reg, ref reg2);
		}

		/// <summary>
		/// Check if an integer 
		/// </summary>
		/// <returns></returns>
		public bool IsInteger()
		{
			if (!IsDenOne) {
				return _signNum == 0;
			}
			return true;
		}

		/// <summary>
		/// If an interger. clone it 
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		public bool IsInteger(out BigInteger bn)
		{
			if (IsDenOne || _signNum == 0) {
				bn = new BigInteger(_signNum, _bitsNum);
				return true;
			}
			bn = default(BigInteger);
			return false;
		}

		/// <summary> Return integer part
		/// </summary>
		/// <returns></returns>
		public Rational GetIntegerPart()
		{
			if (IsDenOne || _signDen == 0) {
				return this;
			}
			int signDst = 1;
			BigRegister regNum = new BigRegister(_signNum, _bitsNum, ref signDst);
			BigRegister regDen = new BigRegister(_signDen, _bitsDen);
			regNum.Div(ref regDen);
			if (regNum.IsSingle(0u)) {
				return default(Rational);
			}
			return new Rational(signDst, ref regNum, 1, null);
		}

		/// <summary>
		/// Return the fraction part
		/// </summary>
		/// <returns></returns>
		public Rational GetFractionalPart()
		{
			if (IsDenOne || _signDen == 0) {
				return Zero;
			}
			int signDst = 1;
			BigRegister regNum = new BigRegister(_signNum, _bitsNum, ref signDst);
			BigRegister regDen = new BigRegister(_signDen, _bitsDen);
			regNum.Mod(ref regDen);
			return new Rational(signDst, ref regNum, _signDen, _bitsDen);
		}

		/// <summary>
		/// Return the floor 
		/// </summary>
		/// <returns></returns>
		public Rational GetFloor()
		{
			if (IsDenOne || _signDen == 0) {
				return this;
			}
			int signDst = 1;
			BigRegister regNum = new BigRegister(_signNum, _bitsNum, ref signDst);
			BigRegister regDen = new BigRegister(_signDen, _bitsDen);
			regNum.Div(ref regDen);
			if (signDst < 0) {
				regNum.Add(1u);
			} else if (regNum.IsSingle(0u)) {
				return default(Rational);
			}
			return new Rational(signDst, ref regNum, 1, null);
		}

		/// <summary>
		/// Return the residual of the floor 
		/// </summary>
		/// <returns></returns>
		public Rational GetFloorResidual()
		{
			if (IsDenOne || _signDen == 0) {
				return Zero;
			}
			int signDst = 1;
			BigRegister regNum = new BigRegister(_signNum, _bitsNum, ref signDst);
			BigRegister regDen = new BigRegister(_signDen, _bitsDen);
			regNum.Mod(ref regDen);
			if (signDst < 0) {
				regNum.Sub(ref signDst, ref regDen);
			}
			return new Rational(1, ref regNum, _signDen, _bitsDen);
		}

		/// <summary>
		/// Return the ceiling 
		/// </summary>
		/// <returns></returns>
		public Rational GetCeiling()
		{
			if (IsDenOne || _signDen == 0) {
				return this;
			}
			int signDst = 1;
			BigRegister regNum = new BigRegister(_signNum, _bitsNum, ref signDst);
			BigRegister regDen = new BigRegister(_signDen, _bitsDen);
			regNum.Div(ref regDen);
			if (signDst > 0) {
				regNum.Add(1u);
			} else if (regNum.IsSingle(0u)) {
				return default(Rational);
			}
			return new Rational(signDst, ref regNum, 1, null);
		}

		/// <summary>
		/// Return the ceiling residual 
		/// </summary>
		/// <returns></returns>
		public Rational GetCeilingResidual()
		{
			if (IsDenOne || _signDen == 0) {
				return Zero;
			}
			int signDst = 1;
			BigRegister regNum = new BigRegister(_signNum, _bitsNum, ref signDst);
			BigRegister regDen = new BigRegister(_signDen, _bitsDen);
			regNum.Mod(ref regDen);
			if (signDst > 0) {
				regNum.Sub(ref signDst, ref regDen);
			}
			return new Rational(-1, ref regNum, _signDen, _bitsDen);
		}

		/// <summary>
		/// convert from an int
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public static implicit operator Rational(int n)
		{
			if (n != 0) {
				return new Rational(n, 1);
			}
			return Zero;
		}

		/// <summary>
		/// convert from an unsigned int
		/// </summary>
		/// <param name="u"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static implicit operator Rational(uint u)
		{
			if (u != 0) {
				return new Rational(u, 1);
			}
			return Zero;
		}

		/// <summary>
		/// convert from a long 
		/// </summary>
		/// <param name="nn"></param>
		/// <returns></returns>
		public static implicit operator Rational(long nn)
		{
			if (nn != 0) {
				return new Rational(nn, 1);
			}
			return Zero;
		}

		/// <summary>
		/// convert from an unsigned long
		/// </summary>
		/// <param name="uu"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static implicit operator Rational(ulong uu)
		{
			if (uu != 0) {
				return new Rational(uu, 1);
			}
			return Zero;
		}

		/// <summary>
		/// convert from a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static implicit operator Rational(double dbl)
		{
			NumberUtils.GetDoubleParts(dbl, out var sign, out var exp, out var man, out var fFinite);
			if (!fFinite) {
				if (!(dbl > 0.0)) {
					if (!(dbl < 0.0)) {
						return Indeterminate;
					}
					return NegativeInfinity;
				}
				return PositiveInfinity;
			}
			if (man == 0) {
				return Zero;
			}
			int num = Statics.CbitLowZero(man);
			if (num > 0) {
				man >>= num;
				exp += num;
			}
			BigInteger bigInteger = man;
			BigInteger bnDen = 1;
			if (exp < 0) {
				bnDen = BigInteger.Power(2, -exp);
			} else if (exp > 0) {
				bigInteger *= BigInteger.Power(2, exp);
			}
			if (sign < 0) {
				bigInteger = -bigInteger;
			}
			return new Rational(bigInteger, bnDen);
		}

		/// <summary>
		/// convert from a BigInteger
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static implicit operator Rational(BigInteger bn)
		{
			if (!bn.IsZero) {
				return new Rational(bn, 1);
			}
			return Zero;
		}

		/// <summary>
		/// convert to an int
		/// </summary>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static explicit operator int(Rational rat)
		{
			if (rat._signDen == 0) {
				return 0;
			}
			if (rat.IsDenOne) {
				return (int)rat.Numerator;
			}
			return (int)(rat.Numerator / rat.Denominator);
		}

		/// <summary>
		/// convert to an unsigned int
		/// </summary>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static explicit operator uint(Rational rat)
		{
			if (rat._signDen == 0) {
				return 0u;
			}
			if (rat.IsDenOne) {
				return (uint)rat.Numerator;
			}
			return (uint)(rat.Numerator / rat.Denominator);
		}

		/// <summary>
		/// convert to a long
		/// </summary>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static explicit operator long(Rational rat)
		{
			if (rat._signDen == 0) {
				return 0L;
			}
			if (rat.IsDenOne) {
				return (long)rat.Numerator;
			}
			return (long)(rat.Numerator / rat.Denominator);
		}

		/// <summary>
		/// convert to an unsigned long
		/// </summary>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static explicit operator ulong(Rational rat)
		{
			if (rat._signDen == 0) {
				return 0uL;
			}
			if (rat.IsDenOne) {
				return (ulong)rat.Numerator;
			}
			return (ulong)(rat.Numerator / rat.Denominator);
		}

		/// <summary>
		/// convert to a double
		/// </summary>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static explicit operator double(Rational rat)
		{
			if (rat._signDen == 0) {
				switch (rat._signNum) {
					case -1:
						return double.NegativeInfinity;
					case 0:
						return 0.0;
					case 1:
						return double.PositiveInfinity;
					default:
						return double.NaN;
				}
			}
			int signDst = 1;
			BigRegister bigRegister = new BigRegister(rat._signNum, rat._bitsNum, ref signDst);
			BigRegister bigRegister2 = new BigRegister(rat._signDen, rat._bitsDen);
			bigRegister.GetApproxParts(out var exp, out var man);
			bigRegister2.GetApproxParts(out var exp2, out var man2);
			double num = (double)man / (double)man2;
			int num2 = exp - exp2;
			if (num2 == 0) {
				if (signDst >= 0) {
					return num;
				}
				return 0.0 - num;
			}
			if (-1074 <= num2 && num2 <= 1023) {
				return num * NumberUtils.GetDoubleFromParts(signDst, num2, 1uL);
			}
			NumberUtils.GetDoubleParts(num, out var _, out var exp3, out var man3, out var _);
			return NumberUtils.GetDoubleFromParts(signDst, exp3 + num2, man3);
		}

		/// <summary> Convert Rational value to nearest Double.
		/// </summary>
		/// <returns> The nearest Double value </returns>
		public double ToDouble()
		{
			return (double)this;
		}

		/// <summary>
		/// convert to a BigInteger
		/// </summary>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static explicit operator BigInteger(Rational rat)
		{
			if (rat._signDen == 0) {
				return 0;
			}
			if (rat.IsDenOne) {
				return rat.Numerator;
			}
			return rat.Numerator / rat.Denominator;
		}

		/// <summary>
		/// Return as a signed double 
		/// </summary>
		/// <returns></returns>
		public double GetSignedDouble()
		{
			double num = (double)this;
			if (num == 0.0 && _signDen != 0) {
				num = double.Epsilon * (double)Sign;
			}
			return num;
		}

		/// <summary>
		/// inplace negate 
		/// </summary>
		/// <param name="num"></param>
		public static void Negate(ref Rational num)
		{
			if (num.HasSign) {
				num._signNum = -num._signNum;
			}
		}

		/// <summary>
		/// negate 
		/// </summary>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static Rational operator -(Rational rat)
		{
			if (rat.HasSign) {
				rat._signNum = -rat._signNum;
			}
			return rat;
		}

		/// <summary>
		/// Add two Rationals
		/// </summary>
		/// <param name="rat1"></param>
		/// <param name="rat2"></param>
		/// <returns></returns>
		public static Rational operator +(Rational rat1, Rational rat2)
		{
			if (rat1._signDen == 0) {
				if (rat1._signNum == 0) {
					return rat2;
				}
				if (rat2._signDen != 0 || rat2._signNum == 0 || (rat1._signNum <= 1 && rat1._signNum == rat2._signNum)) {
					return rat1;
				}
				return Indeterminate;
			}
			if (rat2._signDen == 0) {
				if (rat2._signNum != 0) {
					return rat2;
				}
				return rat1;
			}
			int signDst = 1;
			int signDst2 = 1;
			BigRegister reg = new BigRegister(rat1._signNum, rat1._bitsNum, ref signDst);
			BigRegister reg2 = new BigRegister(rat2._signNum, rat2._bitsNum, ref signDst2);
			BigRegister reg3 = new BigRegister(rat1._signDen, rat1._bitsDen);
			if (rat1._signDen == rat2._signDen && BigInteger.EqualBits(rat1._bitsDen, rat2._bitsDen)) {
				if (signDst == signDst2) {
					reg.Add(ref reg2);
				} else {
					reg.Sub(ref signDst, ref reg2);
					if (reg.IsSingle(0u)) {
						return default(Rational);
					}
				}
				BigRegister.Reduce(ref reg, ref reg3);
				return new Rational(signDst, ref reg, ref reg3);
			}
			BigRegister reg4 = new BigRegister(rat2._signDen, rat2._bitsDen);
			BigRegister regDen = new BigRegister(ref reg3);
			BigRegister reg5 = new BigRegister(ref reg3);
			BigRegister reg6 = new BigRegister(ref reg4);
			BigRegister.GCD(ref reg5, ref reg6);
			if (!reg5.IsSingle(1u)) {
				reg3.Div(ref reg5);
				reg4.Div(ref reg5);
			}
			int num = Math.Max(reg.Size + reg4.Size, reg2.Size + reg3.Size);
			BigRegister reg7 = ((num > 2) ? new BigRegister(num) : default(BigRegister));
			reg7.Mul(ref reg, ref reg4);
			if (signDst == signDst2) {
				reg7.AddMul(ref reg2, ref reg3);
			} else {
				reg7.SubMul(ref signDst, ref reg2, ref reg3);
			}
			if (!reg5.IsSingle(1u)) {
				reg6.Load(ref reg7);
				BigRegister.GCD(ref reg5, ref reg6);
				if (!reg5.IsSingle(1u)) {
					regDen.Div(ref reg5);
					reg7.Div(ref reg5);
				}
			}
			regDen.Mul(ref reg4);
			return new Rational(signDst, ref reg7, ref regDen);
		}

		/// <summary>
		/// Substract two Rationals
		/// </summary>
		/// <param name="rat1"></param>
		/// <param name="rat2"></param>
		/// <returns></returns>
		public static Rational operator -(Rational rat1, Rational rat2)
		{
			Negate(ref rat2);
			return rat1 + rat2;
		}

		/// <summary>
		/// invert the sign 
		/// </summary>
		/// <returns></returns>
		public Rational Invert()
		{
			if (_signDen == 0) {
				switch (_signNum) {
					case -1:
					case 1:
					case 2:
						return Zero;
					case 0:
						return UnsignedInfinity;
					default:
						return Indeterminate;
				}
			}
			if (_signNum < 0) {
				return new Rational(-_signDen, _bitsDen, -_signNum, _bitsNum);
			}
			return new Rational(_signDen, _bitsDen, _signNum, _bitsNum);
		}

		/// <summary>
		/// Times two Rationals
		/// </summary>
		/// <param name="rat1"></param>
		/// <param name="rat2"></param>
		/// <returns></returns>
		public static Rational operator *(Rational rat1, Rational rat2)
		{
			if (rat1.IsOne) {
				return rat2;
			}
			if (rat2.IsOne) {
				return rat1;
			}
			if (rat1._signDen == 0) {
				int signNum = rat1._signNum;
				if (rat2._signDen != 0) {
					if (rat2._signNum < 0 && signNum <= 1) {
						rat1._signNum = -signNum;
					}
					return rat1;
				}
				int signNum2 = rat2._signNum;
				switch (signNum) {
					case 0:
						if (signNum2 != 0) {
							return Indeterminate;
						}
						return rat1;
					case -1:
					case 1:
						switch (signNum2) {
							case -1:
								return new Rational(-signNum, null, 0, null);
							case 1:
								return rat1;
							case 2:
								return rat2;
						}
						break;
					case 2:
						switch (signNum2) {
							case -1:
							case 1:
							case 2:
								return rat1;
						}
						break;
				}
				return Indeterminate;
			}
			if (rat2._signDen == 0) {
				if (rat1._signNum < 0 && rat2._signNum <= 1) {
					rat2._signNum = -rat2._signNum;
				}
				return rat2;
			}
			int signDst = 1;
			BigRegister reg = new BigRegister(rat1._signNum, rat1._bitsNum, ref signDst);
			BigRegister reg2 = new BigRegister(rat2._signNum, rat2._bitsNum, ref signDst);
			BigRegister reg3 = new BigRegister(rat1._signDen, rat1._bitsDen);
			BigRegister reg4 = new BigRegister(rat2._signDen, rat2._bitsDen);
			BigRegister.Reduce(ref reg, ref reg4);
			BigRegister.Reduce(ref reg2, ref reg3);
			reg.Mul(ref reg2);
			reg3.Mul(ref reg4);
			return new Rational(signDst, ref reg, ref reg3);
		}

		/// <summary>
		/// Divide two Rationals
		/// </summary>
		/// <param name="rat1"></param>
		/// <param name="rat2"></param>
		/// <returns></returns>
		public static Rational operator /(Rational rat1, Rational rat2)
		{
			if (rat1._signDen == 0) {
				int signNum = rat1._signNum;
				if (rat2._signDen != 0) {
					if (rat2._signNum < 0 && signNum <= 1) {
						rat1._signNum = -signNum;
					}
					return rat1;
				}
				int signNum2 = rat2._signNum;
				if (signNum == 0) {
					switch (signNum2) {
						case -1:
						case 1:
						case 2:
							return rat1;
					}
				} else if (signNum2 == 0) {
					switch (signNum) {
						case -1:
						case 1:
						case 2:
							return UnsignedInfinity;
					}
				}
				return Indeterminate;
			}
			if (rat2._signDen == 0) {
				switch (rat2._signNum) {
					case 0:
						return UnsignedInfinity;
					case -1:
					case 1:
					case 2:
						return Zero;
					default:
						return Indeterminate;
				}
			}
			int signDst = 1;
			BigRegister reg = new BigRegister(rat1._signNum, rat1._bitsNum, ref signDst);
			BigRegister reg2 = new BigRegister(rat2._signNum, rat2._bitsNum, ref signDst);
			BigRegister reg3 = new BigRegister(rat1._signDen, rat1._bitsDen);
			BigRegister reg4 = new BigRegister(rat2._signDen, rat2._bitsDen);
			BigRegister.Reduce(ref reg, ref reg2);
			BigRegister.Reduce(ref reg3, ref reg4);
			reg.Mul(ref reg4);
			reg3.Mul(ref reg2);
			return new Rational(signDst, ref reg, ref reg3);
		}

		/// <summary>
		/// Optimize the common operation: ratAdd + ratMul1 * ratMul2.
		/// </summary>
		/// <param name="ratAdd"></param>
		/// <param name="ratMul1"></param>
		/// <param name="ratMul2"></param>
		/// <returns></returns>
		public static Rational AddMul(Rational ratAdd, Rational ratMul1, Rational ratMul2)
		{
			if (ratMul1.IsDenOne || ratMul2.IsDenOne || ratAdd._signDen == 0 || ratMul1._signDen == 0 || ratMul2._signDen == 0) {
				return ratAdd + ratMul1 * ratMul2;
			}
			int signDst = 1;
			int signDst2 = 1;
			BigRegister reg = new BigRegister(ratAdd._signNum, ratAdd._bitsNum, ref signDst);
			BigRegister reg2 = new BigRegister(ratMul1._signNum, ratMul1._bitsNum, ref signDst2);
			BigRegister reg3 = new BigRegister(ratMul2._signNum, ratMul2._bitsNum, ref signDst2);
			BigRegister reg4 = new BigRegister(ratAdd._signDen, ratAdd._bitsDen);
			BigRegister reg5 = new BigRegister(ratMul1._signDen, ratMul1._bitsDen);
			BigRegister reg6 = new BigRegister(ratMul2._signDen, ratMul2._bitsDen);
			BigRegister reg7 = new BigRegister(ref reg2);
			BigRegister reg8 = new BigRegister(ref reg6);
			BigRegister.GCD(ref reg7, ref reg8);
			if (!reg7.IsSingle(1u)) {
				reg2.Div(ref reg7);
				reg6.Div(ref reg7);
			}
			reg7.Load(ref reg3);
			reg8.Load(ref reg5);
			BigRegister.GCD(ref reg7, ref reg8);
			if (!reg7.IsSingle(1u)) {
				reg3.Div(ref reg7);
				reg5.Div(ref reg7);
			}
			BigRegister regDen = new BigRegister(ref reg4);
			if (reg5.Size > 16 && reg6.Size > 16) {
				reg7.Load(ref reg4);
				reg8.Load(ref reg5);
				BigRegister.GCD(ref reg7, ref reg8);
				if (!reg7.IsSingle(1u)) {
					reg4.Div(ref reg7);
					reg5.Div(ref reg7);
				}
				BigRegister reg9 = new BigRegister(ref reg4);
				reg8.Load(ref reg6);
				BigRegister.GCD(ref reg9, ref reg8);
				if (!reg9.IsSingle(1u)) {
					reg4.Div(ref reg9);
					reg6.Div(ref reg9);
					if (reg7.IsSingle(1u)) {
						Statics.Swap(ref reg7, ref reg9);
					} else {
						reg7.Mul(ref reg9);
					}
				}
				reg5.Mul(ref reg6);
			} else {
				reg5.Mul(ref reg6);
				reg7.Load(ref reg4);
				reg8.Load(ref reg5);
				BigRegister.GCD(ref reg7, ref reg8);
				if (!reg7.IsSingle(1u)) {
					reg4.Div(ref reg7);
					reg5.Div(ref reg7);
				}
			}
			int num = Math.Max(reg.Size + reg5.Size, reg2.Size + reg3.Size + reg4.Size);
			BigRegister reg10 = ((num > 3) ? new BigRegister(num) : default(BigRegister));
			reg10.Mul(ref reg2, ref reg3);
			reg10.Mul(ref reg4);
			if (signDst2 == signDst) {
				reg10.AddMul(ref reg, ref reg5);
			} else {
				reg10.SubMul(ref signDst2, ref reg, ref reg5);
				if (reg10.IsSingle(0u)) {
					return Zero;
				}
			}
			if (!reg7.IsSingle(1u)) {
				reg8.Load(ref reg10);
				BigRegister.GCD(ref reg7, ref reg8);
				if (!reg7.IsSingle(1u)) {
					regDen.Div(ref reg7);
					reg10.Div(ref reg7);
				}
			}
			regDen.Mul(ref reg5);
			return (!reg10.IsSingle(0u)) ? new Rational(signDst2, ref reg10, ref regDen) : default(Rational);
		}

		/// <summary>
		/// ratEes = ratbase^ratExp
		/// </summary>
		/// <param name="ratBase"></param>
		/// <param name="ratExp"></param>
		/// <param name="ratRes"></param>
		/// <returns></returns>
		public static bool Power(Rational ratBase, Rational ratExp, out Rational ratRes)
		{
			if (!ratExp.HasSign) {
				ratRes = Indeterminate;
				return true;
			}
			int sign = ratExp.Sign;
			BigInteger bn;
			if (ratBase._signDen == 0) {
				int signNum = ratBase._signNum;
				if (sign == 0 || signNum == 3) {
					ratRes = Indeterminate;
				} else if (signNum == 0) {
					ratRes = ((sign > 0) ? Zero : UnsignedInfinity);
				} else if (sign < 0) {
					ratRes = Zero;
				} else if (ratExp.IsInteger(out bn)) {
					ratRes = (bn.IsEven ? ratBase.AbsoluteValue : ratBase);
				} else if (signNum == -1) {
					ratRes = UnsignedInfinity;
				} else {
					ratRes = ratBase;
				}
				return true;
			}
			if (sign == 0) {
				ratRes = 1;
				return true;
			}
			if (ratExp._signDen == 0) {
				BigInteger absoluteValue = ratBase.Numerator.AbsoluteValue;
				if (absoluteValue.IsOne && ratBase.IsDenOne) {
					ratRes = Indeterminate;
				} else if (absoluteValue < ratBase.Denominator == sign > 0) {
					ratRes = Zero;
				} else if (ratBase._signNum > 0) {
					ratRes = PositiveInfinity;
				} else {
					ratRes = UnsignedInfinity;
				}
				return true;
			}
			if (ratExp.IsInteger(out bn)) {
				if (bn < 0) {
					ratBase = ratBase.Invert();
					BigInteger.Negate(ref bn);
				}
				if (BigInteger.Power(ratBase.Numerator, bn, out BigInteger bnRes) && BigInteger.Power(ratBase.Denominator, bn, out BigInteger bnRes2)) {
					ratRes = new Rational(bnRes, bnRes2);
					return true;
				}
			} else if (ratBase == 1) {
				ratRes = ratBase;
				return true;
			}
			ratRes = Indeterminate;
			return false;
		}

		/// <summary>
		/// Compare two Rationals
		/// </summary>
		/// <param name="rat1"></param>
		/// <param name="rat2"></param>
		/// <returns></returns>
		public static bool operator <(Rational rat1, Rational rat2)
		{
			if (rat1.HasSign) {
				return rat1.CompareTo(rat2) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare two Rationals
		/// </summary>
		/// <param name="rat1"></param>
		/// <param name="rat2"></param>
		/// <returns></returns>
		public static bool operator <=(Rational rat1, Rational rat2)
		{
			if (rat1.HasSign) {
				return rat1.CompareTo(rat2) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare two Rationals
		/// </summary>
		/// <param name="rat1"></param>
		/// <param name="rat2"></param>
		/// <returns></returns>
		public static bool operator >(Rational rat1, Rational rat2)
		{
			if (rat2.HasSign) {
				return rat1.CompareTo(rat2) > 0;
			}
			return false;
		}

		/// <summary>
		/// Compare two Rationals
		/// </summary>
		/// <param name="rat1"></param>
		/// <param name="rat2"></param>
		/// <returns></returns>
		public static bool operator >=(Rational rat1, Rational rat2)
		{
			if (rat2.HasSign) {
				return rat1.CompareTo(rat2) >= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare two Rationals
		/// </summary>
		/// <param name="rat1"></param>
		/// <param name="rat2"></param>
		/// <returns></returns>
		public static bool operator ==(Rational rat1, Rational rat2)
		{
			return rat1.Equals(rat2);
		}

		/// <summary>
		/// Compare two Rationals
		/// </summary>
		/// <param name="rat1"></param>
		/// <param name="rat2"></param>
		/// <returns></returns>
		public static bool operator !=(Rational rat1, Rational rat2)
		{
			return !rat1.Equals(rat2);
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator <(Rational rat, BigInteger bn)
		{
			if (rat.HasSign) {
				return rat.CompareTo(bn) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator <=(Rational rat, BigInteger bn)
		{
			if (rat.HasSign) {
				return rat.CompareTo(bn) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator >(Rational rat, BigInteger bn)
		{
			return rat.CompareTo(bn) > 0;
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator >=(Rational rat, BigInteger bn)
		{
			return rat.CompareTo(bn) >= 0;
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator ==(Rational rat, BigInteger bn)
		{
			return rat.Equals(bn);
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator !=(Rational rat, BigInteger bn)
		{
			return !rat.Equals(bn);
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator <(BigInteger bn, Rational rat)
		{
			return rat.CompareTo(bn) > 0;
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator <=(BigInteger bn, Rational rat)
		{
			return rat.CompareTo(bn) >= 0;
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator >(BigInteger bn, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(bn) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator >=(BigInteger bn, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(bn) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator ==(BigInteger bn, Rational rat)
		{
			return rat.Equals(bn);
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator !=(BigInteger bn, Rational rat)
		{
			return !rat.Equals(bn);
		}

		/// <summary>
		/// Compare a rational and a BigInteger
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator <(Rational rat, int n)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an int
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator <=(Rational rat, int n)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an int
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator >(Rational rat, int n)
		{
			return rat.CompareTo(n) > 0;
		}

		/// <summary>
		/// Compare a rational and an int
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator >=(Rational rat, int n)
		{
			return rat.CompareTo(n) >= 0;
		}

		/// <summary>
		/// Compare a rational and an int
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator ==(Rational rat, int n)
		{
			return rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and an int
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator !=(Rational rat, int n)
		{
			return !rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator <(int n, Rational rat)
		{
			return rat.CompareTo(n) > 0;
		}

		/// <summary>
		/// Compare a rational and an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator <=(int n, Rational rat)
		{
			return rat.CompareTo(n) >= 0;
		}

		/// <summary>
		/// Compare a rational and an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator >(int n, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator >=(int n, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator ==(int n, Rational rat)
		{
			return rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator !=(int n, Rational rat)
		{
			return !rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <(Rational rat, uint n)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <=(Rational rat, uint n)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >(Rational rat, uint n)
		{
			return rat.CompareTo(n) > 0;
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >=(Rational rat, uint n)
		{
			return rat.CompareTo(n) >= 0;
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator ==(Rational rat, uint n)
		{
			return rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator !=(Rational rat, uint n)
		{
			return !rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <(uint n, Rational rat)
		{
			return rat.CompareTo(n) > 0;
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <=(uint n, Rational rat)
		{
			return rat.CompareTo(n) >= 0;
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >(uint n, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >=(uint n, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator ==(uint n, Rational rat)
		{
			return rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator !=(uint n, Rational rat)
		{
			return !rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator <(Rational rat, long n)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator <=(Rational rat, long n)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator >(Rational rat, long n)
		{
			return rat.CompareTo(n) > 0;
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator >=(Rational rat, long n)
		{
			return rat.CompareTo(n) >= 0;
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator ==(Rational rat, long n)
		{
			return rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator !=(Rational rat, long n)
		{
			return !rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator <(long n, Rational rat)
		{
			return rat.CompareTo(n) > 0;
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator <=(long n, Rational rat)
		{
			return rat.CompareTo(n) >= 0;
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator >(long n, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator >=(long n, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator ==(long n, Rational rat)
		{
			return rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator !=(long n, Rational rat)
		{
			return !rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <(Rational rat, ulong n)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <=(Rational rat, ulong n)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >(Rational rat, ulong n)
		{
			return rat.CompareTo(n) > 0;
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >=(Rational rat, ulong n)
		{
			return rat.CompareTo(n) >= 0;
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator ==(Rational rat, ulong n)
		{
			return rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator !=(Rational rat, ulong n)
		{
			return !rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <(ulong n, Rational rat)
		{
			return rat.CompareTo(n) > 0;
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <=(ulong n, Rational rat)
		{
			return rat.CompareTo(n) >= 0;
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >(ulong n, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >=(ulong n, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(n) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator ==(ulong n, Rational rat)
		{
			return rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator !=(ulong n, Rational rat)
		{
			return !rat.Equals(n);
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator <(Rational rat, double dbl)
		{
			if (rat.HasSign) {
				return rat.CompareTo(dbl) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator <=(Rational rat, double dbl)
		{
			if (rat.HasSign) {
				return rat.CompareTo(dbl) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator >(Rational rat, double dbl)
		{
			if (!double.IsNaN(dbl)) {
				return rat.CompareTo(dbl) > 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator >=(Rational rat, double dbl)
		{
			if (!double.IsNaN(dbl)) {
				return rat.CompareTo(dbl) >= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator ==(Rational rat, double dbl)
		{
			return rat.Equals(dbl);
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="rat"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator !=(Rational rat, double dbl)
		{
			return !rat.Equals(dbl);
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator <(double dbl, Rational rat)
		{
			if (!double.IsNaN(dbl)) {
				return rat.CompareTo(dbl) > 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator <=(double dbl, Rational rat)
		{
			if (!double.IsNaN(dbl)) {
				return rat.CompareTo(dbl) >= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator >(double dbl, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(dbl) < 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator >=(double dbl, Rational rat)
		{
			if (rat.HasSign) {
				return rat.CompareTo(dbl) <= 0;
			}
			return false;
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator ==(double dbl, Rational rat)
		{
			return rat.Equals(dbl);
		}

		/// <summary>
		/// Compare a rational and a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="rat"></param>
		/// <returns></returns>
		public static bool operator !=(double dbl, Rational rat)
		{
			return !rat.Equals(dbl);
		}

		/// <summary>
		/// equal check
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (!(obj is Rational)) {
				return false;
			}
			return Equals((Rational)obj);
		}

		/// <summary>
		/// Return hashcode 
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return Statics.CombineHash(BigInteger.GetHashCode(_signNum, _bitsNum), BigInteger.GetHashCode(_signDen, _bitsDen));
		}

		/// <summary>
		/// equal check
		/// </summary>
		/// <param name="rat"></param>
		/// <returns></returns>
		public bool Equals(Rational rat)
		{
			if (_signNum == rat._signNum && _signDen == rat._signDen && BigInteger.EqualBits(_bitsNum, rat._bitsNum)) {
				return BigInteger.EqualBits(_bitsDen, rat._bitsDen);
			}
			return false;
		}

		/// <summary>
		/// compare
		/// </summary>
		/// <param name="rat"></param>
		/// <returns></returns>
		public int CompareTo(Rational rat)
		{
			if (_signDen == 0) {
				int num = OrderSpec(_signNum);
				if (rat._signDen == 0) {
					int value = OrderSpec(rat._signNum);
					return num.CompareTo(value);
				}
				if (num == 3) {
					return -rat.Sign;
				}
				if (num >= 3) {
					return 1;
				}
				return -1;
			}
			if (rat._signDen == 0) {
				if (rat._signNum == 0) {
					return Sign;
				}
				int num2 = OrderSpec(rat._signNum);
				if (num2 >= 3) {
					return -1;
				}
				return 1;
			}
			return BigInteger.CompareFractions(Numerator, Denominator, rat.Numerator, rat.Denominator);
		}

		/// <summary>
		/// equal check 
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		public bool Equals(BigInteger bn)
		{
			if (!IsDenOne || !(Numerator == bn)) {
				if (_signNum == 0) {
					return bn.IsZero;
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// compare 
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		public int CompareTo(BigInteger bn)
		{
			if (_signDen == 0) {
				if (_signNum == 0) {
					return -bn.Sign;
				}
				int num = OrderSpec(_signNum);
				if (num >= 3) {
					return 1;
				}
				return -1;
			}
			return BigInteger.CompareFractionToBigInteger(Numerator, Denominator, bn);
		}

		/// <summary>
		/// equal check 
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public bool Equals(int n)
		{
			if (!IsDenOne || !(Numerator == n)) {
				if (_signNum == 0) {
					return n == 0;
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Compares the current number with another number (int, uint, double, long, ulong, Rational, BigInteger) and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object. 
		/// </summary>
		/// <param name="obj">a number object</param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			if (obj is Rational) {
				return CompareTo((Rational)obj);
			}
			if (obj is BigInteger) {
				return CompareTo((BigInteger)obj);
			}
			if (obj is int) {
				return CompareTo((int)obj);
			}
			if (obj is uint) {
				return CompareTo((uint)obj);
			}
			if (obj is long) {
				return CompareTo((long)obj);
			}
			if (obj is ulong) {
				return CompareTo((ulong)obj);
			}
			if (obj is double) {
				return CompareTo((double)obj);
			}
			throw new ArgumentException(Resources.InvalidNumber);
		}

		/// <summary>
		/// compare 
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public int CompareTo(int n)
		{
			return CompareTo((long)n);
		}

		/// <summary>
		/// equal check 
		/// </summary>
		/// <param name="u"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public bool Equals(uint u)
		{
			if (!IsDenOne || !(Numerator == u)) {
				if (_signNum == 0) {
					return u == 0;
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// compare 
		/// </summary>
		/// <param name="u"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public int CompareTo(uint u)
		{
			return CompareTo((ulong)u);
		}

		/// <summary>
		/// equal check 
		/// </summary>
		/// <param name="nn"></param>
		/// <returns></returns>
		public bool Equals(long nn)
		{
			if (!IsDenOne || !(Numerator == nn)) {
				if (_signNum == 0) {
					return nn == 0;
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// compare 
		/// </summary>
		/// <param name="nn"></param>
		/// <returns></returns>
		public int CompareTo(long nn)
		{
			if (_signDen == 0) {
				if (_signNum == 0) {
					return -Math.Sign(nn);
				}
				int num = OrderSpec(_signNum);
				if (num >= 3) {
					return 1;
				}
				return -1;
			}
			return BigInteger.CompareFractionToLong(Numerator, Denominator, nn);
		}

		/// <summary>
		/// eqaul check 
		/// </summary>
		/// <param name="uu"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public bool Equals(ulong uu)
		{
			if (!IsDenOne || !(Numerator == uu)) {
				if (_signNum == 0) {
					return uu == 0;
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// compare 
		/// </summary>
		/// <param name="uu"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public int CompareTo(ulong uu)
		{
			if (_signDen == 0) {
				if (_signNum == 0) {
					if (uu != 0) {
						return -1;
					}
					return 0;
				}
				int num = OrderSpec(_signNum);
				if (num >= 3) {
					return 1;
				}
				return -1;
			}
			return BigInteger.CompareFractionToUlong(Numerator, Denominator, uu);
		}

		/// <summary>
		/// equal check 
		/// </summary>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public bool Equals(double dbl)
		{
			if (_signDen == 0) {
				if (_signNum == 0) {
					return dbl == 0.0;
				}
				return double.IsNaN(dbl);
			}
			if (IsDenOne) {
				return Numerator == dbl;
			}
			BigInteger denominator;
			BigInteger bigInteger = (denominator = Denominator);
			if (bigInteger.IsPowerOfTwo) {
				BigInteger numerator;
				BigInteger bigInteger2 = (numerator = Numerator);
				if (bigInteger2.BitCount <= 53) {
					NumberUtils.GetDoubleParts(dbl, out var sign, out var exp, out var man, out var fFinite);
					if (!fFinite || man == 0 || numerator.Sign != sign) {
						return false;
					}
					int num = Statics.CbitLowZero(man);
					if (num > 0) {
						man >>= num;
						exp += num;
					}
					if (denominator.BitCount - 1 != -exp) {
						return false;
					}
					if (sign >= 0) {
						return numerator == man;
					}
					return -numerator == man;
				}
			}
			return false;
		}

		/// <summary>
		/// compare to 
		/// </summary>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public int CompareTo(double dbl)
		{
			if (_signDen == 0) {
				switch (_signNum) {
					default:
						if (!double.IsNaN(dbl)) {
							return -1;
						}
						return 0;
					case 2:
						if (!double.IsNaN(dbl)) {
							return -1;
						}
						return 1;
					case -1:
						if (!double.IsNaN(dbl)) {
							if (!double.IsNegativeInfinity(dbl)) {
								return -1;
							}
							return 0;
						}
						return 1;
					case 0:
						if (!(dbl > 0.0)) {
							if (dbl != 0.0) {
								return 1;
							}
							return 0;
						}
						return -1;
					case 1:
						if (!double.IsPositiveInfinity(dbl)) {
							return 1;
						}
						return 0;
				}
			}
			if (IsDenOne) {
				return Numerator.CompareTo(dbl);
			}
			NumberUtils.GetDoubleParts(dbl, out var sign, out var exp, out var man, out var fFinite);
			if (!fFinite) {
				if (man == 0) {
					return -sign;
				}
				return 1;
			}
			if (man == 0) {
				return Sign;
			}
			if (Sign != sign) {
				if (sign >= 0) {
					return -1;
				}
				return 1;
			}
			int num = Statics.CbitLowZero(man);
			if (num > 0) {
				man >>= num;
				exp += num;
			}
			BigInteger bigInteger = man;
			BigInteger bnDen = 1;
			if (exp < 0) {
				bnDen = BigInteger.Power(2, -exp);
			} else if (exp > 0) {
				bigInteger *= BigInteger.Power(2, exp);
			}
			if (sign < 0) {
				bigInteger = -bigInteger;
			}
			return BigInteger.CompareFractions(Numerator, Denominator, bigInteger, bnDen);
		}

		/// <summary>
		/// conver to a string 
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (_signDen == 0) {
				switch (_signNum) {
					case -1:
						return "-Infinity";
					case 0:
						return "0";
					case 1:
						return "Infinity";
					case 2:
						return "UnsignedInfinity";
					default:
						return "Indeterminate";
				}
			}
			if (IsDenOne) {
				return Numerator.ToString();
			}
			return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", new object[2] { Numerator, Denominator });
		}

		/// <summary>
		/// Append to a string
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="cchMax"></param>
		public void AppendDecimalString(StringBuilder sb, int cchMax)
		{
			if (_signDen == 0) {
				sb.Append(ToString());
				return;
			}
			if (_signNum < 0) {
				sb.Append('-');
				cchMax--;
			}
			if (cchMax < 3) {
				cchMax = 3;
			}
			int length = sb.Length;
			int length2 = sb.Length;
			BigRegister bigRegister = new BigRegister(_signNum, _bitsNum);
			BigRegister regDen = new BigRegister(_signDen, _bitsDen);
			BigRegister regQuo = default(BigRegister);
			bigRegister.ModDiv(ref regDen, ref regQuo);
			if (!regQuo.IsSingle(0u)) {
				sb.Append(regQuo.GetInteger(1));
				length2 = sb.Length;
				if (length2 - length > cchMax) {
					int num = 2;
					int exp = length2 - length + num - cchMax;
					string text;
					while (num <= (text = exp.ToString(CultureInfo.InvariantCulture)).Length) {
						num = text.Length + 1;
						exp = length2 - length - Math.Max(cchMax - num, 1);
					}
					length2 -= exp;
					RoundDecStr(sb, length, ref length2, ref exp);
					RemoveDecZeros(sb, length, ref length2, ref exp);
					sb.Length = length2;
					sb.Append('e').Append(exp);
					return;
				}
				if (bigRegister.IsSingle(0u)) {
					return;
				}
			}
			int num2 = 0;
			while (sb.Length - length <= cchMax && !bigRegister.IsSingle(0u)) {
				bigRegister.Mul(1000000000u);
				bigRegister.ModDiv(ref regDen, ref regQuo);
				if (sb.Length == length) {
					if (regQuo.IsSingle(0u)) {
						num2 += 9;
						continue;
					}
					sb.Append(regQuo.High);
					num2 += 9 - (sb.Length - length);
				} else {
					sb.AppendFormat("{0:D9}", regQuo.High);
				}
			}
			int ichLim = sb.Length;
			int exp2 = 0;
			RemoveDecZeros(sb, length, ref ichLim, ref exp2);
			if (ichLim - length + num2 >= cchMax) {
				if (num2 > 3) {
					int num3 = -num2 - 1;
					string text2 = num3.ToString(CultureInfo.InvariantCulture);
					int num4 = text2.Length + 1;
					if (ichLim - length >= cchMax - num4 && ichLim > length + 1) {
						ichLim = length + Math.Max(cchMax - num4 - 1, 1);
						exp2 = -ichLim;
						if (RoundDecStr(sb, length, ref ichLim, ref exp2)) {
							num3++;
						}
					}
					sb.Length = ichLim;
					if (ichLim - length > 1) {
						sb.Insert(length + 1, '.');
					}
					sb.Append('e').Append(num3);
					return;
				}
				ichLim = Math.Max(length2, Math.Max(length + 1, length + cchMax - num2 - 1));
				exp2 = -ichLim;
				if (RoundDecStr(sb, length, ref ichLim, ref exp2)) {
					int num5 = length2 - length;
					sb.Length = length + 1;
					if (num2 > 0) {
						sb.Insert(length, '.');
						if (--num2 > 0) {
							sb.Insert(length + 1, new string('0', num2));
						}
					} else if (num5 >= cchMax && num5 >= 3) {
						sb.Append('e').Append(num5);
					} else if (num5 > 0) {
						sb.Append(new string('0', num5));
					}
					return;
				}
				if (ichLim < length2) {
					sb.Length = ichLim;
					sb.Append(new string('0', length2 - ichLim));
					return;
				}
			}
			sb.Length = ichLim;
			if (length2 < ichLim) {
				sb.Insert(length2, '.');
				if (num2 > 0) {
					sb.Insert(length + 1, new string('0', num2));
				}
			}
		}

		private static bool RoundDecStr(StringBuilder sb, int ichMin, ref int ichLim, ref int exp)
		{
			if (sb[ichLim] < '5') {
				return false;
			}
			do {
				if ((sb[ichLim - 1] += '\u0001') <= '9') {
					return false;
				}
				exp++;
				ichLim--;
			}
			while (ichLim != ichMin);
			ichLim = ichMin + 1;
			sb[1] = '1';
			return true;
		}

		private static void RemoveDecZeros(StringBuilder sb, int ichMin, ref int ichLim, ref int exp)
		{
			while (sb[ichLim - 1] == '0') {
				ichLim--;
				exp++;
			}
		}
	}
}
