using System;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class FullReducedCostDbl : PivotStrategyDbl
	{
		protected const double knumCostEps = 1E-12;

		private VectorDouble _vecCost;

		protected VectorDDouble _vecCostFiltered;

		private VectorDouble _vecDual;

		private bool _fRecalcedCosts;

		protected VectorDouble Costs => _vecCost;

		protected VectorDouble Duals => _vecDual;

		public FullReducedCostDbl(SimplexTask thd, PrimalDouble pds)
			: base(thd, pds)
		{
		}

		public override void Init()
		{
			base.Init();
			if (_vecCost == null || _vecCost.RcCount != _varLim)
			{
				_vecCost = new VectorDouble(_varLim);
			}
			if (_vecCostFiltered == null || _vecCostFiltered.RcCount != _varLim)
			{
				_vecCostFiltered = new VectorDDouble(_varLim, 0);
			}
			if (_vecDual == null || _vecDual.RcCount != _rowLim)
			{
				_vecDual = new VectorDouble(_rowLim);
			}
			ComputeCostsCore(_vecCost);
			VerifyCosts();
			CopyCosts();
		}

		/// <summary>
		/// Compute the reduced costs and put them in _vecCost.
		/// </summary>
		protected virtual void ComputeCosts()
		{
			ComputeCostsCore(_vecCost);
			VerifyCosts();
			CopyCosts();
		}

		protected virtual void ComputeCostsCore(VectorDouble vecCost)
		{
			SimplexSolver.ComputeReducedCostsAndDual(base.Pds.Basis, _mod.Matrix, base.Pds.GetCostVector(), 1E-12, vecCost, _vecDual);
		}

		protected void CopyCosts()
		{
			_vecCostFiltered.Clear();
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecCost);
			while (iter.IsValid)
			{
				_vecCostFiltered.SetCoefNonZero(iter.Rc, iter.Value);
				iter.Advance();
			}
		}

		protected override bool ClearSkips()
		{
			if (_vecCostFiltered.EntryCount == _vecCost.EntryCount)
			{
				return false;
			}
			CopyCosts();
			return true;
		}

		protected override void SkipVar(int var, byte skip)
		{
			_vecCostFiltered.RemoveCoef(var);
		}

		protected virtual int GetBestVar(ref VectorDDouble vec, out double numCostRet, out int signRet)
		{
			int[] varIVar = base.Pds.Basis.VarIVar;
			numCostRet = 0.0;
			signRet = 0;
			int result = -1;
			double num = 0.0;
			VectorDDouble.Iter iter = new VectorDDouble.Iter(ref _vecCostFiltered);
			while (iter.IsValid)
			{
				int num2 = iter.Rc(ref vec);
				double num3 = iter.Value(ref vec);
				SimplexVarValKind simplexVarValKind = SimplexBasis.InterpretVvk(varIVar[num2]);
				double num4 = Math.Abs(num3);
				if ((simplexVarValKind == SimplexVarValKind.Lower && num3 < 0.0) || (0.0 < num3 && simplexVarValKind == SimplexVarValKind.Upper) || simplexVarValKind == SimplexVarValKind.Zero)
				{
					iter.Advance();
					if (num4 > num)
					{
						signRet = Math.Sign(num3);
						num = num4;
						result = num2;
					}
				}
				else
				{
					iter.RemoveAndAdvance(ref vec);
				}
			}
			numCostRet = num;
			return result;
		}

		/// <summary> Sets _varEnter and _sign.
		/// </summary>
		protected override bool FindEnteringVar()
		{
			base.VarEnter = GetBestVar(ref _vecCostFiltered, out _dblApproxCost, out var signRet);
			if (0 <= base.VarEnter)
			{
				base.VvkEnter = base.Pds.Basis.GetVvk(base.VarEnter);
				base.Sign = signRet;
				return true;
			}
			return false;
		}

		protected override bool ReduceError()
		{
			if (!base.ReduceError())
			{
				return false;
			}
			_fRecalcedCosts = true;
			ComputeCosts();
			return true;
		}

		protected override void InitFindNext()
		{
			base.InitFindNext();
			_fRecalcedCosts = false;
		}

		protected override bool ValidateCost()
		{
			double num = ComputeRelativeCostFromDelta();
			if (Math.Sign(num) == base.Sign && Math.Abs(num) > base.Pds.CostEpsilon)
			{
				if (_fRecalcedCosts)
				{
					return true;
				}
				double num2 = num / _vecCost.GetCoef(base.VarEnter);
				if (0.5 <= num2 && num2 <= 2.0)
				{
					return true;
				}
				if (base.Logger.ShouldLog(7))
				{
					base.Logger.LogEvent(7, "{0} Detected bad relative cost: {1} {2}", _thd.PivotCount, num, _vecCost.GetCoef(base.VarEnter));
				}
			}
			return false;
		}

		protected override void FixBadCost()
		{
			if (!_fRecalcedCosts)
			{
				_fRecalcedCosts = true;
				ComputeCosts();
			}
			else
			{
				base.FixBadCost();
			}
		}

		protected override void UpdateCosts(double dnumLeaveCost)
		{
			if (base.Pds.Basis.GetDoubleFactorization().EtaCount == 0)
			{
				ComputeCosts();
			}
			else
			{
				UpdateCostsCore(dnumLeaveCost);
			}
		}

		protected virtual void UpdateCostsCore(double dnumLeaveCost)
		{
			double coef = _vecCost.GetCoef(base.VarEnter);
			SimplexFactoredBasis basis = base.Pds.Basis;
			_vecDual.Clear();
			_vecDual.SetCoefNonZero(base.IvarLeave, 1.0);
			basis.InplaceSolveRow(_vecDual);
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecDual);
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
						double coef2 = _vecCost.GetCoef(column);
						double num2 = coef2 - num * rowIter.Approx;
						if (Math.Abs(num2) <= 1E-12 * Math.Abs(coef2))
						{
							_vecCost.RemoveCoef(column);
							_vecCostFiltered.RemoveCoef(column);
						}
						else
						{
							_vecCost.SetCoefNonZero(column, num2);
							_vecCostFiltered.SetCoefNonZero(column, num2);
						}
					}
					rowIter.Advance();
				}
				iter.Advance();
			}
			_vecCost.RemoveCoef(base.VarEnter);
			_vecCostFiltered.RemoveCoef(base.VarEnter);
			double num3;
			if (basis.GetVvk(base.VarLeave) == SimplexVarValKind.Fixed || (num3 = (0.0 - coef) / _numKey - dnumLeaveCost) == 0.0)
			{
				_vecCost.RemoveCoef(base.VarLeave);
				_vecCostFiltered.RemoveCoef(base.VarLeave);
			}
			else
			{
				_vecCost.SetCoefNonZero(base.VarLeave, num3);
				_vecCostFiltered.SetCoefNonZero(base.VarLeave, num3);
			}
			VerifyCostsDbl();
		}

		protected void VerifyCosts()
		{
			base.Logger.ShouldLog(3);
		}

		protected void VerifyCostsDbl()
		{
			base.Logger.ShouldLog(3);
		}
	}
}
