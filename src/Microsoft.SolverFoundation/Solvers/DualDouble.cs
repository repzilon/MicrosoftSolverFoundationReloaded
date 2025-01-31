#define TRACE
using System;
using System.Diagnostics;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class DualDouble : AlgorithmDouble, ISimplexPivotInformation
	{
		private bool _fSteepestEdge;

		private VectorDouble _vecReducedCost;

		private VectorDouble _vecDual;

		private VectorDouble _vecRow;

		private VectorDouble _vecWeightUpdate;

		private double[] _mpvarnumWeights;

		private int _sign;

		private double _numInfeas;

		private double _numCost;

		private int _varLeave;

		private int _ivarLeave;

		private SimplexVarValKind _vvkLeave;

		private double _numScale;

		private int _varEnter;

		private SimplexVarValKind _vvkEnter;

		private VectorDouble _vecDelta;

		public virtual int VarEnter => _varEnter;

		public virtual SimplexVarValKind VvkEnter => _vvkEnter;

		public virtual int VarLeave => _varLeave;

		public virtual int IvarLeave => _ivarLeave;

		public virtual SimplexVarValKind VvkLeave => _vvkLeave;

		public virtual double Scale => _numScale;

		public virtual int Sign => _sign;

		public virtual double Determinant => _vecDelta.GetCoef(_ivarLeave);

		bool ISimplexPivotInformation.IsDouble => true;

		Rational ISimplexPivotInformation.Scale => _numScale;

		Rational ISimplexPivotInformation.Determinant => _vecDelta.GetCoef(_ivarLeave);

		public virtual double ApproxCost => 0.0;

		private double GetUpperBoundPhaseOne(int var)
		{
			if (NumberUtils.IsFinite(_mpvarnumUpper[var]))
			{
				return 0.0;
			}
			if (NumberUtils.IsFinite(_mpvarnumLower[var]))
			{
				return 1.0;
			}
			return 1000.0;
		}

		private double GetLowerBoundPhaseOne(int var)
		{
			if (NumberUtils.IsFinite(_mpvarnumLower[var]))
			{
				return 0.0;
			}
			if (NumberUtils.IsFinite(_mpvarnumUpper[var]))
			{
				return -1.0;
			}
			return -1000.0;
		}

		private double GetVarBoundPhaseOne(int var, SimplexVarValKind vvk)
		{
			switch (vvk)
			{
			default:
				return 0.0;
			case SimplexVarValKind.Lower:
				return GetLowerBoundPhaseOne(var);
			case SimplexVarValKind.Upper:
				return GetUpperBoundPhaseOne(var);
			}
		}

		public DualDouble(SimplexTask thd)
			: base(thd)
		{
		}

		public override void Init(SimplexReducedModel mod, SimplexFactoredBasis bas)
		{
			base.Init(mod, bas);
			_vecReducedCost = new VectorDouble(_varLim);
			_vecDual = new VectorDouble(_rowLim);
			_vecRow = new VectorDouble(_varLim);
			_vecDelta = new VectorDouble(_rowLim);
			_vecWeightUpdate = new VectorDouble(_rowLim);
			_mpvarnumWeights = new double[_varLim];
		}

		protected override LinearResult RunSimplexCore(int igiCur, bool restart)
		{
			LogSource.SimplexTracer.TraceEvent(TraceEventType.Start, 0, "enter RunSimplexCore");
			try
			{
				bool flag = true;
				_fSteepestEdge = _thd.CostingRequested != SimplexCosting.BestReducedCost;
				_thd.CostingUsedDouble = (_fSteepestEdge ? SimplexCosting.SteepestEdge : SimplexCosting.BestReducedCost);
				if (igiCur >= _mod.GoalCount)
				{
					_vecCost.Clear();
					_vecReducedCost.Clear();
					FactorResultFlags factorResultFlags = FactorBasis();
					if ((factorResultFlags & FactorResultFlags.Abort) != 0)
					{
						return LinearResult.Invalid;
					}
					if (!restart && _fSteepestEdge)
					{
						InitWeights();
					}
					while (true)
					{
						ComputeBasicValues();
						try
						{
							return RepeatPivots(fPhaseOne: false);
						}
						catch (SimplexFactorException ex)
						{
							LogSource.SimplexTracer.TraceEvent(TraceEventType.Verbose, 0, Resources.RepairedSingularBasis, _thd.PivotCount);
							if ((ex.Flags & FactorResultFlags.Abort) != 0)
							{
								return LinearResult.Invalid;
							}
							_log.LogEvent(6, Resources.RepairedSingularBasis, _thd.PivotCount);
							flag = false;
						}
					}
				}
				while (true)
				{
					int goalVar = _mod.GetGoalVar(igiCur);
					double num = (_mod.IsGoalMinimize(igiCur) ? 1.0 : (-1.0));
					if (_vecCost.EntryCount != 1 || _vecCost.GetCoef(goalVar) != num)
					{
						_vecCost.Clear();
						_vecCost.SetCoefNonZero(goalVar, num);
					}
					if (flag)
					{
						FactorResultFlags factorResultFlags2 = FactorBasis();
						if ((factorResultFlags2 & FactorResultFlags.Abort) != 0)
						{
							break;
						}
						if (!restart && _fSteepestEdge)
						{
							InitWeights();
						}
					}
					ComputeReducedCosts();
					try
					{
						DualFeasibilityCorrection();
						LinearResult linearResult;
						if (QueryNeedPhaseOne())
						{
							ComputeBasicValuesPhaseOne();
							bool flag2;
							try
							{
								linearResult = RepeatPivots(fPhaseOne: true);
							}
							finally
							{
								flag2 = InitVarValuesPostPhaseOne();
							}
							switch (linearResult)
							{
							default:
								return linearResult;
							case LinearResult.UnboundedDual:
								return LinearResult.InfeasibleOrUnbounded;
							case LinearResult.Optimal:
								break;
							}
							if (!flag2)
							{
								return LinearResult.InfeasibleOrUnbounded;
							}
						}
						else
						{
							ComputeBasicValues();
						}
						_vecCost.Clear();
						_vecCost.SetCoefNonZero(_mod.GetGoalVar(igiCur), _mod.IsGoalMinimize(igiCur) ? 1 : (-1));
						linearResult = RepeatPivots(fPhaseOne: false);
						if ((linearResult == LinearResult.Feasible || linearResult == LinearResult.Optimal) && (!_fValidValues || _bas.IsDoubleNonBasicChanged))
						{
							ComputeBasicValues(_rgnumBasic);
							_fValidValues = true;
							_bas.IsDoubleNonBasicChanged = false;
						}
						return linearResult;
					}
					catch (SimplexFactorException ex2)
					{
						if ((ex2.Flags & FactorResultFlags.Abort) != 0)
						{
							return LinearResult.Invalid;
						}
						LogSource.SimplexTracer.TraceEvent(TraceEventType.Verbose, 0, Resources.RepairedSingularBasis, _thd.PivotCount);
						_log.LogEvent(6, Resources.RepairedSingularBasis, _thd.PivotCount);
						flag = false;
						if (_fSteepestEdge)
						{
							InitWeights();
						}
					}
					catch (InfeasibleException ex3)
					{
						LogSource.SimplexTracer.TraceEvent(TraceEventType.Verbose, 0, Resources.WanderedToDualInfeasible1, _thd.PivotCount, ex3.Message);
						_log.LogEvent(6, Resources.WanderedToDualInfeasible1, _thd.PivotCount, ex3.Message);
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

		protected bool ReduceError(bool fPhaseOne)
		{
			_thd.CheckDone();
			if (_bas.GetDoubleFactorization().EtaCount == 0)
			{
				return false;
			}
			FactorResultFlags factorResultFlags = _bas.RefactorDoubleBasis(_rgnumBasic);
			if ((factorResultFlags & FactorResultFlags.Completed) != 0)
			{
				PostFactor(factorResultFlags);
			}
			if ((factorResultFlags & FactorResultFlags.Substituted) != 0 || (factorResultFlags & FactorResultFlags.Completed) == 0)
			{
				throw new SimplexFactorException(factorResultFlags);
			}
			ComputeReducedCosts();
			DualFeasibilityCorrection();
			if (!fPhaseOne && QueryNeedPhaseOne())
			{
				throw new InfeasibleException("ReduceError");
			}
			return true;
		}

		protected void InitWeights()
		{
			_thd.CheckDone();
			int num = _rowLim;
			while (--num >= 0)
			{
				int basicVar = _bas.GetBasicVar(num);
				_vecDual.Clear();
				_vecDual.SetCoefNonZero(num, 1.0);
				_bas.InplaceSolveRow(_vecDual);
				_mpvarnumWeights[basicVar] = SimplexSolver.ComputeNorm2(_vecDual, -1);
				if (_mpvarnumWeights[basicVar] < 0.0001)
				{
					_mpvarnumWeights[basicVar] = 0.0001;
				}
			}
		}

		protected void ComputeBasicValuesPhaseOne()
		{
			ComputeBasicValuesPhaseOne(_rgnumBasic);
			_bas.IsDoubleNonBasicChanged = false;
		}

		/// <summary> This initializes variable values assuming _bas is set correctly
		/// and the basis is factored.
		/// </summary>
		protected void ComputeBasicValuesPhaseOne(double[] rgnumDst)
		{
			_thd.CheckDone();
			Array.Clear(rgnumDst, 0, _rowLim);
			for (int i = 0; i < _varLim; i++)
			{
				double num;
				switch (_bas.GetVvk(i))
				{
				case SimplexVarValKind.Lower:
					num = GetLowerBoundPhaseOne(i);
					break;
				case SimplexVarValKind.Upper:
					num = GetUpperBoundPhaseOne(i);
					break;
				default:
					continue;
				}
				if (num != 0.0)
				{
					CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mod.Matrix, i);
					while (colIter.IsValid)
					{
						rgnumDst[colIter.Row] -= num * colIter.Approx;
						colIter.Advance();
					}
				}
			}
			_thd.CheckDone();
			_bas.InplaceSolveCol(rgnumDst);
		}

		/// <summary> This assumes we've just completed phase one and _vecReducedCost is valid.
		/// </summary>
		protected bool InitVarValuesPostPhaseOne()
		{
			_thd.CheckDone();
			bool flag = false;
			int num = _varLim;
			while (--num >= 0)
			{
				switch (_bas.GetVvk(num))
				{
				case SimplexVarValKind.Lower:
					if (!NumberUtils.IsFinite(_mpvarnumLower[num]))
					{
						if (NumberUtils.IsFinite(_mpvarnumUpper[num]))
						{
							_bas.MinorPivot(num, SimplexVarValKind.Upper);
							flag = flag || _vecReducedCost.GetCoef(num) > _numCostEpsilon;
						}
						else
						{
							_bas.MinorPivot(num, SimplexVarValKind.Zero);
							flag = flag || Math.Abs(_vecReducedCost.GetCoef(num)) > _numCostEpsilon;
						}
					}
					break;
				case SimplexVarValKind.Upper:
					if (!NumberUtils.IsFinite(_mpvarnumUpper[num]))
					{
						if (NumberUtils.IsFinite(_mpvarnumLower[num]))
						{
							_bas.MinorPivot(num, SimplexVarValKind.Lower);
							flag = flag || _vecReducedCost.GetCoef(num) < 0.0 - _numCostEpsilon;
						}
						else
						{
							_bas.MinorPivot(num, SimplexVarValKind.Zero);
							flag = flag || Math.Abs(_vecReducedCost.GetCoef(num)) > _numCostEpsilon;
						}
					}
					break;
				}
			}
			ComputeBasicValues();
			return !flag;
		}

		protected bool DualFeasibilityCorrection()
		{
			_thd.CheckDone();
			bool flag = false;
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecReducedCost);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				double value = iter.Value;
				switch (_bas.GetVvk(rc))
				{
				case SimplexVarValKind.Lower:
					if (value < 0.0 && (value < 0.0 - _numCostEpsilon || NumberUtils.IsFinite(_mpvarnumUpper[rc])))
					{
						_bas.MinorPivot(rc, SimplexVarValKind.Upper);
						flag = true;
					}
					break;
				case SimplexVarValKind.Upper:
					if (value > 0.0 && (value > _numCostEpsilon || NumberUtils.IsFinite(_mpvarnumLower[rc])))
					{
						_bas.MinorPivot(rc, SimplexVarValKind.Lower);
						flag = true;
					}
					break;
				case SimplexVarValKind.Zero:
					if (value < 0.0 - _numCostEpsilon)
					{
						_bas.MinorPivot(rc, SimplexVarValKind.Upper);
						flag = true;
					}
					else if (value > _numCostEpsilon)
					{
						_bas.MinorPivot(rc, SimplexVarValKind.Lower);
						flag = true;
					}
					break;
				}
				iter.Advance();
			}
			if (flag)
			{
				_fValidValues = false;
			}
			return flag;
		}

		protected bool QueryNeedPhaseOne()
		{
			_thd.CheckDone();
			int num = _varLim;
			while (--num >= 0)
			{
				switch (_bas.GetVvk(num))
				{
				case SimplexVarValKind.Lower:
					if (!NumberUtils.IsFinite(_mpvarnumLower[num]))
					{
						if (_vecReducedCost.GetCoef(num) > _numCostEpsilon)
						{
							return true;
						}
						if (NumberUtils.IsFinite(_mpvarnumUpper[num]))
						{
							_bas.MinorPivot(num, SimplexVarValKind.Upper);
						}
						else
						{
							_bas.MinorPivot(num, SimplexVarValKind.Zero);
						}
					}
					break;
				case SimplexVarValKind.Upper:
					if (!NumberUtils.IsFinite(_mpvarnumUpper[num]))
					{
						if (_vecReducedCost.GetCoef(num) < 0.0 - _numCostEpsilon)
						{
							return true;
						}
						if (NumberUtils.IsFinite(_mpvarnumLower[num]))
						{
							_bas.MinorPivot(num, SimplexVarValKind.Lower);
						}
						else
						{
							_bas.MinorPivot(num, SimplexVarValKind.Zero);
						}
					}
					break;
				}
			}
			return false;
		}

		/// <summary>
		/// This repeatedly pivots.
		/// </summary>
		protected LinearResult RepeatPivots(bool fPhaseOne)
		{
			_thd.ResetIpiMin();
			Func<bool> func = ((!_fSteepestEdge) ? (fPhaseOne ? new Func<bool>(FindLeavingVarDantzig1) : new Func<bool>(FindLeavingVarDantzig2)) : (fPhaseOne ? new Func<bool>(FindLeavingVarSteepest1) : new Func<bool>(FindLeavingVarSteepest2)));
			do
			{
				_thd.CheckDone();
				if (!_thd.Params.NotifyFindNext(_thd.Tid, _thd, fDouble: true))
				{
					return LinearResult.Interrupted;
				}
				while (true)
				{
					_thd.CheckDone();
					if (!func())
					{
						if (!ReduceError(fPhaseOne))
						{
							if (!_fValidValues)
							{
								_fValidValues = true;
							}
							return LinearResult.Optimal;
						}
						continue;
					}
					_thd.CheckDone();
					ComputeTableauRow();
					double coef;
					while (true)
					{
						_thd.CheckDone();
						if (!FindEnteringVar())
						{
							return LinearResult.UnboundedDual;
						}
						_vvkEnter = _bas.GetVvk(_varEnter);
						_thd.CheckDone();
						SimplexSolver.ComputeColumnDelta(_mod.Matrix, _varEnter, _bas, _vecDelta);
						coef = _vecDelta.GetCoef(_ivarLeave);
						if (!(Math.Abs(coef) <= _numVarEpsilon))
						{
							break;
						}
						if (!ReduceError(fPhaseOne))
						{
							LogSource.SimplexTracer.TraceEvent(TraceEventType.Verbose, 0, Resources.RemovedEnteringCandidate12, _thd.PivotCount, _varEnter, _vecRow.GetCoef(_varEnter));
							if (_log.ShouldLog(7))
							{
								_log.LogEvent(7, Resources.RemovedEnteringCandidate12, _thd.PivotCount, _varEnter, _vecRow.GetCoef(_varEnter));
							}
							_vecRow.RemoveCoef(_varEnter);
							continue;
						}
						goto IL_0145;
					}
					break;
					IL_0145:
					LogSource.SimplexTracer.TraceEvent(TraceEventType.Verbose, 0, "{0} Factored due to small numDelta: {1}, {2}", _thd.PivotCount, _numScale, coef);
					_log.LogEvent(5, "{0} Factored due to small numDelta: {1}, {2}", _thd.PivotCount, _numScale, coef);
				}
				_thd.CheckDone();
				if (!_thd.Params.NotifyStartPivot(_thd.Tid, _thd, this))
				{
					return LinearResult.Interrupted;
				}
				if (fPhaseOne)
				{
					_cpiv1++;
				}
				else
				{
					_cpiv2++;
				}
				if (_numScale == 0.0)
				{
					_cpivDegen++;
				}
				if (!DoPivot(fPhaseOne))
				{
					return LinearResult.Interrupted;
				}
			}
			while (_thd.Params.NotifyEndPivot(_thd.Tid, _thd, this));
			return LinearResult.Interrupted;
		}

		protected bool DoPivot(bool fPhaseOne)
		{
			_thd.CheckDone();
			SimplexVarValKind vvkLeave = _vvkLeave;
			if (_fSteepestEdge)
			{
				UpdateBasicValuesAndWeights(fPhaseOne, fValues: true);
			}
			if (_mpvarnumLower[_varLeave] == _mpvarnumUpper[_varLeave])
			{
				vvkLeave = SimplexVarValKind.Fixed;
			}
			_thd.CheckDone();
			FactorResultFlags factorResultFlags = _bas.MajorPivot(_varEnter, _ivarLeave, _varLeave, vvkLeave, _vecDelta, _rgnumBasic);
			_fValidValues = false;
			if (factorResultFlags != 0)
			{
				if ((factorResultFlags & FactorResultFlags.Permuted) != 0)
				{
					_ivarLeave = _bas.GetBasisSlot(_varEnter);
				}
				PostFactor(factorResultFlags);
				_thd.RecordPivot(this, vvkLeave);
				if ((factorResultFlags & FactorResultFlags.Abort) != 0)
				{
					return false;
				}
				if ((factorResultFlags & FactorResultFlags.Substituted) != 0)
				{
					throw new SimplexFactorException(factorResultFlags);
				}
				ComputeReducedCosts();
				if (DualFeasibilityCorrection() && !fPhaseOne && QueryNeedPhaseOne())
				{
					throw new InfeasibleException("DoPivot");
				}
				if (fPhaseOne)
				{
					ComputeBasicValuesPhaseOne();
				}
				else
				{
					ComputeBasicValues();
				}
			}
			else
			{
				_thd.RecordPivot(this, vvkLeave);
				UpdateReducedCosts(fPhaseOne);
				if (!_fSteepestEdge)
				{
					UpdateBasicValues(fPhaseOne);
				}
				_fValidValues = !fPhaseOne;
				_bas.IsDoubleNonBasicChanged = false;
			}
			return true;
		}

		protected virtual void UpdateReducedCosts(bool fPhaseOne)
		{
			_thd.CheckDone();
			if (_numScale != 0.0)
			{
				double num = (double)(-_sign) * _numScale;
				Vector<double>.Iter iter = new Vector<double>.Iter(_vecRow);
				while (iter.IsValid)
				{
					int rc = iter.Rc;
					if (rc != _varEnter)
					{
						double coef = _vecReducedCost.GetCoef(rc);
						if (SlamToZero(coef, coef += num * iter.Value))
						{
							_vecReducedCost.RemoveCoef(rc);
						}
						else
						{
							_vecReducedCost.SetCoefNonZero(rc, coef);
							switch (_bas.GetVvk(rc))
							{
							case SimplexVarValKind.Lower:
								if (coef < 0.0 - _numCostEpsilon)
								{
									throw new InfeasibleException("UpdateReducedCosts");
								}
								break;
							case SimplexVarValKind.Upper:
								if (coef > _numCostEpsilon)
								{
									throw new InfeasibleException("UpdateReducedCosts");
								}
								break;
							case SimplexVarValKind.Zero:
								if (Math.Abs(coef) > _numCostEpsilon)
								{
									throw new InfeasibleException("UpdateReducedCosts");
								}
								break;
							}
						}
					}
					iter.Advance();
				}
				_vecReducedCost.SetCoefNonZero(_varLeave, num);
			}
			_vecReducedCost.RemoveCoef(_varEnter);
		}

		protected virtual void UpdateBasicValues(bool fPhaseOne)
		{
			_thd.CheckDone();
			double coef = _vecDelta.GetCoef(_ivarLeave);
			double num = _numInfeas / coef;
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecDelta);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				if (rc != _ivarLeave && SlamToZero(_rgnumBasic[rc], _rgnumBasic[rc] -= num * iter.Value))
				{
					_rgnumBasic[rc] = 0.0;
				}
				iter.Advance();
			}
			_rgnumBasic[_ivarLeave] = (fPhaseOne ? GetVarBoundPhaseOne(_varEnter, _vvkEnter) : GetVarBound(_varEnter, _vvkEnter)) + num;
		}

		protected virtual void UpdateBasicValuesAndWeights(bool fPhaseOne, bool fValues)
		{
			_thd.CheckDone();
			double coef = _vecDelta.GetCoef(_ivarLeave);
			double num = coef * coef;
			double num2 = SimplexSolver.ComputeNorm2(_vecDual, -1);
			double num3 = num2 / num;
			_vecWeightUpdate.CopyFrom(_vecDual);
			_bas.InplaceSolveCol(_vecWeightUpdate);
			double num4 = -2.0 / coef;
			double num5 = _numInfeas / coef;
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecDelta);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				if (rc != _ivarLeave)
				{
					double value = iter.Value;
					if (fValues && SlamToZero(_rgnumBasic[rc], _rgnumBasic[rc] -= num5 * value))
					{
						_rgnumBasic[rc] = 0.0;
					}
					int basicVar = _bas.GetBasicVar(rc);
					double val = value * (value * num3 + num4 * _vecWeightUpdate.GetCoef(rc)) + _mpvarnumWeights[basicVar];
					_mpvarnumWeights[basicVar] = Math.Max(val, 0.0001);
				}
				iter.Advance();
			}
			if (fValues)
			{
				_rgnumBasic[_ivarLeave] = (fPhaseOne ? GetVarBoundPhaseOne(_varEnter, _vvkEnter) : GetVarBound(_varEnter, _vvkEnter)) + num5;
			}
			_mpvarnumWeights[_varLeave] = 0.0;
			_mpvarnumWeights[_varEnter] = num3;
		}

		protected virtual void ComputeReducedCosts()
		{
			_thd.CheckDone();
			SimplexSolver.ComputeReducedCostsAndDual(_bas, _mod.Matrix, _vecCost, _numZeroRatio, _vecReducedCost, _vecDual);
			VerifyCosts();
		}

		protected bool FindLeavingVarDantzig1()
		{
			_thd.CheckDone();
			_varLeave = -1;
			_numInfeas = 0.0;
			int num = _rowLim;
			while (--num >= 0)
			{
				double basicValue = GetBasicValue(num);
				double num2 = Math.Abs(basicValue);
				if (!(num2 > Math.Abs(_numInfeas)) || !(num2 > _pricingEpsilon))
				{
					continue;
				}
				int basicVar = _bas.GetBasicVar(num);
				if (basicValue < 0.0)
				{
					if (NumberUtils.IsFinite(_mpvarnumLower[basicVar]))
					{
						_varLeave = basicVar;
						_ivarLeave = num;
						_numInfeas = basicValue;
						_vvkLeave = SimplexVarValKind.Lower;
						_sign = -1;
					}
				}
				else if (basicValue > 0.0 && NumberUtils.IsFinite(_mpvarnumUpper[basicVar]))
				{
					_varLeave = basicVar;
					_ivarLeave = num;
					_numInfeas = basicValue;
					_vvkLeave = SimplexVarValKind.Upper;
					_sign = 1;
				}
			}
			return _varLeave >= 0;
		}

		protected bool FindLeavingVarDantzig2()
		{
			_thd.CheckDone();
			_varLeave = -1;
			_numInfeas = 0.0;
			int num = _rowLim;
			while (--num >= 0)
			{
				double basicValue = GetBasicValue(num);
				int basicVar = _bas.GetBasicVar(num);
				if (basicValue < _mpvarnumLower[basicVar])
				{
					basicValue -= _mpvarnumLower[basicVar];
					if (0.0 - basicValue > Math.Abs(_numInfeas) && Math.Abs(basicValue) > _pricingEpsilon)
					{
						_varLeave = basicVar;
						_ivarLeave = num;
						_numInfeas = basicValue;
						_vvkLeave = SimplexVarValKind.Lower;
						_sign = -1;
					}
				}
				else if (basicValue > _mpvarnumUpper[basicVar])
				{
					basicValue -= _mpvarnumUpper[basicVar];
					if (basicValue > Math.Abs(_numInfeas) && Math.Abs(basicValue) > _pricingEpsilon)
					{
						_varLeave = basicVar;
						_ivarLeave = num;
						_numInfeas = basicValue;
						_vvkLeave = SimplexVarValKind.Upper;
						_sign = 1;
					}
				}
			}
			return _varLeave >= 0;
		}

		protected bool FindLeavingVarSteepest1()
		{
			_thd.CheckDone();
			_varLeave = -1;
			_numCost = 0.0;
			_numInfeas = 0.0;
			int num = _rowLim;
			while (--num >= 0)
			{
				double basicValue = GetBasicValue(num);
				int basicVar = _bas.GetBasicVar(num);
				if (basicValue < 0.0)
				{
					if (NumberUtils.IsFinite(_mpvarnumLower[basicVar]))
					{
						double num2 = basicValue * basicValue / _mpvarnumWeights[basicVar];
						if (num2 > _numCost)
						{
							_varLeave = basicVar;
							_ivarLeave = num;
							_numInfeas = basicValue;
							_numCost = num2;
							_vvkLeave = SimplexVarValKind.Lower;
							_sign = -1;
						}
					}
				}
				else if (basicValue > 0.0 && NumberUtils.IsFinite(_mpvarnumUpper[basicVar]))
				{
					double num3 = basicValue * basicValue / _mpvarnumWeights[basicVar];
					if (num3 > _numCost)
					{
						_varLeave = basicVar;
						_ivarLeave = num;
						_numInfeas = basicValue;
						_numCost = num3;
						_vvkLeave = SimplexVarValKind.Upper;
						_sign = 1;
					}
				}
			}
			return _varLeave >= 0;
		}

		protected bool FindLeavingVarSteepest2()
		{
			_thd.CheckDone();
			_varLeave = -1;
			_numCost = 0.0;
			_numInfeas = 0.0;
			int num = _rowLim;
			while (--num >= 0)
			{
				double basicValue = GetBasicValue(num);
				int basicVar = _bas.GetBasicVar(num);
				if (basicValue < _mpvarnumLower[basicVar] && _mpvarnumLower[basicVar] - basicValue > _numVarEpsilon)
				{
					basicValue -= _mpvarnumLower[basicVar];
					double num2 = basicValue * basicValue / _mpvarnumWeights[basicVar];
					if (num2 > _numCost)
					{
						_varLeave = basicVar;
						_ivarLeave = num;
						_numInfeas = basicValue;
						_numCost = num2;
						_vvkLeave = SimplexVarValKind.Lower;
						_sign = -1;
					}
				}
				else if (basicValue > _mpvarnumUpper[basicVar] && basicValue - _mpvarnumUpper[basicVar] > _numVarEpsilon)
				{
					basicValue -= _mpvarnumUpper[basicVar];
					double num3 = basicValue * basicValue / _mpvarnumWeights[basicVar];
					if (num3 > _numCost)
					{
						_varLeave = basicVar;
						_ivarLeave = num;
						_numInfeas = basicValue;
						_numCost = num3;
						_vvkLeave = SimplexVarValKind.Upper;
						_sign = 1;
					}
				}
			}
			return _varLeave >= 0;
		}

		protected void ComputeTableauRow()
		{
			_thd.CheckDone();
			SimplexBasis bas = _bas;
			_vecDual.Clear();
			_vecDual.SetCoefNonZero(_ivarLeave, 1.0);
			_bas.InplaceSolveRow(_vecDual);
			_thd.CheckDone();
			_vecRow.Clear();
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecDual);
			while (iter.IsValid)
			{
				double value = iter.Value;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mod.Matrix, iter.Rc);
				while (rowIter.IsValid)
				{
					int column = rowIter.Column;
					if (bas.GetVvk(column) >= SimplexVarValKind.Lower)
					{
						double coef = _vecRow.GetCoef(column);
						if (SlamToZero(coef, coef += value * rowIter.Approx))
						{
							_vecRow.RemoveCoef(column);
						}
						else
						{
							_vecRow.SetCoefNonZero(column, coef);
						}
					}
					rowIter.Advance();
				}
				iter.Advance();
			}
		}

		protected bool FindEnteringVar()
		{
			_thd.CheckDone();
			_varEnter = -1;
			_numScale = double.PositiveInfinity;
			double num = double.PositiveInfinity;
			for (Vector<double>.Iter iter = new Vector<double>.Iter(_vecRow); iter.IsValid; iter.Advance())
			{
				double num2 = (double)_sign * iter.Value;
				double num3;
				switch (_bas.GetVvk(iter.Rc))
				{
				case SimplexVarValKind.Lower:
					if (num2 <= 0.0)
					{
						continue;
					}
					num3 = Math.Max(0.0, _vecReducedCost.GetCoef(iter.Rc));
					break;
				case SimplexVarValKind.Upper:
					if (num2 >= 0.0)
					{
						continue;
					}
					num3 = Math.Max(0.0, 0.0 - _vecReducedCost.GetCoef(iter.Rc));
					num2 = 0.0 - num2;
					break;
				case SimplexVarValKind.Zero:
					num3 = 0.0;
					num2 = Math.Abs(num2);
					break;
				default:
					continue;
				}
				double num4 = num3 / num2;
				if (num4 < _numScale || (num4 == _numScale && num2 > num))
				{
					_varEnter = iter.Rc;
					_numScale = num4;
					num = num2;
				}
			}
			return _varEnter >= 0;
		}

		protected void VerifyCosts()
		{
		}
	}
}
