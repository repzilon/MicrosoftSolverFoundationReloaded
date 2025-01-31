using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A log normal distribution.
	/// </summary>
	internal class LogNormalUnivariateDistribution : UnivariateDistribution<double>
	{
		private const double _twoPi = Math.PI * 2.0;

		private const double _epsilon = 1E-10;

		private readonly double _meanLog;

		private readonly double _stdLog;

		private readonly double _stdLogSqr;

		private static readonly NormalUnivariateDistribution _standardNormalDistribution = new NormalUnivariateDistribution(0.0, 1.0);

		/// <summary> The mean.
		/// </summary>
		public override double Mean => Math.Exp(_meanLog + _stdLogSqr / 2.0);

		/// <summary> The variance (square of the standard deviation).
		/// </summary>
		public override double Variance => Math.Exp(_stdLogSqr + 2.0 * _meanLog) * (Math.Exp(_stdLogSqr) - 1.0);

		/// <summary> The lopsidedness of the distribution as defined by the third moment.
		/// </summary>
		public override double Skewness => Math.Sqrt(Math.Exp(_stdLogSqr) - 1.0) * (2.0 + Math.Exp(_stdLogSqr));

		/// <summary> The measure of the 'skinnyness' of the distribution,
		/// defined by the fourth moment.
		/// </summary>
		public override double Kurtosis => Math.Exp(4.0 * _stdLogSqr) + 2.0 * Math.Exp(3.0 * _stdLogSqr) + 3.0 * Math.Exp(2.0 * _stdLogSqr) - 6.0;

		/// <summary>Mean of the variable’s natural logarithm
		/// </summary>
		public double MeanLog => _meanLog;

		/// <summary>Standard deviation of the variable’s natural logarithm
		/// </summary>
		public double StdLog => _stdLog;

		/// <summary> Create a new instance.
		/// </summary>
		/// <param name="meanLog">Mean of the variable’s natural logarithm</param>
		/// <param name="standardDeviationLog">Standard deviation of the variable’s natural logarithm</param>
		/// REVIEW shahark: Basically standard deviation should be bigger than 0. We want to allow 0 so a stochastic
		/// parameter can be switched to a deterministic one easily
		public LogNormalUnivariateDistribution(double meanLog, double standardDeviationLog)
		{
			if (standardDeviationLog < 0.0 || double.IsInfinity(standardDeviationLog) || double.IsNaN(standardDeviationLog))
			{
				throw new ArgumentOutOfRangeException("standardDeviationLog", standardDeviationLog, Resources.TheMeanOfLogMustBeAFiniteNumberStandardDeviationOfLogMustBeANonNegativeNumber);
			}
			if (double.IsInfinity(meanLog) || double.IsNaN(meanLog))
			{
				throw new ArgumentOutOfRangeException("meanLog", meanLog, Resources.TheMeanOfLogMustBeAFiniteNumberStandardDeviationOfLogMustBeANonNegativeNumber);
			}
			_meanLog = meanLog;
			_stdLog = standardDeviationLog;
			_stdLogSqr = Math.Pow(_stdLog, 2.0);
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
			if (_stdLog == 0.0)
			{
				if (Math.Abs(x - Mean) <= 1E-10)
				{
					return double.PositiveInfinity;
				}
				return 0.0;
			}
			if (Math.Abs(x - 0.0) <= 1E-10)
			{
				return 0.0;
			}
			return Math.Exp((0.0 - Math.Pow(Math.Log(x) - _meanLog, 2.0)) / (2.0 * _stdLogSqr)) / (_stdLog * Math.Sqrt(Math.PI * 2.0) * x);
		}

		/// <summary>Compute the cumulative distribution function for the specified value.
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability</returns>
		public override double CumulativeDensity(double x)
		{
			DistributionUtilities.ValidateCumulativeDensityValue(x);
			if (_stdLog == 0.0)
			{
				if (Math.Abs(x - Mean) <= 1E-10)
				{
					return 0.5;
				}
				if (x < Mean)
				{
					return 0.0;
				}
				return 1.0;
			}
			if (x <= 0.0 || Math.Abs(x - 0.0) <= 1E-10)
			{
				return 0.0;
			}
			if (double.IsPositiveInfinity(x))
			{
				return 1.0;
			}
			return 0.5 * (1.0 - DistributionUtilities.ErrorFunction((_meanLog - Math.Log(x)) / (Math.Sqrt(2.0) * _stdLog)));
		}

		/// <summary> The inverse cumulative distribution.
		/// </summary>
		public override double Quantile(double probability)
		{
			DistributionUtilities.ValidateProbability(probability);
			if (Math.Abs(probability - 0.0) <= 1E-10)
			{
				return 0.0;
			}
			if (Math.Abs(probability - 1.0) <= 1E-10)
			{
				return double.PositiveInfinity;
			}
			return Math.Exp(_meanLog + _stdLog * _standardNormalDistribution.Quantile(probability));
		}
	}
}
