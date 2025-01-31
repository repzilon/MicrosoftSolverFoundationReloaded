using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Class of convex (i.e. interval) value sets for specifying the
	///   initial range of variables. Use by default preferably to more
	///   costly SparseSet
	/// </summary>
	/// <remarks>
	///   Naive implementation
	/// </remarks>
	internal class ConvexDomain : DisolverDiscreteDomain
	{
		private long _lowerBound;

		private long _upperBound;

		private static ConvexDomain _empty = new ConvexDomain(1L, -1L);

		public override int Count => (int)(_upperBound - _lowerBound + 1);

		internal override int First => (int)_lowerBound;

		internal override int Last => (int)_upperBound;

		public override ValueKind Kind => ValueKind.Integer;

		internal ConvexDomain(long l, long r)
		{
			_lowerBound = l;
			_upperBound = r;
		}

		internal static ConvexDomain Empty()
		{
			return _empty;
		}

		public override IEnumerable<object> Values()
		{
			for (long i = _lowerBound; i <= _upperBound; i++)
			{
				yield return i;
			}
		}

		internal override IEnumerable<int> Forward(int first, int last)
		{
			long beg = Math.Max(_lowerBound, first);
			long end = Math.Min(_upperBound, last);
			for (long i = beg; i <= end; i++)
			{
				yield return (int)i;
			}
		}

		internal override IEnumerable<int> Backward(int last, int first)
		{
			long beg = Math.Max(_lowerBound, first);
			long end = Math.Min(_upperBound, last);
			for (long i = end; i >= beg; i--)
			{
				yield return (int)i;
			}
		}

		internal override bool Contains(int val)
		{
			if (_lowerBound <= val)
			{
				return val <= _upperBound;
			}
			return false;
		}

		public override bool SetEqual(CspDomain otherDomain)
		{
			if (otherDomain is DisolverSymbolSet)
			{
				return false;
			}
			DisolverDiscreteDomain disolverDiscreteDomain = DisolverDiscreteDomain.SubCast(otherDomain);
			if (_lowerBound == disolverDiscreteDomain.First && _upperBound == disolverDiscreteDomain.Last)
			{
				return Count == disolverDiscreteDomain.Count;
			}
			return false;
		}
	}
}
