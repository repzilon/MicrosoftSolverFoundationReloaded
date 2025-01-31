using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	internal class DiscreteUniformUnivariateDistribution : UniformUnivariateDistribution<int>
	{
		private readonly long _numberOfScenarios;

		private readonly double _quantileTolerance = 1E-07;

		/// <summary> The variance (square of the standard deviation).
		/// </summary>
		public override double Variance => (Math.Pow(_numberOfScenarios, 2.0) - 1.0) / 12.0;

		/// <summary> The measure of the 'skinnyness' of the distribution,
		/// defined by the fourth moment.
		/// </summary>
		public override double Kurtosis
		{
			get
			{
				double num = Math.Pow(_numberOfScenarios, 2.0);
				if (_numberOfScenarios == 1)
				{
					return double.NegativeInfinity;
				}
				return -1.2 * (num + 1.0) / (num - 1.0) + 3.0;
			}
		}

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="lowerBound">Lower bound (inclusive).</param>
		/// <param name="upperBound">Upper bound (inclusive).</param>
		public DiscreteUniformUnivariateDistribution(int lowerBound, int upperBound)
			: base(lowerBound, upperBound)
		{
			if (lowerBound > upperBound)
			{
				throw new ArgumentOutOfRangeException(Resources.LowerBoundCannotBeLargerThanUpperBound);
			}
			_numberOfScenarios = (long)_difference + 1;
		}

		/// <summary>PMF (Probability mass function)
		/// </summary>
		/// <param name="x">value (integer number)</param>
		/// <returns>Probability mass</returns>
		public override double Density(int x)
		{
			if (_lowerBound <= x && x <= _upperBound)
			{
				return 1.0 / (double)_numberOfScenarios;
			}
			return 0.0;
		}

		/// <summary>Compute the cumulative distribution function for the specified value.
		/// </summary>
		/// <param name="x">value (integer number)</param>
		/// <returns>Probability</returns>
		public override double CumulativeDensity(int x)
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
			return (Math.Floor((double)x - (double)_lowerBound) + 1.0) / (_difference + 1.0);
		}

		/// <summary>
		/// The inverse cumulative distribution.
		/// </summary>
		/// <param name="probability"></param>
		/// <param name="epsilon">Threshold</param>
		/// <returns></returns>
		private int Quantile(double probability, double epsilon)
		{
			DistributionUtilities.ValidateProbability(probability);
			double num = probability * (double)_numberOfScenarios;
			double num2 = Math.Floor(num);
			int num3 = ((!(num - num2 < epsilon)) ? ((int)num2) : Math.Max((int)num2 - 1, 0));
			num3 += _lowerBound;
			return Math.Min(num3, _upperBound);
		}

		/// <summary> The inverse cumulative distribution.
		/// </summary>
		public override int Quantile(double probability)
		{
			return Quantile(probability, _quantileTolerance);
		}

		/// <summary> A pseudorandom sample drawn, with replacement, from the population.
		/// </summary>
		/// <remarks>Implementation uses Quantile (inverse CDF for sampling) with the first
		/// random number. This is the inverse transform technique. It will call it with zero tolerance
		/// so randomNumbers[0] which is even slightly bigger than the bounds between two number yields the bigger
		/// one
		/// </remarks>
		public override int Sample(params double[] randomNumbers)
		{
			CheckSampleArgs(randomNumbers);
			return Quantile(randomNumbers[0], 0.0);
		}
	}
}
