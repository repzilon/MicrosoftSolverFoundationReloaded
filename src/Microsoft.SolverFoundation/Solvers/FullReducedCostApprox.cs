using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class FullReducedCostApprox : PivotStrategyExact
	{
		protected const int kcpivRefreshCosts = 100000;

		private VectorRational _vecCost;

		protected VectorDouble _vecCostApprox;

		protected VectorDouble _vecCostApproxFiltered;

		private VectorRational _vecDual;

		protected VectorDouble _vecDualApprox;

		protected bool _fExactCostsValid;

		protected int _cpiv;

		public FullReducedCostApprox(SimplexTask thd, PrimalExact pes)
			: base(thd, pes)
		{
		}

		public override void Init()
		{
			base.Init();
			if (_vecCost == null || _vecCost.RcCount != _varLim)
			{
				_vecCost = new VectorRational(_varLim);
			}
			if (_vecCostApprox == null || _vecCostApprox.RcCount != _varLim)
			{
				_vecCostApprox = new VectorDouble(_varLim);
			}
			if (_vecCostApproxFiltered == null || _vecCostApproxFiltered.RcCount != _varLim)
			{
				_vecCostApproxFiltered = new VectorDouble(_varLim);
			}
			if (_vecDual == null || _vecDual.RcCount != _rowLim)
			{
				_vecDual = new VectorRational(_rowLim);
			}
			if (_vecDualApprox == null || _vecDualApprox.RcCount != _varLim)
			{
				_vecDualApprox = new VectorDouble(_rowLim);
			}
			ComputeCosts();
			_fExactCostsValid = true;
			_cpiv = 0;
		}

		/// <summary>
		/// Compute the reduced costs and put them in _vecCost.
		/// </summary>
		protected virtual void ComputeCosts()
		{
			ComputeCostsCore(_vecCost);
			VerifyCosts();
			CastCosts();
		}

		protected virtual void ComputeCostsCore(VectorRational vecCost)
		{
			SimplexSolver.ComputeReducedCostsAndDual(_pes.Basis, _mod.Matrix, _pes.GetCostVector(), vecCost, _vecDual);
		}

		protected virtual void CastCosts()
		{
			_vecCostApprox.Clear();
			_vecCostApproxFiltered.Clear();
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecCost);
			while (iter.IsValid)
			{
				double signedDouble = iter.Value.GetSignedDouble();
				_vecCostApprox.SetCoefNonZero(iter.Rc, signedDouble);
				_vecCostApproxFiltered.SetCoefNonZero(iter.Rc, signedDouble);
				iter.Advance();
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
			while (true)
			{
				if (!FindEnteringVar())
				{
					if (!ReduceError())
					{
						return false;
					}
					continue;
				}
				ComputeDelta();
				if (ValidateCost())
				{
					break;
				}
				ReduceError();
			}
			FindLeavingVar();
			return true;
		}

		protected virtual bool GetNextVar(double numMin, ref Vector<double>.Iter iter, out int varRet, out double numCostRet, out int signRet)
		{
			SimplexBasis basis = _pes.Basis;
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				double value = iter.Value;
				double num;
				switch (basis.GetVvk(rc))
				{
				case SimplexVarValKind.Lower:
					if (value >= 0.0)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					num = 0.0 - value;
					break;
				case SimplexVarValKind.Upper:
					if (value <= 0.0)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					num = value;
					break;
				case SimplexVarValKind.Zero:
					num = Math.Abs(value);
					break;
				default:
					iter.RemoveAndAdvance();
					continue;
				}
				iter.Advance();
				if (num > numMin)
				{
					signRet = Math.Sign(value);
					numCostRet = num;
					varRet = rc;
					return true;
				}
			}
			numCostRet = 0.0;
			signRet = 0;
			varRet = -1;
			return false;
		}

		/// <summary>
		/// Sets _varEnter and _sign.
		/// </summary>
		protected override bool FindEnteringVar()
		{
			base.VarEnter = -1;
			double numMin = 0.0;
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecCostApproxFiltered);
			int varRet;
			double numCostRet;
			int signRet;
			while (GetNextVar(numMin, ref iter, out varRet, out numCostRet, out signRet))
			{
				numMin = numCostRet;
				base.VarEnter = varRet;
				base.VvkEnter = _pes.Basis.GetVvk(varRet);
				base.Sign = signRet;
				_dblApproxCost = numCostRet;
			}
			return base.VarEnter >= 0;
		}

		protected virtual bool ReduceError()
		{
			if (_fExactCostsValid)
			{
				return false;
			}
			RefreshCosts();
			return true;
		}

		protected virtual void RefreshCosts()
		{
			ComputeCostsCore(_vecCost);
			CastCosts();
			_fExactCostsValid = true;
		}

		protected virtual bool ValidateCost()
		{
			if (_fExactCostsValid)
			{
				return true;
			}
			double signedDouble = ComputeRelativeCostFromDelta().GetSignedDouble();
			double num = signedDouble / _vecCostApprox.GetCoef(base.VarEnter);
			if (signedDouble == 0.0)
			{
				_vecCostApprox.RemoveCoef(base.VarEnter);
				_vecCostApproxFiltered.RemoveCoef(base.VarEnter);
			}
			else
			{
				_vecCostApprox.SetCoefNonZero(base.VarEnter, signedDouble);
				_vecCostApproxFiltered.SetCoefNonZero(base.VarEnter, signedDouble);
			}
			if (0.5 <= num && num <= 2.0)
			{
				return true;
			}
			return false;
		}

		protected override void UpdateCosts(Rational dnumLeaveCost)
		{
			if (++_cpiv % 100000 == 0)
			{
				RefreshCosts();
			}
			else
			{
				UpdateCostsCore(dnumLeaveCost);
			}
		}

		protected virtual void UpdateCostsCore(Rational dnumLeaveCost)
		{
			base.IvarLeave = _pes.Basis.GetBasisSlot(base.VarEnter);
			_fExactCostsValid = false;
			double coef = _vecCostApprox.GetCoef(base.VarEnter);
			_vecDualApprox.Clear();
			_vecDualApprox.SetCoefNonZero(base.IvarLeave, 1.0);
			_pes.Basis.InplaceSolveApproxRow(_vecDualApprox);
			SimplexBasis basis = _pes.Basis;
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecDualApprox);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				double num = iter.Value * coef;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mod.Matrix, rc);
				while (rowIter.IsValid)
				{
					int column = rowIter.Column;
					if (basis.GetVvk(column) >= SimplexVarValKind.Lower)
					{
						double num2 = _vecCostApprox.GetCoef(column) - num * (double)rowIter.Exact;
						if (num2 == 0.0)
						{
							_vecCostApprox.RemoveCoef(column);
							_vecCostApproxFiltered.RemoveCoef(column);
						}
						else
						{
							_vecCostApprox.SetCoefNonZero(column, num2);
							_vecCostApproxFiltered.SetCoefNonZero(column, num2);
						}
					}
					rowIter.Advance();
				}
				iter.Advance();
			}
			_vecCostApprox.RemoveCoef(base.VarEnter);
			_vecCostApproxFiltered.RemoveCoef(base.VarEnter);
			double num3;
			if (basis.GetVvk(base.VarLeave) == SimplexVarValKind.Fixed || (num3 = (0.0 - coef) / (double)_numKey - (double)dnumLeaveCost) == 0.0)
			{
				_vecCostApprox.RemoveCoef(base.VarLeave);
				_vecCostApproxFiltered.RemoveCoef(base.VarLeave);
			}
			else
			{
				_vecCostApprox.SetCoefNonZero(base.VarLeave, num3);
				_vecCostApproxFiltered.SetCoefNonZero(base.VarLeave, num3);
			}
		}

		protected void VerifyCosts()
		{
			if (!base.Logger.ShouldLog(2))
			{
				return;
			}
			VectorRational vectorRational = new VectorRational(_varLim);
			ComputeCostsCore(vectorRational);
			if (vectorRational.EntryCount <= _vecCost.EntryCount)
			{
				Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecCost);
				while (iter.IsValid)
				{
					if (vectorRational.GetCoef(iter.Rc) != iter.Value)
					{
						base.Logger.LogEvent(2, "Bad reduced cost: var = {0}, {1} != {2}", vectorRational.GetCoef(iter.Rc), iter.Value);
						break;
					}
					iter.Advance();
				}
				return;
			}
			Vector<Rational>.Iter iter2 = new Vector<Rational>.Iter(vectorRational);
			while (iter2.IsValid)
			{
				if (_vecCost.GetCoef(iter2.Rc) != iter2.Value)
				{
					base.Logger.LogEvent(2, "Bad reduced cost: var = {0}, {1} != {2}", iter2.Value, _vecCost.GetCoef(iter2.Rc));
					break;
				}
				iter2.Advance();
			}
		}
	}
}
