using System;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> A wrapper for double[] which implements algebraic operators.
	/// </summary>
	internal class Vector : IVector
	{
		private readonly int _start;

		protected readonly double[] _v;

		/// <summary>The start index (usually 0).
		/// </summary>
		public int Start => _start;

		/// <summary> Element accessor for the vector.
		/// </summary>
		/// <remarks>If performance is a concern, directly access the array using V and Start.</remarks>
		public double this[int i]
		{
			get
			{
				return _v[i + _start];
			}
			set
			{
				_v[i + _start] = value;
			}
		}

		/// <summary> The length of the vector.
		/// </summary>
		public virtual int Length
		{
			get
			{
				if (_v != null)
				{
					return _v.Length;
				}
				return 0;
			}
		}

		/// <summary> Construct an empty vector of specified length
		/// </summary>
		public Vector(int length)
		{
			_v = new double[length];
		}

		/// <summary> Construct a vector with the specified values.
		/// </summary>
		public Vector(double[] values)
		{
			_v = values;
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}
		}

		/// <summary> Construct a vector with the specified values and start index.
		/// </summary>
		protected Vector(double[] values, int start)
		{
			_v = values;
			_start = start;
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}
			if (_start < 0 || _start > _v.Length)
			{
				throw new ArgumentOutOfRangeException("start");
			}
		}

		/// <summary> Construct a vector with the specified values and start index.
		/// </summary>
		protected Vector(Vector v, int start)
		{
			_v = v._v;
			_start = start;
			if (v == null)
			{
				throw new ArgumentNullException("v");
			}
			if (_start < 0 || _start > _v.Length)
			{
				throw new ArgumentOutOfRangeException("start");
			}
		}

		public Vector(Vector a, Vector b)
			: this(a.Length + b.Length)
		{
			int num = 0;
			for (int i = 0; i < a.Length; i++)
			{
				this[num++] = a[i];
			}
			for (int j = 0; j < b.Length; j++)
			{
				this[num++] = b[j];
			}
		}

		/// <summary> format values as a string.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < Length; i++)
			{
				if (4096 < stringBuilder.Length)
				{
					break;
				}
				stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "[{0}] {1}, ", new object[2]
				{
					i,
					this[i]
				}));
			}
			return stringBuilder.ToString();
		}

		/// <summary> Throw an exeption if the length of the vectors are not the same
		/// </summary>
		internal int VerifySameLength(Vector y)
		{
			if (Length != y.Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "x", "y" }), "y");
			}
			return Length;
		}

		/// <summary> Throw an exeption if length of the vector is too small.
		/// </summary>
		internal void VerifyMinimumLength(int length)
		{
			if (Length < length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.LengthCanNotBeZero0, new object[1] { "length" }), "length");
			}
		}

		/// <summary> Throw an exception if length of vector is 0.
		/// </summary>
		internal void VerifyNonZeroLength()
		{
			if (Length == 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.LengthCanNotBeZero0, new object[1] { "x" }), "x");
			}
		}

		/// <summary> x[] = 0
		/// </summary>
		public Vector ZeroFill()
		{
			ConstantFill(0.0);
			return this;
		}

		/// <summary> Return maximum value
		/// </summary>
		public double Max()
		{
			VerifyNonZeroLength();
			double num = this[0];
			int num2 = Length;
			while (0 < --num2)
			{
				num = Math.Max(this[num2], num);
			}
			return num;
		}

		/// <summary> Return minimum value
		/// </summary>
		public double Min()
		{
			VerifyNonZeroLength();
			double num = this[0];
			int num2 = Length;
			while (0 < --num2)
			{
				num = Math.Min(this[num2], num);
			}
			return num;
		}

		/// <summary> Inner product as a BigSum.
		/// </summary>
		/// <returns> inner (dot) product of x and y </returns>
		public BigSum BigInnerProduct(Vector y)
		{
			VerifySameLength(y);
			BigSum result = 0.0;
			for (int i = 0; i < Length; i++)
			{
				result.Add(this[i] * y[i]);
			}
			return result;
		}

		/// <summary> return sum of all values
		/// </summary>
		public double Sum()
		{
			double num = 0.0;
			int num2 = Length + _start;
			while (_start <= --num2)
			{
				num += _v[num2];
			}
			return num;
		}

		/// <summary> return sum of all values
		/// </summary>
		public BigSum BigSum()
		{
			BigSum result = 0.0;
			int num = Length;
			while (0 <= --num)
			{
				result.Add(this[num]);
			}
			return result;
		}

		/// <summary>Determines if a vector is null or length 0.
		/// </summary>
		public static bool IsNullOrEmpty(Vector x)
		{
			if (x != null)
			{
				return x.Length == 0;
			}
			return true;
		}

		/// <summary> z[] = x[] + y[] -- pairwize
		/// </summary>
		public static void Add(Vector x, Vector y, Vector z)
		{
			x.VerifySameLength(y);
			int num = x.Length;
			while (0 <= --num)
			{
				z[num] = x[num] + y[num];
			}
		}

		/// <summary> z[] = x[] - y[] -- pairwize
		/// </summary>
		public static void Subtract(Vector x, Vector y, Vector z)
		{
			x.VerifySameLength(y);
			int num = x.Length;
			while (0 <= --num)
			{
				z[num] = x[num] - y[num];
			}
		}

		/// <summary> z[] = x[] * y[] -- (z is preallocated) pairwise
		/// </summary>
		public static void ElementMultiply(Vector x, Vector y, Vector z)
		{
			x.VerifySameLength(y);
			z.VerifyMinimumLength(x.Length);
			int num = x.Length;
			while (0 <= --num)
			{
				z[num] = x[num] * y[num];
			}
		}

		/// <summary> z[] = x[] / y[] -- (z is preallocated) pairwise
		/// </summary>
		public static void ElementDivide(Vector x, Vector y, Vector z)
		{
			x.VerifySameLength(y);
			int num = x.Length;
			while (0 <= --num)
			{
				z[num] = x[num] / y[num];
			}
		}

		/// <summary> y[] += (alpha * x[]) 
		/// </summary>
		public static void Daxpy(double alpha, Vector x, Vector y)
		{
			int num = x.VerifySameLength(y) + x._start;
			int num2 = x._start;
			int num3 = y._start;
			while (num2 < num)
			{
				y._v[num3] += alpha * x._v[num2];
				num2++;
				num3++;
			}
		}

		/// <summary> z = (alpha * x) + (beta * y)
		/// </summary>
		public static void ScaledSum(double alpha, Vector x, double beta, Vector y, Vector z)
		{
			y.VerifySameLength(z);
			int num = x.VerifySameLength(y) + x._start;
			int num2 = x._start;
			int num3 = y._start;
			int num4 = z._start;
			while (num2 < num)
			{
				z._v[num4] = alpha * x._v[num2] + beta * y._v[num3];
				num2++;
				num3++;
				num4++;
			}
		}

		/// <summary> v[] += y[]
		/// </summary>
		public Vector Add(Vector y)
		{
			Add(this, y, this);
			return this;
		}

		/// <summary> v[] -= y[]
		/// </summary>
		public Vector Subtract(Vector y)
		{
			Subtract(this, y, this);
			return this;
		}

		/// <summary> v[] *= y[]
		/// </summary>
		public Vector ElementMultiply(Vector y)
		{
			ElementMultiply(this, y, this);
			return this;
		}

		/// <summary> v[] /= y[]
		/// </summary>
		public Vector ElementDivide(Vector y)
		{
			ElementDivide(this, y, this);
			return this;
		}

		internal static void SumProductRight(double p, Vector _s, double p_3, Vector _r_cs)
		{
			throw new NotImplementedException();
		}

		/// <summary> The contents of the vector.
		/// </summary>
		public virtual double[] ToArray()
		{
			return _v;
		}

		/// <summary> a = 2-norm of vector x.
		/// </summary>
		/// <returns> Euclidean norm of vector x </returns>
		public double Norm2()
		{
			double num = 0.0;
			int num2 = Length + _start;
			while (_start <= --num2)
			{
				num += _v[num2] * _v[num2];
			}
			return Math.Sqrt(num);
		}

		/// <summary> a = Infinity-norm of vector x.
		/// </summary>
		/// <returns> Infinity-norm of vector x </returns>
		public double NormInf()
		{
			double num = 0.0;
			int num2 = Length + _start;
			while (_start <= --num2)
			{
				num = Math.Max(num, Math.Abs(_v[num2]));
			}
			return num;
		}

		/// <summary> v[] = c
		/// </summary>
		public void ConstantFill(double c)
		{
			for (int i = _start; i < _start + Length; i++)
			{
				_v[i] = c;
			}
		}

		/// <summary> v[] += y
		/// </summary>
		public Vector AddConstant(double y)
		{
			int num = Length + _start;
			while (_start <= --num)
			{
				_v[num] += y;
			}
			return this;
		}

		/// <summary> v[] *= y
		/// </summary>
		public Vector ScaleBy(double y)
		{
			int num = Length + _start;
			while (_start <= --num)
			{
				_v[num] *= y;
			}
			return this;
		}

		/// <summary> v[] /= y
		/// </summary>
		public Vector Over(double y)
		{
			int num = Length + _start;
			while (_start <= --num)
			{
				_v[num] /= y;
			}
			return this;
		}

		/// <summary> y[] = 1 / x[] -- pairwise
		/// </summary>
		public void ElementInvert(Vector y)
		{
			int num = VerifySameLength(y) + _start;
			int num2 = _start;
			int num3 = y._start;
			while (num2 < num)
			{
				y._v[num3] = 1.0 / _v[num2];
				num2++;
				num3++;
			}
		}

		/// <summary> z = x[]·y[] 
		/// </summary>
		/// <returns> inner (dot) product of x and y </returns>
		public double InnerProduct(Vector y)
		{
			int num = VerifySameLength(y) + _start;
			double num2 = 0.0;
			int num3 = _start;
			int num4 = y._start;
			while (num3 < num)
			{
				num2 += _v[num3] * y._v[num4];
				num3++;
				num4++;
			}
			return num2;
		}

		/// <summary>Swap entries according to the specified pivot vector.
		/// </summary>
		/// <param name="pivot">Entry i will be swapped with pivot[i].  pivot is NOT a permutation.</param>
		public void Pivot(int[] pivot)
		{
			for (int i = 0; i < Length; i++)
			{
				if (i != pivot[i])
				{
					double num = _v[i];
					_v[i] = _v[pivot[i]];
					_v[pivot[i]] = num;
				}
			}
		}

		/// <summary> Permute from v[i] to v[fromTo[i]]
		/// </summary>
		/// <param name="fromTo"> the fromTo[] vector is an exact 1:1 pairing </param>
		public void Permute(int[] fromTo)
		{
			if (fromTo.Length != Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { fromTo, this }));
			}
			bool[] array = new bool[Length];
			for (int i = 0; i < Length; i++)
			{
				if (array[i])
				{
					continue;
				}
				int num = fromTo[i];
				if (i != num)
				{
					double value = this[i];
					int num2 = i;
					while (i != num)
					{
						this[num2] = this[num];
						array[num] = true;
						num2 = num;
						num = fromTo[num2];
					}
					this[num2] = value;
				}
			}
		}

		/// <summary> Construct a new Vector as a copy of this
		/// </summary>
		public Vector Copy()
		{
			Vector vector = new Vector(Length);
			vector.CopyFrom(this);
			return vector;
		}

		/// <summary> Copy values from another vector.
		/// </summary>
		public void CopyFrom(Vector y)
		{
			VerifySameLength(y);
			Array.Copy(y._v, y._start, _v, _start, y.Length);
		}

		/// <summary> Copy values from another vector.
		/// </summary>
		public static void Copy(Vector source, int sourceIndex, Vector destination, int destinationIndex, int count)
		{
			Array.Copy(source._v, sourceIndex, destination._v, destinationIndex, count);
		}

		/// <summary> [ x[]; y[] ] = this  -- split vector into x and y (pre-allocated memory)
		/// </summary>
		public void Split(Vector x, Vector y)
		{
			if (x.Length + y.Length != Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.LenghtShouldEqualToSumOfLengths012, new object[3] { "z", "x", "y" }));
			}
			int num = x.Length;
			while (0 <= --num)
			{
				x[num] = this[num];
			}
			int length = x.Length;
			int num2 = y.Length;
			while (0 <= --num2)
			{
				y[num2] = this[num2 + length];
			}
		}
	}
}
