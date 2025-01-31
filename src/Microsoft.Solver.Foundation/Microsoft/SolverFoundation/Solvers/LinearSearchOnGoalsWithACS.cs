namespace Microsoft.SolverFoundation.Solvers
{
	internal class LinearSearchOnGoalsWithACS : AdaptiveConflictSeeking
	{
		protected int _tighteningGoalN;

		protected int[] _optimalValues;

		protected int[] _goalBaseLine;

		protected bool _fGoalsImproved;

		protected bool _fFeasible;

		protected int _iRecordedDecisions;

		protected CspSolverTerm[] _snapshotSearchVars;

		protected int _snapshotDepth;

		protected BranchingDecision[] _snapshotDecisions;

		protected int[] _snapshotDecisionVarValues;

		protected int _snapshotPriorCoverage;

		protected int _snapshotBackTrackCtr;

		protected int _snapshotLastRestartAt;

		internal LinearSearchOnGoalsWithACS(ConstraintSystem model)
			: base(model)
		{
			_depth = _goalCount;
			_tighteningGoalN = 0;
			_optimalValues = new int[_goalCount];
			_goalBaseLine = new int[_goalCount];
			for (int i = 0; i < _goalCount; i++)
			{
				_optimalValues[i] = int.MaxValue;
				_goalBaseLine[i] = -1;
			}
			_fGoalsImproved = true;
			_fFeasible = false;
			_iRecordedDecisions = -1;
		}

		/// <summary> Choose the next undecided Decision Variable
		/// </summary>
		/// <returns> false if no decision variables remain to be tried </returns>
		internal override bool NextDecisionVariable()
		{
			bool? flag = null;
			while (!flag.HasValue)
			{
				flag = NextDecisionVariableCore();
			}
			return flag.Value;
		}

		/// <summary> Decide the next decision variable for branching
		/// </summary>
		/// <returns>true iff we have found a decision var for branching</returns>
		private bool DecideNextDecisionVar()
		{
			int depth = _depth;
			if (_depth < _searchVars.Length)
			{
				if (_iRecordedDecisions >= 0 && _iRecordedDecisions < _snapshotDecisionVarValues.Length)
				{
					CspSolverTerm term = _snapshotDecisions[_iRecordedDecisions].Term;
					if (1 < term.Count)
					{
						_decisions.Push(new ForwardDecision(this, term, _model._changes.Count, depth));
						_depth++;
						_iRecordedDecisions--;
						return true;
					}
					_iRecordedDecisions = -1;
				}
				do
				{
					if (_searchFirstCount <= _depth)
					{
						OptimizeDecisionOrder();
					}
					CspSolverTerm cspSolverTerm = _searchVars[_depth++];
					if (1 < cspSolverTerm.Count)
					{
						_decisions.Push(new ForwardDecision(this, cspSolverTerm, _model._changes.Count, depth));
						return true;
					}
				}
				while (_depth <= _searchFirstCount && _depth < _searchVars.Length);
				_depth--;
			}
			return false;
		}

		/// <summary> Wrapper of DecisionNextDecisionVar that uses return values to indicate different scenarios
		/// </summary>
		/// <returns>true: found the next decision var; false: found optimal solution; null: found feasible solution, took the snapshot of the decsion stack, backtracked to the top, and tightened the goal</returns>
		private bool? NextDecisionVariableCore()
		{
			if (DecideNextDecisionVar())
			{
				return true;
			}
			if (_tighteningGoalN >= _goalCount)
			{
				return false;
			}
			if (SnapshotDecisions())
			{
				_iRecordedDecisions = _snapshotDecisions.Length - 1;
			}
			else
			{
				_iRecordedDecisions = -1;
			}
			return null;
		}

		internal bool AreGoalsImproved()
		{
			return _fGoalsImproved;
		}

		/// <summary> Choose the next undecided Decision Variable
		/// </summary>
		/// <returns> false if no decision variables remain to be tried </returns>
		internal override bool NextDecisionVariableYieldOnSuboptimal()
		{
			if (DecideNextDecisionVar())
			{
				return true;
			}
			if (_tighteningGoalN == _goalCount)
			{
				_tighteningGoalN++;
			}
			else if (_tighteningGoalN > _goalCount)
			{
				_fGoalsImproved = false;
			}
			return false;
		}

		internal override bool TakeNecessarySnapshots()
		{
			if (_tighteningGoalN >= _goalCount)
			{
				return false;
			}
			if (SnapshotDecisions())
			{
				_iRecordedDecisions = _snapshotDecisions.Length - 1;
			}
			else
			{
				_iRecordedDecisions = -1;
			}
			return true;
		}

		/// <summary>
		/// Add a new decision layer when we find a feasible solution. -- lengliu
		/// </summary>
		private bool SnapshotDecisions()
		{
			_fFeasible = true;
			_optimalValues[_tighteningGoalN] = _searchVars[_tighteningGoalN].Last;
			_snapshotDepth = _depth;
			_snapshotPriorCoverage = _priorCoverage;
			_snapshotBackTrackCtr = _backTrackCtr;
			_snapshotLastRestartAt = _lastRestartAt;
			_snapshotSearchVars = new CspSolverTerm[_searchVars.Length];
			for (int i = 0; i < _searchVars.Length; i++)
			{
				_snapshotSearchVars[i] = _searchVars[i];
			}
			_snapshotDecisions = _decisions.ToArray();
			_snapshotDecisionVarValues = new int[_snapshotDecisions.Length];
			for (int j = 0; j < _snapshotDecisions.Length; j++)
			{
				_snapshotDecisionVarValues[j] = _snapshotDecisions[j].Value;
			}
			if (!TightenGoal())
			{
				RestoreSnapshot();
				return false;
			}
			return true;
		}

		/// <summary>
		/// Rewind all previous range restrictions to goals upto the point of _tighteningGoalN
		/// </summary>
		private void UndoGoalSettings()
		{
			for (int num = _tighteningGoalN; num >= 0; num--)
			{
				if (_goalBaseLine[num] >= 0)
				{
					_model.Backtrack(_goalBaseLine[num]);
				}
			}
		}

		/// <summary>
		/// Set optimal values to goals that are earlier than _tighteningGoalN
		/// </summary>
		private void SetEarlyGoals()
		{
			for (int i = 0; i < _tighteningGoalN; i++)
			{
				_goalBaseLine[i] = _model._changes.Count;
				_searchVars[i].Intersect(_optimalValues[i], _optimalValues[i], out var conflict);
				_propagator.Propagate(out conflict);
			}
		}

		/// <summary>
		/// Try to tighten the range of current working goal (indexed by _tighteningGoalN)
		/// </summary>
		/// <returns>true if and only if no conflicts found after the goal is tightened</returns>
		private bool TightenGoal()
		{
			while (0 < _decisions.Count)
			{
				_decisions.Pop().Backtrack();
			}
			_depth = _goalCount;
			UndoGoalSettings();
			SetEarlyGoals();
			_goalBaseLine[_tighteningGoalN] = _model._changes.Count;
			_searchVars[_tighteningGoalN].Intersect(_searchVars[_tighteningGoalN].First, _optimalValues[_tighteningGoalN] - 1, out var conflict);
			if (conflict != null)
			{
				return false;
			}
			if (!_propagator.Propagate(out conflict))
			{
				return false;
			}
			return true;
		}

		/// <summary> With the current decision variable, narrow its value set
		/// </summary>
		/// <returns> False when all values of this variable have been exhausted </returns>
		internal override bool TryNextValue()
		{
			while (0 < _decisions.Count && !_decisions.Peek().TryNextValue())
			{
				_iRecordedDecisions = -1;
				_depth = _decisions.Pop().OldDepth;
			}
			if (_goalCount < _depth)
			{
				return true;
			}
			if (_fFeasible && _tighteningGoalN < _goalCount)
			{
				RestoreSnapshot();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Restore the previous decision snapshot.
		/// </summary>
		private void RestoreSnapshot()
		{
			while (0 < _decisions.Count)
			{
				_decisions.Pop().Backtrack();
			}
			UndoGoalSettings();
			_tighteningGoalN++;
			SetEarlyGoals();
			_depth = _snapshotDepth;
			_priorCoverage = _snapshotPriorCoverage;
			_backTrackCtr = _snapshotBackTrackCtr;
			_lastRestartAt = _snapshotLastRestartAt;
			for (int i = 0; i < _searchVars.Length; i++)
			{
				_searchVars[i] = _snapshotSearchVars[i];
			}
			int num = _goalCount;
			for (int num2 = _snapshotDecisions.Length - 1; num2 >= 0; num2--)
			{
				if (_snapshotDecisions[num2].Term.Count != 1)
				{
					_decisions.Push(new ForwardDecision(this, _snapshotDecisions[num2].Term, _model._changes.Count, num));
					num++;
					_decisions.Peek().Value = _snapshotDecisionVarValues[num2];
					_propagator.Propagate(out var _);
				}
			}
			_depth = num;
		}
	}
}
