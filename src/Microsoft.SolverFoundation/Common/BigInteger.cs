using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// An arbitary pecision integer 
	/// </summary>
	[CLSCompliant(true)]
	public struct BigInteger : IComparable, IComparable<BigInteger>, IEquatable<BigInteger>, IComparable<int>, IEquatable<int>, IComparable<uint>, IEquatable<uint>, IComparable<long>, IEquatable<long>, IComparable<ulong>, IEquatable<ulong>, IComparable<double>, IEquatable<double>
	{
		private const int knMaskHighBit = int.MinValue;

		private const uint kuMaskHighBit = 2147483648u;

		private const int kcbitUint = 32;

		private const int kcbitUlong = 64;

		private int _sign;

		private uint[] _bits;

		private static BigInteger s_bnMinInt = new BigInteger(-1, new uint[1] { 2147483648u });

		internal int _Sign => _sign;

		internal uint[] _Bits => _bits;

		/// <summary>
		/// count of the number of bits in an integer
		/// </summary>
		public int BitCount => GetBitCount(_sign, _bits);

		/// <summary>
		/// is the number 2^x
		/// </summary>
		public bool IsPowerOfTwo
		{
			get
			{
				if (_bits == null) {
					if ((_sign & (_sign - 1)) == 0) {
						return _sign != 0;
					}
					return false;
				}
				if (_sign != 1) {
					return false;
				}
				int num = Length(_bits) - 1;
				if ((_bits[num] & (_bits[num] - 1)) != 0) {
					return false;
				}
				while (--num >= 0) {
					if (_bits[num] != 0) {
						return false;
					}
				}
				return true;
			}
		}

		/// <summary>
		/// zero test 
		/// </summary>
		public bool IsZero => _sign == 0;

		/// <summary>
		/// one test 
		/// </summary>
		public bool IsOne
		{
			get
			{
				if (_sign == 1) {
					return _bits == null;
				}
				return false;
			}
		}

		/// <summary>
		/// eveness test 
		/// </summary>
		public bool IsEven
		{
			get
			{
				if (_bits != null) {
					return (_bits[0] & 1) == 0;
				}
				return (_sign & 1) == 0;
			}
		}

		/// <summary>
		/// get sign 
		/// </summary>
		public int Sign => (_sign >> 31) - (-_sign >> 31);

		/// <summary>
		/// get absolute value
		/// </summary>
		public BigInteger AbsoluteValue
		{
			get
			{
				BigInteger result = default(BigInteger);
				result._bits = _bits;
				result._sign = (int)Statics.Abs(_sign);
				return result;
			}
		}

		internal BigInteger(int n, uint[] rgu)
		{
			_sign = n;
			_bits = rgu;
		}

		internal BigInteger(int n)
		{
			if (n == int.MinValue) {
				this = s_bnMinInt;
				return;
			}
			_sign = n;
			_bits = null;
		}

		private static int Length(uint[] rgu)
		{
			int num = rgu.Length;
			if (rgu[num - 1] != 0) {
				return num;
			}
			return num - 1;
		}

		[Conditional("DEBUG")]
		private void AssertValid()
		{
		}

		[Conditional("DEBUG")]
		internal static void AssertValid(int sign, uint[] bits)
		{
		}

		internal static int GetBitCount(int sign, uint[] bits)
		{
			if (bits == null) {
				return 32 - Statics.CbitHighZero(Statics.Abs(sign));
			}
			int num = Length(bits);
			return num * 32 - Statics.CbitHighZero(bits[num - 1]);
		}

		/// <summary>
		/// convert an int to a BigInteger
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public static implicit operator BigInteger(int n)
		{
			if (n == int.MinValue) {
				return s_bnMinInt;
			}
			return new BigInteger(n, null);
		}

		/// <summary>
		/// convert an unsigned int to a BigInteger
		/// </summary>
		/// <param name="u"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static implicit operator BigInteger(uint u)
		{
			if (u <= int.MaxValue) {
				return new BigInteger((int)u, null);
			}
			return new BigInteger(1, new uint[1] { u });
		}

		/// <summary>
		/// convert a long to a BigInteger
		/// </summary>
		/// <param name="nn"></param>
		/// <returns></returns>
		public static implicit operator BigInteger(long nn)
		{
			int n;
			ulong num;
			if (nn >= 0) {
				if (nn <= int.MaxValue) {
					return new BigInteger((int)nn, null);
				}
				n = 1;
				num = (ulong)nn;
			} else {
				if (nn > int.MinValue) {
					return new BigInteger((int)nn, null);
				}
				if (nn == int.MinValue) {
					return s_bnMinInt;
				}
				n = -1;
				num = (ulong)(-nn);
			}
			if (num <= uint.MaxValue) {
				return new BigInteger(n, new uint[1] { (uint)num });
			}
			return new BigInteger(n, new uint[2]
			{
				Statics.GetLo(num),
				Statics.GetHi(num)
			});
		}

		/// <summary>
		/// convert a unsinged long to a BigInteger
		/// </summary>
		/// <param name="uu"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static implicit operator BigInteger(ulong uu)
		{
			if (uu <= int.MaxValue) {
				return new BigInteger((int)uu, null);
			}
			if (uu <= uint.MaxValue) {
				return new BigInteger(1, new uint[1] { (uint)uu });
			}
			return new BigInteger(1, new uint[2]
			{
				Statics.GetLo(uu),
				Statics.GetHi(uu)
			});
		}

		/// <summary>
		/// convert BigInteger to int 
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static explicit operator int(BigInteger bn)
		{
			if (bn._bits == null) {
				return bn._sign;
			}
			if (bn._sign > 0) {
				return (int)bn._bits[0];
			}
			return (int)(0 - bn._bits[0]);
		}

		/// <summary>
		/// convert BigInteger to unsigned int 
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static explicit operator uint(BigInteger bn)
		{
			if (bn._bits == null) {
				return (uint)bn._sign;
			}
			if (bn._sign > 0) {
				return bn._bits[0];
			}
			return 0 - bn._bits[0];
		}

		/// <summary>
		/// convert BigInteger to long 
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static explicit operator long(BigInteger bn)
		{
			if (bn._bits == null) {
				return bn._sign;
			}
			ulong num = ((Length(bn._bits) <= 1) ? bn._bits[0] : Statics.MakeUlong(bn._bits[1], bn._bits[0]));
			if (bn._sign > 0) {
				return (long)num;
			}
			return (long)(0L - num);
		}

		/// <summary>
		/// convert BigInteger to unsigned long 
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static explicit operator ulong(BigInteger bn)
		{
			if (bn._bits == null) {
				return (ulong)bn._sign;
			}
			ulong num = ((Length(bn._bits) <= 1) ? bn._bits[0] : Statics.MakeUlong(bn._bits[1], bn._bits[0]));
			if (bn._sign > 0) {
				return num;
			}
			return 0L - num;
		}

		/// <summary>
		/// convert BigInteger to double 
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static explicit operator double(BigInteger bn)
		{
			if (bn._bits == null) {
				return bn._sign;
			}
			int sign = 1;
			new BigRegister(bn, ref sign).GetApproxParts(out var exp, out var man);
			return NumberUtils.GetDoubleFromParts(sign, exp, man);
		}

		/// <summary>
		/// convert double to BigInteger
		/// </summary>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static explicit operator BigInteger(double dbl)
		{
			NumberUtils.GetDoubleParts(dbl, out var sign, out var exp, out var man, out var fFinite);
			if (!fFinite) {
				return default(BigInteger);
			}
			if (man == 0) {
				return default(BigInteger);
			}
			BigInteger result = default(BigInteger);
			if (exp <= 0) {
				if (exp <= -64) {
					return default(BigInteger);
				}
				result = man >> -exp;
				if (sign < 0) {
					result._sign = -result._sign;
				}
			} else if (exp <= 11) {
				result = man << exp;
				if (sign < 0) {
					result._sign = -result._sign;
				}
			} else {
				man <<= 11;
				exp -= 11;
				int num = (exp - 1) / 32 + 1;
				int num2 = num * 32 - exp;
				result._bits = new uint[num + 2];
				result._bits[num + 1] = (uint)(man >> num2 + 32);
				result._bits[num] = (uint)(man >> num2);
				if (num2 > 0) {
					result._bits[num - 1] = (uint)((int)man << 32 - num2);
				}
				result._sign = sign;
			}
			return result;
		}

		/// <summary>
		/// BigInteger And operator
		/// </summary>
		public static BigInteger operator &(BigInteger left, BigInteger right)
		{
			if (left._bits == null) {
				if (left._sign == 0) {
					return left;
				}
				if (left._sign == -1) {
					return right;
				}
				if (right._bits == null) {
					return new BigInteger(left._sign & right._sign);
				}
			} else if (right._bits == null) {
				if (right._sign == 0) {
					return right;
				}
				if (right._sign == -1) {
					return left;
				}
			}
			int sign = 1;
			int sign2 = 1;
			BigRegister bigRegister = new BigRegister(left, ref sign);
			BigRegister reg = new BigRegister(right, ref sign2);
			bigRegister.BitAnd((uint)(sign >> 1), ref reg, (uint)(sign2 >> 1));
			return bigRegister.GetInteger(sign & sign2);
		}

		/// <summary>
		/// BigInteger | operator
		/// </summary>
		public static BigInteger operator |(BigInteger left, BigInteger right)
		{
			if (left._bits == null) {
				if (left._sign == 0) {
					return right;
				}
				if (left._sign == -1) {
					return left;
				}
				if (right._bits == null) {
					return new BigInteger(left._sign | right._sign);
				}
			} else if (right._bits == null) {
				if (right._sign == 0) {
					return left;
				}
				if (right._sign == -1) {
					return right;
				}
			}
			int sign = 1;
			int sign2 = 1;
			BigRegister bigRegister = new BigRegister(left, ref sign);
			BigRegister reg = new BigRegister(right, ref sign2);
			bigRegister.BitOr((uint)(sign >> 1), ref reg, (uint)(sign2 >> 1));
			return bigRegister.GetInteger(sign | sign2);
		}

		/// <summary>
		/// BigInteger ^ operator
		/// </summary>
		public static BigInteger operator ^(BigInteger left, BigInteger right)
		{
			if (left._bits == null) {
				if (left._sign == 0) {
					return right;
				}
				if (left._sign == -1) {
					return ~right;
				}
				if (right._bits == null) {
					return new BigInteger(left._sign ^ right._sign);
				}
			} else if (right._bits == null) {
				if (right._sign == 0) {
					return left;
				}
				if (right._sign == -1) {
					return ~left;
				}
			}
			int sign = 1;
			int sign2 = 1;
			BigRegister bigRegister = new BigRegister(left, ref sign);
			BigRegister reg = new BigRegister(right, ref sign2);
			bigRegister.BitXor((uint)(sign >> 1), ref reg, (uint)(sign2 >> 1));
			return bigRegister.GetInteger(sign ^ sign2 ^ 1);
		}

		/// <summary>
		/// BigInteger left shift operator
		/// </summary>
		public static BigInteger operator <<(BigInteger value, int shift)
		{
			if (shift == 0 || value.IsZero) {
				return value;
			}
			int sign = 1;
			BigRegister bigRegister = new BigRegister(value, ref sign);
			bigRegister.BitShiftLeft(sign, shift);
			return bigRegister.GetInteger(sign);
		}

		/// <summary>
		/// BigInteger right shift operator
		/// </summary>
		public static BigInteger operator >>(BigInteger value, int shift)
		{
			if (shift == 0 || value.IsZero) {
				return value;
			}
			int sign = 1;
			BigRegister bigRegister = new BigRegister(value, ref sign);
			bigRegister.BitShiftRight(sign, shift);
			return bigRegister.GetInteger(sign);
		}

		/// <summary>
		/// BigInteger ~ operator
		/// </summary>
		public static BigInteger operator ~(BigInteger value)
		{
			return -AddOne(value);
		}

		/// <summary>
		/// TestBit 
		/// </summary>
		public bool TestBit(long ibit)
		{
			if (ibit <= 0) {
				if (ibit == 0) {
					return !IsEven;
				}
				return false;
			}
			if (_bits == null && ibit < 32) {
				return (_sign & (1 << (int)ibit)) != 0;
			}
			int sign = 1;
			return new BigRegister(this, ref sign).TestBit(sign, ibit);
		}

		/// <summary>
		/// inplace negate a BigInteger
		/// </summary>
		/// <param name="bn"></param>
		public static void Negate(ref BigInteger bn)
		{
			bn._sign = -bn._sign;
		}

		/// <summary>
		/// negate a BigInteger
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static BigInteger operator -(BigInteger bn)
		{
			bn._sign = -bn._sign;
			return bn;
		}

		/// <summary>
		/// BigInteger + operator
		/// </summary>
		public static BigInteger operator +(BigInteger bn)
		{
			return bn;
		}

		/// <summary>
		/// BigInteger ++ operator
		/// </summary>
		public static BigInteger operator ++(BigInteger value)
		{
			return AddOne(value);
		}

		private static BigInteger AddOne(BigInteger value)
		{
			if (value._bits == null) {
				int num = value._sign + 1;
				if (num == int.MinValue) {
					return -s_bnMinInt;
				}
				return new BigInteger(num);
			}
			int sign = 1;
			BigRegister bigRegister = new BigRegister(value, ref sign);
			if (sign > 0) {
				bigRegister.Add(1u);
			} else {
				bigRegister.Sub(ref sign, 1u);
			}
			return bigRegister.GetInteger(sign);
		}

		/// <summary>
		/// BigInteger -- operator
		/// </summary>
		public static BigInteger operator --(BigInteger value)
		{
			return SubOne(value);
		}

		private static BigInteger SubOne(BigInteger value)
		{
			if (value._bits == null) {
				return new BigInteger(value._sign - 1);
			}
			int sign = 1;
			BigRegister bigRegister = new BigRegister(value, ref sign);
			if (sign > 0) {
				bigRegister.Sub(ref sign, 1u);
			} else {
				bigRegister.Add(1u);
			}
			return bigRegister.GetInteger(sign);
		}

		/// <summary>
		/// Add two BigIntegers
		/// </summary>
		/// <param name="bn1"></param>
		/// <param name="bn2"></param>
		/// <returns></returns>
		public static BigInteger operator +(BigInteger bn1, BigInteger bn2)
		{
			if (bn2.IsZero) {
				return bn1;
			}
			if (bn1.IsZero) {
				return bn2;
			}
			int sign = 1;
			int sign2 = 1;
			BigRegister bigRegister = new BigRegister(bn1, ref sign);
			BigRegister reg = new BigRegister(bn2, ref sign2);
			if (sign == sign2) {
				bigRegister.Add(ref reg);
			} else {
				bigRegister.Sub(ref sign, ref reg);
			}
			return bigRegister.GetInteger(sign);
		}

		/// <summary>
		/// Minus two BigIntegers
		/// </summary>
		/// <param name="bn1"></param>
		/// <param name="bn2"></param>
		/// <returns></returns>
		public static BigInteger operator -(BigInteger bn1, BigInteger bn2)
		{
			if (bn2.IsZero) {
				return bn1;
			}
			if (bn1.IsZero) {
				return -bn2;
			}
			int sign = 1;
			int sign2 = -1;
			BigRegister bigRegister = new BigRegister(bn1, ref sign);
			BigRegister reg = new BigRegister(bn2, ref sign2);
			if (sign == sign2) {
				bigRegister.Add(ref reg);
			} else {
				bigRegister.Sub(ref sign, ref reg);
			}
			return bigRegister.GetInteger(sign);
		}

		/// <summary>
		/// Times two BigIntegers
		/// </summary>
		/// <param name="bn1"></param>
		/// <param name="bn2"></param>
		/// <returns></returns>
		public static BigInteger operator *(BigInteger bn1, BigInteger bn2)
		{
			int sign = 1;
			BigRegister bigRegister = new BigRegister(bn1, ref sign);
			BigRegister regMul = new BigRegister(bn2, ref sign);
			bigRegister.Mul(ref regMul);
			return bigRegister.GetInteger(sign);
		}

		/// <summary>
		/// Divide two BigIntegers
		/// </summary>
		/// <param name="bnNum"></param>
		/// <param name="bnDen"></param>
		/// <returns></returns>
		public static BigInteger operator /(BigInteger bnNum, BigInteger bnDen)
		{
			int sign = 1;
			BigRegister bigRegister = new BigRegister(bnNum, ref sign);
			BigRegister regDen = new BigRegister(bnDen, ref sign);
			bigRegister.Div(ref regDen);
			return bigRegister.GetInteger(sign);
		}

		/// <summary>
		/// mod two BigIntegers
		/// </summary>
		/// <param name="bnNum"></param>
		/// <param name="bnDen"></param>
		/// <returns></returns>
		public static BigInteger operator %(BigInteger bnNum, BigInteger bnDen)
		{
			int sign = 1;
			int sign2 = 1;
			BigRegister bigRegister = new BigRegister(bnNum, ref sign);
			BigRegister regDen = new BigRegister(bnDen, ref sign2);
			bigRegister.Mod(ref regDen);
			return bigRegister.GetInteger(sign);
		}

		/// <summary>
		/// Divide a BigInterg by uDen, return the quotient and the remainder.
		/// </summary>
		/// <param name="bnNum"></param>
		/// <param name="uDen"></param>
		/// <param name="bnQuo"></param>
		/// <param name="bnRem"></param>
		[CLSCompliant(false)]
		public static void DivModOne(BigInteger bnNum, uint uDen, out BigInteger bnQuo, out BigInteger bnRem)
		{
			int sign = 1;
			BigRegister bigRegister = new BigRegister(bnNum, ref sign);
			uint num = bigRegister.DivMod(uDen);
			bnQuo = bigRegister.GetInteger(sign);
			bnRem = sign * num;
		}

		/// <summary>
		/// Divide a BigInterg by uDen, return the quotient and the remainder.
		/// </summary>
		/// <param name="bnNum"></param>
		/// <param name="bnDen"></param>
		/// <param name="bnQuo"></param>
		/// <param name="bnRem"></param>
		public static void DivMod(BigInteger bnNum, BigInteger bnDen, out BigInteger bnQuo, out BigInteger bnRem)
		{
			int sign = 1;
			int sign2 = 1;
			BigRegister bigRegister = new BigRegister(bnNum, ref sign);
			BigRegister regDen = new BigRegister(bnDen, ref sign2);
			BigRegister regQuo = default(BigRegister);
			bigRegister.ModDiv(ref regDen, ref regQuo);
			bnQuo = regQuo.GetInteger(sign * sign2);
			bnRem = bigRegister.GetInteger(sign);
		}

		/// <summary>
		/// Compute GCD of two BigIntergers
		/// </summary>
		/// <param name="bn1"></param>
		/// <param name="bn2"></param>
		/// <returns></returns>
		public static BigInteger Gcd(BigInteger bn1, BigInteger bn2)
		{
			BigRegister reg = new BigRegister(bn1);
			BigRegister reg2 = new BigRegister(bn2);
			BigRegister.GCD(ref reg, ref reg2);
			return reg.GetInteger(1);
		}

		private static void MulUpper(ref uint uHiRes, ref int cuRes, uint uHiMul, int cuMul)
		{
			ulong uu = (ulong)uHiRes * (ulong)uHiMul;
			uint num = Statics.GetHi(uu);
			if (num != 0) {
				if (Statics.GetLo(uu) != 0 && ++num == 0) {
					num = 1u;
					cuRes++;
				}
				uHiRes = num;
				cuRes += cuMul;
			} else {
				uHiRes = Statics.GetLo(uu);
				cuRes += cuMul - 1;
			}
		}

		private static void MulLower(ref uint uHiRes, ref int cuRes, uint uHiMul, int cuMul)
		{
			ulong uu = (ulong)uHiRes * (ulong)uHiMul;
			uint hi = Statics.GetHi(uu);
			if (hi != 0) {
				uHiRes = hi;
				cuRes += cuMul;
			} else {
				uHiRes = Statics.GetLo(uu);
				cuRes += cuMul - 1;
			}
		}

		/// <summary>
		/// Compute power of two BigIntergers, and return a BigInteger
		/// </summary>
		/// <param name="bnBase"></param>
		/// <param name="bnExp"></param>
		/// <param name="bnRes"></param>
		/// <returns></returns>
		public static bool Power(BigInteger bnBase, BigInteger bnExp, out BigInteger bnRes)
		{
			if (bnBase._bits == null) {
				if (bnBase._sign == 1) {
					bnRes = bnBase;
					return true;
				}
				if (bnBase._sign == -1) {
					if (bnExp._bits != null) {
						bnRes = (((bnExp._bits[0] & 1) != 0) ? bnBase : ((BigInteger)1));
					} else {
						bnRes = (((bnExp._sign & 1) != 0) ? bnBase : ((BigInteger)1));
					}
					return true;
				}
				if (bnBase._sign == 0) {
					bnRes = bnBase;
					return bnExp._sign > 0;
				}
			}
			if (bnExp._sign <= 0) {
				bnRes = 1;
				return bnExp._sign == 0;
			}
			if (bnExp._bits != null) {
				bnRes = 0;
				return false;
			}
			if (bnExp._sign == 1) {
				bnRes = bnBase;
				return true;
			}
			int sign = bnExp._sign;
			int sign2 = 1;
			BigRegister reg = new BigRegister(bnBase, ref sign2);
			int cuRes = reg.Size;
			int cuRes2 = cuRes;
			uint uHiRes = reg.High;
			uint uHiRes2 = uHiRes + 1;
			if (uHiRes2 == 0) {
				cuRes2++;
				uHiRes2 = 1u;
			}
			int cuRes3 = 1;
			int cuRes4 = 1;
			uint uHiRes3 = 1u;
			uint uHiRes4 = 1u;
			bnRes = 0;
			int num = sign;
			while (true) {
				if ((num & 1) != 0) {
					MulUpper(ref uHiRes4, ref cuRes4, uHiRes2, cuRes2);
					MulLower(ref uHiRes3, ref cuRes3, uHiRes, cuRes);
					if (cuRes3 > 67108864 || cuRes4 > 134217728) {
						return false;
					}
				}
				if ((num >>= 1) == 0) {
					break;
				}
				MulUpper(ref uHiRes2, ref cuRes2, uHiRes2, cuRes2);
				MulLower(ref uHiRes, ref cuRes, uHiRes, cuRes);
				if (cuRes > 67108864 || cuRes2 > 134217728) {
					return false;
				}
			}
			if (cuRes4 > 1) {
				reg.EnsureWritable(cuRes4, 0);
			}
			BigRegister b = new BigRegister(cuRes4);
			BigRegister a = new BigRegister(cuRes4);
			a.Set(1u);
			if ((sign & 1) == 0) {
				sign2 = 1;
			}
			int num2 = sign;
			while (true) {
				if ((num2 & 1) != 0) {
					Statics.Swap(ref a, ref b);
					a.Mul(ref reg, ref b);
				}
				if ((num2 >>= 1) == 0) {
					break;
				}
				Statics.Swap(ref reg, ref b);
				reg.Mul(ref b, ref b);
			}
			bnRes = a.GetInteger(sign2);
			return true;
		}

		/// <summary>
		/// Compute power of two BigIntergers and return a Rational number
		/// </summary>
		/// <param name="bnBase"></param>
		/// <param name="bnExp"></param>
		/// <param name="ratRes"></param>
		/// <returns></returns>
		public static bool Power(BigInteger bnBase, BigInteger bnExp, out Rational ratRes)
		{
			BigInteger bnRes;
			if (bnExp._sign >= 0) {
				if (Power(bnBase, bnExp, out bnRes)) {
					ratRes = bnRes;
					return true;
				}
				ratRes = Rational.One;
				return true;
			}
			if (Power(bnBase, -bnExp, out bnRes)) {
				ratRes = ((Rational)bnRes).Invert();
				return true;
			}
			ratRes = Rational.Indeterminate;
			return false;
		}

		/// <summary>
		/// Compute the power 
		/// </summary>
		/// <param name="bnBase"></param>
		/// <param name="bnExp"></param>
		/// <returns>a BigInteger</returns>
		public static BigInteger Power(BigInteger bnBase, BigInteger bnExp)
		{
			if (!Power(bnBase, bnExp, out BigInteger bnRes)) {
				throw new InvalidOperationException();
			}
			return bnRes;
		}

		/// <summary>
		/// Compute factorial of a BigInteger
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="bnRes"></param>
		/// <returns></returns>
		public static bool TryFactorial(BigInteger bn, out BigInteger bnRes)
		{
			BigRegister bigRegister = default(BigRegister);
			if (bn._sign < 0 || bn._bits != null || !bigRegister.ComputeFactorial(bn._sign)) {
				bnRes = default(BigInteger);
				return false;
			}
			bnRes = bigRegister.GetInteger(1);
			return true;
		}

		/// <summary>
		/// Compute factorial of a BigInteger
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static BigInteger Factorial(BigInteger bn)
		{
			if (!TryFactorial(bn, out var bnRes)) {
				throw new ArgumentOutOfRangeException(Resources.InvalidFactorialOperand);
			}
			return bnRes;
		}

		internal static int GetDiffLength(uint[] rgu1, uint[] rgu2, int cu)
		{
			int num = cu;
			while (--num >= 0) {
				if (rgu1[num] != rgu2[num]) {
					return num + 1;
				}
			}
			return 0;
		}

		/// <summary>
		/// less than 
		/// </summary>
		/// <param name="bn1"></param>
		/// <param name="bn2"></param>
		/// <returns></returns>
		public static bool operator <(BigInteger bn1, BigInteger bn2)
		{
			return bn1.CompareTo(bn2) < 0;
		}

		/// <summary>
		/// less than or equal to
		/// </summary>
		/// <param name="bn1"></param>
		/// <param name="bn2"></param>
		/// <returns></returns>
		public static bool operator <=(BigInteger bn1, BigInteger bn2)
		{
			return bn1.CompareTo(bn2) <= 0;
		}

		/// <summary>
		/// greater than 
		/// </summary>
		/// <param name="bn1"></param>
		/// <param name="bn2"></param>
		/// <returns></returns>
		public static bool operator >(BigInteger bn1, BigInteger bn2)
		{
			return bn1.CompareTo(bn2) > 0;
		}

		/// <summary>
		/// greater than or equal to 
		/// </summary>
		/// <param name="bn1"></param>
		/// <param name="bn2"></param>
		/// <returns></returns>
		public static bool operator >=(BigInteger bn1, BigInteger bn2)
		{
			return bn1.CompareTo(bn2) >= 0;
		}

		/// <summary>
		/// equal 
		/// </summary>
		/// <param name="bn1"></param>
		/// <param name="bn2"></param>
		/// <returns></returns>
		public static bool operator ==(BigInteger bn1, BigInteger bn2)
		{
			return bn1.Equals(bn2);
		}

		/// <summary>
		/// not equal 
		/// </summary>
		/// <param name="bn1"></param>
		/// <param name="bn2"></param>
		/// <returns></returns>
		public static bool operator !=(BigInteger bn1, BigInteger bn2)
		{
			return !bn1.Equals(bn2);
		}

		/// <summary>
		/// compare to an int 
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator <(BigInteger bn, int n)
		{
			return bn.CompareTo(n) < 0;
		}

		/// <summary>
		/// compare to an int
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator <=(BigInteger bn, int n)
		{
			return bn.CompareTo(n) <= 0;
		}

		/// <summary>
		/// compare to an int
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator >(BigInteger bn, int n)
		{
			return bn.CompareTo(n) > 0;
		}

		/// <summary>
		/// compare to an int
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator >=(BigInteger bn, int n)
		{
			return bn.CompareTo(n) >= 0;
		}

		/// <summary>
		/// compare to an int
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator ==(BigInteger bn, int n)
		{
			return bn.Equals(n);
		}

		/// <summary>
		/// compare to an int
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator !=(BigInteger bn, int n)
		{
			return !bn.Equals(n);
		}

		/// <summary>
		/// compare by an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator <(int n, BigInteger bn)
		{
			return bn.CompareTo(n) > 0;
		}

		/// <summary>
		/// compare by an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator <=(int n, BigInteger bn)
		{
			return bn.CompareTo(n) >= 0;
		}

		/// <summary>
		/// compare by an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator >(int n, BigInteger bn)
		{
			return bn.CompareTo(n) < 0;
		}

		/// <summary>
		/// compare by an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator >=(int n, BigInteger bn)
		{
			return bn.CompareTo(n) <= 0;
		}

		/// <summary>
		/// compare by an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator ==(int n, BigInteger bn)
		{
			return bn.Equals(n);
		}

		/// <summary>
		/// compare by an int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator !=(int n, BigInteger bn)
		{
			return !bn.Equals(n);
		}

		/// <summary>
		/// compare to an unsigned int
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <(BigInteger bn, uint n)
		{
			return bn.CompareTo(n) < 0;
		}

		/// <summary>
		/// compare to an unsigned int
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <=(BigInteger bn, uint n)
		{
			return bn.CompareTo(n) <= 0;
		}

		/// <summary>
		/// compare to an unsigned int
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >(BigInteger bn, uint n)
		{
			return bn.CompareTo(n) > 0;
		}

		/// <summary>
		/// compare to an unsigned int
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >=(BigInteger bn, uint n)
		{
			return bn.CompareTo(n) >= 0;
		}

		/// <summary>
		/// compare to an unsigned int
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator ==(BigInteger bn, uint n)
		{
			return bn.Equals(n);
		}

		/// <summary>
		/// compare to an unsigned int
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator !=(BigInteger bn, uint n)
		{
			return !bn.Equals(n);
		}

		/// <summary>
		/// compare by an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <(uint n, BigInteger bn)
		{
			return bn.CompareTo(n) > 0;
		}

		/// <summary>
		/// compare by an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <=(uint n, BigInteger bn)
		{
			return bn.CompareTo(n) >= 0;
		}

		/// <summary>
		/// compare by an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >(uint n, BigInteger bn)
		{
			return bn.CompareTo(n) < 0;
		}

		/// <summary>
		/// compare by an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >=(uint n, BigInteger bn)
		{
			return bn.CompareTo(n) <= 0;
		}

		/// <summary>
		/// compare by an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator ==(uint n, BigInteger bn)
		{
			return bn.Equals(n);
		}

		/// <summary>
		/// compare by an unsigned int
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator !=(uint n, BigInteger bn)
		{
			return !bn.Equals(n);
		}

		/// <summary>
		/// compare to a long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator <(BigInteger bn, long n)
		{
			return bn.CompareTo(n) < 0;
		}

		/// <summary>
		/// compare to a long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator <=(BigInteger bn, long n)
		{
			return bn.CompareTo(n) <= 0;
		}

		/// <summary>
		/// compare to a long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator >(BigInteger bn, long n)
		{
			return bn.CompareTo(n) > 0;
		}

		/// <summary>
		/// compare to a long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator >=(BigInteger bn, long n)
		{
			return bn.CompareTo(n) >= 0;
		}

		/// <summary>
		/// compare to a long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator ==(BigInteger bn, long n)
		{
			return bn.Equals(n);
		}

		/// <summary>
		/// compare to a long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static bool operator !=(BigInteger bn, long n)
		{
			return !bn.Equals(n);
		}

		/// <summary>
		/// compare by a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator <(long n, BigInteger bn)
		{
			return bn.CompareTo(n) > 0;
		}

		/// <summary>
		/// compare by a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator <=(long n, BigInteger bn)
		{
			return bn.CompareTo(n) >= 0;
		}

		/// <summary>
		/// compare by a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator >(long n, BigInteger bn)
		{
			return bn.CompareTo(n) < 0;
		}

		/// <summary>
		/// compare by a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator >=(long n, BigInteger bn)
		{
			return bn.CompareTo(n) <= 0;
		}

		/// <summary>
		/// compare by a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator ==(long n, BigInteger bn)
		{
			return bn.Equals(n);
		}

		/// <summary>
		/// compare by a long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator !=(long n, BigInteger bn)
		{
			return !bn.Equals(n);
		}

		/// <summary>
		/// compare to an unsigned long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <(BigInteger bn, ulong n)
		{
			return bn.CompareTo(n) < 0;
		}

		/// <summary>
		/// compare to an unsigned long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <=(BigInteger bn, ulong n)
		{
			return bn.CompareTo(n) <= 0;
		}

		/// <summary>
		/// compare to an unsigned long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >(BigInteger bn, ulong n)
		{
			return bn.CompareTo(n) > 0;
		}

		/// <summary>
		/// compare to an unsigned long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >=(BigInteger bn, ulong n)
		{
			return bn.CompareTo(n) >= 0;
		}

		/// <summary>
		/// compare to an unsigned long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator ==(BigInteger bn, ulong n)
		{
			return bn.Equals(n);
		}

		/// <summary>
		/// compare to an unsigned long
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator !=(BigInteger bn, ulong n)
		{
			return !bn.Equals(n);
		}

		/// <summary>
		/// compare by an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <(ulong n, BigInteger bn)
		{
			return bn.CompareTo(n) > 0;
		}

		/// <summary>
		/// compare by an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator <=(ulong n, BigInteger bn)
		{
			return bn.CompareTo(n) >= 0;
		}

		/// <summary>
		/// compare by an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >(ulong n, BigInteger bn)
		{
			return bn.CompareTo(n) < 0;
		}

		/// <summary>
		/// compare by an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator >=(ulong n, BigInteger bn)
		{
			return bn.CompareTo(n) <= 0;
		}

		/// <summary>
		/// compare by an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator ==(ulong n, BigInteger bn)
		{
			return bn.Equals(n);
		}

		/// <summary>
		/// compare by an unsigned long
		/// </summary>
		/// <param name="n"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static bool operator !=(ulong n, BigInteger bn)
		{
			return !bn.Equals(n);
		}

		/// <summary>
		/// compare to a double
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator <(BigInteger bn, double dbl)
		{
			return bn.CompareTo(dbl) < 0;
		}

		/// <summary>
		/// compare to a double
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator <=(BigInteger bn, double dbl)
		{
			return bn.CompareTo(dbl) <= 0;
		}

		/// <summary>
		/// compare to a double
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator >(BigInteger bn, double dbl)
		{
			if (bn.CompareTo(dbl) > 0) {
				return !double.IsNaN(dbl);
			}
			return false;
		}

		/// <summary>
		/// compare to a double
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator >=(BigInteger bn, double dbl)
		{
			if (bn.CompareTo(dbl) >= 0) {
				return !double.IsNaN(dbl);
			}
			return false;
		}

		/// <summary>
		/// compare to a double
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator ==(BigInteger bn, double dbl)
		{
			return bn.Equals(dbl);
		}

		/// <summary>
		/// compare to a double
		/// </summary>
		/// <param name="bn"></param>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public static bool operator !=(BigInteger bn, double dbl)
		{
			return !bn.Equals(dbl);
		}

		/// <summary>
		/// compare by a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator <(double dbl, BigInteger bn)
		{
			if (bn.CompareTo(dbl) > 0) {
				return !double.IsNaN(dbl);
			}
			return false;
		}

		/// <summary>
		/// compare by a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator <=(double dbl, BigInteger bn)
		{
			if (bn.CompareTo(dbl) >= 0) {
				return !double.IsNaN(dbl);
			}
			return false;
		}

		/// <summary>
		/// compare by a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator >(double dbl, BigInteger bn)
		{
			return bn.CompareTo(dbl) < 0;
		}

		/// <summary>
		/// compare by a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator >=(double dbl, BigInteger bn)
		{
			return bn.CompareTo(dbl) <= 0;
		}

		/// <summary>
		/// compare by a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator ==(double dbl, BigInteger bn)
		{
			return bn.Equals(dbl);
		}

		/// <summary>
		/// compare by a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static bool operator !=(double dbl, BigInteger bn)
		{
			return !bn.Equals(dbl);
		}

		/// <summary>
		/// override equals comparison 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (!(obj is BigInteger)) {
				return false;
			}
			return Equals((BigInteger)obj);
		}

		/// <summary>
		/// Compares the current number with another number (int, uint, double, long, ulong, Rational, BigInteger) and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.  
		/// </summary>
		/// <param name="obj">a number object</param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
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
			if (obj is Rational rational) {
				return -rational.CompareTo(this);
			}
			throw new ArgumentException(Resources.InvalidNumber);
		}

		/// <summary>
		/// override hashcode 
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return GetHashCode(_sign, _bits);
		}

		internal static int GetHashCode(int sign, uint[] bits)
		{
			if (bits == null) {
				return sign;
			}
			int num = sign;
			int num2 = Length(bits);
			while (--num2 >= 0) {
				num = Statics.CombineHash(num, (int)bits[num2]);
			}
			return num;
		}

		/// <summary>
		/// Check if two BigIntegers are equal 
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		public bool Equals(BigInteger bn)
		{
			if (_sign == bn._sign) {
				return EqualBits(_bits, bn._bits);
			}
			return false;
		}

		internal static bool EqualBits(uint[] bits1, uint[] bits2)
		{
			if (bits1 == bits2) {
				return true;
			}
			if (bits1 == null || bits2 == null) {
				return false;
			}
			int num = Length(bits1);
			if (num != Length(bits2)) {
				return false;
			}
			int diffLength = GetDiffLength(bits1, bits2, num);
			return diffLength == 0;
		}

		/// <summary>
		/// Compare two BigIntegers
		/// </summary>
		/// <param name="bn"></param>
		/// <returns></returns>
		public int CompareTo(BigInteger bn)
		{
			if ((_sign ^ bn._sign) < 0) {
				if (_sign >= 0) {
					return 1;
				}
				return -1;
			}
			if (_bits == null) {
				if (bn._bits == null) {
					if (_sign >= bn._sign) {
						if (_sign <= bn._sign) {
							return 0;
						}
						return 1;
					}
					return -1;
				}
				return -bn._sign;
			}
			int num;
			int num2;
			if (bn._bits == null || (num = Length(_bits)) > (num2 = Length(bn._bits))) {
				return _sign;
			}
			if (num < num2) {
				return -_sign;
			}
			int diffLength = GetDiffLength(_bits, bn._bits, num);
			if (diffLength == 0) {
				return 0;
			}
			if (_bits[diffLength - 1] >= bn._bits[diffLength - 1]) {
				return _sign;
			}
			return -_sign;
		}

		/// <summary>
		/// Whether equal to int
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public bool Equals(int n)
		{
			if (_bits == null) {
				return _sign == n;
			}
			if (n == int.MinValue && _sign < 0 && _bits[0] == 2147483648u) {
				return Length(_bits) == 1;
			}
			return false;
		}

		/// <summary>
		/// Compare with an int
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public int CompareTo(int n)
		{
			if (_bits == null) {
				return _sign.CompareTo(n);
			}
			if (n == int.MinValue && _sign < 0 && _bits[0] == 2147483648u && Length(_bits) == 1) {
				return 0;
			}
			return _sign;
		}

		/// <summary>
		/// Whether equal to an unsigned int
		/// </summary>
		/// <param name="u"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public bool Equals(uint u)
		{
			if (_sign < 0) {
				return false;
			}
			if (_bits == null) {
				return _sign == (int)u;
			}
			if (_bits[0] == u) {
				return Length(_bits) == 1;
			}
			return false;
		}

		/// <summary>
		/// Compare to an unsigned int
		/// </summary>
		/// <param name="u"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public int CompareTo(uint u)
		{
			if (_sign < 0) {
				return -1;
			}
			if (_bits == null) {
				uint sign = (uint)_sign;
				return sign.CompareTo(u);
			}
			if (Length(_bits) > 1) {
				return 1;
			}
			return _bits[0].CompareTo(u);
		}

		/// <summary>
		/// Whether equal to a long 
		/// </summary>
		/// <param name="nn"></param>
		/// <returns></returns>
		public bool Equals(long nn)
		{
			if (_bits == null) {
				return _sign == nn;
			}
			int num;
			if ((_sign ^ nn) < 0 || (num = Length(_bits)) > 2) {
				return false;
			}
			ulong num2 = (ulong)((nn < 0) ? (-nn) : nn);
			if (num == 1) {
				return _bits[0] == num2;
			}
			return Statics.MakeUlong(_bits[1], _bits[0]) == num2;
		}

		/// <summary>
		/// Compare to a long 
		/// </summary>
		/// <param name="nn"></param>
		/// <returns></returns>
		public int CompareTo(long nn)
		{
			if (_bits == null) {
				return ((long)_sign).CompareTo(nn);
			}
			int num;
			if ((_sign ^ nn) < 0 || (num = Length(_bits)) > 2) {
				return _sign;
			}
			ulong value = (ulong)((nn < 0) ? (-nn) : nn);
			ulong num2 = ((num == 2) ? Statics.MakeUlong(_bits[1], _bits[0]) : _bits[0]);
			return _sign * num2.CompareTo(value);
		}

		/// <summary>
		/// Whether equal to a unsigned long 
		/// </summary>
		/// <param name="uu"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public bool Equals(ulong uu)
		{
			if (_sign < 0) {
				return false;
			}
			if (_bits == null) {
				return (ulong)_sign == uu;
			}
			int num = Length(_bits);
			if (num > 2) {
				return false;
			}
			if (num == 1) {
				return _bits[0] == uu;
			}
			return Statics.MakeUlong(_bits[1], _bits[0]) == uu;
		}

		/// <summary>
		/// Compare to a unsigned long 
		/// </summary>
		/// <param name="uu"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public int CompareTo(ulong uu)
		{
			if (_sign < 0) {
				return -1;
			}
			if (_bits == null) {
				return ((ulong)_sign).CompareTo(uu);
			}
			int num = Length(_bits);
			if (num > 2) {
				return 1;
			}
			return ((num == 2) ? Statics.MakeUlong(_bits[1], _bits[0]) : _bits[0]).CompareTo(uu);
		}

		/// <summary>
		/// Whether equal to a double 
		/// </summary>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public bool Equals(double dbl)
		{
			if (_bits == null) {
				return dbl == (double)_sign;
			}
			NumberUtils.GetDoubleParts(dbl, out var sign, out var exp, out var man, out var fFinite);
			if (!fFinite || man == 0 || _sign != sign) {
				return false;
			}
			int num;
			if (exp <= 11) {
				if (exp <= -64 || (num = Length(_bits)) >= 3) {
					return false;
				}
				if (exp < 0) {
					if ((man & (uint)((1 << -exp) - 1)) != 0) {
						return false;
					}
					man >>= -exp;
				} else {
					man <<= exp;
				}
				if (num == 1) {
					return _bits[0] == man;
				}
				if (_bits[0] == Statics.GetLo(man)) {
					return _bits[1] == Statics.GetHi(man);
				}
				return false;
			}
			if ((num = Length(_bits)) <= 2) {
				return false;
			}
			man <<= 11;
			exp -= 11;
			int num2 = (exp - 1) / 32 + 3;
			if (num2 != num) {
				return false;
			}
			int num3 = num2 * 32 - exp - 64;
			if (_bits[--num2] != (uint)(man >> num3 + 32) || _bits[--num2] != (uint)(man >> num3) || (num3 > 0 && _bits[--num2] != (uint)((int)man << 32 - num3))) {
				return false;
			}
			while (--num2 >= 0) {
				if (_bits[num2] != 0) {
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Compare to a double
		/// </summary>
		/// <param name="dbl"></param>
		/// <returns></returns>
		public int CompareTo(double dbl)
		{
			if (_bits == null) {
				return -dbl.CompareTo(_sign);
			}
			NumberUtils.GetDoubleParts(dbl, out var sign, out var exp, out var man, out var fFinite);
			if (!fFinite) {
				if (man == 0) {
					return -sign;
				}
				return 1;
			}
			if (man == 0) {
				return Math.Sign(_sign);
			}
			if (_sign != sign) {
				if (_sign >= 0) {
					return 1;
				}
				return -1;
			}
			int num;
			if (exp <= 11) {
				if (exp <= -64 || (num = Length(_bits)) >= 3) {
					return sign;
				}
				ulong num2 = ((num == 1) ? _bits[0] : Statics.MakeUlong(_bits[1], _bits[0]));
				if (exp < 0) {
					if ((man & (ulong)((1L << -exp) - 1)) != 0) {
						if (man >> -exp < num2) {
							return sign;
						}
						return -sign;
					}
					man >>= -exp;
				} else {
					man <<= exp;
				}
				if (num2 >= man) {
					if (num2 <= man) {
						return 0;
					}
					return sign;
				}
				return -sign;
			}
			if ((num = Length(_bits)) <= 2) {
				return -sign;
			}
			man <<= 11;
			exp -= 11;
			int num3 = (exp - 1) / 32 + 3;
			if (num3 != num) {
				if (num3 >= num) {
					return -sign;
				}
				return sign;
			}
			int num4 = num3 * 32 - exp - 64;
			uint num5;
			uint num6;
			if ((num5 = _bits[--num3]) != (num6 = (uint)(man >> num4 + 32)) || (num5 = _bits[--num3]) != (num6 = (uint)(man >> num4)) || (num4 > 0 && (num5 = _bits[--num3]) != (num6 = (uint)((int)man << 32 - num4)))) {
				if (num5 >= num6) {
					return sign;
				}
				return -sign;
			}
			while (--num3 >= 0) {
				if (_bits[num3] != 0) {
					return sign;
				}
			}
			return 0;
		}

		/// <summary>
		/// Compare integer fractions 
		/// </summary>
		/// <param name="bnNum1"></param>
		/// <param name="bnDen1"></param>
		/// <param name="bnNum2"></param>
		/// <param name="bnDen2"></param>
		/// <returns></returns>
		public static int CompareFractions(BigInteger bnNum1, BigInteger bnDen1, BigInteger bnNum2, BigInteger bnDen2)
		{
			if (bnDen1 == bnDen2) {
				return bnNum1.CompareTo(bnNum2);
			}
			if (bnNum1._bits == null && bnDen1._bits == null && bnNum2._bits == null && bnDen2._bits == null) {
				return Math.Sign((long)bnNum1._sign * (long)bnDen2._sign - (long)bnNum2._sign * (long)bnDen1._sign);
			}
			if (bnNum1._sign == 0) {
				return -Math.Sign(bnNum2._sign);
			}
			int num;
			if (bnNum1._sign < 0) {
				if (bnNum2._sign >= 0) {
					return -1;
				}
				bnNum1._sign = -bnNum1._sign;
				bnNum2._sign = -bnNum2._sign;
				num = -1;
			} else {
				if (bnNum2._sign <= 0) {
					return 1;
				}
				num = 1;
			}
			int num2 = bnNum1.CompareTo(bnNum2);
			int num3 = bnDen1.CompareTo(bnDen2);
			if (num2 == 0) {
				return -num * num3;
			}
			if (num2 != num3) {
				return num * num2;
			}
			int bitCount = bnNum1.BitCount;
			int bitCount2 = bnDen1.BitCount;
			int bitCount3 = bnNum2.BitCount;
			int bitCount4 = bnDen2.BitCount;
			int num4 = bitCount + bitCount4;
			int num5 = bitCount3 + bitCount2;
			if (num4 <= num5 - 2) {
				return -num;
			}
			if (num5 <= num4 - 2) {
				return num;
			}
			BigInteger bigInteger = bnNum1 * bnDen2;
			BigInteger bn = bnNum2 * bnDen1;
			return num * bigInteger.CompareTo(bn);
		}

		/// <summary>
		/// Compare a fraction with a BigInteger. This does NOT assume that GCD(bnNum, bnDen) is 1.
		/// </summary>
		/// <param name="bnNum"></param>
		/// <param name="bnDen"></param>
		/// <param name="bn"></param>
		/// <returns></returns>
		public static int CompareFractionToBigInteger(BigInteger bnNum, BigInteger bnDen, BigInteger bn)
		{
			if (bnDen == 1) {
				return bnNum.CompareTo(bn);
			}
			if (bnNum._bits == null && bnDen._bits == null && bn._bits == null) {
				return Math.Sign(bnNum._sign - (long)bn._sign * (long)bnDen._sign);
			}
			if (bnNum._sign == 0) {
				return -Math.Sign(bn._sign);
			}
			int num;
			if (bnNum._sign < 0) {
				if (bn._sign >= 0) {
					return -1;
				}
				bnNum._sign = -bnNum._sign;
				bn._sign = -bn._sign;
				num = -1;
			} else {
				if (bn._sign <= 0) {
					return 1;
				}
				num = 1;
			}
			if (bnNum <= bn) {
				return -num;
			}
			int bitCount = bnNum.BitCount;
			int bitCount2 = bnDen.BitCount;
			int bitCount3 = bn.BitCount;
			int num2 = bitCount2 + bitCount3;
			if (bitCount <= num2 - 2) {
				return -num;
			}
			if (bitCount > num2) {
				return num;
			}
			return num * bnNum.CompareTo(bnDen * bn);
		}

		/// <summary>
		/// Compare a fraction to a long. This does NOT assume that GCD(bnNum, bnDen) is 1.
		/// </summary>
		/// <param name="bnNum"></param>
		/// <param name="bnDen"></param>
		/// <param name="nn"></param>
		/// <returns></returns>
		public static int CompareFractionToLong(BigInteger bnNum, BigInteger bnDen, long nn)
		{
			if (nn == 0) {
				return Math.Sign(bnNum._sign);
			}
			if (nn > 0) {
				if (bnNum._sign <= 0) {
					return -1;
				}
				return CompareFractionToUlongCore(bnNum, bnDen, (ulong)nn);
			}
			if (bnNum._sign >= 0) {
				return 1;
			}
			return -CompareFractionToUlongCore(-bnNum, bnDen, (ulong)(-nn));
		}

		/// <summary>
		/// Compare a fraction to an unsigned long. This does NOT assume that GCD(bnNum, bnDen) is 1.
		/// </summary>
		/// <param name="bnNum"></param>
		/// <param name="bnDen"></param>
		/// <param name="uu"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static int CompareFractionToUlong(BigInteger bnNum, BigInteger bnDen, ulong uu)
		{
			if (uu == 0) {
				return Math.Sign(bnNum._sign);
			}
			if (bnNum._sign <= 0) {
				return -1;
			}
			return CompareFractionToUlongCore(bnNum, bnDen, uu);
		}

		private static int CompareFractionToUlongCore(BigInteger bnNum, BigInteger bnDen, ulong uu)
		{
			if (bnDen == 1) {
				return bnNum.CompareTo(uu);
			}
			if (bnNum._bits == null) {
				if (bnDen._bits != null) {
					return -1;
				}
				if ((uint)bnNum._sign <= uu) {
					return -1;
				}
				return Math.Sign(bnNum._sign - (long)bnDen._sign * (long)uu);
			}
			int bitCount = bnNum.BitCount;
			int bitCount2 = bnDen.BitCount;
			int num = 64 - Statics.CbitHighZero(uu);
			int num2 = bitCount2 + num;
			if (bitCount <= num2 - 2) {
				return -1;
			}
			if (bitCount > num2) {
				return 1;
			}
			return bnNum.CompareTo(bnDen * uu);
		}

		/// <summary>
		/// convert to strings 
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (_bits == null) {
				return _sign.ToString(CultureInfo.InvariantCulture);
			}
			int num = Length(_bits);
			int num2 = num * 10 / 9 + 2;
			uint[] array = new uint[num2];
			int num3 = 0;
			int num4 = num;
			while (--num4 >= 0) {
				uint num5 = _bits[num4];
				for (int i = 0; i < num3; i++) {
					ulong num6 = Statics.MakeUlong(array[i], num5);
					array[i] = (uint)(num6 % 1000000000);
					num5 = (uint)(num6 / 1000000000);
				}
				if (num5 != 0) {
					array[num3++] = num5 % 1000000000;
					num5 /= 1000000000;
					if (num5 != 0) {
						array[num3++] = num5;
					}
				}
			}
			int num7 = num3 * 9 + 1;
			char[] array2 = new char[num7];
			int num8 = num7;
			for (int j = 0; j < num3 - 1; j++) {
				uint num9 = array[j];
				int num10 = 9;
				while (--num10 >= 0) {
					array2[--num8] = (char)(48 + num9 % 10);
					num9 /= 10;
				}
			}
			for (uint num11 = array[num3 - 1]; num11 != 0; num11 /= 10) {
				array2[--num8] = (char)(48 + num11 % 10);
			}
			if (_sign < 0) {
				array2[--num8] = '-';
			}
			return new string(array2, num8, num7 - num8);
		}

		/// <summary>
		/// conver to a string in hex 
		/// </summary>
		/// <returns></returns>
		public string ToHexString()
		{
			if (_bits == null && _sign >= 0) {
				return _sign.ToString("X", CultureInfo.InvariantCulture);
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (_sign < 0) {
				stringBuilder.Append('-');
			}
			stringBuilder.Append("0x");
			if (_bits == null) {
				stringBuilder.AppendFormat("{0:X}", (uint)(-_sign));
			} else {
				int num = Length(_bits);
				stringBuilder.AppendFormat("{0:X}", _bits[num - 1]);
				int num2 = num - 1;
				while (--num2 >= 0) {
					stringBuilder.AppendFormat("{0:X8}", _bits[num2]);
				}
			}
			return stringBuilder.ToString();
		}
	}
}
