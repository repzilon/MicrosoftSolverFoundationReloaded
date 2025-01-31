using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>A Local Search Move Strategy in which we select the best
	///          move in a constraint-driven way
	/// </summary>
	internal class LS_ConstraintGuidedMoveStrategy : LS_Strategy
	{
		public LS_ConstraintGuidedMoveStrategy(int randomSeed)
			: base(randomSeed)
		{
		}

		protected override void Initialize(ILocalSearchProcess solver)
		{
		}

		/// <summary>Computes the next move considered by the local search solver
		/// </summary>
		public LocalSearch.Move NextMove(ILocalSearchProcess S)
		{
			CheckSolver(S);
			KeyValuePair<CspTerm, int> keyValuePair = base.Solver.PickViolatedVariable(_prng);
			CspVariable cspVariable = keyValuePair.Key as CspVariable;
			int value = keyValuePair.Value;
			int integerValue = base.Solver.GetIntegerValue(cspVariable);
			int value2 = value;
			int num = base.Solver.EvaluateFlip(cspVariable, value);
			if (num >= 0)
			{
				foreach (int item in LargeSample(cspVariable))
				{
					if (S.Model.CheckAbort())
					{
						return RandomFlip();
					}
					if (item != value && item != integerValue)
					{
						int num2 = base.Solver.EvaluateFlip(cspVariable, item);
						if (num2 < num)
						{
							value2 = item;
							num = num2;
						}
						if (num2 < 0)
						{
							break;
						}
					}
				}
			}
			if (num <= 0)
			{
				return LocalSearch.CreateVariableFlip(cspVariable, value2);
			}
			return RandomFlip();
		}
	}
}
