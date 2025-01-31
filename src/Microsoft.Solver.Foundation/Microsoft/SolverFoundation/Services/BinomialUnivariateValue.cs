using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	internal sealed class BinomialUnivariateValue : DistributedValue
	{
		private readonly BinomialUnivariateDistribution _binomialUnivariateDistribution;

		/// <summary> The number of possible realizations.
		/// </summary>
		public override int ScenariosCount => _binomialUnivariateDistribution.NumberOfTrials + 1;

		/// <summary> All possible scenarios.
		/// </summary>
		/// <remarks>The scenarios are in the order of 0, numberOfTrials, 1, numberOfTrials - 1, and so on</remarks>
		public override IEnumerable<Scenario> Scenarios
		{
			get
			{
				int numberOfTrials = _binomialUnivariateDistribution.NumberOfTrials;
				double successProbability = _binomialUnivariateDistribution.SuccessProbability;
				double failProbability = 1.0 - successProbability;
				double probabilityFromStart = Math.Pow(failProbability, numberOfTrials);
				double probabilityFromEnd = Math.Pow(successProbability, numberOfTrials);
				if (probabilityFromStart < double.Epsilon || probabilityFromEnd < double.Epsilon)
				{
					throw new MsfException(string.Format(CultureInfo.CurrentCulture, Resources.CannotEnumerateScenariosForBinomialValueProbabilityOfAScenarioWithSuccessfulTrialsIsInsufficientlyLarge, new object[1] { (!(probabilityFromStart < double.Epsilon)) ? numberOfTrials : 0 }));
				}
				yield return new Scenario(probabilityFromStart, 0);
				yield return new Scenario(probabilityFromEnd, numberOfTrials);
				int maxNumberOfsuccess = (int)Math.Ceiling((double)numberOfTrials / 2.0);
				for (int numberOfsuccesses = 1; numberOfsuccesses < maxNumberOfsuccess; numberOfsuccesses++)
				{
					probabilityFromStart *= successProbability * (double)(numberOfTrials - numberOfsuccesses + 1);
					probabilityFromStart /= failProbability * (double)numberOfsuccesses;
					probabilityFromEnd *= failProbability * (double)(numberOfTrials - numberOfsuccesses + 1);
					probabilityFromEnd /= successProbability * (double)numberOfsuccesses;
					yield return new Scenario(probabilityFromStart, numberOfsuccesses);
					yield return new Scenario(probabilityFromEnd, numberOfTrials);
				}
				if (numberOfTrials % 2 == 0)
				{
					probabilityFromStart *= successProbability * (double)(maxNumberOfsuccess + 1);
					probabilityFromStart /= failProbability * (double)maxNumberOfsuccess;
					yield return new Scenario(probabilityFromStart, maxNumberOfsuccess);
				}
			}
		}

		public BinomialUnivariateValue(int numberOfTrials, double successProbability)
		{
			_binomialUnivariateDistribution = new BinomialUnivariateDistribution(numberOfTrials, successProbability);
			Distribution = _binomialUnivariateDistribution;
		}

		/// <summary> Returns a string representation of the distribution.
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.CurrentCulture, "Number of trials = {0}, Success probability = {1}", new object[2] { _binomialUnivariateDistribution.NumberOfTrials, _binomialUnivariateDistribution.SuccessProbability });
		}
	}
}
