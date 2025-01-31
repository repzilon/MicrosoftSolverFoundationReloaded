#define TRACE
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// </summary>
	internal class SamplingEngine
	{
		private readonly SolverContext _context;

		private readonly PseudoRandom _psueudoRandomGenerator;

		/// <summary>
		/// Each stratum has 1/number of samples
		/// </summary>
		private Interval<double>[] _strata;

		/// <summary>
		/// This is mapping from distributed Value, sample number --&gt; stratum to sample with
		/// </summary>
		private int[][] _shuffles;

		/// <summary>
		/// For not using GetContext() each time
		/// </summary>
		private SolverContext Context => _context;

		internal SamplingEngine(SolverContext context, int seed)
		{
			_psueudoRandomGenerator = PseudoRandom.Create(seed);
			_context = context;
		}

		/// <summary>
		/// Sample from the distributions, gets back sampleCount samples of the  Creates the scenarios, for each scenario sets value for the IDistributedValues and returns
		/// the probability of the scenario. Hydrator should build the second stage constraints/part of goal 
		/// when iterating over the scenarios
		/// </summary>
		/// <returns>enumaration of probability of each scenario</returns>
		internal IEnumerable<Rational> GetAllScenarios(DistributedValue[] distributedValues, int sampleCount, SamplingMethod samplingMethod)
		{
			if (sampleCount <= 0)
			{
				throw new ArgumentOutOfRangeException("sampleCount");
			}
			Rational probability = Rational.One / sampleCount;
			Context.TraceSource.TraceInformation("Samples: {0}, distributions: {1}, method: {2}", sampleCount, distributedValues.Length, samplingMethod.ToString());
			Init(sampleCount, distributedValues, samplingMethod);
			for (int i = 0; i < sampleCount; i++)
			{
				int numberOfDistributedValue = 0;
				foreach (DistributedValue distributedValue in distributedValues)
				{
					int randomNumberNeeded = distributedValue.Distribution.RandomNumberNeeded;
					if (!distributedValue.Distribution.SupportsLatinHypercube && samplingMethod == SamplingMethod.LatinHypercube)
					{
						throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resources.Distribution0WithItsCurrentParametersCannotBeSampledWithLatinHypercubeMethod, new object[1] { distributedValue.Distribution.GetType().Name }));
					}
					if (randomNumberNeeded < 0)
					{
						Func<double> randomNumberDelegate = GetRandomNumberDelegate(numberOfDistributedValue, i, samplingMethod);
						distributedValue.CurrentSample = distributedValue.Distribution.SampleDouble(randomNumberDelegate);
					}
					else
					{
						double[] array = new double[randomNumberNeeded];
						for (int k = 0; k < randomNumberNeeded; k++)
						{
							array[k] = GetRandomNumber(numberOfDistributedValue, i + k, samplingMethod);
						}
						distributedValue.CurrentSample = distributedValue.Distribution.SampleDouble(array);
					}
					numberOfDistributedValue++;
				}
				yield return probability;
			}
		}

		private double GetRandomNumber(int distributedValue, int sampleNumber, SamplingMethod samplingMethod)
		{
			switch (samplingMethod)
			{
			case SamplingMethod.MonteCarlo:
				return _psueudoRandomGenerator.NextDouble();
			case SamplingMethod.LatinHypercube:
				return _psueudoRandomGenerator.NextDouble(_strata[_shuffles[distributedValue][sampleNumber]]);
			default:
				throw new NotSupportedException();
			}
		}

		private Func<double> GetRandomNumberDelegate(int distributedValue, int sampleNumber, SamplingMethod samplingMethod)
		{
			return () => GetRandomNumber(distributedValue, sampleNumber, samplingMethod);
		}

		/// <summary> Prepare to generate scenarios.
		/// </summary>
		private void Init(int sampleCount, DistributedValue[] distributedValues, SamplingMethod samplingMethod)
		{
			if (samplingMethod == SamplingMethod.LatinHypercube)
			{
				_strata = new Interval<double>[sampleCount];
				double lowerBound = 0.0;
				double num = sampleCount;
				for (int i = 0; i < sampleCount; i++)
				{
					_strata[i] = new Interval<double>(lowerBound, IntervalBoundKind.Closed, (double)(i + 1) / num, IntervalBoundKind.Open);
					lowerBound = _strata[i].UpperBound;
				}
				FillShuffles(sampleCount, distributedValues);
			}
		}

		/// <summary>
		/// Fills the shuffle mapping, using modification of Durstenfeld for Fisher and Yates' algorithm
		/// Durstenfeld, Richard (July 1964). "Algorithm 235: Random permutation"
		/// for example, when sampleCount is 5 and ranomValuesCount is 3
		/// 0 1 2 3 4  
		/// 1 3 2 4 0
		/// 2 1 4 0 3
		/// </summary>
		private void FillShuffles(int sampleCount, DistributedValue[] distributedValues)
		{
			_shuffles = new int[distributedValues.Length][];
			for (int i = 0; i < distributedValues.Length; i++)
			{
				_shuffles[i] = new int[sampleCount];
				for (int j = 0; j < sampleCount; j++)
				{
					_shuffles[i][j] = j;
				}
			}
			for (int num = sampleCount - 1; num > 0; num--)
			{
				DiscreteUniformUnivariateDistribution discreteUniformUnivariateDistribution = new DiscreteUniformUnivariateDistribution(0, num);
				object.Equals(discreteUniformUnivariateDistribution.RandomNumberNeeded, 1);
				for (int k = 1; k < distributedValues.Length; k++)
				{
					int num2 = discreteUniformUnivariateDistribution.Sample(_psueudoRandomGenerator.NextDouble());
					if (num2 != num)
					{
						Statics.Swap(ref _shuffles[k][num2], ref _shuffles[k][num]);
					}
				}
			}
		}
	}
}
