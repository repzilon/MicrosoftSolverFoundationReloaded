using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A base class for all logic operators
	/// </summary>
	internal abstract class LogicFunction : BooleanFunction
	{
		internal LogicFunction(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
		{
			ExcludeNonBooleans(inputs);
			_scales = null;
		}

		/// <summary> Counts the number of inputs of the term that are 
		///           currently satisfied 
		/// </summary>
		protected int NumberInputsSatisfied(LocalSearchSolver ls)
		{
			int num = 0;
			CspSolverTerm[] inputs = _inputs;
			foreach (CspSolverTerm expr in inputs)
			{
				if (BooleanFunction.IsSatisfied(ls[expr]))
				{
					num++;
				}
			}
			return num;
		}

		/// <summary> Picks an input, preferably one that is satisfied
		/// </summary>
		protected CspSolverTerm PickSatisfiedInput(LocalSearchSolver ls)
		{
			CspSolverTerm result = _inputs[0];
			foreach (CspSolverTerm item in RandomlyEnumerateSubterms(ls.RandomSource))
			{
				if (BooleanFunction.IsSatisfied(ls[item]))
				{
					result = item;
					break;
				}
			}
			return result;
		}

		/// <summary> Picks an input, preferably one that is violated
		/// </summary>
		protected CspSolverTerm PickViolatedInput(LocalSearchSolver ls)
		{
			CspSolverTerm result = _inputs[0];
			foreach (CspSolverTerm item in RandomlyEnumerateSubterms(ls.RandomSource))
			{
				if (!BooleanFunction.IsSatisfied(ls[item]))
				{
					result = item;
					break;
				}
			}
			return result;
		}

		/// <summary> Selects a subterm that is in a direction opposite
		///           to the one that is targetted for this function
		/// </summary>
		protected KeyValuePair<CspSolverTerm, int> SelectSubtermWithWrongPolarity(LocalSearchSolver ls, int target)
		{
			bool flag = target == 1;
			CspSolverTerm key = _inputs[0];
			foreach (CspSolverTerm item in RandomlyEnumerateSubterms(ls.RandomSource))
			{
				if (BooleanFunction.IsSatisfied(ls[item]) == !flag)
				{
					key = item;
					break;
				}
			}
			return new KeyValuePair<CspSolverTerm, int>(key, target);
		}
	}
}
