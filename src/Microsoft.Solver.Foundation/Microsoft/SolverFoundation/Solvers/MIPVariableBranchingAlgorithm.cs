using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class MIPVariableBranchingAlgorithm
	{
		/// <summary> pick a branching variable within a node
		/// </summary>
		public abstract bool FindBranchVariable(MipNode node, out int ivar, out int branchVar, out double branchingLowerBound, out double branchingUpperBound);

		/// <summary> update the branching algorithm after node relaxation is solved
		/// </summary>
		public abstract void UpdateAfterNodeSolve(MipNode node, LinearResult relaxationResult);

		/// <summary> return branching candidates so that specific branching algorithm can decide on which candidate the solver should branch
		/// </summary>
		public virtual IEnumerable<BasicVariableValueTuple> VariableBranchingCandidates(MipNode node)
		{
			SimplexReducedModel mod = node.Task.Model;
			SimplexBasis bas = node.Task.Basis;
			int rowPos = mod.RowLim;
			while (true)
			{
				int num;
				rowPos = (num = rowPos - 1);
				if (num < 0)
				{
					break;
				}
				int var = bas.GetBasicVar(rowPos);
				if (mod.IsVarInteger(var))
				{
					double numVal = node.GetUserVarValue(var);
					if (!Statics.IsInteger(numVal))
					{
						yield return new BasicVariableValueTuple(var, rowPos, numVal);
					}
				}
			}
		}
	}
}
