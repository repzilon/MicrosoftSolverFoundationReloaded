using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// The Domain of variables used by local search.
	/// Contain real values. 
	/// Can be sampled in several ways, exhaustively enumerated when finite, etc.
	/// </summary>
	internal abstract class LocalSearchDomain : IEnumerable<double>, IEnumerable
	{
		/// <summary>
		/// Max number of attempts when we pick a new value for a variable 
		/// that has THIS domain, and try to generate a value different from
		/// the variable's current value
		/// </summary>
		internal const int AttemptsCount = 10;

		/// <summary>
		/// The domain for Booleans
		/// </summary>
		public static readonly LocalSearchDomain Booleans = new LocalSearchIntegerInterval(0L, 1L);

		/// <summary>
		/// The default domain is the maximum real domain
		/// </summary>
		public static readonly LocalSearchDomain DefaultDomain = new LocalSearchContinuousInterval(double.MinValue, double.MaxValue);

		/// <summary>
		/// Number of elements in the domain; 
		/// by convention long.MaxValue if the domain is continuous
		/// </summary>
		public abstract long Cardinality { get; }

		/// <summary>
		/// Difference between the Lower and Upper bounds
		/// (in particular 0 if identical)
		/// </summary>
		public double Width
		{
			get
			{
				double num = Upper - Lower;
				if (!double.IsNaN(num) && !double.IsPositiveInfinity(num))
				{
					return num;
				}
				return double.MaxValue;
			}
		}

		/// <summary>
		/// Smallest value in the domain
		/// </summary>
		public abstract double Lower { get; }

		/// <summary>
		/// Highest value in the domain
		/// </summary>
		public abstract double Upper { get; }

		/// <summary>
		/// True if the domain has finite cardinality
		/// </summary>
		public bool IsFinite => Cardinality != long.MaxValue;

		/// <summary>
		/// True if the domain contains only integer values
		/// </summary>
		public abstract bool IsDiscrete { get; }

		/// <summary>
		/// Extract an element from the domain, uniformly at random
		/// </summary>
		public abstract double Sample(Random prng);

		/// <summary>
		/// Select at random a value from the domain that is 
		/// preferably at a limited distance from the current value. 
		/// </summary>
		/// <remarks>
		/// The distance contract is not guaranteed to be respected,
		/// only given as hint and respected if doing it is easy to do
		/// and is valuable. 
		/// </remarks>
		/// <param name="prng">
		/// A pseudo-random number generator
		/// </param>
		/// <param name="currentValue">
		/// An initial value from the domain
		/// </param>
		/// <param name="distance">
		/// Maximum distance allowed for the new point
		/// </param>
		public abstract double PickNeighbour(Random prng, double currentValue, double distance);

		/// <summary>
		/// True if the value is included in the Domain
		/// </summary>
		public abstract bool Contains(double val);

		/// <summary>
		/// Protected method used for enumeration
		/// </summary>
		protected virtual IEnumerable<double> Enumerate()
		{
			throw new NotSupportedException();
		}

		IEnumerator<double> IEnumerable<double>.GetEnumerator()
		{
			return Enumerate().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Enumerate().GetEnumerator();
		}

		internal static bool SortedUniqueIncreasing<T>(T[] list) where T : struct, IComparable<T>
		{
			if (list.Length == 0)
			{
				return true;
			}
			T val = list[0];
			for (int i = 1; i < list.Length; i++)
			{
				if (val.CompareTo(list[i]) < 0)
				{
					val = list[i];
					continue;
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Ceiling. Result is bound to be within MinValue, MaxValue 
		/// </summary>
		internal static long Ceiling(double x)
		{
			return CastAsInteger(Math.Ceiling(x));
		}

		/// <summary>
		/// Floor. Result is bound to be within MinValue, MaxValue
		/// </summary>
		internal static long Floor(double x)
		{
			return CastAsInteger(Math.Floor(x));
		}

		/// <summary>
		/// A cast operation that bounds the result 
		/// to be within MinValue, MaxValue
		/// </summary>
		internal static long CastAsInteger(double x)
		{
			if (x >= 9.223372036854776E+18)
			{
				return long.MaxValue;
			}
			if (x <= -9.223372036854776E+18)
			{
				return long.MinValue;
			}
			return (long)x;
		}
	}
}
