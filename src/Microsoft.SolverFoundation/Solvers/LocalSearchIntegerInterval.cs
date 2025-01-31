using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Domain of integer values of the form left .. right
	/// </summary>
	internal sealed class LocalSearchIntegerInterval : LocalSearchDomain
	{
		/// <summary>
		/// Under this limit the domains are considered small and 
		/// we essentially ignore any distance
		/// </summary>
		internal const int SmallDomainLimit = 10;

		internal static readonly long MaxSafeValue = 4611686018427387903L;

		internal static readonly long MinSafeValue = -MaxSafeValue;

		private long _lower;

		private long _upper;

		public override long Cardinality
		{
			get
			{
				if ((_lower <= MinSafeValue && _upper >= 0) || (_lower <= 0 && _upper >= MaxSafeValue))
				{
					return long.MaxValue;
				}
				return _upper - _lower + 1;
			}
		}

		public override bool IsDiscrete => true;

		public override double Lower => _lower;

		public override double Upper => _upper;

		public LocalSearchIntegerInterval(long lower, long upper)
		{
			_lower = lower;
			_upper = upper;
		}

		public override double Sample(Random prng)
		{
			return NextInteger(prng, _lower, _upper);
		}

		public override double PickNeighbour(Random prng, double currentValue, double distance)
		{
			long l;
			long r;
			if (distance >= base.Width || (MinSafeValue <= _lower && _upper <= MaxSafeValue && _upper - _lower < 10))
			{
				l = _lower;
				r = _upper;
			}
			else
			{
				if (!Contains(currentValue))
				{
					currentValue = ((currentValue > (double)_upper) ? _upper : _lower);
				}
				l = Math.Max(_lower, LocalSearchDomain.Floor(currentValue - distance));
				r = Math.Min(_upper, LocalSearchDomain.Ceiling(currentValue + distance));
			}
			int num = 0;
			long num2;
			while (true)
			{
				num2 = NextInteger(prng, l, r);
				if ((double)num2 != currentValue || num >= 10)
				{
					break;
				}
				num++;
			}
			return num2;
		}

		/// <summary>
		/// both bounds are INCLUSIVE
		/// </summary>
		internal static long NextInteger(Random prng, long l, long r)
		{
			if (l == r)
			{
				return l;
			}
			if (r != long.MaxValue)
			{
				r++;
			}
			return prng.NextLong(l, r);
		}

		public override bool Contains(double val)
		{
			if (EvaluationStatics.IsInteger64(val) && (double)_lower <= val)
			{
				return val <= (double)_upper;
			}
			return false;
		}

		protected override IEnumerable<double> Enumerate()
		{
			for (double i = _lower; i <= (double)_upper; i += 1.0)
			{
				yield return i;
			}
		}
	}
}
