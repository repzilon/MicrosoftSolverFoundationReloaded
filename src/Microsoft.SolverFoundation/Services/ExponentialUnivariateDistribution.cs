using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>An Exponential distribution.
	/// </summary>
	internal class ExponentialUnivariateDistribution : UnivariateDistribution<double>
	{
		private readonly double _rate;

		/// <summary> The mean.
		/// </summary>
		public override double Mean => 1.0 / _rate;

		/// <summary> The variance (square of the standard deviation).
		/// </summary>
		public override double Variance => 1.0 / Math.Pow(_rate, 2.0);

		/// <summary> The lopsidedness of the distribution as defined by the third moment.
		/// </summary>
		public override double Skewness => 2.0;

		/// <summary> The measure of the 'skinnyness' of the distribution,
		/// defined by the fourth moment.
		/// </summary>
		public override double Kurtosis => 9.0;

		public double Rate => _rate;

		/// <summary> Create a new instance.
		/// </summary>
		/// <param name="rate">Rate</param>
		public ExponentialUnivariateDistribution(double rate)
		{
			if (rate <= 0.0 || double.IsInfinity(rate) || double.IsNaN(rate))
			{
				throw new ArgumentOutOfRangeException("rate", rate, Resources.TheRateMustBeAFinitePositiveNumber);
			}
			_rate = rate;
		}

		/// <summary>PDF (Probability density function)
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability density</returns>
		public override double Density(double x)
		{
			if (x < 0.0)
			{
				return 0.0;
			}
			return _rate * Math.Exp((0.0 - _rate) * x);
		}

		/// <summary>Compute the cumulative distribution function for the specified value.
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability</returns>
		public override double CumulativeDensity(double x)
		{
			DistributionUtilities.ValidateCumulativeDensityValue(x);
			if (x < 0.0)
			{
				return 0.0;
			}
			return 1.0 - Math.Exp((0.0 - _rate) * x);
		}

		/// <summary> The inverse cumulative distribution.
		/// </summary>
		public override double Quantile(double probability)
		{
			DistributionUtilities.ValidateProbability(probability);
			return (0.0 - Math.Log(1.0 - probability)) / _rate;
		}
	}
}
