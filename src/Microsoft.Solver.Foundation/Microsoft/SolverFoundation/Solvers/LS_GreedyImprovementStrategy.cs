using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>A Local Search Move Strategy in which we scan all variables 
	///          greedily in a round-robin fashion and try a small number of 
	///          values for each considered variable. 
	/// </summary>
	internal class LS_GreedyImprovementStrategy : LS_Strategy
	{
		private int _current;

		public LS_GreedyImprovementStrategy(int randomSeed)
			: base(randomSeed)
		{
			_current = -1;
		}

		/// <summary>Computes the next move considered by the local search solver
		/// </summary>
		public LocalSearch.Move NextMove(ILocalSearchProcess S)
		{
			CheckSolver(S);
			List<CspVariable> variablesExcludingConstants = base.Model._variablesExcludingConstants;
			uint count = (uint)variablesExcludingConstants.Count;
			for (uint num = Math.Max(2u, count >> 4); num != 0; num--)
			{
				if (++_current >= count)
				{
					_current = 0;
				}
				CspVariable cspVariable = variablesExcludingConstants[_current];
				int integerValue = base.Solver.GetIntegerValue(cspVariable);
				int value = integerValue;
				int num2 = 0;
				foreach (int item in LargeSample(cspVariable))
				{
					if (S.Model.CheckAbort())
					{
						return RandomFlip();
					}
					if (item != integerValue)
					{
						int num3 = base.Solver.EvaluateFlip(cspVariable, item);
						if (num3 < num2)
						{
							num2 = num3;
							value = item;
						}
					}
				}
				if (num2 < 0)
				{
					return LocalSearch.CreateVariableFlip(cspVariable, value);
				}
			}
			return RandomFlip();
		}
	}
}
