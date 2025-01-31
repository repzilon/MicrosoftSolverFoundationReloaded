#define TRACE
using System;
using System.Diagnostics;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class PrimalDouble : AlgorithmDouble
	{
		/// <summary> A threshold for enabling more costly anti-degeneracy algorithms
		/// </summary>
		private const int kDegenerateThreshold = 1000;

		private PivotStrategyDbl _ps;

		private bool[] _mpvarfPerturbed;

		private Random _randPerturb;

		/// <summary> A measure of consecutive recent degenerate leaving variables
		/// </summary>
		private int _recentDegeneracyMetric;

		/// <summary> Used to modify degeneracy metric to allow varying thresholds.
		/// </summary>
		private int _degeneracyFloor;

		/// <summary> Test if it is time to apply priority to removing fixed variables from the basis.
		/// </summary>
		public bool RemoveFixedLeaving => 15000 < 16 * _recentDegeneracyMetric;

		public PrimalDouble(SimplexTask thd)
			: base(thd)
		{
		}

		public void SetBasicValue(int ivar, double num)
		{
			_rgnumBasic[ivar] = num;
		}

		protected override LinearResult RunSimplexCore(int igiCur, bool restart)
		{
			LogSource.SimplexTracer.TraceEvent(TraceEventType.Start, 0, "enter RunSimplexCore");
			try
			{
				bool flag = true;
				if (_fShiftBounds)
				{
					_mpvarfPerturbed = new bool[_mod.VarLim];
					_randPerturb = new Random(4660);
				}
				while (true)
				{
					if (flag)
					{
						FactorResultFlags factorResultFlags = FactorBasis();
						if ((factorResultFlags & FactorResultFlags.Abort) != 0)
						{
							break;
						}
					}
					ComputeBasicValues();
					try
					{
						if (PrePhaseOne())
						{
							LinearResult linearResult;
							try
							{
								linearResult = RepeatPivots(1);
							}
							finally
							{
								RemoveShifts();
								RestoreBoundsAfterPhaseOne(igiCur);
							}
							switch (linearResult)
							{
							default:
								return linearResult;
							case LinearResult.UnboundedPrimal:
								LogSource.SimplexTracer.TraceData(TraceEventType.Information, 0, Resources.InfeasiblePrimalDueToUnboundedPrimal);
								return LinearResult.InfeasiblePrimal;
							case LinearResult.Optimal:
								break;
							}
							LogSource.SimplexTracer.TraceData(TraceEventType.Information, 0, Resources.Phase1Optimal);
							for (int i = 0; i < _mod.RowLim; i++)
							{
								int basicVar = _bas.GetBasicVar(i);
								double num = _rgnumBasic[i];
								if (!(_mpvarnumLower[basicVar] - _numVarEpsilon <= num) || !(num <= _mpvarnumUpper[basicVar] + _numVarEpsilon))
								{
									return LinearResult.InfeasiblePrimal;
								}
							}
						}
						if (igiCur >= _mod.GoalCount)
						{
							return LinearResult.Optimal;
						}
						_vecCost.Clear();
						_vecCost.SetCoefNonZero(_mod.GetGoalVar(igiCur), _mod.IsGoalMinimize(igiCur) ? 1 : (-1));
						try
						{
							return RepeatPivots(2);
						}
						finally
						{
							RemoveShifts();
						}
					}
					catch (SimplexFactorException ex)
					{
						if ((ex.Flags & FactorResultFlags.Abort) != 0)
						{
							return LinearResult.Invalid;
						}
						_log.LogEvent(6, Resources.RepairedSingularBasis, _thd.PivotCount);
						LogSource.SimplexTracer.TraceData(TraceEventType.Error, 0, Resources.RepairedSingularBasis, _thd.PivotCount);
						flag = false;
					}
					catch (InfeasibleException ex2)
					{
						_log.LogEvent(6, Resources.WanderedToInfeasible1, _thd.PivotCount, ex2.Message);
						LogSource.SimplexTracer.TraceEvent(TraceEventType.Error, 0, Resources.WanderedToInfeasible1, _thd.PivotCount, ex2.Message);
						flag = _bas.GetDoubleFactorization().EtaCount > 0;
						if (_thd.InfeasibleCount == 0)
						{
							return LinearResult.InfeasiblePrimal;
						}
					}
				}
				return LinearResult.Invalid;
			}
			finally
			{
				LogSource.SimplexTracer.TraceEvent(TraceEventType.Stop, 0, "exit RunSimplexCore");
			}
		}

		protected bool PrePhaseOne()
		{
			_thd.CheckDone();
			_vecCost.Clear();
			for (int i = 0; i < _mod.RowLim; i++)
			{
				int basicVar = _bas.GetBasicVar(i);
				double num = _rgnumBasic[i];
				if (num < _mpvarnumLower[basicVar])
				{
					if (num < _mpvarnumLower[basicVar] - _numVarEpsilon)
					{
						_mpvarnumUpper[basicVar] = _mpvarnumLower[basicVar];
						_mpvarnumLower[basicVar] = double.NegativeInfinity;
						_vecCost.SetCoefNonZero(basicVar, -1.0);
					}
					else
					{
						_rgnumBasic[i] = _mpvarnumLower[basicVar];
					}
				}
				else if (num > _mpvarnumUpper[basicVar])
				{
					if (num > _mpvarnumUpper[basicVar] + _numVarEpsilon)
					{
						_mpvarnumLower[basicVar] = _mpvarnumUpper[basicVar];
						_mpvarnumUpper[basicVar] = double.PositiveInfinity;
						_vecCost.SetCoefNonZero(basicVar, 1.0);
					}
					else
					{
						_rgnumBasic[i] = _mpvarnumUpper[basicVar];
					}
				}
			}
			return _vecCost.EntryCount > 0;
		}

		public bool Unshift(int var)
		{
			if (_fShiftBounds && _mpvarfPerturbed[var])
			{
				_mpvarnumUpper[var] = _thd.BoundManager.GetUpperBoundDbl(var);
				_mpvarnumLower[var] = _thd.BoundManager.GetLowerBoundDbl(var);
				_mpvarfPerturbed[var] = false;
				return true;
			}
			return false;
		}

		protected bool TryUnshift(int var, double num)
		{
			bool flag = false;
			if (_fShiftBounds && _mpvarfPerturbed[var])
			{
				double upperBoundDbl = _thd.BoundManager.GetUpperBoundDbl(var);
				if (upperBoundDbl < _mpvarnumUpper[var])
				{
					if (num <= upperBoundDbl)
					{
						_mpvarnumUpper[var] = upperBoundDbl;
					}
					else
					{
						flag = true;
					}
				}
				double lowerBoundDbl = _thd.BoundManager.GetLowerBoundDbl(var);
				if (_mpvarnumLower[var] < lowerBoundDbl)
				{
					if (lowerBoundDbl <= num)
					{
						_mpvarnumLower[var] = lowerBoundDbl;
					}
					else
					{
						flag = true;
					}
				}
				_mpvarfPerturbed[var] = flag;
			}
			return !flag;
		}

		protected void RemoveShifts()
		{
			if (_fShiftBounds)
			{
				for (int i = 0; i < _mod.VarLim; i++)
				{
					Unshift(i);
				}
			}
		}

		protected void RestoreBoundsAfterPhaseOne(int igiCur)
		{
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecCost);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				_mpvarnumUpper[rc] = _thd.BoundManager.GetUpperBoundDbl(rc);
				_mpvarnumLower[rc] = _thd.BoundManager.GetLowerBoundDbl(rc);
				if (_fShiftBounds)
				{
					_mpvarfPerturbed[rc] = false;
				}
				iter.Advance();
			}
			_vecCost.Clear();
			InitGoalBounds(igiCur);
		}

		protected bool CleanBasicValues()
		{
			_thd.CheckDone();
			for (int i = 0; i < _mod.RowLim; i++)
			{
				int basicVar = _bas.GetBasicVar(i);
				double num = _rgnumBasic[i];
				if (num < _mpvarnumLower[basicVar])
				{
					double num2 = _mpvarnumLower[basicVar] - num;
					if (Math.Abs(num2) > _numVarEpsilon && Math.Abs(num2 / _mpvarnumLower[basicVar]) > _numVarEpsilon)
					{
						LogSource.SimplexTracer.TraceInformation("varName = {0}, num = {1}, lower bound = {2}, epsilon = {3}", _thd.Solver.GetKeyFromIndex(_mod.GetVid(basicVar)), num, _mpvarnumLower[basicVar], _numVarEpsilon);
						return false;
					}
					_rgnumBasic[i] = _mpvarnumLower[basicVar];
				}
				else if (num > _mpvarnumUpper[basicVar])
				{
					double num3 = num - _mpvarnumUpper[basicVar];
					if (num3 > _numVarEpsilon && Math.Abs(num3 / _mpvarnumUpper[basicVar]) > _numVarEpsilon)
					{
						LogSource.SimplexTracer.TraceInformation("varName = {0}, num = {1}, upper bound = {2}, epsilon = {3}", _thd.Solver.GetKeyFromIndex(_mod.GetVid(basicVar)), num, _mpvarnumUpper[basicVar], _numVarEpsilon);
						return false;
					}
					_rgnumBasic[i] = _mpvarnumUpper[basicVar];
				}
				else
				{
					TryUnshift(basicVar, num);
				}
			}
			return true;
		}

		/// <summary>
		/// Attempts to reduce error in the system. Typically called by the
		/// PivotStrategy. This does NOT update the PivotStrategy!
		/// </summary>
		public bool ReduceError(PivotStrategy<double> ps)
		{
			if (_bas.GetDoubleFactorization().EtaCount == 0)
			{
				return false;
			}
			_log.LogEvent(5, Resources.ReducingError, _thd.PivotCount);
			FactorResultFlags factorResultFlags = _bas.RefactorDoubleBasis(_rgnumBasic);
			if ((factorResultFlags & FactorResultFlags.Completed) != 0)
			{
				PostFactor(factorResultFlags);
			}
			if ((factorResultFlags & FactorResultFlags.Substituted) != 0 || (factorResultFlags & FactorResultFlags.Completed) == 0)
			{
				throw new SimplexFactorException(factorResultFlags);
			}
			ComputeBasicValues();
			if (!CleanBasicValues())
			{
				throw new InfeasibleException("ReduceError");
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
			double num = _thd.BoundManager.GetLowerBoundDbl(var);
			double num2 = _thd.BoundManager.GetUpperBoundDbl(var);
			if (double.IsNegativeInfinity(num) && double.IsPositiveInfinity(num2) && _mod.FindGoal(var, out var igi))
			{
				Rational num3 = _thd.OptimalGoalValues[igi];
				if (!_mod.IsGoalMinimize(igi))
				{
					Rational.Negate(ref num3);
				}
				num = (num2 = _mod.MapValueFromExactToDouble(var, num3));
			}
			if (_fShiftBounds && _mpvarfPerturbed[var])
			{
				_mpvarfPerturbed[var] = false;
			}
			_mpvarnumLower[var] = num;
			_mpvarnumUpper[var] = num2;
		}

		/// <summary>
		/// This repeatedly pivots.
		/// </summary>
		protected LinearResult RepeatPivots(int phase)
		{
			_thd.CheckDone();
			LogSource.SimplexTracer.TraceEvent(TraceEventType.Start, 0, "enter RepeatPivots {0}", phase);
			_thd.ResetIpiMin();
			if (phase == 1)
			{
				Statics.Swap(ref _cpiv1, ref _cpiv2);
			}
			try
			{
				if (_ps == null || _ps.Model != _mod)
				{
					switch (_thd.CostingRequested)
					{
					default:
						_thd.CostingUsedDouble = SimplexCosting.SteepestEdge;
						if (_thd.Solver.IsSpecialOrderedSet && _thd._fSOSFastPath)
						{
							_ps = new FullWeightedCostDblSOS(_thd, this);
						}
						else
						{
							_ps = new FullWeightedCostDbl(_thd, this);
						}
						break;
					case SimplexCosting.BestReducedCost:
						_thd.CostingUsedDouble = SimplexCosting.BestReducedCost;
						_ps = new FullReducedCostDbl(_thd, this);
						break;
					case SimplexCosting.Partial:
						_thd.CostingUsedDouble = SimplexCosting.Partial;
						if (_thd.Solver.IsSpecialOrderedSet && _thd._fSOSFastPath)
						{
							_ps = new PartialPricingDblSOS(_thd, this, 50);
						}
						else
						{
							_ps = new PartialPricingDbl(_thd, this, 50);
						}
						break;
					case SimplexCosting.NewPartial:
						_thd.CostingUsedDouble = SimplexCosting.NewPartial;
						_ps = new NewPartialPricingDbl(_thd, this, 50);
						break;
					}
				}
				_ps.Init();
				do
				{
					_thd.CheckDone();
					if (!_thd.Params.NotifyFindNext(_thd.Tid, _thd, fDouble: true))
					{
						LogSource.SimplexTracer.TraceEvent(TraceEventType.Stop, 0, Resources.InterruptedExitRepeatPivots0, phase);
						return LinearResult.Interrupted;
					}
					if (!_ps.FindNext())
					{
						LogSource.SimplexTracer.TraceEvent(TraceEventType.Stop, 0, Resources.OptimalExitRepeatPivots0, phase);
						return LinearResult.Optimal;
					}
					NotifyLeaving(_ps.Scale);
					_thd.CheckDone();
					if (!_thd.Params.NotifyStartPivot(_thd.Tid, _thd, _ps))
					{
						return LinearResult.Interrupted;
					}
					if (_ps.IvarLeave < 0)
					{
						if (double.IsPositiveInfinity(_ps.Scale))
						{
							return LinearResult.UnboundedPrimal;
						}
						_cpiv2++;
						_bas.MinorPivot(_ps.VarEnter, 2 + (3 - _ps.VvkEnter));
						_fValidValues = false;
						_thd.RecordPivot(_ps, _bas.GetVvk(_ps.VarLeave));
						if (!UpdateBasicValues(_ps))
						{
							throw new InfeasibleException("UpdateBasicValues");
						}
						_fValidValues = true;
						_bas.IsDoubleNonBasicChanged = false;
						_thd.CheckDone();
						_ps.Commit(0.0);
					}
					else
					{
						_cpiv2++;
						if (_ps.Scale <= _numCostEpsilon / 100.0)
						{
							_cpivDegen++;
						}
						if (!DoPivot(_ps, phase))
						{
							LogSource.SimplexTracer.TraceEvent(TraceEventType.Stop, 0, Resources.InterruptedExitRepeatPivots0, phase);
							return LinearResult.Interrupted;
						}
					}
				}
				while (_thd.Params.NotifyEndPivot(_thd.Tid, _thd, _ps));
				LogSource.SimplexTracer.TraceEvent(TraceEventType.Stop, 0, Resources.InterruptedExitRepeatPivots0, phase);
				return LinearResult.Interrupted;
			}
			finally
			{
				if (phase == 1)
				{
					Statics.Swap(ref _cpiv1, ref _cpiv2);
				}
			}
		}

		protected bool DoPivot(PivotStrategyDbl ps, int phase)
		{
			_thd.CheckDone();
			double dnumLeaveCost = 0.0;
			SimplexVarValKind simplexVarValKind = ps.VvkLeave;
			if (phase == 1 && (dnumLeaveCost = _vecCost.GetCoef(ps.VarLeave)) != 0.0)
			{
				RestoreBounds(ps.VarLeave);
				ZeroCost(ps.VarLeave);
				simplexVarValKind = ((simplexVarValKind != SimplexVarValKind.Lower) ? SimplexVarValKind.Lower : SimplexVarValKind.Upper);
			}
			if (_mpvarnumLower[ps.VarLeave] == _mpvarnumUpper[ps.VarLeave])
			{
				simplexVarValKind = SimplexVarValKind.Fixed;
			}
			_thd.CheckDone();
			FactorResultFlags factorResultFlags = _bas.MajorPivot(ps.VarEnter, ps.IvarLeave, ps.VarLeave, simplexVarValKind, ps.Delta, _rgnumBasic);
			_fValidValues = false;
			if (factorResultFlags != 0)
			{
				if ((factorResultFlags & FactorResultFlags.Permuted) != 0)
				{
					ps.IvarLeave = _bas.GetBasisSlot(ps.VarEnter);
				}
				PostFactor(factorResultFlags);
				_thd.RecordPivot(ps, simplexVarValKind);
				if ((factorResultFlags & FactorResultFlags.Abort) != 0)
				{
					return false;
				}
				if ((factorResultFlags & FactorResultFlags.Substituted) != 0)
				{
					throw new SimplexFactorException(factorResultFlags);
				}
				ComputeBasicValues();
				if (!CleanBasicValues())
				{
					throw new InfeasibleException("CleanBasicValues");
				}
			}
			else
			{
				_thd.RecordPivot(ps, simplexVarValKind);
				_rgnumBasic[ps.IvarLeave] = GetVarBound(ps.VarEnter, ps.VvkEnter) - (double)ps.Sign * ps.Scale;
				if (!UpdateBasicValues(ps))
				{
					throw new InfeasibleException("UpdateBasicValues");
				}
				_fValidValues = true;
			}
			_thd.CheckDone();
			ps.Commit(dnumLeaveCost);
			if (_log.ShouldLog(0))
			{
				double[] rgnum = _thd.GetTempArrayDbl(_mod.RowLim, fClear: false);
				ComputeBasicValues(rgnum);
				_thd.Solver.VerifyCloseRel(_rgnumBasic, rgnum, _mod.RowLim, 8.0 * _numVarEpsilon, 0, Resources.LargeErrorInBasicValue0);
				_thd.ReleaseTempArray(ref rgnum);
			}
			return true;
		}

		/// <summary> This recomputes basic variable values after a pivot.  Results in _rgnumBasic.
		///           Parameter ps is the most recent pivot.
		/// </summary>
		protected bool UpdateBasicValues(PivotStrategyDbl ps)
		{
			_thd.CheckDone();
			double num = (double)ps.Sign * ps.Scale;
			if (num == 0.0)
			{
				return true;
			}
			Vector<double>.Iter iter = new Vector<double>.Iter(ps.Delta);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				if (rc != ps.IvarLeave)
				{
					if (SlamToZero(_rgnumBasic[rc], _rgnumBasic[rc] += num * iter.Value))
					{
						_rgnumBasic[rc] = 0.0;
					}
					int basicVar = _bas.GetBasicVar(rc);
					if (_rgnumBasic[rc] < _mpvarnumLower[basicVar])
					{
						double num2 = _mpvarnumLower[basicVar] - _rgnumBasic[rc];
						if (num2 > _numVarEpsilon && Math.Abs(num2 / _mpvarnumLower[basicVar]) > _numVarEpsilon)
						{
							return false;
						}
						_rgnumBasic[rc] = _mpvarnumLower[basicVar];
					}
					else if (_rgnumBasic[rc] > _mpvarnumUpper[basicVar])
					{
						double num3 = _rgnumBasic[rc] - _mpvarnumUpper[basicVar];
						if (num3 > _numVarEpsilon && Math.Abs(num3 / _mpvarnumUpper[basicVar]) > _numVarEpsilon)
						{
							return false;
						}
						_rgnumBasic[rc] = _mpvarnumUpper[basicVar];
					}
					else if (_fShiftBounds)
					{
						TryUnshift(basicVar, _rgnumBasic[rc]);
					}
				}
				iter.Advance();
			}
			return true;
		}

		/// <summary> Note each time we successfully find a non-degenerate base.
		/// </summary>
		public void NotifyLeaving(double scale)
		{
			if (_fShiftBounds)
			{
				if (0.0 < scale)
				{
					_recentDegeneracyMetric = _degeneracyFloor;
				}
				else
				{
					_recentDegeneracyMetric++;
				}
			}
		}

		/// <summary> Test if leaving-variable degeneracy avoidance algorithms should be used.
		/// </summary>
		public bool ApplyAntidegeneracyLeaving()
		{
			if (1000 < _recentDegeneracyMetric)
			{
				_degeneracyFloor = 500;
				return true;
			}
			return false;
		}

		public double NextPerturbation()
		{
			return _randPerturb.NextDouble();
		}

		public void ShiftLowerBound(int var, double numBound)
		{
			_mpvarnumLower[var] = numBound;
			_mpvarfPerturbed[var] = true;
		}

		public void ShiftUpperBound(int var, double numBound)
		{
			_mpvarnumUpper[var] = numBound;
			_mpvarfPerturbed[var] = true;
		}
	}
}
