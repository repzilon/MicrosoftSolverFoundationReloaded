using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>A simple initialization strategy in which all or some
	///          of the variables are re-set to a value picked uniformly at
	///          random from their domain
	/// </summary>
	internal class LS_RandomInitializationStrategy : LS_Strategy
	{
		private readonly double _percentage;

		private int _nbRestarts;

		public LS_RandomInitializationStrategy(int randomSeed, double percentage)
			: base(randomSeed)
		{
			percentage = Math.Max(0.0, percentage);
			percentage = Math.Min(1.0, percentage);
			_percentage = percentage;
		}

		public Dictionary<CspTerm, int> NextConfiguration(ILocalSearchProcess solver)
		{
			CheckSolver(solver);
			bool flag = _percentage == 1.0;
			_nbRestarts++;
			if (flag)
			{
				return FullConfiguration();
			}
			return Perturbation();
		}

		private Dictionary<CspTerm, int> FullConfiguration()
		{
			List<CspVariable> variablesExcludingConstants = base.Model._variablesExcludingConstants;
			int count = variablesExcludingConstants.Count;
			Dictionary<CspTerm, int> dictionary = new Dictionary<CspTerm, int>(count);
			foreach (CspVariable item in variablesExcludingConstants)
			{
				int value = item.FiniteValue.Pick(_prng);
				dictionary.Add(item, value);
			}
			return dictionary;
		}

		private Dictionary<CspTerm, int> Perturbation()
		{
			List<CspVariable> variablesExcludingConstants = base.Model._variablesExcludingConstants;
			int count = variablesExcludingConstants.Count;
			int num = Math.Max(1, (int)(_percentage * (double)count));
			Dictionary<CspTerm, int> dictionary = new Dictionary<CspTerm, int>(count);
			for (int i = 0; i < num; i++)
			{
				CspVariable cspVariable = variablesExcludingConstants[_prng.Next(count)];
				if (!dictionary.ContainsKey(cspVariable))
				{
					dictionary.Add(cspVariable, cspVariable.FiniteValue.Pick(_prng));
				}
			}
			return dictionary;
		}
	}
}
