using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class NewPartialPricingDbl : PivotStrategyDbl
	{
		protected const double knumCostEps = 1E-10;

		private int _cvarTarget;

		private double _numThresh;

		private Heap<int> _heap;

		private VectorDouble _vecCost;

		private VectorDouble _vecDual;

		private double[] _mpvarnumWeights;

		private double[] _mpvarnumWeightsInit;

		private int _start;

		private int _fraction = 1;

		public NewPartialPricingDbl(SimplexTask thd, PrimalDouble pds, int cvarTarget)
			: base(thd, pds)
		{
			_cvarTarget = Math.Max(40, cvarTarget);
			Func<int, int, bool> fnReverse = (int var1, int var2) => WeightedCost(var1) > WeightedCost(var2);
			_heap = new Heap<int>(fnReverse);
		}

		private double WeightedCost(double dbl, int var)
		{
			return dbl * dbl / _mpvarnumWeights[var];
		}

		private double WeightedCost(int var)
		{
			return WeightedCost(_vecCost.GetCoef(var), var);
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

		private static int RelativePrime(int big, int little)
		{
			int num = big % little;
			while (num == 0 || little % num == 0)
			{
				little++;
				num = big % little;
			}
			return little;
		}

		/// <summary>
		/// Compute the reduced costs and put them in _rgnumCost.
		/// </summary>
		protected virtual void ComputeCosts()
		{
			SimplexFactoredBasis basis = base.Pds.Basis;
			VectorDouble costVector = base.Pds.GetCostVector();
			int entryCount = costVector.EntryCount;
			List<int> list = new List<int>(_heap.Count);
			_vecDual.Clear();
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecCost);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				if (basis.GetBasisSlot(rc) < 0)
				{
					list.Add(rc);
				}
				iter.Advance();
			}
			_heap.Clear();
			_vecCost.Clear();
			if (0 >= entryCount)
			{
				return;
			}
			Vector<double>.Iter iter2 = new Vector<double>.Iter(costVector);
			while (iter2.IsValid)
			{
				int rc2 = iter2.Rc;
				int basisSlot = basis.GetBasisSlot(rc2);
				if (basisSlot >= 0)
				{
					_vecDual.SetCoefNonZero(basisSlot, iter2.Value);
				}
				else if (basis.GetVvk(rc2) >= SimplexVarValKind.Lower)
				{
					_vecCost.SetCoefNonZero(rc2, iter2.Value);
				}
				iter2.Advance();
			}
			if (_vecDual.EntryCount <= 0)
			{
				return;
			}
			int[] array = list.ToArray();
			Array.Sort(array);
			basis.InplaceSolveRow(_vecDual);
			double costEpsilon = base.Pds.CostEpsilon;
			double minWeight = 0.0;
			foreach (int var in array)
			{
				FilterThruHeap(basis.GetVvk(var), var, costEpsilon, ref minWeight);
			}
			int num = ((1 == _fraction) ? 1 : RelativePrime(_varLim, 4));
			int val = Math.Max(_cvarTarget * 4, _varLim / _fraction);
			_fraction = 4;
			val = Math.Min(_varLim, val);
			int num2 = 0;
			int num3 = _start % _varLim;
			while (num2 < val || (_heap.Count == 0 && num2 < _varLim))
			{
				num2++;
				if (0.0 == _vecCost.GetCoef(num3))
				{
					FilterThruHeap(basis.GetVvk(num3), num3, costEpsilon, ref minWeight);
				}
				num3 = (num3 + num) % _varLim;
			}
			_start++;
			if (_vecCost.EntryCount == 0)
			{
				_numThresh = costEpsilon;
			}
			else
			{
				_numThresh = Math.Max(costEpsilon, Math.Abs(_vecCost.GetCoef(_heap.Top)) / 2.0);
			}
		}

		protected virtual void FilterThruHeap(SimplexVarValKind vvk, int var, double numEps, ref double minWeight)
		{
			if (vvk < SimplexVarValKind.Lower)
			{
				return;
			}
			double num = _vecCost.GetCoef(var);
			double num2 = Math.Abs(num);
			CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mod.Matrix, var);
			while (colIter.IsValid)
			{
				num -= colIter.Approx * _vecDual.GetCoef(colIter.Row);
				colIter.Advance();
			}
			if (Math.Abs(num) <= 1E-10 * num2)
			{
				_vecCost.RemoveCoef(var);
				return;
			}
			if ((vvk == SimplexVarValKind.Lower && num >= 0.0 - numEps) || (vvk == SimplexVarValKind.Upper && num <= numEps) || (vvk == SimplexVarValKind.Zero && Math.Abs(num) <= numEps))
			{
				_vecCost.RemoveCoef(var);
				return;
			}
			_mpvarnumWeights[var] = _mpvarnumWeightsInit[var];
			if (_heap.Count < _cvarTarget)
			{
				_vecCost.SetCoefNonZero(var, num);
				_heap.Add(var);
				if (_heap.Count == _cvarTarget)
				{
					minWeight = WeightedCost(_heap.Top);
				}
			}
			else if (WeightedCost(num, var) > minWeight)
			{
				_vecCost.RemoveCoef(_heap.Pop());
				_vecCost.SetCoefNonZero(var, num);
				_heap.Add(var);
				minWeight = WeightedCost(_heap.Top);
			}
			else
			{
				_vecCost.RemoveCoef(var);
			}
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
			_dblApproxCost = 0.0;
			base.VarEnter = -1;
			SimplexBasis basis = base.Pds.Basis;
			Vector<double>.Iter iter = new Vector<double>.Iter(_vecCost);
			while (iter.IsValid)
			{
				int rc = iter.Rc;
				double value = iter.Value;
				SimplexVarValKind vvk = basis.GetVvk(rc);
				if (vvk < SimplexVarValKind.Lower || (vvk == SimplexVarValKind.Lower && value >= 0.0 - _numThresh) || (vvk == SimplexVarValKind.Upper && value <= _numThresh) || (vvk == SimplexVarValKind.Zero && Math.Abs(value) <= _numThresh))
				{
					iter.RemoveAndAdvance();
					continue;
				}
				iter.Advance();
				double num = WeightedCost(value, rc);
				if (_dblApproxCost < num)
				{
					_dblApproxCost = num;
					base.VarEnter = rc;
					base.VvkEnter = vvk;
					base.Sign = Math.Sign(value);
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
			double num = ComputeRelativeCostFromDelta() * (double)base.Sign;
			if (base.Pds.CostEpsilon / 2.0 < num)
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
			if (_vecCost.EntryCount * 2 <= _cvarTarget)
			{
				ComputeCosts();
			}
			else
			{
				UpdateCostsAndWeights(dnumLeaveCost);
			}
		}

		/// <summary> Update the relative costs and weights.
		/// </summary>
		protected virtual void UpdateCostsAndWeights(double dnumLeaveCost)
		{
			_vecDual.Clear();
			_vecDual.SetCoefNonZero(base.IvarLeave, 1.0);
			base.Pds.Basis.InplaceSolveRow(_vecDual);
			double num = ComputeDeltaNorm2();
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
					num3 += _vecDual.GetCoef(colIter.Row) * colIter.Approx;
					colIter.Advance();
				}
				double num4 = num3 * coef;
				double value = iter.Value;
				if (Math.Abs(num4) <= 1E-10 * Math.Abs(value))
				{
					iter.Advance();
					continue;
				}
				double num5 = value - num4;
				if (Math.Abs(num5) <= 1E-10 * Math.Abs(value))
				{
					iter.RemoveAndAdvance();
					continue;
				}
				iter.Advance();
				_vecCost.SetCoefNonZero(rc, num5);
				double num6 = num3 * num3;
				double num7 = (num - num2 + 1.0) * num6 + _mpvarnumWeights[rc];
				double num8 = (num + 1.0) * num6 + 1.0;
				double num9 = ((num7 >= num8) ? num7 : num8);
				_mpvarnumWeights[rc] = num9;
			}
		}

		private double ComputeDeltaNorm2()
		{
			double num = 0.0;
			Vector<double>.Iter iter = new Vector<double>.Iter(base.Delta);
			while (iter.IsValid)
			{
				if (iter.Rc != _ivarKey)
				{
					double value = iter.Value;
					num += value * value;
				}
				iter.Advance();
			}
			return num;
		}
	}
}
