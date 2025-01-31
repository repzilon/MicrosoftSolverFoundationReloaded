using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Computes pseudo-costs for mixed-integer programs.
	/// </summary>
	/// <remarks>
	/// Pseudo costs are used to approximate the increase in the objective function (remember we are looking for
	/// a minimum) based on the change in a variable. The variable that is expected to give the biggest increase is
	/// selected as the branching variable. (Having a big increase makes it more likely that the node can be cut from
	/// the search tree.)
	/// Pseudo costs are based on historical data. Whenever a variable is selected during the branch and bound process,
	/// the increase in the objective function is recorded. The pseudo cost is the average of the increase divided by 
	/// the fractional part (up or down) of the variable.
	/// If a variable is not yet costed, its pseudo cost is approximated by the average pseudo cost of the costed variables.
	///
	/// When using strong costs, the actual increase in objective function is computed (by resolving the problem) instead of 
	/// relying on historical data only. This gives a more accurate initial value for the pseudo-cost at the top of the tree 
	/// where good branching matters most. Since this operation is expansive, we only perform it for un-initialized pseudo-costs.
	/// We revert to the normal algorithm when the pseudo-costs are initialized.
	/// </remarks>
	internal class PseudoCosts
	{
		/// <summary>
		/// Record of a variable pseudo costs.
		/// </summary>
		private class PseudoCost
		{
			private double _downPseudoCost;

			private double _upPseudoCost;

			private int _downSelectionCount;

			private int _upSelectionCount;

			public double DownPseudoCost => _downPseudoCost;

			public double UpPseudoCost => _upPseudoCost;

			public int DownSelectionCount => _downSelectionCount;

			public int UpSelectionCount => _upSelectionCount;

			public void UpdateDownPseudoCost(Rational fractionalPart, Rational objectiveIncrease)
			{
				double num = (double)(objectiveIncrease / fractionalPart);
				_downPseudoCost = _downPseudoCost / (double)(_downSelectionCount + 1) * (double)_downSelectionCount + num / (double)(_downSelectionCount + 1);
				_downSelectionCount++;
			}

			public void UpdateUpPseudoCost(Rational fractionalPart, Rational objectiveIncrease)
			{
				double num = (double)(objectiveIncrease / fractionalPart);
				_upPseudoCost = _upPseudoCost / (double)(_upSelectionCount + 1) * (double)_upSelectionCount + num / (double)(_upSelectionCount + 1);
				_upSelectionCount++;
			}
		}

		private readonly bool _useStrongCosts;

		private readonly SimplexTask _thread;

		private readonly SimplexTask _workerThread;

		private readonly Dictionary<int, PseudoCost> _pseudoCosts;

		private double _downPseudoCostsTotal;

		private double _upPseudoCostsTotal;

		private int _downSelectionCountsTotal;

		private int _upSelectionCountsTotal;

		private Rational _maximumIncrease;

		internal SimplexTask Thread => _thread;

		public bool UseStrongCosts => _useStrongCosts;

		internal Rational MaximumIncrease => _maximumIncrease;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="thread">The thread requiring the pseudo costs.</param>
		/// <param name="strongCosts">Whether to use strong costs to initialize the pseudo-costs.</param>
		public PseudoCosts(SimplexTask thread, bool strongCosts)
		{
			_useStrongCosts = strongCosts;
			_pseudoCosts = new Dictionary<int, PseudoCost>();
			_thread = thread;
			SimplexSolverParams prm = new SimplexSolverParams
			{
				UseExact = false,
				Algorithm = SimplexAlgorithmKind.Dual
			};
			if (strongCosts)
			{
				_workerThread = new SimplexTask(thread.Solver, 0, prm, fForceExact: false);
			}
		}

		/// <summary>
		/// Computes the score of a variable.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="variable">The variable to score.</param>
		/// <param name="value">The current value of the variable.</param>
		/// <returns>
		/// The score represents how good the variable is for branching. Pseudo costs are used to approximate the
		/// increase in the objective function for the down and up branches. The scoring method combines the two
		/// estimates into one score.
		/// </returns>
		public double Score(Node node, int variable, Rational value)
		{
			return Score(GetEstimatedGoalDownIncrease(node, variable, value), GetEstimatedGoalUpIncrease(node, variable, value));
		}

		/// <summary>
		/// Gets how many times the variable has been used.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public int GetUsage(int variable)
		{
			PseudoCost pseudoCost = GetPseudoCost(variable);
			return pseudoCost.DownSelectionCount + pseudoCost.UpSelectionCount;
		}

		/// <summary>
		/// Updates the pseudo cost of a variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="fractionalPart"></param>
		/// <param name="objectiveIncrease"></param>
		public void UpdateDownPseudoCost(int variable, Rational fractionalPart, Rational objectiveIncrease)
		{
			if (objectiveIncrease > _maximumIncrease)
			{
				_maximumIncrease = objectiveIncrease;
			}
			if (!_pseudoCosts.TryGetValue(variable, out var value))
			{
				value = new PseudoCost();
				_pseudoCosts.Add(variable, value);
			}
			if (value.DownSelectionCount > 0)
			{
				_downPseudoCostsTotal -= value.DownPseudoCost;
			}
			value.UpdateDownPseudoCost(fractionalPart, objectiveIncrease);
			_downPseudoCostsTotal += value.DownPseudoCost;
			if (value.DownSelectionCount == 1)
			{
				_downSelectionCountsTotal++;
			}
		}

		/// <summary>
		/// Updates the pseudo cost of a variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="fractionalPart"></param>
		/// <param name="objectiveIncrease"></param>
		public void UpdateUpPseudoCost(int variable, Rational fractionalPart, Rational objectiveIncrease)
		{
			if (objectiveIncrease > _maximumIncrease)
			{
				_maximumIncrease = objectiveIncrease;
			}
			if (!_pseudoCosts.TryGetValue(variable, out var value))
			{
				value = new PseudoCost();
				_pseudoCosts.Add(variable, value);
			}
			if (value.UpSelectionCount > 0)
			{
				_upPseudoCostsTotal -= value.UpPseudoCost;
			}
			value.UpdateUpPseudoCost(fractionalPart, objectiveIncrease);
			_upPseudoCostsTotal += value.UpPseudoCost;
			if (value.UpSelectionCount == 1)
			{
				_upSelectionCountsTotal++;
			}
		}

		/// <summary>
		/// Gets the estimated increase in the goal value for a given variable.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="branchingVariable"></param>
		/// <param name="branchingValue"></param>
		/// <returns></returns>
		public double GetEstimatedGoalUpIncrease(Node node, int branchingVariable, Rational branchingValue)
		{
			InitializeUpPseudoCost(node, branchingVariable, branchingValue);
			return GetUpPseudoCost(branchingVariable) * (double)(branchingValue.GetCeiling() - branchingValue);
		}

		/// <summary>
		/// Gets the estimated increase in the goal value for a given variable.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="branchingVariable"></param>
		/// <param name="branchingValue"></param>
		/// <returns></returns>
		public double GetEstimatedGoalDownIncrease(Node node, int branchingVariable, Rational branchingValue)
		{
			InitializeDownPseudoCost(node, branchingVariable, branchingValue);
			return GetDownPseudoCost(branchingVariable) * (double)(branchingValue - branchingValue.GetFloor());
		}

		private void InitializeDownPseudoCost(Node node, int branchingVariable, Rational branchingValue)
		{
			PseudoCost pseudoCost = GetPseudoCost(branchingVariable);
			if (pseudoCost == null)
			{
				pseudoCost = new PseudoCost();
				_pseudoCosts.Add(branchingVariable, pseudoCost);
			}
			if (pseudoCost.DownSelectionCount == 0 && UseStrongCosts)
			{
				_workerThread.InitBranchAndBound();
				SimplexBasis simplexBasis = _workerThread.Basis.Clone();
				simplexBasis.SetToSlacks();
				_workerThread.Basis.SetTo(simplexBasis);
				node.ApplyConstraints(_workerThread);
				UpperBoundConstraint upperBoundConstraint = new UpperBoundConstraint(branchingVariable, branchingValue, null);
				upperBoundConstraint.ApplyConstraint(_workerThread);
				LinearResult linearResult = _workerThread.RunSimplex(null, restart: true);
				upperBoundConstraint.ResetConstraint(_workerThread);
				node.ResetConstraints(_workerThread);
				if (linearResult == LinearResult.Optimal)
				{
					UpdateDownPseudoCost(branchingVariable, branchingValue - branchingValue.GetFloor(), _workerThread.OptimalGoalValues[0] - node.LowerBoundGoalValue[0]);
				}
			}
		}

		private void InitializeUpPseudoCost(Node node, int branchingVariable, Rational branchingValue)
		{
			PseudoCost pseudoCost = GetPseudoCost(branchingVariable);
			if (pseudoCost == null)
			{
				pseudoCost = new PseudoCost();
				_pseudoCosts.Add(branchingVariable, pseudoCost);
			}
			if (pseudoCost.UpSelectionCount == 0 && UseStrongCosts)
			{
				_workerThread.InitBranchAndBound();
				SimplexBasis simplexBasis = _workerThread.Basis.Clone();
				simplexBasis.SetToSlacks();
				_workerThread.Basis.SetTo(simplexBasis);
				node.ApplyConstraints(_workerThread);
				LowerBoundConstraint lowerBoundConstraint = new LowerBoundConstraint(branchingVariable, branchingValue, null);
				lowerBoundConstraint.ApplyConstraint(_workerThread);
				LinearResult linearResult = _workerThread.RunSimplex(null, restart: true);
				lowerBoundConstraint.ResetConstraint(_workerThread);
				node.ResetConstraints(_workerThread);
				if (linearResult == LinearResult.Optimal)
				{
					UpdateUpPseudoCost(branchingVariable, branchingValue.GetCeiling() - branchingValue, _workerThread.OptimalGoalValues[0] - node.LowerBoundGoalValue[0]);
				}
			}
		}

		/// <summary>
		/// Gets the pseudo cost for a variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns>The pseudo cost or null if the variable has not been costed.</returns>
		private PseudoCost GetPseudoCost(int variable)
		{
			_pseudoCosts.TryGetValue(variable, out var value);
			return value;
		}

		/// <summary>
		/// Gets the pseudo cost associated with branching down on a given variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		private double GetDownPseudoCost(int variable)
		{
			PseudoCost pseudoCost = GetPseudoCost(variable);
			if (pseudoCost != null && pseudoCost.DownSelectionCount > 0)
			{
				return pseudoCost.DownPseudoCost;
			}
			if (_downSelectionCountsTotal > 0)
			{
				return _downPseudoCostsTotal / (double)_downSelectionCountsTotal;
			}
			return (double)_thread.Model.Matrix.GetCoefExact(_thread.Model.GetGoalRow(0), _thread.Model.GetVar(variable)).AbsoluteValue;
		}

		/// <summary>
		/// Gets the pseudo cost associated with branching up on a given variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		private double GetUpPseudoCost(int variable)
		{
			PseudoCost pseudoCost = GetPseudoCost(variable);
			if (pseudoCost != null && pseudoCost.UpSelectionCount > 0)
			{
				return pseudoCost.UpPseudoCost;
			}
			if (_upSelectionCountsTotal > 0)
			{
				return _upPseudoCostsTotal / (double)_upSelectionCountsTotal;
			}
			return (double)_thread.Model.Matrix.GetCoefExact(_thread.Model.GetGoalRow(0), _thread.Model.GetVar(variable)).AbsoluteValue;
		}

		/// <summary>
		/// Combines the expected increases in objective function into a single score.
		/// </summary>
		/// <param name="expectedDownIncrease"></param>
		/// <param name="expectedUpIncrease"></param>
		/// <returns></returns>
		private static double Score(double expectedDownIncrease, double expectedUpIncrease)
		{
			double num = 1.0 / 6.0;
			return (1.0 - num) * ((expectedDownIncrease < expectedUpIncrease) ? expectedDownIncrease : expectedUpIncrease) + num * ((expectedDownIncrease < expectedUpIncrease) ? expectedUpIncrease : expectedDownIncrease);
		}
	}
}
