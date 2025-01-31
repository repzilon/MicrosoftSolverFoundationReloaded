using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class MIPMostFractionalVariableBranching : MIPVariableBranchingAlgorithm
	{
		/// <summary> pick a branching variable within a node
		/// </summary>
		/// <remarks>No update is needed for most fractional branching</remarks>
		public override void UpdateAfterNodeSolve(MipNode node, LinearResult relaxationResult)
		{
		}

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
			double num = 0.0;
			foreach (BasicVariableValueTuple item in VariableBranchingCandidates(node))
			{
				double fraction = Statics.GetFraction(item.Val);
				if (fraction > num)
				{
					branchVar = item.Var;
					ivar = item.Row;
					rational = item.Val;
					num = fraction;
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
