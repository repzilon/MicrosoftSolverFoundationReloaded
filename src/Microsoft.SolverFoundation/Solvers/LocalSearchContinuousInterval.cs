using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Continuous interval of values
	/// </summary>
	/// <remarks>
	/// Right now the only truly continuous domain that we handle is interval;
	/// unions of intervals would in general probably make sense
	/// </remarks>
	internal sealed class LocalSearchContinuousInterval : LocalSearchDomain
	{
		private double _lower;

		private double _upper;

		public override long Cardinality => long.MaxValue;

		public override bool IsDiscrete => false;

		public override double Lower => _lower;

		public override double Upper => _upper;

		public LocalSearchContinuousInterval(double lower, double upper)
		{
			_lower = lower;
			_upper = upper;
		}

		public override double Sample(Random prng)
		{
			return prng.NextDouble(_lower, _upper);
		}

		public override double PickNeighbour(Random prng, double currentValue, double distance)
		{
			if (distance >= base.Width)
			{
				return Sample(prng);
			}
			if (!Contains(currentValue))
			{
				currentValue = ((currentValue > _upper) ? _upper : _lower);
			}
			double left = Math.Max(_lower, currentValue - distance);
			double right = Math.Min(_upper, currentValue + distance);
			return prng.NextDouble(left, right);
		}

		public override bool Contains(double val)
		{
			if (_lower <= val)
			{
				return val <= _upper;
			}
			return false;
		}
	}
}
