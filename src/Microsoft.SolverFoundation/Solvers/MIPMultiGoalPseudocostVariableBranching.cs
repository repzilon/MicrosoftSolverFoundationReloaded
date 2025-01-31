using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class MIPMultiGoalPseudocostVariableBranching : MIPPseudocostVariableBranching
	{
		private OptimalGoalValues _bestPseudocost;

		private OptimalGoalValues _candidatePseudocost;

		private OptimalGoalValues[] _varUpPseudocostMap;

		private OptimalGoalValues[] _varDownPseudocostMap;

		public MIPMultiGoalPseudocostVariableBranching(SimplexReducedModel mod)
			: base(mod)
		{
			_varUpPseudocostMap = new OptimalGoalValues[_varLim];
			_varDownPseudocostMap = new OptimalGoalValues[_varLim];
			for (int i = 0; i < _varLim; i++)
			{
				InitializePseudocost(mod, out _varUpPseudocostMap[i], out _varDownPseudocostMap[i]);
			}
			_candidatePseudocost = new OptimalGoalValues(mod.GoalCount);
		}

		/// <summary> pick a branching variable within a node
		/// </summary>
		public override void UpdateAfterNodeSolve(MipNode node, LinearResult relaxationResult)
		{
			if (relaxationResult == LinearResult.Optimal && node.Parent != null)
			{
				OptimalGoalValues optimalGoalValues = node.Task.OptimalGoalValues;
				OptimalGoalValues relaxationOptimalGoalValues = node.Parent.RelaxationOptimalGoalValues;
				int branchingVar = node.BranchingVar;
				OptimalGoalValues optimalGoalValues2 = Difference(optimalGoalValues, relaxationOptimalGoalValues);
				if (node.IsLowerBound)
				{
					_varDownPseudocostMap[branchingVar] = optimalGoalValues2;
				}
				else
				{
					_varUpPseudocostMap[branchingVar] = optimalGoalValues2;
				}
			}
		}

		protected override void InitializeBestPseudocost(int goalCount)
		{
			_bestPseudocost = new OptimalGoalValues(goalCount);
			for (int i = 0; i < goalCount; i++)
			{
				_bestPseudocost[i] = 0;
			}
		}

		protected override void ComputeCandidatePseudocost(int var)
		{
			SumPseudocost(_varUpPseudocostMap[var], _varDownPseudocostMap[var]);
		}

		protected override bool IsBetterPseudocost()
		{
			if (_candidatePseudocost > _bestPseudocost)
			{
				_bestPseudocost = _candidatePseudocost.Clone();
				return true;
			}
			return false;
		}

		private static void InitializePseudocost(SimplexReducedModel mod, out OptimalGoalValues up, out OptimalGoalValues down)
		{
			up = new OptimalGoalValues(mod.GoalCount);
			down = new OptimalGoalValues(mod.GoalCount);
			for (int i = 0; i < mod.GoalCount; i++)
			{
				up[i] = 0;
				down[i] = 0;
			}
		}

		private void SumPseudocost(OptimalGoalValues ogv1, OptimalGoalValues ogv2)
		{
			for (int i = 0; i < ogv1.Count; i++)
			{
				_candidatePseudocost[i] = ogv1[i] + ogv2[i];
			}
		}

		private static OptimalGoalValues Difference(OptimalGoalValues ogv1, OptimalGoalValues ogv2)
		{
			OptimalGoalValues optimalGoalValues = new OptimalGoalValues(ogv1.Count);
			for (int i = 0; i < ogv1.Count; i++)
			{
				optimalGoalValues[i] = ogv1[i] - ogv2[i];
				if (optimalGoalValues[i].AbsoluteValue < 1E-08)
				{
					optimalGoalValues[i] = 0;
				}
			}
			return optimalGoalValues;
		}
	}
}
