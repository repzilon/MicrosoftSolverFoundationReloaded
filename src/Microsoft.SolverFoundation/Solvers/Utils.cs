using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Declaration of a number of utilities
	/// </summary>
	internal static class Utils
	{
		/// <summary>
		///   Unlikely constant value, used when int not initialized
		/// </summary>
		public const int NonInitialized = -1234567890;

		/// <summary>
		///   Default minimum value of an integer variable.
		///   Any value below this will be ignored
		/// </summary>
		public const long DefaultMinValue = -4611686018427387903L;

		/// <summary>
		///   Default maximum value of an integer variable
		///   Any value beyond this will be ignored
		/// </summary>
		public const long DefaultMaxValue = 4611686018427387903L;

		/// <summary>
		///   Maximum in a enumeration (slow)
		/// </summary>
		public static T Max<T>(IEnumerable<T> list) where T : IComparable
		{
			IEnumerator<T> enumerator = list.GetEnumerator();
			enumerator.MoveNext();
			T val = enumerator.Current;
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				if (current.CompareTo(val) > 0)
				{
					val = current;
				}
			}
			return val;
		}

		/// <summary>
		///   Maximum in a enumeration (slow)
		/// </summary>
		public static T Min<T>(IEnumerable<T> list) where T : IComparable
		{
			IEnumerator<T> enumerator = list.GetEnumerator();
			enumerator.MoveNext();
			T val = enumerator.Current;
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				if (current.CompareTo(val) < 0)
				{
					val = current;
				}
			}
			return val;
		}

		/// <summary>
		///   Swap two elements of the same type
		/// </summary>
		public static void Swap<T>(ref T left, ref T right)
		{
			T val = left;
			left = right;
			right = val;
		}

		/// <summary>
		///   Permutates the elements of an array
		/// </summary>
		public static void RandomPermutation<T>(T[] array, Random prng)
		{
			int num = array.Length;
			for (int num2 = num - 1; num2 > 0; num2--)
			{
				int num3 = prng.Next(0, num);
				Swap(ref array[num3], ref array[num2]);
			}
		}

		/// <summary>
		///   A method that returns always true.
		///   Yes - this is sometimes (moderatly) useful.
		/// </summary>
		/// <returns></returns>
		public static bool AlwaysTrue()
		{
			return true;
		}

		/// <summary>
		///   A method that returns always false.
		///   Yes - this is sometimes (moderatly) useful.
		/// </summary>
		public static bool AlwaysFalse()
		{
			return false;
		}

		/// <summary>
		///   Generic method checking whether all elements
		///   in a collection satisfy a certain condition.
		/// </summary>
		public static bool TrueForAll<T>(IEnumerable<T> collec, UnaryPredicate<T> cond)
		{
			foreach (T item in collec)
			{
				if (!cond(item))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		///   Applies a function to all the elements in a collection
		///   and returns the results.
		/// </summary>
		public static IEnumerable<B> Apply<A, B>(IEnumerable<A> list, UnaryFunction<A, B> fun)
		{
			foreach (A a in list)
			{
				yield return fun(a);
			}
		}

		/// <summary>
		///   Computes Ceil(i/j).
		///   The result is checked and guaranteed to be over-approximated
		/// </summary>
		public static long CeilOfDiv(long i, long j)
		{
			if (i == 0)
			{
				return 0L;
			}
			double a = (double)i / (double)j;
			long result = (long)Math.Ceiling(a);
			long num = result * j;
			if ((j > 0) ? (num < i) : (num > i))
			{
				CorrectCeilOfDiv(i, j, ref result);
			}
			return result;
		}

		/// <summary>
		///   Given two ints i and j and a result which approximates
		///   Ceil(i/j) fix correct the result until it's a correct 
		///   over-approximation 
		/// </summary>
		public static void CorrectCeilOfDiv(long i, long j, ref long result)
		{
			checked
			{
				if (j > 0)
				{
					int num = 1;
					while (result * j < i)
					{
						result += num;
						num = unchecked(num * 2);
					}
				}
				else
				{
					int num2 = 1;
					while (result * j > i)
					{
						result += num2;
						num2 = unchecked(num2 * 2);
					}
				}
			}
		}

		/// <summary>
		///   Computes Floor(i/j).
		///   The result is checked and guaranteed to be under-approximated
		/// </summary>
		public static long FloorOfDiv(long i, long j)
		{
			if (i == 0)
			{
				return 0L;
			}
			double d = (double)i / (double)j;
			long result = (long)Math.Floor(d);
			long num = result * j;
			if ((j > 0) ? (num > i) : (num < i))
			{
				CorrectFloorOfDiv(i, j, ref result);
			}
			return result;
		}

		/// <summary>
		///   Given two ints i and j and a result which approximates
		///   Floor(i/j) fix correct the result until it's a correct 
		///   under-approximation 
		/// </summary>
		public static void CorrectFloorOfDiv(long i, long j, ref long result)
		{
			checked
			{
				if (j > 0)
				{
					int num = 1;
					while (result * j > i)
					{
						result -= num;
						num = unchecked(num * 2);
					}
				}
				else
				{
					int num2 = 1;
					while (result * j < i)
					{
						result -= num2;
						num2 = unchecked(num2 * 2);
					}
				}
			}
		}

		/// <summary>
		///   From and element e, returns the set {e}
		/// </summary>
		public static IEnumerable<A> Singleton<A>(A elt)
		{
			yield return elt;
		}

		/// <summary>
		///   From two elements x, y; returns the pair {x, y}
		/// </summary>
		public static IEnumerable<A> Pair<A>(A x, A y)
		{
			yield return x;
			yield return y;
		}

		/// <summary>
		///   True iff the array contains values in
		///   strictly ascending order
		/// </summary>
		internal static bool IsOrderedUnique<A>(A[] array) where A : IComparable<A>
		{
			if (array.Length == 0)
			{
				return true;
			}
			int num = array.Length;
			A val = array[0];
			for (int i = 1; i < num; i++)
			{
				A val2 = array[i];
				if (val.CompareTo(val2) >= 0)
				{
					return false;
				}
				val = val2;
			}
			return true;
		}

		/// <summary>
		///   extract from a (possibly unordered/redundant) collection the
		///   ordered, non-redundant list of its values
		/// </summary>
		public static List<A> GetOrderedUnique<A>(IEnumerable<A> collection) where A : IComparable<A>
		{
			List<A> list = new List<A>(collection);
			if (list.Count != 0)
			{
				list.Sort();
				int num = 1;
				int i = 1;
				int count = list.Count;
				A val = list[0];
				for (; i < count; i++)
				{
					A val2 = list[i];
					if (val.CompareTo(val2) != 0)
					{
						val = (list[num] = val2);
						num++;
					}
				}
				list.RemoveRange(num, list.Count - num);
			}
			list.TrimExcess();
			return list;
		}

		/// <summary>
		///   true iff the set of values in the collection intersects the
		///   set of values in the interval
		/// </summary>
		public static bool Intersects(IEnumerable<Interval> collection, Interval itv)
		{
			foreach (Interval item in collection)
			{
				if (item.Upper >= itv.Lower && itv.Upper >= item.Lower)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		///   Within a sorted-increasing array of integers,
		///   locates the position of the first element
		///   that is greater or equal to a specified value.
		///   If no such position exists returns array length.
		/// </summary>
		public static int PositionFirstGreaterEqual(long[] array, long v)
		{
			int num = Array.BinarySearch(array, v);
			if (num < 0)
			{
				num = ~num;
			}
			return num;
		}

		/// <summary>
		///   Within a sorted-increasing list of integers,
		///   locates the position of the first element
		///   that is greater or equal to a specified value.
		///   If no such position exists returns array length.
		/// </summary>
		public static int PositionFirstGreaterEqual(List<long> list, long v)
		{
			int num = list.BinarySearch(v);
			if (num < 0)
			{
				num = ~num;
			}
			return num;
		}

		/// <summary>
		///   Validation method; false if the product of two integers
		///   overflows
		/// </summary>
		public static bool ProductDoesNotOverflow(long l, long r)
		{
			try
			{
				long num = checked(l * r);
				return num == l * r;
			}
			catch (OverflowException)
			{
				return false;
			}
		}
	}
}
