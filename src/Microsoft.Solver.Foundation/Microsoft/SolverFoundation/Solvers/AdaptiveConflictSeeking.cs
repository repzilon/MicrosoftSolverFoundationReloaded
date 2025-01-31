namespace Microsoft.SolverFoundation.Solvers
{
	internal class AdaptiveConflictSeeking : TreeSearchSolver
	{
		protected static readonly byte[] _rgWeight = new byte[9] { 0, 1, 2, 3, 4, 5, 4, 3, 2 };

		internal AdaptiveConflictSeeking(ConstraintSystem model)
			: base(model)
		{
		}

		/// <summary> Choose the next undecided Decision Variable
		/// </summary>
		/// <returns> false if no decision variables remain to be tried </returns>
		internal override bool NextDecisionVariable()
		{
			int depth = _depth;
			if (_depth < _searchVars.Length)
			{
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

		internal override bool NextDecisionVariableYieldOnSuboptimal()
		{
			return NextDecisionVariable();
		}

		/// <summary> Choose the best next variable.
		/// </summary>
		protected void OptimizeDecisionOrder()
		{
			int num = ScanA();
			if (num < _searchVars.Length)
			{
				CspSolverTerm cspSolverTerm = _searchVars[_depth];
				_searchVars[_depth] = _searchVars[num];
				_searchVars[num] = cspSolverTerm;
			}
		}

		/// <summary> Scan for maximizing Credit * card
		/// </summary>
		internal int ScanA()
		{
			int result = _searchVars.Length;
			double num = 0.0;
			for (int i = _depth; i < _searchVars.Length; i++)
			{
				CspSolverTerm cspSolverTerm = _searchVars[i];
				if (1 < cspSolverTerm.Count && cspSolverTerm.Participates)
				{
					double num2 = cspSolverTerm.Count;
					num2 = (double)(1 + cspSolverTerm.Dependents.Count) / (num2 * num2 + 16.0);
					if (num <= num2)
					{
						result = i;
						num = num2;
					}
				}
			}
			return result;
		}

		/// <summary> With the current decision variable, narrow its value set
		/// </summary>
		/// <returns> False when all values of this variable have been exhausted </returns>
		internal override bool TryNextValue()
		{
			while (0 < _decisions.Count && !_decisions.Peek().TryNextValue())
			{
				_depth = _decisions.Pop().OldDepth;
				if (_model.Parameters.RestartEnabled && Restart())
				{
					return true;
				}
			}
			return 0 < _depth;
		}

		/// <summary> Go back to find the most recent decision we are allowed to change.
		///             Remove all decisions made since then.
		///             Alter the order of our strategy for future.
		/// </summary>
		/// <returns></returns>
		internal bool Restart()
		{
			_backTrackCtr++;
			int num = _backTrackCtr - _lastRestartAt;
			if (10 < num && _model._changes.Count < _priorCoverage)
			{
				if (!LearnClauses())
				{
					return false;
				}
				_lastRestartAt = _backTrackCtr;
				while (0 < _decisions.Count)
				{
					_decisions.Peek().Backtrack();
					_depth = _decisions.Pop().OldDepth;
				}
				_priorCoverage /= 2;
				return true;
			}
			_priorCoverage = (4 * _priorCoverage + 2 * _model._changes.Count) / 7;
			return false;
		}

		/// <summary> Learning clauses here is not the same as the classic technique.
		///             Instead of pushing backwards from the contradiction to find
		///             the causes, we go directly to the root - the current guesses.
		///             The guess sequence is analyzed to yield not just the current
		///             failure but also all those we enumerated to get to here, and
		///             then it is simplified by the factoring technique described
		///             below.  We only need to call for this on a restart, not on
		///             every backtrack.  The result is compact and complete, so it
		///             has the guaranteed property that we will cut short any future
		///             exploration of the same subtrees of the solution (so we
		///             converge, an important consideration when using restarts).
		/// </summary>
		/// <returns> true if we learned, false if we discovered we were at the end.
		/// </returns>
		protected bool LearnClauses()
		{
			while (0 < _decisions.Count && _decisions.Peek().IsFinal())
			{
				_depth = _decisions.Pop().OldDepth;
			}
			if (_decisions.Count == 0)
			{
				return false;
			}
			int count = _decisions.Count;
			CspSolverTerm[] array = new CspSolverTerm[count + 1];
			CspSolverDomain[] array2 = new CspSolverDomain[count + 1];
			int[] rgForbidden = new int[count + 1];
			int num = count - 1;
			while (0 <= num)
			{
				BranchingDecision branchingDecision = _decisions.Pop();
				_model.Backtrack(branchingDecision.BaselineChange);
				array[num] = branchingDecision.Term;
				array2[num] = branchingDecision.Guesses;
				num--;
			}
			_depth = 0;
			Forbid(0, 0, count, array2, array, rgForbidden);
			return true;
		}

		protected void Forbid(int prefix, int first, int N, CspSolverDomain[] guessSets, CspSolverTerm[] terms, int[] rgForbidden)
		{
			CspSolverDomain cspSolverDomain = null;
			int num = 0;
			while (first < N && num < 2)
			{
				cspSolverDomain = guessSets[first];
				num = cspSolverDomain.Count;
				rgForbidden[first++] = cspSolverDomain.First;
			}
			int num2 = ((first < N && 1 < num) ? 1 : 0);
			if (1 == num - num2)
			{
				rgForbidden[first - 1] = cspSolverDomain.First;
				sForbiddenList.Create(_model, terms, rgForbidden, prefix, first - prefix);
			}
			else if (1 < num - num2)
			{
				cspSolverDomain.Exclude(out var newD, cspSolverDomain.Last);
				sForbiddenRange.Create(_model, terms, rgForbidden, prefix, first - prefix, newD);
			}
			if (0 < num2)
			{
				rgForbidden[first - 1] = cspSolverDomain.Last;
				CspSolverTerm cspSolverTerm = terms[first];
				CspSolverTerm cspSolverTerm2 = (terms[first] = _model.CreateBoolean(null) as CspSolverTerm);
				rgForbidden[first] = 0;
				sForbiddenList.Create(_model, terms, rgForbidden, prefix, first + 1 - prefix);
				terms[first - 1] = cspSolverTerm2;
				rgForbidden[first - 1] = 1;
				terms[first] = cspSolverTerm;
				Forbid(first - 1, first, N, guessSets, terms, rgForbidden);
			}
		}
	}
}
