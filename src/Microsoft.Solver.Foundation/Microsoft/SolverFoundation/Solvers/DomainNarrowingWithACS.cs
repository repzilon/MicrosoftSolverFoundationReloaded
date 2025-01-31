using System.Collections.Generic;
using System.Threading;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class DomainNarrowingWithACS : AdaptiveConflictSeeking
	{
		private static AutoResetEvent _pauseAutoReset = new AutoResetEvent(initialState: false);

		protected int _startingBaseline;

		protected int _fixedUserVarCount;

		protected volatile bool _fUserVarFixing;

		protected Dictionary<CspUserVariable, int> _mpFixedUserVars = new Dictionary<CspUserVariable, int>();

		protected CspSolverTerm[] _rgUserVars;

		internal DomainNarrowingWithACS(ConstraintSystem model)
			: base(model)
		{
			int num = _initSearchVarList.Count - _goalCount;
			_searchVars = new CspSolverTerm[num];
			_initSearchVarList.CopyTo(_goalCount, _searchVars, 0, num);
			_searchFirstCount -= _goalCount;
			_rgUserVars = new CspSolverTerm[_userVarCount];
			_initSearchVarList.CopyTo(_goalCount, _rgUserVars, 0, _userVarCount);
			_startingBaseline = _model._changes.Count;
		}

		/// <summary>
		/// Tell the decision algorithm to stop running.
		/// </summary>
		internal void AbortDomainNarrowing()
		{
			lock (this)
			{
				_model.AbortDomainNarrowing = true;
				_pauseAutoReset.Set();
			}
		}

		/// <summary>
		/// Set flags to fix/unfix the user var to a value
		/// </summary>
		/// <param name="var"></param>
		/// <returns>true iff the user var needs to be fixed to a new value</returns>
		internal bool TryFixUserVariable(CspUserVariable var)
		{
			lock (_mpFixedUserVars)
			{
				_fUserVarFixing = true;
				int fixedValue = var.GetFixedValue();
				if (_mpFixedUserVars.TryGetValue(var, out var value))
				{
					if (!var.IsFixing())
					{
						_mpFixedUserVars.Remove(var);
						_pauseAutoReset.Set();
						return false;
					}
					if (value == fixedValue)
					{
						_pauseAutoReset.Set();
						return false;
					}
					_mpFixedUserVars[var] = fixedValue;
				}
				else
				{
					if (!var.IsFixing())
					{
						_pauseAutoReset.Set();
						return false;
					}
					_mpFixedUserVars.Add(var, fixedValue);
				}
				_pauseAutoReset.Set();
			}
			return true;
		}

		/// <summary>
		/// Check if user has fixed/freed variables from outside.
		/// </summary>
		/// <returns>true iff there is need to fix new user vars</returns>
		private bool IsFixChangedUserVar()
		{
			if (!_fUserVarFixing)
			{
				return false;
			}
			lock (_mpFixedUserVars)
			{
				if (!_fUserVarFixing)
				{
					return false;
				}
				_fUserVarFixing = false;
				FullBacktrack();
				ClearFeasibleValues();
				SetupFixedUserVar();
				for (int i = 0; i < _userVarCount; i++)
				{
					CspUserVariable cspUserVariable = _searchVars[i] as CspUserVariable;
					cspUserVariable.EnableEvent();
				}
				_pauseAutoReset.Reset();
			}
			RaiseUserVarUpdate();
			return true;
		}

		private void FullBacktrack()
		{
			BacktrackTo(0);
			_depth = 0;
			_model.Backtrack(_startingBaseline);
		}

		private void BacktrackToTopFreeUserVar()
		{
			BacktrackTo(_fixedUserVarCount + 1);
		}

		private void BacktrackTo(int level)
		{
			while (level < _decisions.Count)
			{
				_depth = _decisions.Peek().OldDepth;
				_decisions.Pop().Backtrack();
			}
		}

		private void ClearFeasibleValues()
		{
			for (int i = 0; i < _userVarCount; i++)
			{
				CspUserVariable cspUserVariable = _rgUserVars[i] as CspUserVariable;
				cspUserVariable.ClearFeasibleValues();
			}
		}

		private void ConflictingValues()
		{
			ClearFeasibleValues();
			_fixedUserVarCount = _mpFixedUserVars.Count;
		}

		private bool SetupFixedUserVar()
		{
			int oldDepth = 0;
			int num = _userVarCount - 1;
			for (int i = 0; i < _userVarCount; i++)
			{
				CspUserVariable cspUserVariable = _rgUserVars[i] as CspUserVariable;
				if (_mpFixedUserVars.TryGetValue(cspUserVariable, out var value))
				{
					if (!cspUserVariable.Contains(value))
					{
						ConflictingValues();
						return false;
					}
					_decisions.Push(new ForwardDecision(this, cspUserVariable, _model._changes.Count, oldDepth));
					if (cspUserVariable.FiniteValue.Count != 1)
					{
						_decisions.Peek().Value = value;
					}
					_propagator.Propagate(out var conflict);
					if (conflict != null)
					{
						ConflictingValues();
						return false;
					}
					cspUserVariable.DecideValue(value);
					cspUserVariable.SetFinished();
					_searchVars[oldDepth++] = cspUserVariable;
				}
				else
				{
					_searchVars[num--] = cspUserVariable;
				}
			}
			_fixedUserVarCount = _mpFixedUserVars.Count;
			_depth = _fixedUserVarCount;
			for (int j = _fixedUserVarCount; j < _userVarCount; j++)
			{
				CspUserVariable cspUserVariable2 = _searchVars[j] as CspUserVariable;
				if (cspUserVariable2.Count == 1)
				{
					cspUserVariable2.DecideValue(cspUserVariable2.FiniteValue.First);
					cspUserVariable2.SetFinished();
				}
			}
			return true;
		}

		/// <summary>
		/// Start the loop of narrowing domains of user variables.
		/// </summary>
		internal void SearchForValidValues()
		{
			_startingBaseline = _model._changes.Count;
			while (!_model.AbortDomainNarrowing && !_model.CheckAbort())
			{
				InnerSearch();
				while (!_model.AbortDomainNarrowing && !IsFixChangedUserVar() && !_model.CheckAbort())
				{
					_pauseAutoReset.WaitOne();
				}
			}
			FullBacktrack();
		}

		private IEnumerable<int> PickNextUserVar()
		{
			for (int i = _fixedUserVarCount; i < _userVarCount; i++)
			{
				yield return i;
			}
		}

		private void InnerSearch()
		{
			_depth = _fixedUserVarCount;
			foreach (int item in PickNextUserVar())
			{
				swap(_fixedUserVarCount, item);
				CspUserVariable cspUserVariable = _searchVars[_fixedUserVarCount] as CspUserVariable;
				int count = _model._changes.Count;
				int num = cspUserVariable.SetWorkingDomain();
				CspSolverTerm conflict;
				bool flag = !_propagator.Propagate(out conflict);
				if (num != 0 && !flag)
				{
					bool flag2 = false;
					while (!flag2)
					{
						if (_model.AbortDomainNarrowing || _fUserVarFixing || _model.CheckAbort())
						{
							return;
						}
						if (!NextDecisionVariable())
						{
							DecideFeasibleValues(item + 1);
							BacktrackToTopFreeUserVar();
							RaiseUserVarUpdate();
						}
						do
						{
							conflict = null;
							if (_model.AbortDomainNarrowing || _fUserVarFixing || _model.CheckAbort())
							{
								return;
							}
							if (!TryNextValue())
							{
								flag2 = true;
								break;
							}
						}
						while (!_propagator.Propagate(out conflict));
					}
				}
				_model.Backtrack(count);
				cspUserVariable.SetFinished();
				if (cspUserVariable.TestedValues.Count <= 0)
				{
					ClearFeasibleValues();
					for (int i = 0; i < _userVarCount; i++)
					{
						CspUserVariable cspUserVariable2 = _searchVars[i] as CspUserVariable;
						cspUserVariable2.SetFinished();
						cspUserVariable2.EnableEvent();
						cspUserVariable2.Update();
					}
					break;
				}
				RaiseUserVarUpdate();
			}
		}

		private void swap(int i, int j)
		{
			if (i != j)
			{
				CspSolverTerm cspSolverTerm = _searchVars[i];
				_searchVars[i] = _searchVars[j];
				_searchVars[j] = cspSolverTerm;
			}
		}

		private void RaiseUserVarUpdate()
		{
			for (int i = 0; i < _userVarCount; i++)
			{
				CspUserVariable cspUserVariable = _rgUserVars[i] as CspUserVariable;
				cspUserVariable.Update();
			}
		}

		private void DecideFeasibleValues(int level)
		{
			CspUserVariable cspUserVariable = _searchVars[_fixedUserVarCount] as CspUserVariable;
			cspUserVariable.DecideValue(cspUserVariable.FiniteValue.First);
			for (int i = level; i < _userVarCount; i++)
			{
				cspUserVariable = _searchVars[i] as CspUserVariable;
				cspUserVariable.DecideValue(cspUserVariable.FiniteValue.First);
			}
		}

		internal override bool TryNextValue()
		{
			while (_fixedUserVarCount < _decisions.Count && !_decisions.Peek().TryNextValue())
			{
				_depth = _decisions.Pop().OldDepth;
			}
			return _fixedUserVarCount < _decisions.Count;
		}
	}
}
