using System;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class FullWeightedCostDbl : FullReducedCostDbl
	{
		protected double[] _mpvarnumWeights;

		protected double[] _mpvarnumWeightsInit;

		protected double[] _rgnumProd;

		public FullWeightedCostDbl(SimplexTask thd, PrimalDouble pds)
			: base(thd, pds)
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

		protected override int GetBestVar(ref VectorDDouble vec, out double numCostRet, out int signRet)
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
				double num4 = _mpvarnumWeights[num2];
				SimplexVarValKind simplexVarValKind = SimplexBasis.InterpretVvk(varIVar[num2]);
				double num5 = num3 * num3;
				double num6 = num4 * num;
				if ((simplexVarValKind == SimplexVarValKind.Lower && num3 < 0.0) || (0.0 < num3 && simplexVarValKind == SimplexVarValKind.Upper) || simplexVarValKind == SimplexVarValKind.Zero)
				{
					iter.Advance();
					if (num5 > num6)
					{
						signRet = Math.Sign(num3);
						num = num5 / num4;
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

		/// <summary> The weights are reset whenever the relative costs are computed from scratch.
		/// </summary>
		protected override void ComputeCosts()
		{
			base.ComputeCosts();
			Array.Copy(_mpvarnumWeightsInit, _mpvarnumWeights, _varLim);
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
					num += colIter.Approx * colIter.Approx;
					colIter.Advance();
				}
				_mpvarnumWeightsInit[i] = num + 1.0;
			}
		}

		/// <summary> Update the relative costs and weights against
		///           a basis which has changed by committing a pivot.
		/// </summary>
		protected override void UpdateCostsCore(double dnumLeaveCost)
		{
			base.Duals.Clear();
			base.Duals.SetCoefNonZero(base.IvarLeave, 1.0);
			base.Pds.Basis.InplaceSolveRow(base.Duals);
			bool[] colSets = SimplexSolver.ComputeProductNonBasic(base.Pds.Basis, _mod.Matrix, base.Duals, _rgnumProd);
			UpdateCostsAndWeightsUsingProd(dnumLeaveCost, colSets);
			VerifyCostsDbl();
		}

		private void UpdateCostsAndWeightsUsingProd(double dnumLeaveCost, bool[] colSets)
		{
			double num = SimplexSolver.ComputeNorm2(base.Delta, _ivarKey);
			double num2 = _numKey * _numKey;
			double coef = base.Costs.GetCoef(base.VarEnter);
			int num3 = colSets.Length;
			while (0 <= --num3)
			{
				if (!colSets[num3])
				{
					continue;
				}
				int num4 = num3 << 5;
				int num5 = Math.Min(num4 + 32, _varLim);
				while (num4 <= --num5)
				{
					double num6 = _rgnumProd[num5];
					if (num6 != 0.0)
					{
						_rgnumProd[num5] = 0.0;
						double coef2 = base.Costs.GetCoef(num5);
						double num7 = coef2 - num6 * coef;
						if (Math.Abs(num7) <= 1E-12 * Math.Abs(coef2))
						{
							base.Costs.RemoveCoef(num5);
							_vecCostFiltered.RemoveCoef(num5);
						}
						else
						{
							base.Costs.SetCoefNonZero(num5, num7);
							_vecCostFiltered.SetCoefNonZero(num5, num7);
						}
						double num8 = num6 * num6;
						double num9 = (num - num2 + 1.0) * num8 + _mpvarnumWeights[num5];
						double num10 = (num + 1.0) * num8 + 1.0;
						double num11 = ((num9 >= num10) ? num9 : num10);
						_mpvarnumWeights[num5] = num11;
					}
				}
			}
			_mpvarnumWeights[base.VarEnter] = 0.0;
			_mpvarnumWeights[base.VarLeave] = 1.0 + (1.0 + num) / num2;
			base.Costs.RemoveCoef(base.VarEnter);
			_vecCostFiltered.RemoveCoef(base.VarEnter);
			double num12;
			if (base.Pds.Basis.GetVvk(base.VarLeave) == SimplexVarValKind.Fixed || (num12 = (0.0 - coef) / _numKey - dnumLeaveCost) == 0.0)
			{
				base.Costs.RemoveCoef(base.VarLeave);
				_vecCostFiltered.RemoveCoef(base.VarLeave);
			}
			else
			{
				base.Costs.SetCoefNonZero(base.VarLeave, num12);
				_vecCostFiltered.SetCoefNonZero(base.VarLeave, num12);
			}
		}
	}
}
