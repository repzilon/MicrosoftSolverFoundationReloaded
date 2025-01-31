using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Discrete sceanrios distribution.
	/// </summary>
	internal class ScenariosDistribution : UnivariateDistribution<double>
	{
		private class ScenarioValueComparer : IComparer<Scenario>
		{
			public int Compare(Scenario x, Scenario y)
			{
				double num = x.Value.ToDouble() - y.Value.ToDouble();
				if (!(Math.Abs(num) < DiscreteScenariosValue.RoundoffTolerance))
				{
					if (!(num > 0.0))
					{
						return -1;
					}
					return 1;
				}
				return 0;
			}
		}

		private readonly List<Scenario> _scenarios;

		private readonly List<double> _cumulativeProbabilities;

		/// <summary> The mean.
		/// </summary>
		public override double Mean
		{
			get
			{
				if (_scenarios.Count == 0)
				{
					return double.NaN;
				}
				double num = 0.0;
				for (int i = 0; i < _scenarios.Count; i++)
				{
					num += _scenarios[i].Probability.ToDouble() * _scenarios[i].Value.ToDouble();
				}
				return num;
			}
		}

		/// <summary> The variance (square of the standard deviation).
		/// </summary>
		public override double Variance
		{
			get
			{
				int count = _scenarios.Count;
				if (count <= 1)
				{
					return double.NaN;
				}
				double num = 0.0;
				double mean = Mean;
				for (int i = 0; i < count; i++)
				{
					double num2 = _scenarios[i].Value.ToDouble() - mean;
					num += num2 * num2 * _scenarios[i].Probability.ToDouble();
				}
				return num;
			}
		}

		/// <summary> The lopsidedness of the distribution as defined by the third moment.
		/// </summary>
		public override double Skewness
		{
			get
			{
				double variance = Variance;
				if (Math.Abs(variance) <= 1E-12)
				{
					return double.NaN;
				}
				double num = 0.0;
				double mean = Mean;
				for (int i = 0; i < _scenarios.Count; i++)
				{
					num += Math.Pow(_scenarios[i].Value.ToDouble() - mean, 3.0) * _scenarios[i].Probability.ToDouble();
				}
				return num / Math.Pow(variance, 1.5);
			}
		}

		/// <summary> The measure of the 'skinnyness' of the distribution,
		/// defined by the fourth moment.
		/// </summary>
		public override double Kurtosis
		{
			get
			{
				double variance = Variance;
				if (Math.Abs(variance) <= 1E-12)
				{
					return double.NaN;
				}
				double num = 0.0;
				double mean = Mean;
				for (int i = 0; i < _scenarios.Count; i++)
				{
					num += Math.Pow(_scenarios[i].Value.ToDouble() - mean, 4.0) * _scenarios[i].Probability.ToDouble();
				}
				return num / (variance * variance);
			}
		}

		public ScenariosDistribution(IEnumerable<Scenario> scenarios)
		{
			int capacity = scenarios.Count();
			_scenarios = new List<Scenario>(capacity);
			_scenarios.AddRange(scenarios.OrderBy((Scenario i) => i.Value));
			_cumulativeProbabilities = new List<double>(capacity);
			Rational zero = Rational.Zero;
			foreach (Scenario scenario in _scenarios)
			{
				zero += scenario.Probability;
				_cumulativeProbabilities.Add(zero.ToDouble());
			}
		}

		/// <summary>PDF (Probability density function)
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability density</returns>
		public override double Density(double x)
		{
			DistributionUtilities.ValidateCumulativeDensityValue(x);
			if (Scenario.IsValidScenarioValue(x))
			{
				int num = _scenarios.BinarySearch(new Scenario(0.5, x), new ScenarioValueComparer());
				if (num < 0)
				{
					return 0.0;
				}
				return _scenarios[num].Probability.ToDouble();
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
			if (Scenario.IsValidScenarioValue(x))
			{
				int num = _scenarios.BinarySearch(new Scenario(0.5, x), new ScenarioValueComparer());
				if (num >= 0)
				{
					return _cumulativeProbabilities[num];
				}
				num = ~num - 1;
				if (num < 0)
				{
					return 0.0;
				}
				return _cumulativeProbabilities[num];
			}
			if (double.IsPositiveInfinity(x))
			{
				return 1.0;
			}
			return 0.0;
		}

		/// <summary> The inverse cumulative distribution.
		/// </summary>
		public override double Quantile(double probability)
		{
			DistributionUtilities.ValidateProbability(probability);
			int num = _cumulativeProbabilities.BinarySearch(probability);
			if (num < 0)
			{
				num = ~num;
			}
			return (double)_scenarios[num].Value;
		}
	}
}
