using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal class ScenarioGenerator
	{
		private const int DefaultSeed = 123456;

		private const SamplingMethod _defaultSamplingMethod = SamplingMethod.MonteCarlo;

		private readonly SolverContext _context;

		private readonly DistributedValue[] _distributedValues;

		private readonly int _maxScenariosForExactSolution;

		private SamplingEngine _sampleEngine;

		private readonly SamplingMethod _samplingMethod;

		/// <summary>The number of samples taken.
		/// </summary>
		internal int SampleCount { get; private set; }

		/// <summary>Random seed
		/// </summary>
		internal int RandomSeed { get; private set; }

		/// <summary>Indicates if sampling will be used for scenario generation.
		/// </summary>
		internal bool SamplingNeeded => ScenarioCount > _maxScenariosForExactSolution;

		/// <summary>The sampling method.
		/// </summary>
		internal SamplingMethod SamplingMethod => _samplingMethod;

		/// <summary>The total number of independent scenarios in the model.
		/// </summary>
		internal int ScenarioCount
		{
			get
			{
				int num = 1;
				DistributedValue[] distributedValues = _distributedValues;
				foreach (DistributedValue distributedValue in distributedValues)
				{
					if (distributedValue.ScenariosCount != int.MaxValue && int.MaxValue / distributedValue.ScenariosCount - 1 >= num)
					{
						num *= distributedValue.ScenariosCount;
						continue;
					}
					return int.MaxValue;
				}
				return num;
			}
		}

		internal ScenarioGenerator(SolverContext context, SamplingParameters samplingParameters, IEnumerable<DistributedValue> distributedValues, StochasticDirective directive)
		{
			if (distributedValues == null)
			{
				throw new ArgumentNullException("distributedValues");
			}
			if (directive == null)
			{
				throw new ArgumentNullException("directive");
			}
			_distributedValues = distributedValues.ToArray();
			DistributedValue[] distributedValues2 = _distributedValues;
			foreach (DistributedValue distributedValue in distributedValues2)
			{
				if (distributedValue == null)
				{
					throw new ArgumentNullException("distributedValues");
				}
			}
			SampleCount = samplingParameters.SampleCount;
			_maxScenariosForExactSolution = directive.MaximumScenarioCountBeforeSampling;
			if (SamplingNeeded)
			{
				if (samplingParameters.SamplingMethod == SamplingMethod.Automatic)
				{
					_samplingMethod = SamplingMethod.MonteCarlo;
				}
				else
				{
					_samplingMethod = samplingParameters.SamplingMethod;
				}
			}
			else
			{
				_samplingMethod = SamplingMethod.NoSampling;
			}
			if (samplingParameters.RandomSeed != 0)
			{
				RandomSeed = samplingParameters.RandomSeed;
			}
			else
			{
				RandomSeed = 123456;
			}
			_context = context;
		}

		/// <summary>
		/// Creates the scenarios. For each scenario sets value for the IDistributedValues and returns
		/// the probability of the scenario. 
		/// </summary>
		/// <remarks>
		/// Each call will start with the same seed and same numbers.
		/// </remarks>
		/// <returns>enumaration of probability of each scenario</returns>
		internal IEnumerable<Rational> GetAllScenarios()
		{
			return GetAllScenarios(startOver: true);
		}

		/// <summary>
		/// Creates the scenarios. For each scenario sets value for the IDistributedValues and returns
		/// the probability of the scenario. 
		/// </summary>
		/// <param name="startOver">Whether to start over (so that same scenarios come back each time).</param>
		/// <returns>enumaration of probability of each scenario</returns>
		internal IEnumerable<Rational> GetAllScenarios(bool startOver)
		{
			if (SamplingNeeded)
			{
				if (startOver || _sampleEngine == null)
				{
					_sampleEngine = new SamplingEngine(_context, RandomSeed);
				}
				return _sampleEngine.GetAllScenarios(_distributedValues, SampleCount, _samplingMethod);
			}
			return GetAllScenarios(0, 1);
		}

		/// <summary>
		/// Recursive method for getting all scenarion for the finite case
		/// When yeilding a result each IDistributedValue has its CurrentValue set to a fix number 
		/// that belongs to this scenario
		/// Remark: Rational values are converted to double here
		/// </summary>
		/// <param name="distributedValuePlace">which Distributed Value is being changed now</param>
		/// <param name="comulativeProbability">comulative probability for this scenario considering all predecessors DistributionValue</param>
		/// <returns></returns>
		private IEnumerable<Rational> GetAllScenarios(int distributedValuePlace, Rational comulativeProbability)
		{
			if (distributedValuePlace == _distributedValues.Length - 1)
			{
				foreach (Scenario scenario in _distributedValues[distributedValuePlace].Scenarios)
				{
					_distributedValues[distributedValuePlace].CurrentSample = (double)scenario.Value;
					yield return comulativeProbability * scenario.Probability;
				}
				yield break;
			}
			foreach (Scenario scenario2 in _distributedValues[distributedValuePlace].Scenarios)
			{
				_distributedValues[distributedValuePlace].CurrentSample = (double)scenario2.Value;
				IEnumerable<Rational> recEnum = GetAllScenarios(distributedValuePlace + 1, comulativeProbability * scenario2.Probability);
				foreach (Rational item in recEnum)
				{
					yield return item;
				}
			}
		}
	}
}
