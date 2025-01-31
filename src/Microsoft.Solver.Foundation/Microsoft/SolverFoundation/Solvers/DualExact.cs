using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class DualExact : AlgorithmRational, ISimplexPivotInformation
	{
		internal VectorRational _vecReducedCost;

		internal VectorRational _vecDual;

		private VectorRational _vecRow;

		private int _sign;

		private Rational _numInfeas;

		private int _varLeave;

		private int _ivarLeave;

		private SimplexVarValKind _vvkLeave;

		private Rational _numScale;

		private int _varEnter;

		private SimplexVarValKind _vvkEnter;

		private VectorRational _vecDelta;

		public virtual int VarEnter => _varEnter;

		public virtual SimplexVarValKind VvkEnter => _vvkEnter;

		public virtual int VarLeave => _varLeave;

		public virtual int IvarLeave => _ivarLeave;

		public virtual SimplexVarValKind VvkLeave => _vvkLeave;

		public virtual Rational Scale => _numScale;

		public virtual int Sign => _sign;

		public virtual Rational Determinant => _vecDelta.GetCoef(_ivarLeave);

		bool ISimplexPivotInformation.IsDouble => false;

		Rational ISimplexPivotInformation.Scale => _numScale;

		Rational ISimplexPivotInformation.Determinant => _vecDelta.GetCoef(_ivarLeave);

		public virtual double ApproxCost => 0.0;

		private Rational GetUpperBoundPhaseOne(int var)
		{
			if (_mpvarnumUpper[var].IsFinite)
			{
				return default(Rational);
			}
			if (_mpvarnumLower[var].IsFinite)
			{
				return 1;
			}
			return 1000;
		}

		private Rational GetLowerBoundPhaseOne(int var)
		{
			if (_mpvarnumLower[var].IsFinite)
			{
				return default(Rational);
			}
			if (_mpvarnumUpper[var].IsFinite)
			{
				return -1;
			}
			return -1000;
		}

		private Rational GetVarBoundPhaseOne(int var, SimplexVarValKind vvk)
		{
			switch (vvk)
			{
			default:
				return default(Rational);
			case SimplexVarValKind.Lower:
				return GetLowerBoundPhaseOne(var);
			case SimplexVarValKind.Upper:
				return GetUpperBoundPhaseOne(var);
			}
		}

		public DualExact(SimplexTask thd)
			: base(thd)
		{
		}

		public override void Init(SimplexReducedModel mod, SimplexFactoredBasis bas)
		{
			base.Init(mod, bas);
			_vecReducedCost = new VectorRational(_varLim);
			_vecDual = new VectorRational(_rowLim);
			_vecRow = new VectorRational(_varLim);
			_vecDelta = new VectorRational(_rowLim);
		}

		public override bool RunSimplex(int cpivMax, bool fStopAtNextGoal, OptimalGoalValues ogvMin, out LinearResult res)
		{
			_thd.CostingUsedExact = SimplexCosting.BestReducedCost;
			InitBounds(ogvMin, out var igiCur, out var fStrict);
			if (igiCur >= _mod.GoalCount)
			{
				_vecCost.Clear();
				_vecReducedCost.Clear();
				while (true)
				{
					EnsureFactorization();
					_fValidReducedCosts = true;
					if (!_fValidValues)
					{
						ComputeBasicValues();
					}
					try
					{
						return RepeatPivots(fPhaseOne: false, ref cpivMax, out res);
					}
					catch (SimplexFactorException)
					{
						_log.LogEvent(6, Resources.RepairedSingularBasis, _thd.PivotCount);
					}
				}
			}
			while (true)
			{
				int goalVar = _mod.GetGoalVar(igiCur);
				bool flag = _mod.IsGoalMinimize(igiCur);
				Rational rational = (flag ? 1 : (-1));
				if (_vecCost.EntryCount != 1 || _vecCost.GetCoef(goalVar) != rational)
				{
					_vecCost.Clear();
					_vecCost.SetCoefNonZero(goalVar, rational);
					_fValidReducedCosts = false;
				}
				EnsureFactorization();
				if (!_fValidReducedCosts)
				{
					ComputeReducedCosts();
				}
				try
				{
					bool flag3;
					if (PrePhaseOne())
					{
						ComputeBasicValuesPhaseOne();
						bool flag2;
						try
						{
							flag2 = RepeatPivots(fPhaseOne: true, ref cpivMax, out res);
						}
						finally
						{
							flag3 = InitVarValuesPostPhaseOne();
						}
						if (!flag2)
						{
							return false;
						}
						switch (res)
						{
						default:
							return true;
						case LinearResult.UnboundedDual:
							res = LinearResult.InfeasibleOrUnbounded;
							return true;
						case LinearResult.Optimal:
							break;
						}
						if (!flag3)
						{
							res = LinearResult.InfeasibleOrUnbounded;
							return true;
						}
					}
					else if (!_fValidValues)
					{
						ComputeBasicValues();
					}
					if (!RepeatPivots(fPhaseOne: false, ref cpivMax, out res))
					{
						return false;
					}
					if (res != LinearResult.Optimal)
					{
						return true;
					}
					flag3 = true;
					Rational varValue = GetVarValue(goalVar);
					Rational num = varValue;
					if (!flag)
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
						res = LinearResult.InfeasibleOrUnbounded;
						return true;
					}
					_thd.OptimalGoalValues[igiCur] = num;
					ref Rational reference = ref _mpvarnumUpper[goalVar];
					reference = (_mpvarnumLower[goalVar] = varValue);
					if (++igiCur >= _mod.GoalCount)
					{
						return true;
					}
					if (fStopAtNextGoal)
					{
						return false;
					}
				}
				catch (SimplexFactorException)
				{
					_log.LogEvent(6, Resources.RepairedSingularBasis, _thd.PivotCount);
				}
			}
		}

		protected void ComputeBasicValuesPhaseOne()
		{
			ComputeBasicValuesPhaseOne(_rgnumBasic);
			_fValidValues = true;
			_bas.IsExactNonBasicChanged = false;
		}

		/// <summary> This initializes variable values assuming _bas is set correctly
		/// and the basis is factored.
		/// </summary>
		protected void ComputeBasicValuesPhaseOne(Rational[] rgnumDst)
		{
			_thd.CheckDone();
			Array.Clear(rgnumDst, 0, _rowLim);
			for (int i = 0; i < _varLim; i++)
			{
				Rational num;
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
				if (!num.IsZero)
				{
					Rational.Negate(ref num);
					CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mod.Matrix, i);
					while (colIter.IsValid)
					{
						int row = colIter.Row;
						ref Rational reference = ref rgnumDst[row];
						reference = Rational.AddMul(rgnumDst[row], num, colIter.Exact);
						colIter.Advance();
					}
				}
			}
			_bas.InplaceSolveCol(rgnumDst);
		}

		protected bool InitVarValuesPostPhaseOne()
		{
			bool flag = false;
			int num = _varLim;
			while (--num >= 0)
			{
				switch (_bas.GetVvk(num))
				{
				case SimplexVarValKind.Lower:
					if (!_mpvarnumLower[num].IsFinite)
					{
						if (_mpvarnumUpper[num].IsFinite)
						{
							_bas.MinorPivot(num, SimplexVarValKind.Upper);
							flag = flag || _vecReducedCost.GetCoef(num).Sign > 0;
						}
						else
						{
							_bas.MinorPivot(num, SimplexVarValKind.Zero);
							flag = flag || !_vecReducedCost.GetCoef(num).IsZero;
						}
					}
					break;
				case SimplexVarValKind.Upper:
					if (!_mpvarnumUpper[num].IsFinite)
					{
						if (_mpvarnumLower[num].IsFinite)
						{
							_bas.MinorPivot(num, SimplexVarValKind.Lower);
							flag = flag || _vecReducedCost.GetCoef(num).Sign < 0;
						}
						else
						{
							_bas.MinorPivot(num, SimplexVarValKind.Zero);
							flag = flag || !_vecReducedCost.GetCoef(num).IsZero;
						}
					}
					break;
				}
			}
			ComputeBasicValues();
			return !flag;
		}

		protected bool PrePhaseOne()
		{
			_thd.CheckDone();
			bool flag = false;
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecReducedCost);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				Rational value = iter.Value;
				switch (_bas.GetVvk(rc))
				{
				case SimplexVarValKind.Lower:
					if (value.Sign < 0)
					{
						_bas.MinorPivot(rc, SimplexVarValKind.Upper);
						flag = flag || _mpvarnumUpper[rc].IsPositiveInfinity;
					}
					break;
				case SimplexVarValKind.Upper:
					if (value.Sign > 0)
					{
						_bas.MinorPivot(rc, SimplexVarValKind.Lower);
						flag = flag || _mpvarnumLower[rc].IsNegativeInfinity;
					}
					break;
				case SimplexVarValKind.Zero:
					_bas.MinorPivot(rc, (value.Sign < 0) ? SimplexVarValKind.Upper : SimplexVarValKind.Lower);
					flag = true;
					break;
				}
				iter.Advance();
			}
			return flag;
		}

		/// <summary>
		/// This repeatedly pivots.
		/// </summary>
		protected bool RepeatPivots(bool fPhaseOne, ref int cpivMax, out LinearResult res)
		{
			_thd.ResetIpiMin();
			while (cpivMax > 0)
			{
				_thd.CheckDone();
				if (!_thd.Params.NotifyFindNext(_thd.Tid, _thd, fDouble: false))
				{
					res = LinearResult.Interrupted;
					return true;
				}
				if (!FindLeavingVar(fPhaseOne))
				{
					res = LinearResult.Optimal;
					return true;
				}
				ComputeTableauRow();
				if (!FindEnteringVar())
				{
					res = LinearResult.UnboundedDual;
					return true;
				}
				_vvkEnter = _bas.GetVvk(_varEnter);
				_thd.CheckDone();
				SimplexSolver.ComputeColumnDelta(_mod.Matrix, _varEnter, _bas, _vecDelta);
				if (!_thd.Params.NotifyStartPivot(_thd.Tid, _thd, this))
				{
					res = LinearResult.Interrupted;
					return true;
				}
				_thd.CheckDone();
				if (fPhaseOne)
				{
					_cpiv1++;
				}
				else
				{
					_cpiv2++;
				}
				cpivMax--;
				if (_numScale.IsZero)
				{
					_cpivDegen++;
				}
				if (!DoPivot(fPhaseOne))
				{
					res = LinearResult.Interrupted;
					return true;
				}
				if (!_thd.Params.NotifyEndPivot(_thd.Tid, _thd, this))
				{
					res = LinearResult.Interrupted;
					return true;
				}
			}
			res = LinearResult.Interrupted;
			return false;
		}

		protected bool DoPivot(bool fPhaseOne)
		{
			Rational coef = _vecDelta.GetCoef(_ivarLeave);
			_thd.CheckDone();
			if (!_numScale.IsZero)
			{
				Rational rational = -_sign * _numScale;
				Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecRow);
				while (iter.IsValid)
				{
					Rational num = Rational.AddMul(_vecReducedCost.GetCoef(iter.Rc), rational, iter.Value);
					if (num.IsZero)
					{
						_vecReducedCost.RemoveCoef(iter.Rc);
					}
					else
					{
						_vecReducedCost.SetCoefNonZero(iter.Rc, num);
					}
					iter.Advance();
				}
				_vecReducedCost.SetCoefNonZero(_varLeave, rational);
			}
			_vecReducedCost.RemoveCoef(_varEnter);
			_thd.CheckDone();
			Rational rational2 = _numInfeas / coef;
			AlgorithmRational.AddToScale(_rgnumBasic, _rowLim, -rational2, _vecDelta);
			ref Rational reference = ref _rgnumBasic[_ivarLeave];
			reference = (fPhaseOne ? GetVarBoundPhaseOne(_varEnter, _vvkEnter) : GetVarBound(_varEnter, _vvkEnter)) + rational2;
			SimplexVarValKind vvkLeave = _vvkLeave;
			if (_mpvarnumLower[_varLeave] == _mpvarnumUpper[_varLeave])
			{
				vvkLeave = SimplexVarValKind.Fixed;
				_vecReducedCost.RemoveCoef(_varLeave);
			}
			_thd.CheckDone();
			FactorResultFlags factorResultFlags = _bas.MajorPivot(_varEnter, _ivarLeave, _varLeave, vvkLeave, _vecDelta, _rgnumBasic);
			_thd.RecordPivot(this, vvkLeave);
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
			VerifyCosts();
			if (_log.ShouldLog(0))
			{
				Rational[] rgnum = _thd.GetTempArrayExact(_rowLim, fClear: false);
				if (fPhaseOne)
				{
					ComputeBasicValuesPhaseOne(rgnum);
				}
				else
				{
					ComputeBasicValues(rgnum);
				}
				_thd.Solver.VerifySame(_rgnumBasic, rgnum, _rowLim, 0, Resources.WrongBasicVariableValue012);
				_thd.ReleaseTempArray(ref rgnum);
			}
			return true;
		}

		/// <summary>
		/// Compute the reduced costs and put them in _vecCost.
		/// </summary>
		protected virtual void ComputeReducedCosts()
		{
			_thd.CheckDone();
			SimplexSolver.ComputeReducedCostsAndDual(_bas, _mod.Matrix, _vecCost, _vecReducedCost, _vecDual);
			VerifyCosts();
			_fValidReducedCosts = true;
		}

		protected bool FindLeavingVar(bool fPhaseOne)
		{
			_thd.CheckDone();
			_varLeave = -1;
			if (fPhaseOne)
			{
				int num = _rowLim;
				while (--num >= 0)
				{
					Rational basicValue = GetBasicValue(num);
					int basicVar = _bas.GetBasicVar(num);
					if (basicValue.Sign < 0)
					{
						if (_mpvarnumLower[basicVar].IsFinite && (_varLeave < 0 || -basicValue > _numInfeas.AbsoluteValue))
						{
							_varLeave = basicVar;
							_ivarLeave = num;
							_numInfeas = basicValue;
							_vvkLeave = SimplexVarValKind.Lower;
							_sign = -1;
						}
					}
					else if (basicValue.Sign > 0 && _mpvarnumUpper[basicVar].IsFinite && (_varLeave < 0 || basicValue > _numInfeas.AbsoluteValue))
					{
						_varLeave = basicVar;
						_ivarLeave = num;
						_numInfeas = basicValue;
						_vvkLeave = SimplexVarValKind.Upper;
						_sign = 1;
					}
				}
			}
			else
			{
				int num2 = _rowLim;
				while (--num2 >= 0)
				{
					Rational basicValue2 = GetBasicValue(num2);
					int basicVar2 = _bas.GetBasicVar(num2);
					if (basicValue2 < _mpvarnumLower[basicVar2])
					{
						basicValue2 -= _mpvarnumLower[basicVar2];
						if (_varLeave < 0 || -basicValue2 > _numInfeas.AbsoluteValue)
						{
							_varLeave = basicVar2;
							_ivarLeave = num2;
							_numInfeas = basicValue2;
							_vvkLeave = SimplexVarValKind.Lower;
							_sign = -1;
						}
					}
					else if (basicValue2 > _mpvarnumUpper[basicVar2])
					{
						basicValue2 -= _mpvarnumUpper[basicVar2];
						if (_varLeave < 0 || basicValue2 > _numInfeas.AbsoluteValue)
						{
							_varLeave = basicVar2;
							_ivarLeave = num2;
							_numInfeas = basicValue2;
							_vvkLeave = SimplexVarValKind.Upper;
							_sign = 1;
						}
					}
				}
			}
			return _varLeave >= 0;
		}

		/// <summary>
		/// This computes the _ivarLeave'th row of the tableau.
		/// Of course we never realize the full tableau.
		/// </summary>
		protected void ComputeTableauRow()
		{
			_thd.CheckDone();
			SimplexBasis bas = _bas;
			_vecDual.Clear();
			_vecDual.SetCoefNonZero(_ivarLeave, 1);
			_bas.InplaceSolveRow(_vecDual);
			_thd.CheckDone();
			_vecRow.Clear();
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecDual);
			while (iter.IsValid)
			{
				Rational value = iter.Value;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mod.Matrix, iter.Rc);
				while (rowIter.IsValid)
				{
					int column = rowIter.Column;
					if (bas.GetVvk(column) >= SimplexVarValKind.Lower)
					{
						Rational num = Rational.AddMul(_vecRow.GetCoef(column), value, rowIter.Exact);
						if (num.IsZero)
						{
							_vecRow.RemoveCoef(column);
						}
						else
						{
							_vecRow.SetCoefNonZero(column, num);
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
			_numScale = Rational.PositiveInfinity;
			for (Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecRow); iter.IsValid; iter.Advance())
			{
				Rational rational = iter.Value;
				if (_sign < 0)
				{
					rational = -rational;
				}
				switch (_bas.GetVvk(iter.Rc))
				{
				case SimplexVarValKind.Lower:
					if (rational.Sign <= 0)
					{
						continue;
					}
					break;
				case SimplexVarValKind.Upper:
					if (rational.Sign >= 0)
					{
						continue;
					}
					break;
				case SimplexVarValKind.Zero:
					break;
				default:
					continue;
				}
				Rational rational2 = _vecReducedCost.GetCoef(iter.Rc) / rational;
				if (_numScale > rational2)
				{
					_varEnter = iter.Rc;
					_numScale = rational2;
					if (_numScale == 0)
					{
						return true;
					}
				}
			}
			return _varEnter >= 0;
		}

		protected void VerifyCosts()
		{
			if (!_log.ShouldLog(2))
			{
				return;
			}
			_thd.CheckDone();
			VectorRational vectorRational = new VectorRational(_varLim);
			SimplexSolver.ComputeReducedCostsAndDual(_bas, _mod.Matrix, _vecCost, vectorRational, _vecDual);
			if (vectorRational.EntryCount <= _vecReducedCost.EntryCount)
			{
				Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecReducedCost);
				while (iter.IsValid)
				{
					if (vectorRational.GetCoef(iter.Rc) != iter.Value)
					{
						_log.LogEvent(2, "Bad reduced cost: var = {0}, {1} != {2}", iter.Rc, vectorRational.GetCoef(iter.Rc), iter.Value);
						break;
					}
					iter.Advance();
				}
				return;
			}
			Vector<Rational>.Iter iter2 = new Vector<Rational>.Iter(vectorRational);
			while (iter2.IsValid)
			{
				if (_vecReducedCost.GetCoef(iter2.Rc) != iter2.Value)
				{
					_log.LogEvent(2, "Bad reduced cost: var = {0}, {1} != {2}", iter2.Rc, iter2.Value, _vecReducedCost.GetCoef(iter2.Rc));
					break;
				}
				iter2.Advance();
			}
		}
	}
}
