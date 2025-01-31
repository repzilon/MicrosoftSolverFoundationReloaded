using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Static Utilities
	/// </summary>
	internal static class EvaluationStatics
	{
		internal static IEnumerable<T> Enumerate<T>(T x) where T : class
		{
			yield return x;
		}

		internal static IEnumerable<T> Enumerate<T>(T x, T y) where T : class
		{
			yield return x;
			yield return y;
		}

		internal static IEnumerable<T> Enumerate<T>(T x, T y, T z)
		{
			yield return x;
			yield return y;
			yield return z;
		}

		/// <summary>
		/// Union of two IEnumerables
		/// </summary>
		/// <remarks>
		/// If using .NET framework 4 this is simply Enumerable.Union, which 
		/// takes care correctly of co/contra-variance.
		/// </remarks>
		internal static IEnumerable<T> Union<A, B, T>(IEnumerable<A> list1, IEnumerable<B> list2) where A : T where B : T
		{
			foreach (A elt in list1)
			{
				yield return (T)(object)elt;
			}
			foreach (B elt2 in list2)
			{
				yield return (T)(object)elt2;
			}
		}

		/// <summary>
		/// Amortized expensive assertions, ran only probabilistically
		/// to amortize its cost
		/// </summary>
		[Conditional("DEBUG")]
		internal static void ExpensiveAssert(Func<bool> condition)
		{
		}

		/// <summary>
		/// Randomly permutates the elements in an array
		/// </summary>
		public static void Permutate<T>(T[] array, Random prng)
		{
			for (int num = array.Length - 1; num > 0; num--)
			{
				int num2 = prng.Next(0, num + 1);
				T val = array[num2];
				array[num2] = array[num];
				array[num] = val;
			}
		}

		/// <summary>
		/// True if the two sequences have the same elements
		/// (set semantics: multiple occurrences 
		/// </summary>
		public static bool SetEqual<T>(IEnumerable<T> seq1, IEnumerable<T> seq2)
		{
			HashSet<T> hashSet = new HashSet<T>(seq1);
			return hashSet.SetEquals(seq2);
		}

		/// <summary>
		/// Creates a new array that contains the content of the array from 
		/// indexes first to last, inclusive
		/// </summary>
		/// <remarks>
		/// (1) Could not find a way to do that by more direct use of library calls?
		/// (2) I like extension methods - now they might polute the Intellisense for
		/// arrays in the whole project, so using good old static method here
		/// </remarks>
		internal static T[] SnapshotRange<T>(T[] array, int first, int last)
		{
			int num = last - first + 1;
			T[] array2 = new T[num];
			Array.Copy(array, first, array2, 0, num);
			return array2;
		}

		/// <summary>
		/// Can the double be stored as an 32-bit integer value?
		/// </summary>
		internal static bool IsInteger32(double val)
		{
			int num = (int)val;
			return (double)num == val;
		}

		/// <summary>
		/// Can the double be stored as an 64-bit integer value?
		/// </summary>
		internal static bool IsInteger64(double val)
		{
			long num = (long)val;
			return (double)num == val;
		}

		/// <summary>
		/// Generation of 64-bit pseudo-random number.
		/// For consistency with Next(int) the number is non-negative
		/// </summary>
		internal static long NextLong(this Random prng)
		{
			byte[] array = new byte[8];
			prng.NextBytes(array);
			long value = BitConverter.ToInt64(array, 0);
			return Math.Abs(value);
		}

		/// <summary>
		/// Generation of a double pseudo-random nonnegative number
		/// not bounded to be in [0,1], .e.g any representable double.
		/// The number can be positive or negative. It is never
		/// an infinity, a NaN, MinValue or MaxValue
		/// </summary>
		internal static double NextUnboundedDouble(this Random prng)
		{
			byte[] array = new byte[8];
			double num;
			do
			{
				prng.NextBytes(array);
				num = BitConverter.ToDouble(array, 0);
			}
			while (!IsFiniteNonExtremal(num));
			return num;
		}

		internal static bool IsFiniteNonExtremal(double v)
		{
			if (!double.IsInfinity(v) && !double.IsNaN(v) && v != double.MinValue)
			{
				return v != double.MaxValue;
			}
			return false;
		}

		/// <summary>
		/// Selects a double uniformly at random within the bounds
		/// </summary>
		internal static double NextDouble(this Random prng, double left, double right)
		{
			if ((left == double.MinValue || left == double.NegativeInfinity) && (right == double.MaxValue || right == double.PositiveInfinity))
			{
				return prng.NextUnboundedDouble();
			}
			double num = right - left;
			if (!IsFiniteNonExtremal(num))
			{
				double num2;
				do
				{
					num2 = prng.NextUnboundedDouble();
				}
				while (!(left <= num2) || !(num2 < right));
				return num2;
			}
			double val = left + num * prng.NextDouble();
			val = Math.Max(val, left);
			return Math.Min(val, right);
		}

		/// <summary>
		/// Selects a random element within the left bound inclusive and 
		/// the right bound EXCLUSIVE
		/// </summary>
		internal static long NextLong(this Random prng, long left, long right)
		{
			if (left >= right)
			{
				throw new ArgumentOutOfRangeException("left");
			}
			if (int.MinValue <= left && right <= int.MaxValue)
			{
				return prng.Next((int)left, (int)right);
			}
			if (left + 1 == right)
			{
				return left;
			}
			BigInteger bigInteger = right;
			BigInteger bigInteger2 = bigInteger - left;
			bigInteger = prng.NextLong();
			bigInteger <<= 1;
			bigInteger |= (BigInteger)prng.Next(2);
			bigInteger %= bigInteger2;
			bigInteger += (BigInteger)left;
			return (long)bigInteger;
		}
	}
}
