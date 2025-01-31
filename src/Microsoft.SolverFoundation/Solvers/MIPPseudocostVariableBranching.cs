using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class MIPPseudocostVariableBranching : MIPVariableBranchingAlgorithm
	{
		protected int _varLim;

		public MIPPseudocostVariableBranching(SimplexReducedModel mod)
		{
			_varLim = mod._varLim;
		}

		protected abstract void InitializeBestPseudocost(int goalCount);

		protected abstract void ComputeCandidatePseudocost(int var);

		protected abstract bool IsBetterPseudocost();

		/// <summary>
		/// Find a var and value to branch
		/// </summary>
		/// <param name="node">Current MIP node</param>
		/// <param name="ivar">row index of the var</param>
		/// <param name="branchVar">var to branch</param>
		/// <param name="branchingLowerBound">lower bound value to branch on (value is in the reduced model space)</param>
		/// <param name="branchingUpperBound">upper bound value to branch on (value is in the reduced model space)</param>
		/// <returns>true if branching var is found</returns>
		public override bool FindBranchVariable(MipNode node, out int ivar, out int branchVar, out double branchingLowerBound, out double branchingUpperBound)
		{
			SimplexReducedModel model = node.Task.Model;
			branchVar = -1;
			branchingLowerBound = 0.0;
			branchingUpperBound = 0.0;
			ivar = -1;
			Rational rational = 0;
			InitializeBestPseudocost(model.GoalCount);
			List<BasicVariableValueTuple> list = new List<BasicVariableValueTuple>();
			foreach (BasicVariableValueTuple item in VariableBranchingCandidates(node))
			{
				list.Add(item);
				ComputeCandidatePseudocost(item.Var);
				if (IsBetterPseudocost())
				{
					branchVar = item.Var;
					ivar = item.Row;
					rational = item.Val;
				}
			}
			if (branchVar < 0)
			{
				double num = 0.0;
				foreach (BasicVariableValueTuple item2 in list)
				{
					double fraction = Statics.GetFraction(item2.Val);
					if (fraction > num)
					{
						branchVar = item2.Var;
						ivar = item2.Row;
						rational = item2.Val;
						num = fraction;
					}
				}
			}
			if (branchVar >= 0)
			{
				Rational floor = rational.GetFloor();
				branchingUpperBound = model.MapValueFromExactToDouble(branchVar, model.MapValueFromVidToVar(branchVar, floor));
				branchingLowerBound = model.MapValueFromExactToDouble(branchVar, model.MapValueFromVidToVar(branchVar, floor + 1));
			}
			return branchVar >= 0;
		}
	}
}
