using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A normal (Gaussian) distribution.
	/// </summary>
	internal class NormalUnivariateDistribution : UnivariateDistribution<double>
	{
		private const double _twoPi = Math.PI * 2.0;

		private readonly double _mean;

		private readonly double _standardDeviation;

		/// <summary> The mean.
		/// </summary>
		public override double Mean => _mean;

		/// <summary> The variance (square of the standard deviation).
		/// </summary>
		public override double Variance => Math.Pow(_standardDeviation, 2.0);

		/// <summary> The lopsidedness of the distribution as defined by the third moment.
		/// </summary>
		public override double Skewness => 0.0;

		/// <summary> The measure of the 'skinnyness' of the distribution,
		/// defined by the fourth moment.
		/// </summary>
		public override double Kurtosis => 3.0;

		public double StandardDeviation => _standardDeviation;

		/// <summary> Create a new instance.
		/// </summary>
		/// <param name="mean">Location</param>
		/// <param name="standardDeviation">Scale</param>
		/// REVIEW shahark: Basically standardDeviation should be bigger than 0. We want to allow 0 so a stochastic
		/// parameter can be switched to a deterministic one easily
		public NormalUnivariateDistribution(double mean, double standardDeviation)
		{
			if (standardDeviation < 0.0 || double.IsInfinity(standardDeviation) || double.IsNaN(standardDeviation))
			{
				throw new ArgumentOutOfRangeException("standardDeviation", standardDeviation, Resources.TheMeanMustBeAFiniteNumberStandardDeviationMustBeANonNegativeNumber);
			}
			if (double.IsInfinity(mean) || double.IsNaN(mean))
			{
				throw new ArgumentOutOfRangeException("mean", mean, Resources.TheMeanMustBeAFiniteNumberStandardDeviationMustBeANonNegativeNumber);
			}
			_mean = mean;
			_standardDeviation = standardDeviation;
		}

		/// <summary>PDF (Probability density function)
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability density</returns>
		public override double Density(double x)
		{
			if (_standardDeviation == 0.0)
			{
				if (x != _mean)
				{
					return 0.0;
				}
				return double.PositiveInfinity;
			}
			return Math.Pow(Math.PI * 2.0, -0.5) / _standardDeviation * Math.Exp((0.0 - Math.Pow(x - _mean, 2.0)) / (2.0 * Math.Pow(_standardDeviation, 2.0)));
		}

		/// <summary>Compute the cumulative distribution function for the specified value.
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability</returns>
		public override double CumulativeDensity(double x)
		{
			DistributionUtilities.ValidateCumulativeDensityValue(x);
			if (_standardDeviation == 0.0)
			{
				if (x < _mean)
				{
					return 0.0;
				}
				if (x > _mean)
				{
					return 1.0;
				}
				return 0.5;
			}
			if (double.IsNegativeInfinity(x))
			{
				return 0.0;
			}
			if (double.IsPositiveInfinity(x))
			{
				return 1.0;
			}
			return 0.5 * (1.0 + DistributionUtilities.ErrorFunction((x - _mean) / (_standardDeviation * Math.Sqrt(2.0))));
		}

		/// <summary> The inverse cumulative distribution.
		/// </summary>
		public override double Quantile(double probability)
		{
			DistributionUtilities.ValidateProbability(probability);
			if (_standardDeviation == 0.0)
			{
				return _mean;
			}
			if (probability == 0.0)
			{
				return double.NegativeInfinity;
			}
			if (probability == 1.0)
			{
				return double.PositiveInfinity;
			}
			return _mean + _standardDeviation * DistributionUtilities.InverseCumulativeStandardNormal(probability);
		}
	}
}
