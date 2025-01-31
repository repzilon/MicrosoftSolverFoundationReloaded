using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal class NonlinearSolutionMapping : PluginSolutionMapping
	{
		private readonly IRowVariableSolver _solver;

		private readonly INonlinearSolution _solution;

		internal NonlinearSolutionMapping(Model model, IRowVariableSolver solver, INonlinearSolution solution, int[] decisionVids, ValueTable<int>[] decisionIndexedVids, int[] goalVids, Constraint[] rowConstraint, int[] rowSubconstraint, object[][] rowIndexes)
			: base(model, decisionVids, decisionIndexedVids, goalVids, rowConstraint, rowSubconstraint, rowIndexes)
		{
			_solver = solver;
			_solution = solution;
		}

		internal override Rational GetValue(int vidVar)
		{
			return _solver.GetValue(vidVar);
		}

		internal override int GetSolverVariableCount()
		{
			return _solver.VariableCount;
		}

		internal override int GetSolverGoalCount()
		{
			return ((IGoalModel)_solver).GoalCount;
		}

		internal override IEnumerable<int> GetSolverRowIndices()
		{
			return _solver.RowIndices;
		}

		internal override bool IsGoal(int vidRow)
		{
			return ((IGoalModel)_solver).IsGoal(vidRow);
		}

		internal override bool ShouldExtractDecisionValues(SolverQuality quality)
		{
			switch (quality)
			{
			case SolverQuality.Unknown:
				return _solution.Result != NonlinearResult.Invalid;
			default:
				return false;
			case SolverQuality.Optimal:
			case SolverQuality.Feasible:
			case SolverQuality.LocalOptimal:
			case SolverQuality.LocalInfeasible:
				return true;
			}
		}

		internal override bool IsGoalOptimal(int goalNumber)
		{
			_solution.GetSolvedGoal(goalNumber, out var _, out var _, out var _, out var optimal);
			return optimal;
		}
	}
}
