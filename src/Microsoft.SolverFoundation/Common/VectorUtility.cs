using System;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> The VectorUtility class contains a number of methods useful
	///           for working with vectors of double.  It specializes in
	///           extension methods for the double[] type.
	/// </summary>
	internal static class VectorUtility
	{
		/// <summary> The convenience of "params", useful anywhere
		/// </summary>
		public static char[] Params(params char[] x)
		{
			return x;
		}

		/// <summary> The convenience of "params", useful anywhere
		/// </summary>
		public static int[] Params(params int[] x)
		{
			return x;
		}

		/// <summary> The convenience of "params", useful anywhere
		/// </summary>
		public static double[] Params(params double[] x)
		{
			return x;
		}

		/// <summary> The convenience of "params", useful anywhere
		/// </summary>
		public static T[] Params<T>(params T[] x)
		{
			return x;
		}

		/// <summary> Throw an exeption if length of vectors is 0 or if the length of the 
		/// the vectors are not the same
		/// </summary>
		/// <param name="x">vector x</param>
		/// <param name="y">vector y</param>
		internal static void VerifyVectorsAreSameSize(double[] x, double[] y)
		{
			VerifyVectorHasNonZeroLength(x);
			if (x.Length != y.Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "x", "y" }), "y");
			}
		}

		/// <summary>
		/// Throw an exeption if length of vector is 0, 
		/// </summary>
		/// <param name="x">the vector to check</param>
		internal static void VerifyVectorHasNonZeroLength(double[] x)
		{
			if (x.Length == 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.LengthCanNotBeZero0, new object[1] { "x" }), "x");
			}
		}

		/// <summary> Copy x into y, reusing y if possible, creating new otherwise
		/// </summary>
		/// <param name="x"> source values </param>
		/// <param name="y"> array to be overwritten or created with x' values </param>
		public static void CopyOver(this double[] x, ref double[] y)
		{
			if (y == null || x.Length != y.Length)
			{
				y = x.Clone() as double[];
			}
			else
			{
				x.CopyTo(y, 0);
			}
		}

		/// <summary> Copy x into y, reusing y if possible, creating new otherwise
		/// </summary>
		/// <param name="x"> source values </param>
		/// <param name="sourceIndex"> copy all values from startFrom up to end </param>
		/// <param name="y"> array to be overwritten or created with x' values </param>
		public static void CopyOver(this double[] x, int sourceIndex, ref double[] y)
		{
			if (y == null || x.Length - sourceIndex != y.Length)
			{
				y = new double[x.Length - sourceIndex];
			}
			Array.Copy(x, sourceIndex, y, 0, y.Length);
		}

		/// <summary> Copy x into y, reusing y if possible, creating new otherwise
		/// </summary>
		/// <param name="x"> source values </param>
		/// <param name="count"> copy first count values, Length of result </param>
		/// <param name="y"> array to be overwritten or created with x' values </param>
		public static void CopyOver(this double[] x, ref double[] y, int count)
		{
			if (y == null || count != y.Length)
			{
				y = new double[count];
			}
			Array.Copy(x, y, count);
		}

		/// <summary> x[] = 0 -- (extension method)
		/// </summary>
		public static void ZeroFill(this double[] x)
		{
			Array.Clear(x, 0, x.Length);
		}

		/// <summary> x[] = c -- (extension method)
		/// </summary>
		public static void ConstantFill(this double[] x, double c)
		{
			int num = x.Length;
			while (0 <= --num)
			{
				x[num] = c;
			}
		}

		/// <summary> x[] = c -- (extension method)
		/// </summary>
		public static void ConstantFill(this int[] x, int c)
		{
			int num = x.Length;
			while (0 <= --num)
			{
				x[num] = c;
			}
		}

		/// <summary> z[] = x[] + y[] -- pairwise (extension method)
		/// </summary>
		/// <returns> a new vector the same length as x and y </returns>
		public static double[] Plus(this double[] x, double[] y)
		{
			VerifyVectorsAreSameSize(x, y);
			double[] array = new double[x.Length];
			int num = x.Length;
			while (0 <= --num)
			{
				array[num] = x[num] + y[num];
			}
			return array;
		}

		/// <summary> z[] = x[] - y[] -- (z is preallocated) pairwize (extension method)
		/// </summary>
		public static void SubtractInto(this double[] z, double[] x, double[] y)
		{
			VerifyVectorsAreSameSize(x, y);
			VerifyVectorsAreSameSize(x, z);
			int num = x.Length;
			while (0 <= --num)
			{
				z[num] = x[num] - y[num];
			}
		}

		/// <summary> x[] += y -- add scalar y to every element of x
		/// </summary>
		/// <returns> a new vector the same length as x  </returns>
		public static void Increment(this double[] x, double y)
		{
			VerifyVectorHasNonZeroLength(x);
			int num = x.Length;
			while (0 <= --num)
			{
				x[num] += y;
			}
		}

		/// <summary> x[] == y[] -- pairwize 
		/// </summary>
		/// <returns>true if both are pairwize equal</returns>
		public static bool AreExactlyTheSame(double[] x, double[] y)
		{
			VerifyVectorsAreSameSize(x, y);
			int num = x.Length;
			while (0 <= --num)
			{
				if (x[num] != y[num])
				{
					return false;
				}
			}
			return true;
		}

		/// <summary> z[] = x[] - y[] -- pairwise (extension method)
		/// </summary>
		/// <returns> a new vector the same length as x and y </returns>
		public static double[] Minus(this double[] x, double[] y)
		{
			VerifyVectorsAreSameSize(x, y);
			double[] array = new double[x.Length];
			array.SubtractInto(x, y);
			return array;
		}

		/// <summary> return Max value found in x[]
		/// </summary>
		/// <returns> the maximal value in the array </returns>
		public static double Max(this double[] x)
		{
			VerifyVectorHasNonZeroLength(x);
			double num = x[0];
			int num2 = x.Length;
			while (0 < --num2)
			{
				num = Math.Max(x[num2], num);
			}
			return num;
		}

		/// <summary> return Min value found in x[]
		/// </summary>
		/// <returns> the minimal value in the array </returns>
		public static double Min(this double[] x)
		{
			VerifyVectorHasNonZeroLength(x);
			double num = x[0];
			int num2 = x.Length;
			while (0 < --num2)
			{
				num = Math.Min(x[num2], num);
			}
			return num;
		}

		/// <summary> z[] = x[] * y[] -- pairwise (extension method)
		/// </summary>
		/// <returns> a new vector the same length as x and y </returns>
		public static double[] Times(this double[] x, double[] y)
		{
			VerifyVectorsAreSameSize(x, y);
			double[] array = new double[x.Length];
			int num = x.Length;
			while (0 <= --num)
			{
				array[num] = x[num] * y[num];
			}
			return array;
		}

		/// <summary> z[] = x[] / y[] -- pairwise (extension method)
		/// </summary>
		/// <returns> a new vector the same length as x and y </returns>
		public static double[] Over(this double[] x, double[] y)
		{
			VerifyVectorsAreSameSize(x, y);
			double[] array = new double[x.Length];
			int num = x.Length;
			while (0 <= --num)
			{
				array[num] = x[num] / y[num];
			}
			return array;
		}

		/// <summary> x[] += (y[] * c) -- (extension method)
		/// Uses the Daxpy method
		/// </summary>
		/// <returns> a new vector the same length as x and y </returns>
		public static void AddScaledVector(this double[] x, double[] y, double c)
		{
			VerifyVectorsAreSameSize(x, y);
			Daxpy(c, y, x);
		}

		/// <summary> x[] *= a        -- (extension method)
		/// </summary>
		public static void ScaleBy(this double[] x, double a)
		{
			if (1.0 != a)
			{
				int num = x.Length;
				while (0 <= --num)
				{
					x[num] *= a;
				}
			}
		}

		/// <summary> x[] *= a        -- (extension method)
		/// </summary>
		public static void ScaleBy(this double[] x, double a, int start, int count)
		{
			if (1.0 != a)
			{
				int num = start + count - 1;
				while (start <= --num)
				{
					x[num] *= a;
				}
			}
		}

		/// <summary> x[] = y[] * a        -- (extension method)
		/// </summary>update x[] with scaled y[]
		public static void ScaleIntoMe(this double[] x, double[] y, double a)
		{
			VerifyVectorsAreSameSize(x, y);
			int num = x.Length;
			while (0 <= --num)
			{
				x[num] = y[num] * a;
			}
		}

		/// <summary> z = x[]·y[] -- (extension method)
		/// </summary>
		/// <returns> inner (dot) product of x and y </returns>
		public static double InnerProduct(this double[] x, double[] y)
		{
			if (x.Length != y.Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "x", "y" }), "x");
			}
			double num = 0.0;
			int num2 = x.Length;
			while (0 <= --num2)
			{
				num += x[num2] * y[num2];
			}
			return num;
		}

		/// <summary> y[] = a*x[] + y[] -- implement BLAS daxpy operation
		/// </summary>
		public static void Daxpy(double a, double[] x, double[] y)
		{
			VerifyVectorsAreSameSize(x, y);
			int num = x.Length;
			while (0 <= --num)
			{
				y[num] += a * x[num];
			}
		}

		/// <summary> z[] = x[] * y[] -- pairwise (extension method)
		/// </summary>
		public static void ElemMultiply(double[] x, double[] y, double[] z)
		{
			VerifyVectorsAreSameSize(x, y);
			VerifyVectorsAreSameSize(x, z);
			int num = x.Length;
			while (0 <= --num)
			{
				z[num] = x[num] * y[num];
			}
		}

		/// <summary> z[] = x[] / y[] -- pairwise (extension method)
		/// </summary>
		public static void ElemDivide(double[] x, double[] y, double[] z)
		{
			VerifyVectorsAreSameSize(x, y);
			VerifyVectorsAreSameSize(x, z);
			int num = x.Length;
			while (0 <= --num)
			{
				z[num] = x[num] / y[num];
			}
		}

		/// <summary> a = 2-norm of vector x -- (extension method)
		/// </summary>
		/// <returns> Euclidean norm of vector x </returns>
		public static double Norm2(this double[] x)
		{
			double num = 0.0;
			int num2 = x.Length;
			while (0 <= --num2)
			{
				num += x[num2] * x[num2];
			}
			return Math.Sqrt(num);
		}

		/// <summary> a = Infinity-norm of vector x -- (extension method)
		/// </summary>
		/// <returns> Infinity-norm of vector x </returns>
		public static double NormInf(this double[] x)
		{
			double num = 0.0;
			int num2 = x.Length;
			while (0 <= --num2)
			{
				num = Math.Max(num, Math.Abs(x[num2]));
			}
			return num;
		}

		/// <summary> z[] = [ x[]; y[] ] -- vector concatenation (pre-allocated memory)
		/// </summary>
		public static void Concat(double[] x, double[] y, double[] z)
		{
			if (x.Length + y.Length != z.Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.LenghtShouldEqualToSumOfLengths012, new object[3] { "z", "x", "y" }));
			}
			int num = x.Length;
			while (0 <= --num)
			{
				z[num] = x[num];
			}
			int num2 = y.Length;
			while (0 <= --num2)
			{
				z[num2 + x.Length] = y[num2];
			}
		}

		/// <summary> [ x[]; y[] ] = z[]  -- split vector z into x and y (pre-allocated memory)
		/// </summary>
		public static void Split(double[] z, double[] x, double[] y)
		{
			if (x.Length + y.Length != z.Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.LenghtShouldEqualToSumOfLengths012, new object[3] { "z", "x", "y" }));
			}
			int num = x.Length;
			while (0 <= --num)
			{
				x[num] = z[num];
			}
			int num2 = y.Length;
			while (0 <= --num2)
			{
				y[num2] = z[num2 + x.Length];
			}
		}

		/// <summary> Test if a sequence of integers has values all unique in ascending order
		/// </summary>
		/// <returns> true iff unique values in ascending order </returns>
		public static bool AreUniqueAscending(int[] seq, int first, int beyond)
		{
			if (first + 1 < beyond)
			{
				int num = seq[first];
				for (int i = first + 1; i < beyond; i++)
				{
					if (seq[i] <= num)
					{
						return false;
					}
					num = seq[i];
				}
			}
			return true;
		}
	}
}
