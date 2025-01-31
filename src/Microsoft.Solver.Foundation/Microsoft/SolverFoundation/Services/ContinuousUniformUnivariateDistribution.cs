using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	internal class ContinuousUniformUnivariateDistribution : UniformUnivariateDistribution<double>
	{
		/// <summary> The variance (square of the standard deviation).
		/// </summary>
		public override double Variance => Math.Pow(_difference, 2.0) / 12.0;

		/// <summary> The measure of the 'skinnyness' of the distribution,
		/// defined by the fourth moment.
		/// </summary>
		public override double Kurtosis => 1.8;

		/// <summary> Create a new instance.
		/// </summary>
		/// <param name="lowerBound">Lower bound (inclusive)</param>
		/// <param name="upperBound">Upper bound (inclusive)</param>
		public ContinuousUniformUnivariateDistribution(double lowerBound, double upperBound)
			: base(lowerBound, upperBound)
		{
			if (double.IsNaN(lowerBound) || double.IsInfinity(lowerBound))
			{
				throw new ArgumentOutOfRangeException("lowerBound", lowerBound, Resources.TheLowerBoundAndTheUpperBoundMustBeFiniteNumbers);
			}
			if (double.IsNaN(upperBound) || double.IsInfinity(upperBound))
			{
				throw new ArgumentOutOfRangeException("upperBound", upperBound, Resources.TheLowerBoundAndTheUpperBoundMustBeFiniteNumbers);
			}
			if (lowerBound > upperBound)
			{
				throw new ArgumentOutOfRangeException(Resources.LowerBoundCannotBeLargerThanUpperBound);
			}
		}

		/// <summary>PDF (Probability density function)
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability density</returns>
		public override double Density(double x)
		{
			if (_lowerBound <= x && x <= _upperBound)
			{
				return 1.0 / _difference;
			}
			return 0.0;
		}

		/// <summary>Compute the cumulative distribution function for the specified value.
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability</returns>
		public override double CumulativeDensity(double x)
		{
			DistributionUtilities.ValidateCumulativeDensityValue(x);
			if (x < _lowerBound)
			{
				return 0.0;
			}
			if (_upperBound <= x)
			{
				return 1.0;
			}
			return (x - _lowerBound) / _difference;
		}

		/// <summary> The inverse cumulative distribution.
		/// </summary>
		public override double Quantile(double probability)
		{
			DistributionUtilities.ValidateProbability(probability);
			if (probability == 1.0)
			{
				return _upperBound;
			}
			return probability * _difference + _lowerBound;
		}
	}
}
