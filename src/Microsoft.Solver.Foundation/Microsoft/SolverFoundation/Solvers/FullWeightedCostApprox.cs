using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class FullWeightedCostApprox : FullReducedCostApprox
	{
		private const int kcpivRefreshWeights = 50;

		protected double[] _mpvarnumWeights;

		private double[] _mpvarnumWeightsInit;

		private double[] _rgnumProd;

		public FullWeightedCostApprox(SimplexTask thd, PrimalExact pes)
			: base(thd, pes)
		{
		}

		public override void Init()
		{
			base.Init();
			if (_mpvarnumWeights == null || _mpvarnumWeights.Length < _varLim)
			{
				_mpvarnumWeights = new double[_varLim];
			}
			if (_mpvarnumWeightsInit == null || _mpvarnumWeightsInit.Length < _varLim)
			{
				_mpvarnumWeightsInit = new double[_varLim];
			}
			if (_rgnumProd == null || _rgnumProd.Length < _varLim)
			{
				_rgnumProd = new double[_varLim];
			}
			InitWeightsInit();
			Array.Copy(_mpvarnumWeightsInit, _mpvarnumWeights, _varLim);
		}

		protected override bool GetNextVar(double numMin, ref Vector<double>.Iter iter, out int varRet, out double numCostRet, out int signRet)
		{
			SimplexBasis basis = _pes.Basis;
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				double value = iter.Value;
				switch (basis.GetVvk(rc))
				{
				case SimplexVarValKind.Lower:
					if (value >= 0.0)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					break;
				case SimplexVarValKind.Upper:
					if (value <= 0.0)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					break;
				default:
					iter.RemoveAndAdvance();
					continue;
				case SimplexVarValKind.Zero:
					break;
				}
				iter.Advance();
				double num = value * value / _mpvarnumWeights[rc];
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

		/// <summary> Compute the actual column norms for initial weights.
		/// </summary>
		protected virtual void InitWeightsInit()
		{
			for (int i = 0; i < _varLim; i++)
			{
				double num = 0.0;
				CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mod.Matrix, i);
				while (colIter.IsValid)
				{
					double num2 = (double)colIter.Exact;
					num += num2 * num2;
					colIter.Advance();
				}
				_mpvarnumWeightsInit[i] = num + 1.0;
			}
		}

		protected override void RefreshCosts()
		{
			base.RefreshCosts();
			Array.Copy(_mpvarnumWeightsInit, _mpvarnumWeights, _varLim);
		}

		protected override void UpdateCostsCore(Rational dnumLeaveCost)
		{
			if (_cpiv % 50 == 0)
			{
				Array.Copy(_mpvarnumWeightsInit, _mpvarnumWeights, _varLim);
				base.UpdateCostsCore(dnumLeaveCost);
			}
			else
			{
				UpdateCostsAndWeights(dnumLeaveCost, fCosts: true);
			}
		}

		protected virtual void UpdateCostsAndWeights(Rational dnumLeaveCost, bool fCosts)
		{
			base.IvarLeave = _pes.Basis.GetBasisSlot(base.VarEnter);
			if (fCosts)
			{
				_fExactCostsValid = false;
			}
			_vecDualApprox.Clear();
			_vecDualApprox.SetCoefNonZero(base.IvarLeave, 1.0);
			_pes.Basis.InplaceSolveApproxRow(_vecDualApprox);
			SimplexBasis basis = _pes.Basis;
			SimplexSolver.ComputeProductNonBasicFromExact(basis, _mod.Matrix, _vecDualApprox, _rgnumProd);
			double num = SimplexSolver.ComputeDblNorm2(base.Delta, _ivarKey);
			double num2 = (double)_numKey;
			num2 *= num2;
			double num3 = (fCosts ? _vecCostApprox.GetCoef(base.VarEnter) : 0.0);
			int num4 = _varLim;
			while (true)
			{
				if (--num4 >= 0 && _rgnumProd[num4] == 0.0)
				{
					continue;
				}
				if (num4 < 0)
				{
					break;
				}
				double num5 = _rgnumProd[num4];
				_rgnumProd[num4] = 0.0;
				if (fCosts)
				{
					double num6 = _vecCostApprox.GetCoef(num4) - num5 * num3;
					if (num6 == 0.0)
					{
						_vecCostApprox.RemoveCoef(num4);
						_vecCostApproxFiltered.RemoveCoef(num4);
					}
					else
					{
						_vecCostApprox.SetCoefNonZero(num4, num6);
						_vecCostApproxFiltered.SetCoefNonZero(num4, num6);
					}
				}
				double num7 = num5 * num5;
				double num8 = (num - num2 + 1.0) * num7 + _mpvarnumWeights[num4];
				double num9 = (num + 1.0) * num7 + 1.0;
				double num10 = ((num8 >= num9) ? num8 : num9);
				_mpvarnumWeights[num4] = num10;
			}
			_mpvarnumWeights[base.VarEnter] = 0.0;
			_mpvarnumWeights[base.VarLeave] = 1.0 + (1.0 + num) / num2;
			if (fCosts)
			{
				_vecCostApprox.RemoveCoef(base.VarEnter);
				_vecCostApproxFiltered.RemoveCoef(base.VarEnter);
				double num11;
				if (basis.GetVvk(base.VarLeave) == SimplexVarValKind.Fixed || (num11 = (0.0 - num3) / (double)_numKey - (double)dnumLeaveCost) == 0.0)
				{
					_vecCostApprox.RemoveCoef(base.VarLeave);
					_vecCostApproxFiltered.RemoveCoef(base.VarLeave);
				}
				else
				{
					_vecCostApprox.SetCoefNonZero(base.VarLeave, num11);
					_vecCostApproxFiltered.SetCoefNonZero(base.VarLeave, num11);
				}
			}
		}
	}
}
