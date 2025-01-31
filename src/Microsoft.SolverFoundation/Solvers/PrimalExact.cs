using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class PrimalExact : AlgorithmRational
	{
		private PivotStrategyExact _ps;

		public PrimalExact(SimplexTask thd)
			: base(thd)
		{
		}

		public override bool RunSimplex(int cpivMax, bool fStopAtNextGoal, OptimalGoalValues ogvMin, out LinearResult res)
		{
			InitBounds(ogvMin, out var igiCur, out var fStrict);
			while (true)
			{
				try
				{
					EnsureExactVars();
					if (PrePhaseOne())
					{
						int cpiv = _cpiv1;
						bool flag;
						try
						{
							_fValidReducedCosts = false;
							flag = RepeatPivots(1, cpivMax, out res);
						}
						finally
						{
							RestoreBoundsAfterPhaseOne(igiCur);
							_fValidReducedCosts = false;
						}
						if (!flag)
						{
							return false;
						}
						cpivMax -= _cpiv1 - cpiv;
						switch (res)
						{
						default:
							return true;
						case LinearResult.UnboundedPrimal:
							res = LinearResult.InfeasiblePrimal;
							return true;
						case LinearResult.Optimal:
							break;
						}
						if (!IsPrimalFeasible())
						{
							res = LinearResult.InfeasiblePrimal;
							return true;
						}
					}
					while (igiCur < _mod.GoalCount)
					{
						_thd.CheckDone();
						if (cpivMax <= 0)
						{
							res = LinearResult.Interrupted;
							return false;
						}
						int goalVar = _mod.GetGoalVar(igiCur);
						bool flag2 = _mod.IsGoalMinimize(igiCur);
						Rational rational = (flag2 ? 1 : (-1));
						if (_vecCost.EntryCount != 1 || _vecCost.GetCoef(goalVar) != rational)
						{
							_vecCost.Clear();
							_vecCost.SetCoefNonZero(goalVar, rational);
							_fValidReducedCosts = false;
						}
						if (!RepeatPivots(2, cpivMax, out res))
						{
							return false;
						}
						if (res != LinearResult.Optimal)
						{
							return true;
						}
						bool flag3 = true;
						Rational varValue = GetVarValue(goalVar);
						Rational num = varValue;
						if (!flag2)
						{
							Rational.Negate(ref num);
						}
						if (!fStrict)
						{
							int num2 = num.CompareTo(ogvMin[igiCur]);
							if (num2 > 0)
							{
								flag3 = false;
							}
							else if (num2 < 0)
							{
								fStrict = true;
							}
						}
						if (!flag3)
						{
							res = LinearResult.InfeasiblePrimal;
							return true;
						}
						_thd.OptimalGoalValues[igiCur] = num;
						ref Rational reference = ref _mpvarnumUpper[goalVar];
						reference = (_mpvarnumLower[goalVar] = varValue);
						igiCur++;
						if (fStopAtNextGoal)
						{
							break;
						}
					}
					res = LinearResult.Optimal;
					return igiCur >= _mod.GoalCount;
				}
				catch (SimplexFactorException)
				{
					_log.LogEvent(6, Resources.RepairedSingularBasis, _thd.PivotCount);
				}
			}
		}

		protected bool PrePhaseOne()
		{
			_thd.CheckDone();
			bool flag = false;
			for (int i = 0; i < _mod.RowLim; i++)
			{
				int basicVar = _bas.GetBasicVar(i);
				Rational rational = _rgnumBasic[i];
				bool flag2 = rational < _mpvarnumLower[basicVar];
				bool flag3 = !flag2 && rational > _mpvarnumUpper[basicVar];
				if (flag2 || flag3)
				{
					if (!flag)
					{
						_vecCost.Clear();
						flag = true;
					}
					if (flag2)
					{
						ref Rational reference = ref _mpvarnumUpper[basicVar];
						reference = _mpvarnumLower[basicVar];
						ref Rational reference2 = ref _mpvarnumLower[basicVar];
						reference2 = Rational.NegativeInfinity;
						_vecCost.SetCoefNonZero(basicVar, -1);
					}
					else
					{
						ref Rational reference3 = ref _mpvarnumLower[basicVar];
						reference3 = _mpvarnumUpper[basicVar];
						ref Rational reference4 = ref _mpvarnumUpper[basicVar];
						reference4 = Rational.PositiveInfinity;
						_vecCost.SetCoefNonZero(basicVar, 1);
					}
				}
			}
			return flag;
		}

		protected void RestoreBoundsAfterPhaseOne(int igiCur)
		{
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecCost);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				ref Rational reference = ref _mpvarnumUpper[rc];
				reference = _thd.BoundManager.GetUpperBound(rc);
				ref Rational reference2 = ref _mpvarnumLower[rc];
				reference2 = _thd.BoundManager.GetLowerBound(rc);
				iter.Advance();
			}
			_vecCost.Clear();
			InitGoalBounds(igiCur);
		}

		/// <summary>
		/// This repeatedly pivots.
		/// </summary>
		protected bool RepeatPivots(int phase, int cpivMax, out LinearResult res)
		{
			_thd.CheckDone();
			_thd.ResetIpiMin();
			if (_ps == null || _ps.Model != _mod)
			{
				SimplexCosting costingRequested = _thd.CostingRequested;
				if (costingRequested != SimplexCosting.BestReducedCost)
				{
					_thd.CostingUsedExact = SimplexCosting.SteepestEdge;
					if (_thd.Solver.IsSpecialOrderedSet && _thd._fSOSFastPath)
					{
						_ps = new FullWeightedCostExactSOS2(_thd, this);
					}
					else
					{
						_ps = new FullWeightedCostApprox(_thd, this);
					}
				}
				else
				{
					_thd.CostingUsedExact = SimplexCosting.BestReducedCost;
					_ps = new FullReducedCostApprox(_thd, this);
				}
				_fValidReducedCosts = false;
			}
			if (!_fValidReducedCosts)
			{
				_ps.Init();
				_fValidReducedCosts = true;
			}
			for (int i = 0; i <= cpivMax; i++)
			{
				_thd.CheckDone();
				if (!_thd.Params.NotifyFindNext(_thd.Tid, _thd, fDouble: false))
				{
					res = LinearResult.Interrupted;
					return true;
				}
				if (!_ps.FindNext())
				{
					res = LinearResult.Optimal;
					return true;
				}
				_thd.CheckDone();
				if (!_thd.Params.NotifyStartPivot(_thd.Tid, _thd, _ps))
				{
					res = LinearResult.Interrupted;
					return true;
				}
				if (_ps.IvarLeave < 0)
				{
					if (_ps.Scale.IsPositiveInfinity)
					{
						res = LinearResult.UnboundedPrimal;
						return true;
					}
					if (phase == 1)
					{
						_cpiv1++;
					}
					else
					{
						_cpiv2++;
					}
					_bas.MinorPivot(_ps.VarEnter, 2 + (3 - _ps.VvkEnter));
					_thd.RecordPivot(_ps, _bas.GetVvk(_ps.VarLeave));
					_thd.CheckDone();
					AlgorithmRational.AddToScale(_rgnumBasic, _mod.RowLim, _ps.Sign * _ps.Scale, _ps.Delta);
					_bas.IsExactNonBasicChanged = false;
					_thd.CheckDone();
					_ps.Commit(0);
				}
				else
				{
					if (phase == 1)
					{
						_cpiv1++;
					}
					else
					{
						_cpiv2++;
					}
					if (_ps.Scale.IsZero)
					{
						_cpivDegen++;
					}
					if (!DoPivot(phase))
					{
						res = LinearResult.Interrupted;
						return true;
					}
				}
				if (!_thd.Params.NotifyEndPivot(_thd.Tid, _thd, _ps))
				{
					res = LinearResult.Interrupted;
					return true;
				}
			}
			res = LinearResult.Interrupted;
			return false;
		}

		protected bool DoPivot(int phase)
		{
			_thd.CheckDone();
			Rational dnumLeaveCost = 0;
			SimplexVarValKind simplexVarValKind = _ps.VvkLeave;
			if (phase == 1)
			{
				Rational rational = (dnumLeaveCost = _vecCost.GetCoef(_ps.VarLeave));
				if (!rational.IsZero)
				{
					RestoreBounds(_ps.VarLeave);
					ZeroCost(_ps.VarLeave);
					simplexVarValKind = 2 + (3 - simplexVarValKind);
				}
			}
			if (_mpvarnumLower[_ps.VarLeave] == _mpvarnumUpper[_ps.VarLeave])
			{
				simplexVarValKind = SimplexVarValKind.Fixed;
			}
			_thd.CheckDone();
			AlgorithmRational.AddToScale(_rgnumBasic, _mod.RowLim, _ps.Sign * _ps.Scale, _ps.Delta);
			ref Rational reference = ref _rgnumBasic[_ps.IvarLeave];
			reference = GetVarBound(_ps.VarEnter, _ps.VvkEnter) - _ps.Sign * _ps.Scale;
			_thd.CheckDone();
			FactorResultFlags factorResultFlags = _bas.MajorPivot(_ps.VarEnter, _ps.IvarLeave, _ps.VarLeave, simplexVarValKind, _ps.Delta, _rgnumBasic);
			_thd.RecordPivot(_ps, simplexVarValKind);
			if (factorResultFlags != 0)
			{
				PostFactor(factorResultFlags);
				if ((factorResultFlags & FactorResultFlags.Abort) != 0)
				{
					return false;
				}
				if ((factorResultFlags & FactorResultFlags.Substituted) != 0)
				{
					throw new SimplexFactorException(factorResultFlags);
				}
			}
			_thd.CheckDone();
			_ps.Commit(dnumLeaveCost);
			if (_log.ShouldLog(0))
			{
				Rational[] rgnum = _thd.GetTempArrayExact(_mod.RowLim, fClear: false);
				ComputeBasicValues(rgnum);
				_thd.Solver.VerifySame(_rgnumBasic, rgnum, _mod.RowLim, 0, Resources.WrongBasicVariableValue012);
				_thd.ReleaseTempArray(ref rgnum);
			}
			return true;
		}

		protected void ZeroCost(int var)
		{
			_vecCost.RemoveCoef(var);
			_thd.ResetIpiMin();
		}

		protected void RestoreBounds(int var)
		{
			Rational num = _thd.BoundManager.GetLowerBound(var);
			Rational rational = _thd.BoundManager.GetUpperBound(var);
			if (num.IsNegativeInfinity && rational.IsPositiveInfinity && _mod.FindGoal(var, out var igi))
			{
				num = _thd.OptimalGoalValues[igi];
				if (!_mod.IsGoalMinimize(igi))
				{
					Rational.Negate(ref num);
				}
				rational = num;
			}
			_mpvarnumLower[var] = num;
			_mpvarnumUpper[var] = rational;
		}
	}
}
