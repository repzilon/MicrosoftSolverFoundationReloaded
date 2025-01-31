using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class PartialPricingDbl : PivotStrategyDbl
	{
		private const double knumCostEps = 1E-12;

		private int _cvarTarget;

		private int _cvarStart;

		protected double _numThresh;

		private Heap<int> _heap;

		protected VectorDouble _vecCost;

		private VectorDouble _vecDual;

		protected double[] _mpvarnumWeights;

		private double[] _mpvarnumWeightsInit;

		public PartialPricingDbl(SimplexTask thd, PrimalDouble pds, int cvarTarget)
			: base(thd, pds)
		{
			_cvarTarget = Math.Max(40, cvarTarget);
			Func<int, int, bool> fnReverse = (int var1, int var2) => WeightedCost(var1) > WeightedCost(var2);
			_heap = new Heap<int>(fnReverse);
		}

		private double WeightedCost(int var)
		{
			double coef = _vecCost.GetCoef(var);
			return coef * coef / _mpvarnumWeights[var];
		}

		public override void Init()
		{
			base.Init();
			if (_vecCost == null || _vecCost.RcCount != _varLim)
			{
				_vecCost = new VectorDouble(_varLim);
			}
			if (_vecDual == null || _vecDual.RcCount != _rowLim)
			{
				_vecDual = new VectorDouble(_rowLim);
			}
			if (_mpvarnumWeights == null || _mpvarnumWeights.Length < _varLim)
			{
				_mpvarnumWeights = new double[_varLim];
			}
			if (_mpvarnumWeightsInit == null || _mpvarnumWeightsInit.Length < _varLim)
			{
				_mpvarnumWeightsInit = new double[_varLim];
			}
			InitWeightsInit();
			ComputeCosts();
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

		/// <summary>
		/// Compute the reduced costs and put them in _rgnumCost.
		/// </summary>
		protected virtual void ComputeCosts()
		{
			ComputeCostsCore(_vecCost);
			FilterCosts();
		}

		protected virtual void ComputeCostsCore(VectorDouble vecCost)
		{
			SimplexSolver.ComputeReducedCostsAndDual(base.Pds.Basis, _mod.Matrix, base.Pds.GetCostVector(), 1E-12, vecCost, _vecDual);
		}

		protected virtual void FilterCosts()
		{
			_heap.Clear();
			SimplexBasis basis = base.Pds.Basis;
			double costEpsilon = base.Pds.CostEpsilon;
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecCost);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				double value = iter.Value;
				switch (basis.GetVvk(rc))
				{
				case SimplexVarValKind.Lower:
					if (value >= 0.0 - costEpsilon)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					break;
				case SimplexVarValKind.Upper:
					if (value <= costEpsilon)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					break;
				case SimplexVarValKind.Zero:
					if (Math.Abs(value) <= costEpsilon)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					break;
				default:
					iter.RemoveAndAdvance();
					continue;
				}
				iter.Advance();
				_mpvarnumWeights[rc] = _mpvarnumWeightsInit[rc];
				if (_heap.Count < _cvarTarget)
				{
					_heap.Add(rc);
					continue;
				}
				int top = _heap.Top;
				if (WeightedCost(rc) > WeightedCost(top))
				{
					_heap.Pop();
					iter.RemoveSeen(top);
					_heap.Add(rc);
				}
				else
				{
					iter.RemoveSeen(rc);
				}
			}
			if (_vecCost.EntryCount == 0)
			{
				_numThresh = costEpsilon;
			}
			else
			{
				_numThresh = Math.Max(costEpsilon, Math.Abs(_vecCost.GetCoef(_heap.Top)) / 2.0);
			}
			_cvarStart = _vecCost.EntryCount;
		}

		protected override bool ClearSkips()
		{
			return false;
		}

		protected override void SkipVar(int var, byte skip)
		{
			_vecCost.RemoveCoef(var);
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
				if (!FindEnteringVar())
				{
					if (flag)
					{
						return false;
					}
					ComputeCosts();
					flag = true;
					if (!FindEnteringVar())
					{
						if (!ReduceError())
						{
							return false;
						}
						if (!FindEnteringVar())
						{
							return false;
						}
					}
				}
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
			return true;
		}

		/// <summary>
		/// Sets _varEnter and _sign.
		/// </summary>
		protected override bool FindEnteringVar()
		{
			double num = 0.0;
			base.VarEnter = -1;
			SimplexBasis basis = base.Pds.Basis;
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecCost);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				double value = iter.Value;
				SimplexVarValKind vvk = basis.GetVvk(rc);
				int sign;
				switch (vvk)
				{
				case SimplexVarValKind.Lower:
					if (value >= 0.0 - _numThresh)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					sign = -1;
					break;
				case SimplexVarValKind.Upper:
					if (value <= _numThresh)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					sign = 1;
					break;
				case SimplexVarValKind.Zero:
					sign = Math.Sign(value);
					if (Math.Abs(value) <= _numThresh)
					{
						iter.RemoveAndAdvance();
						continue;
					}
					break;
				default:
					iter.RemoveAndAdvance();
					continue;
				}
				iter.Advance();
				value = value * value / _mpvarnumWeights[rc];
				if (num < value)
				{
					num = value;
					base.VarEnter = rc;
					base.VvkEnter = vvk;
					base.Sign = sign;
					_dblApproxCost = value;
				}
			}
			return base.VarEnter >= 0;
		}

		protected override bool ReduceError()
		{
			if (!base.ReduceError())
			{
				return false;
			}
			ComputeCosts();
			return true;
		}

		protected override bool ValidateCost()
		{
			double value = ComputeRelativeCostFromDelta();
			if (Math.Sign(value) == base.Sign && Math.Abs(value) > base.Pds.CostEpsilon / 2.0)
			{
				return true;
			}
			return false;
		}

		protected override void FixBadCost()
		{
			_vecCost.RemoveCoef(base.VarEnter);
		}

		protected override void UpdateCosts(double dnumLeaveCost)
		{
			if (base.Pds.Basis.GetDoubleFactorization().EtaCount == 0)
			{
				ComputeCosts();
			}
			else if (_vecCost.EntryCount <= _cvarStart / 10)
			{
				ComputeCosts();
			}
			else
			{
				UpdateCostsAndWeights(dnumLeaveCost);
			}
		}

		protected virtual void UpdateCostsAndWeights(double dnumLeaveCost)
		{
			_vecDual.Clear();
			_vecDual.SetCoefNonZero(base.IvarLeave, 1.0);
			base.Pds.Basis.InplaceSolveRow(_vecDual);
			double num = SimplexSolver.ComputeNorm2(base.Delta, _ivarKey);
			double num2 = _numKey * _numKey;
			double coef = _vecCost.GetCoef(base.VarEnter);
			_vecCost.RemoveCoef(base.VarEnter);
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecCost);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				double num3 = 0.0;
				CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mod.Matrix, rc);
				while (colIter.IsValid)
				{
					int row = colIter.Row;
					double coef2 = _vecDual.GetCoef(row);
					if (coef2 != 0.0)
					{
						double value = num3;
						num3 += coef2 * colIter.Approx;
						if (Math.Abs(num3) <= 1E-12 * Math.Abs(value))
						{
							num3 = 0.0;
						}
					}
					colIter.Advance();
				}
				if (num3 == 0.0)
				{
					iter.Advance();
					continue;
				}
				double num4 = iter.Value - num3 * coef;
				if (num4 == 0.0)
				{
					iter.RemoveAndAdvance();
					continue;
				}
				iter.Advance();
				_vecCost.SetCoefNonZero(rc, num4);
				double num5 = num3 * num3;
				double num6 = (num - num2 + 1.0) * num5 + _mpvarnumWeights[rc];
				double num7 = (num + 1.0) * num5 + 1.0;
				double num8 = ((num6 >= num7) ? num6 : num7);
				_mpvarnumWeights[rc] = num8;
			}
		}
	}
}
