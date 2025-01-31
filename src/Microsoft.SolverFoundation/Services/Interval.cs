using System;
using System.Diagnostics;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>An interval over a total order.
	/// </summary>
	/// <remarks>
	/// Need to change to use a sparse set of intervals for most of the applications of this class.
	/// </remarks>
	/// <typeparam name="Type"></typeparam>
	[DebuggerDisplay("[{_lowerBound}, {_upperBound}]")]
	internal sealed class Interval<Type> where Type : IComparable<Type>
	{
		private readonly Type _lowerBound;

		private readonly IntervalBoundKind _lowerBoundKind;

		private readonly Type _upperBound;

		private readonly IntervalBoundKind _upperBoundKind;

		/// <summary>
		/// Lower bound of the interval.
		/// </summary>
		public Type LowerBound => _lowerBound;

		/// <summary>
		/// Upper bound of the interval.
		/// </summary>
		public Type UpperBound => _upperBound;

		public IntervalBoundKind LowerBoundKind => _lowerBoundKind;

		public IntervalBoundKind UpperBoundKind => _upperBoundKind;

		public bool LowerBoundClosed => _lowerBoundKind == IntervalBoundKind.Closed;

		public bool LowerBoundOpen => _lowerBoundKind == IntervalBoundKind.Open;

		public bool UpperBoundClosed => _upperBoundKind == IntervalBoundKind.Closed;

		public bool UpperBoundOpen => _upperBoundKind == IntervalBoundKind.Open;

		/// <summary>
		/// Create the closed interval [lowerBound, upperBound].
		/// </summary>
		/// <param name="lowerBound"></param>
		/// <param name="upperBound"></param>
		public Interval(Type lowerBound, Type upperBound)
			: this(lowerBound, IntervalBoundKind.Closed, upperBound, IntervalBoundKind.Closed)
		{
		}

		/// <summary>
		/// Create an interval. Intervals may be open or closed on either side. Intervals with
		/// closed bounds includes the boundary point and intervals with open bounds do not. Thus
		/// <code>new Interval(1, IntervalBoundKind.Open, 10, IntervalBoundKind.Closed)</code>
		/// includes 10 and 2 but not 1.
		/// </summary>
		/// <remarks>
		/// Bounds like (1,1) are permitted but define empty sets.
		/// </remarks>
		/// <param name="lowerBound"></param>
		/// <param name="lowerBoundKind"></param>
		/// <param name="upperBound"></param>
		/// <param name="upperBoundKind"></param>
		public Interval(Type lowerBound, IntervalBoundKind lowerBoundKind, Type upperBound, IntervalBoundKind upperBoundKind)
		{
			if (lowerBound.CompareTo(upperBound) > 0)
			{
				throw new ArgumentOutOfRangeException("lowerBound", Resources.LowerBoundCannotBeLargerThanUpperBound);
			}
			_lowerBound = lowerBound;
			_lowerBoundKind = lowerBoundKind;
			_upperBound = upperBound;
			_upperBoundKind = upperBoundKind;
		}

		public bool Contains(Type element)
		{
			if (LowerBoundClosed ? (_lowerBound.CompareTo(element) <= 0) : (_lowerBound.CompareTo(element) < 0))
			{
				if (!UpperBoundClosed)
				{
					return _upperBound.CompareTo(element) > 0;
				}
				return _upperBound.CompareTo(element) >= 0;
			}
			return false;
		}
	}
}
