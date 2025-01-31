using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A geometric distribution.
	/// <remarks>
	/// The probability distribution is of the number Y = X - 1 of Bernoulli trials 
	/// failures before the first success, supported on the set { 0, 1, 2, 3, ... }.
	/// Note that there is different convention which prefer to define the distribution 
	/// instead supported on the set {1, 2, 3, ... } as the first successed trial
	/// </remarks>  
	/// </summary>
	internal class GeometricUnivariateDistribution : UnivariateDistribution<int>
	{
		private const double _aroundOneTolerance = 1E-10;

		private readonly double _successProbability;

		private readonly double _q;

		/// <summary> The mean.
		/// </summary>
		public override double Mean
		{
			get
			{
				if (_successProbability == 1.0)
				{
					return 0.0;
				}
				return (1.0 - _successProbability) / _successProbability;
			}
		}

		/// <summary> The variance (square of the standard deviation).
		/// </summary>
		public override double Variance
		{
			get
			{
				if (_successProbability == 1.0)
				{
					return 0.0;
				}
				return (1.0 - _successProbability) / Math.Pow(_successProbability, 2.0);
			}
		}

		/// <summary> The lopsidedness of the distribution as defined by the third moment.
		/// </summary>
		public override double Skewness
		{
			get
			{
				if (_successProbability == 1.0)
				{
					return double.PositiveInfinity;
				}
				return (2.0 - _successProbability) / Math.Sqrt(1.0 - _successProbability);
			}
		}

		/// <summary> The measure of the 'skinnyness' of the distribution,
		/// defined by the fourth moment.
		/// </summary>
		public override double Kurtosis
		{
			get
			{
				if (_successProbability == 1.0)
				{
					return double.PositiveInfinity;
				}
				return 3.0 + (6.0 + Math.Pow(_successProbability, 2.0) / (1.0 - _successProbability));
			}
		}

		public double SuccessProbability => _successProbability;

		/// <summary> Create a new instance.
		/// </summary>
		/// <param name="successProbability">Probability for success</param>
		public GeometricUnivariateDistribution(double successProbability)
		{
			if (!DistributionUtilities.IsNonzeroProbability(successProbability))
			{
				throw new ArgumentOutOfRangeException("successProbability", successProbability, Resources.TheSuccessProbabilityMustBeANonZeroProbability);
			}
			if (DistributionUtilities.EqualsOne(successProbability, 1E-10))
			{
				_successProbability = 1.0;
			}
			else
			{
				_successProbability = successProbability;
			}
			_q = 1.0 - _successProbability;
		}

		/// <summary>PMF (Probability mass function)
		/// </summary>
		/// <param name="x">value (integer number)</param>
		/// <returns>Probability mass</returns>
		public override double Density(int x)
		{
			if (x < 0)
			{
				return 0.0;
			}
			return Math.Pow(_q, x) * _successProbability;
		}

		/// <summary>Compute the cumulative distribution function for the specified value.
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability</returns>
		public override double CumulativeDensity(int x)
		{
			DistributionUtilities.ValidateCumulativeDensityValue(x);
			if (x < 0)
			{
				return 0.0;
			}
			return 1.0 - Math.Pow(_q, (long)x + 1L);
		}

		public override int Sample(params double[] randomNumbers)
		{
			CheckSampleArgs(randomNumbers);
			double result = QuantileCore(randomNumbers[0]);
			return QuantileAsInt32(result, Resources.ErrorSamplingResultNeedsToBeInteger);
		}

		/// <summary> The inverse cumulative distribution.
		/// </summary>
		public override int Quantile(double probability)
		{
			double result = QuantileCore(probability);
			return QuantileAsInt32(result, Resources.ResultNeedsToBeInteger);
		}

		/// <summary>Returns Quantile without casting to integer 
		/// called from Quantile and Sample methods
		/// </summary>
		private double QuantileCore(double probability)
		{
			DistributionUtilities.ValidateProbability(probability);
			if (DistributionUtilities.EqualsOne(probability, 1E-10))
			{
				return double.PositiveInfinity;
			}
			if (_successProbability == 1.0)
			{
				return 0.0;
			}
			return Math.Floor(Math.Log(1.0 - probability) / Math.Log(_q));
		}

		/// <summary>Checks if double result of quantile can be case to 32 bit integer. 
		/// If not throw an exception. 
		/// </summary>
		/// <param name="result"></param>
		/// <param name="errorMessage">Different error massage for sampling and quantile</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException"></exception>
		/// <returns>result converted to 32 bit integer</returns>
		private static int QuantileAsInt32(double result, string errorMessage)
		{
			if (result > 2147483647.0)
			{
				throw new MsfException(errorMessage);
			}
			return (int)result;
		}
	}
}
