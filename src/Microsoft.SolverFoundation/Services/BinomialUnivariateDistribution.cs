using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A Binomial distribution.
	/// <remarks>
	/// Binomial distribution is the discrete probability distribution of the 
	/// number of successes in a sequence of n Bernoulli trials 
	/// </remarks>  
	/// </summary>
	internal class BinomialUnivariateDistribution : UnivariateDistribution<int>
	{
		private const int _npThreshold = 10;

		private readonly double _successProbability;

		private readonly int _numberOfTrials;

		private readonly double _numberOfTrialsTimesSuccessProbability;

		private readonly double _successProbabilityComplimentary;

		private readonly double _p;

		private readonly double _np;

		private readonly double _q;

		private readonly bool _useInverseTransform;

		/// <summary> The mean.
		/// </summary>
		public override double Mean => _numberOfTrialsTimesSuccessProbability;

		/// <summary> The variance (square of the standard deviation).
		/// </summary>
		public override double Variance => _numberOfTrialsTimesSuccessProbability * _successProbabilityComplimentary;

		/// <summary> The lopsidedness of the distribution as defined by the third moment.
		/// </summary>
		public override double Skewness => (1.0 - 2.0 * _successProbability) / Math.Sqrt(_numberOfTrialsTimesSuccessProbability * _successProbabilityComplimentary);

		/// <summary> The measure of the 'skinnyness' of the distribution,
		/// defined by the fourth moment.
		/// </summary>
		public override double Kurtosis => 3.0 + (1.0 - 6.0 * _successProbability * _successProbabilityComplimentary) / (_numberOfTrialsTimesSuccessProbability * _successProbabilityComplimentary);

		/// <summary>
		/// -1 is the value for a dynamic (not a priori known) number 
		/// of random numbers needed
		/// If np is small we return 1 and use the inverse CDF
		/// </summary>
		public override int RandomNumberNeeded
		{
			get
			{
				if (_useInverseTransform)
				{
					return 1;
				}
				return -1;
			}
		}

		public double SuccessProbability => _successProbability;

		public int NumberOfTrials => _numberOfTrials;

		/// <summary> Create a new instance.
		/// </summary>
		/// <param name="numberOfTrials">Number of trials</param>
		/// <param name="successProbability">Probability for success</param>
		/// <remarks>If the case of : numberOfTrials * min(successProbability, 1 - successProbability) &gt;= 10 
		/// the distribution cannot be sampled with Latin hypercube method
		/// </remarks>
		public BinomialUnivariateDistribution(int numberOfTrials, double successProbability)
		{
			if (!DistributionUtilities.IsValidProbability(successProbability))
			{
				throw new ArgumentOutOfRangeException("successProbability", successProbability, Resources.TheSuccessProbabilityMustBeAProbabilityAndTheNumberOfTrialsMustBeAPositiveNumber);
			}
			DistributionUtilities.ValidateProbability(successProbability);
			if (numberOfTrials <= 0)
			{
				throw new ArgumentOutOfRangeException("numberOfTrials", numberOfTrials, Resources.TheSuccessProbabilityMustBeAProbabilityAndTheNumberOfTrialsMustBeAPositiveNumber);
			}
			_successProbability = successProbability;
			_numberOfTrials = numberOfTrials;
			_numberOfTrialsTimesSuccessProbability = _successProbability * (double)_numberOfTrials;
			_successProbabilityComplimentary = 1.0 - _successProbability;
			_p = Math.Min(_successProbability, _successProbabilityComplimentary);
			_np = (double)_numberOfTrials * _p;
			_q = 1.0 - _p;
			if (_np >= 10.0)
			{
				_useInverseTransform = false;
			}
			else
			{
				_useInverseTransform = true;
			}
		}

		/// <summary>PMF (Probability mass function)
		/// </summary>
		/// <param name="x">value (integer number)</param>
		/// <returns>Probability mass</returns>
		public override double Density(int x)
		{
			throw new NotSupportedException();
		}

		/// <summary>Compute the cumulative distribution function for the specified value.
		/// </summary>
		/// <param name="x">value (Real number)</param>
		/// <returns>Probability</returns>
		public override double CumulativeDensity(int x)
		{
			DistributionUtilities.ValidateCumulativeDensityValue(x);
			throw new NotSupportedException();
		}

		/// <summary> The inverse cumulative distribution.
		/// </summary>
		/// <remarks>This is linear in numberOfTrials, min(successProbability, 1 - successProbability)
		/// and probability. May take a long time for distribitions with large numberOfTrials
		/// </remarks>
		public override int Quantile(double probability)
		{
			DistributionUtilities.ValidateProbability(probability);
			probability = MapProbability(probability);
			if (Math.Abs(probability - 1.0) <= 1E-10)
			{
				return MapResult(_numberOfTrials);
			}
			double num = Math.Pow(_q, _numberOfTrials);
			double num2 = _p / _q;
			double num3 = (double)(_numberOfTrials + 1) * num2;
			int num4 = 0;
			double num5 = probability;
			while (!(num5 <= num))
			{
				num5 -= num;
				num4++;
				num *= num3 / (double)num4 - num2;
				if (num4 > _numberOfTrials)
				{
					throw new MsfException(Resources.SamplingGettingQuantileFromTheBinomialDistributionFailed);
				}
			}
			return MapResult(num4);
		}

		/// <summary>Sample the distribution, using a delegate for retrieving unifom numbers
		/// </summary>
		/// <param name="generateUniformNumber">Delegate for getting uniform numbers</param>
		/// <returns></returns>
		public override double SampleDouble(Func<double> generateUniformNumber)
		{
			int num = MapResult(AcceptanceRejection(generateUniformNumber));
			return num;
		}

		/// <summary>For large n
		/// Doesn't seem to be very good for np &gt; 30000
		/// Algorithm from W. Hormann: "The Generation of Binomial Random Variables"
		/// </summary>
		/// <remarks>This is algorithm BTRD (Binomial Transformed Rejection with Decomposition, have nothing
		/// to do with bender's decomposition)
		/// </remarks>
		/// <param name="generateUniformNumber"></param>
		/// <returns>sampled integer from the distribution</returns>
		private int AcceptanceRejection(Func<double> generateUniformNumber)
		{
			double num = _np * _q;
			double num2 = Math.Sqrt(num);
			double num3 = 1.15 + 2.53 * num2;
			double num4 = -0.0873 + 0.0248 * num3 + 0.01 * _p;
			double num5 = 2.0 * num4;
			double num6 = _np + 0.5;
			double num7 = 0.92 - 4.2 / num3;
			double num10;
			double num9;
			double num8;
			double num11 = (num10 = (num9 = (num8 = double.NaN)));
			int num12 = (int)Math.Floor((double)(_numberOfTrials + 1) * _p);
			double num13 = 0.86 * num7;
			for (int i = 0; i <= 1000; i++)
			{
				double num14 = generateUniformNumber();
				double num15;
				if (num14 <= num13)
				{
					num15 = num14 / num7 - 0.43;
					return (int)Math.Floor((num5 / (0.5 - Math.Abs(num15)) + num3) * num15 + num6);
				}
				if (num14 >= num7)
				{
					num15 = generateUniformNumber() - 0.5;
				}
				else
				{
					num15 = num14 / num7 - 0.93;
					num15 = (double)Math.Sign(num15) * 0.5 - num15;
					num14 = generateUniformNumber() * num7;
				}
				double num16 = 0.5 - Math.Abs(num15);
				double num17 = Math.Floor((num5 / num16 + num3) * num15 + num6);
				if (num17 < 0.0 || num17 > (double)_numberOfTrials)
				{
					continue;
				}
				int num18 = (int)num17;
				if (double.IsNaN(num11))
				{
					num11 = (2.83 + 5.1 / num3) * num2;
					num9 = _p / _q;
					num8 = (double)(_numberOfTrials + 1) * num9;
				}
				num14 *= num11 / (num4 / (num16 * num16) + num3);
				int num19 = Math.Abs(num18 - num12);
				if (num19 <= 15)
				{
					double num20 = 1.0;
					if (num12 < num18)
					{
						for (int j = num12 + 1; j <= num18; j++)
						{
							num20 *= num8 / (double)j - num9;
						}
					}
					else
					{
						for (int k = num18 + 1; k <= num12; k++)
						{
							num14 *= num8 / (double)k - num9;
						}
					}
					if (num14 <= num20)
					{
						return num18;
					}
					continue;
				}
				num14 = Math.Log(num14);
				double num21 = (double)num19 / num * ((((double)num19 / 3.0 + 0.625) * (double)num19 + 1.0 / 6.0) / num + 0.5);
				double num22 = -0.5 * (double)num19 * (double)num19 / num;
				if (num14 < num22 - num21)
				{
					return num18;
				}
				if (!(num14 > num22 + num21))
				{
					int num23 = _numberOfTrials - num18 + 1;
					int num24 = _numberOfTrials - num12 + 1;
					if (double.IsNaN(num10))
					{
						num10 = ((double)num12 + 0.5) * Math.Log((double)(num12 + 1) / (num9 * (double)num24)) + DistributionUtilities.StirlingCorrectionTerm(num12) + DistributionUtilities.StirlingCorrectionTerm(_numberOfTrials - num12);
					}
					double num25 = num10 + (double)(_numberOfTrials + 1) * Math.Log((double)num24 / (double)num23) + ((double)num18 + 0.5) * Math.Log((double)num23 * num9 / (double)(num18 + 1)) - DistributionUtilities.StirlingCorrectionTerm(num18) - DistributionUtilities.StirlingCorrectionTerm(_numberOfTrials - num18);
					if (num14 <= num25)
					{
						return num18;
					}
				}
			}
			throw new MsfException(Resources.SamplingGettingQuantileFromTheBinomialDistributionFailed);
		}

		/// <summary>When _successProbability is bigger than 1/2 we work with
		/// 1-_successProbability, so we need to take the complementary of probability
		/// for Qualtile calculation
		/// </summary>
		/// <param name="probability"></param>
		/// <returns></returns>
		private double MapProbability(double probability)
		{
			if (_successProbability != _p)
			{
				return 1.0 - probability;
			}
			return probability;
		}

		/// <summary>When _successProbability is bigger than 1/2 we work with
		/// 1-_successProbability, so we need to map back when having the result
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		private int MapResult(int x)
		{
			if (_successProbability != _p)
			{
				return _numberOfTrials - x;
			}
			return x;
		}
	}
}
