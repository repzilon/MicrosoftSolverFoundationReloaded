using System;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class MIPSingleGoalPseudocostVariableBranching : MIPPseudocostVariableBranching
	{
		private double _bestPseudocost;

		private double _candidatePseudocost;

		private double[] _varUpPseudocostMap;

		private double[] _varDownPseudocostMap;

		public MIPSingleGoalPseudocostVariableBranching(SimplexReducedModel mod)
			: base(mod)
		{
			_varUpPseudocostMap = new double[_varLim];
			_varDownPseudocostMap = new double[_varLim];
			for (int i = 0; i < _varLim; i++)
			{
				InitializePseudocost(out _varUpPseudocostMap[i], out _varDownPseudocostMap[i]);
			}
		}

		/// <summary> pick a branching variable within a node
		/// </summary>
		public override void UpdateAfterNodeSolve(MipNode node, LinearResult relaxationResult)
		{
			if (node.Parent == null)
			{
				return;
			}
			OptimalGoalValues optimalGoalValues = node.Task.OptimalGoalValues;
			OptimalGoalValues relaxationOptimalGoalValues = node.Parent.RelaxationOptimalGoalValues;
			int branchingVar = node.BranchingVar;
			double num = (optimalGoalValues[0] - relaxationOptimalGoalValues[0]).ToDouble();
			if (node.IsLowerBound)
			{
				if (Math.Abs(num) >= 1E-08)
				{
					_varDownPseudocostMap[branchingVar] = num;
				}
			}
			else if (Math.Abs(num) >= 1E-08)
			{
				_varUpPseudocostMap[branchingVar] = num;
			}
		}

		protected override void InitializeBestPseudocost(int goalCount)
		{
			_bestPseudocost = 0.0;
		}

		protected override void ComputeCandidatePseudocost(int var)
		{
			_candidatePseudocost = _varUpPseudocostMap[var] + _varDownPseudocostMap[var];
		}

		protected override bool IsBetterPseudocost()
		{
			if (_candidatePseudocost > _bestPseudocost)
			{
				_bestPseudocost = _candidatePseudocost;
				return true;
			}
			return false;
		}

		private static void InitializePseudocost(out double up, out double down)
		{
			up = (down = 0.0);
		}
	}
}
