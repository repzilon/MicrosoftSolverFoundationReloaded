using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class PivotStrategyDbl : PivotStrategy<double>
	{
		private readonly PrimalDouble _pds;

		private VectorDouble _vecDelta;

		public VectorDouble Delta => _vecDelta;

		protected PrimalDouble Pds => _pds;

		protected PivotStrategyDbl(SimplexTask thd, PrimalDouble pds)
			: base(thd)
		{
			_pds = pds;
		}

		protected abstract bool ClearSkips();

		protected abstract void SkipVar(int var, byte skip);

		public override void Init()
		{
			base.Init();
			if (_vecDelta == null || _vecDelta.RcCount != _rowLim)
			{
				_vecDelta = new VectorDouble(_rowLim);
			}
		}

		/// <summary>
		/// Calls FindEnteringVar to set _varEnter, _vvkEnter, and _sign.
		/// Calls ComputeDelta to set _vecDelta.
		/// Calls ValidateCost to validate the entering variable.
		/// Calls FindLeavingVar to set _scale, _ivarLeave, _varLeave, and _vvkLeave.
		/// </summary>
		public override bool FindNext()
		{
			InitFindNext();
			bool flag = false;
			while (true)
			{
				_thd.CheckDone();
				if (!FindEnteringVar())
				{
					if (!ReduceError())
					{
						if (flag || !ClearSkips())
						{
							return false;
						}
						flag = true;
					}
				}
				else
				{
					ComputeDelta();
					if (!ValidateCost())
					{
						FixBadCost();
					}
					else if (FindLeavingVar())
					{
						break;
					}
				}
			}
			return true;
		}

		protected virtual void InitFindNext()
		{
		}

		protected abstract bool FindEnteringVar();

		protected virtual bool ReduceError()
		{
			return _pds.ReduceError(this);
		}

		protected virtual void ComputeDelta()
		{
			SimplexSolver.ComputeColumnDelta(_mod.Matrix, base.VarEnter, _pds.Basis, _vecDelta);
			_ivarKey = -1;
			_numKey = double.NaN;
		}

		protected virtual double ComputeRelativeCostFromDelta()
		{
			SimplexBasis basis = _pds.Basis;
			double num = _pds.Cost(base.VarEnter);
			Vector<double>.Iter iter = new Vector<double>.Iter(_pds.GetCostVector());
			while (iter.IsValid)
			{
				int basisSlot = basis.GetBasisSlot(iter.Rc);
				if (basisSlot >= 0)
				{
					double coef = _vecDelta.GetCoef(basisSlot);
					if (coef != 0.0)
					{
						num -= coef * iter.Value;
					}
				}
				iter.Advance();
			}
			return num;
		}

		protected virtual bool ValidateCost()
		{
			double value = ComputeRelativeCostFromDelta();
			if (Math.Sign(value) == base.Sign)
			{
				return Math.Abs(value) > _pds.CostEpsilon;
			}
			return false;
		}

		protected virtual void FixBadCost()
		{
			if (!ReduceError())
			{
				SkipVar(base.VarEnter, 2);
			}
		}

		protected virtual bool FindLeavingVar()
		{
			base.Scale = _pds.GetUpperBound(base.VarEnter) - _pds.GetLowerBound(base.VarEnter);
			double num = 1.0;
			bool flag = false;
			bool removeFixedLeaving = _pds.RemoveFixedLeaving;
			bool flag2 = _pds.ApplyAntidegeneracyLeaving();
			double num2 = _pds.VarEpsilon * 64.0;
			base.IvarLeave = -1;
			base.VarLeave = base.VarEnter;
			base.VvkLeave = ((base.Sign < 0) ? SimplexVarValKind.Lower : SimplexVarValKind.Upper);
			SimplexBasis basis = _pds.Basis;
			for (Vector<double>.Iter iter = new Vector<double>.Iter(_vecDelta); iter.IsValid; iter.Advance())
			{
				if (Math.Abs(iter.Value) <= _pds.VarEpsilon)
				{
					continue;
				}
				int rc = iter.Rc;
				double value = iter.Value;
				int basicVar = basis.GetBasicVar(rc);
				double num3 = (double)base.Sign * value;
				SimplexVarValKind simplexVarValKind;
				double lowerBound;
				if (num3 < 0.0)
				{
					lowerBound = _pds.GetLowerBound(basicVar);
					if (double.IsInfinity(lowerBound))
					{
						continue;
					}
					lowerBound = _pds.GetBasicValue(rc) - lowerBound;
					num3 = 0.0 - num3;
					simplexVarValKind = SimplexVarValKind.Lower;
				}
				else
				{
					lowerBound = _pds.GetUpperBound(basicVar);
					if (double.IsInfinity(lowerBound))
					{
						continue;
					}
					lowerBound -= _pds.GetBasicValue(rc);
					simplexVarValKind = SimplexVarValKind.Upper;
				}
				if (lowerBound < 0.0)
				{
					if (lowerBound < 0.0 - _pds.VarEpsilon)
					{
						throw new InfeasibleException("FindLeavingVar");
					}
					lowerBound = 0.0;
				}
				bool flag3 = false;
				if (removeFixedLeaving && 0.0 == lowerBound && flag2 && 0.0 < base.Scale)
				{
					lowerBound = num2 * (1.0 + _pds.NextPerturbation() / 64.0);
					if (simplexVarValKind == SimplexVarValKind.Lower)
					{
						double lowerBound2 = _pds.GetLowerBound(basicVar);
						_pds.ShiftLowerBound(basicVar, lowerBound2 - lowerBound);
					}
					else
					{
						double lowerBound2 = _pds.GetUpperBound(basicVar);
						_pds.ShiftUpperBound(basicVar, lowerBound2 + lowerBound);
					}
				}
				double num4 = lowerBound / num3;
				if (!(num4 > base.Scale) && (0.0 < base.Scale || (!flag && flag3) || ((flag3 || !flag) && num3 > num)))
				{
					base.Scale = num4;
					base.IvarLeave = rc;
					_ivarKey = rc;
					_numKey = _vecDelta.GetCoef(_ivarKey);
					base.VarLeave = basicVar;
					base.VvkLeave = simplexVarValKind;
					num = num3;
					flag = flag3;
				}
			}
			if (num <= num2 && ReduceError())
			{
				base.Logger.LogEvent(5, "{0} Factored due to small numDelta: {1}, {2}", _thd.PivotCount, base.Scale, num);
				return false;
			}
			if (_thd._fSOSFastPath)
			{
				UpdateSOS(base.VarLeave);
			}
			return true;
		}

		/// <summary>Update the sos status due to the leaving var
		/// </summary>
		private void UpdateSOS(int leavingVar)
		{
			if (_thd.Model.IsSOS)
			{
				int row = SOSUtils.GetRow(_thd.Model._mpvarSOS2Row, leavingVar);
				int row2 = SOSUtils.GetRow(_thd.Model._mpvarSOS1Row, leavingVar);
				int key = ((row >= 0) ? row : row2);
				if (row >= 0 || row2 >= 0)
				{
					_thd._sosStatus[key].Remove();
				}
			}
		}

		protected override Rational GetRationalFromNumber(double num)
		{
			return num;
		}
	}
}
