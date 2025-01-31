using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class SimplexBasis
	{
		protected readonly SimplexReducedModel _mod;

		protected readonly SimplexTask _thd;

		/// <summary> The basic variables.
		/// </summary>
		internal int[] _rgvarBasic;

		protected int _cvarBasic;

		/// <summary> For basic variables, this records the location in _rgvarBasic.
		///           For non-basic variables, this records the negative of the SimplexVarValKind.
		/// </summary>
		internal int[] _mpvarivar;

		public SimplexReducedModel Model => _mod;

		public int[] VarIVar => _mpvarivar;

		public SimplexBasis(SimplexTask thd, SimplexReducedModel mod)
		{
			_thd = thd;
			_mod = mod;
			_rgvarBasic = new int[mod.RowLim];
			_mpvarivar = new int[mod.VarLim];
		}

		public SimplexBasis Clone()
		{
			SimplexBasis simplexBasis = new SimplexBasis(_thd, _mod);
			Array.Copy(_rgvarBasic, simplexBasis._rgvarBasic, _rgvarBasic.Length);
			Array.Copy(_mpvarivar, simplexBasis._mpvarivar, _mpvarivar.Length);
			simplexBasis._cvarBasic = _cvarBasic;
			return simplexBasis;
		}

		protected int SetTo(SimplexBasis bas, bool fMinimizeChange)
		{
			if (bas == this)
			{
				return 0;
			}
			if (fMinimizeChange && _cvarBasic == _mod.RowLim && bas._cvarBasic == _mod.RowLim)
			{
				int result = 0;
				int num = _mod.VarLim;
				while (true)
				{
					if (--num < 0)
					{
						return result;
					}
					int num2 = _mpvarivar[num];
					int num3 = bas._mpvarivar[num];
					if (num2 == num3)
					{
						continue;
					}
					if (num2 < 0)
					{
						if (num3 >= 0)
						{
							break;
						}
						result = 1;
						_mpvarivar[num] = num3;
					}
					else if (num3 < 0)
					{
						break;
					}
				}
			}
			Array.Copy(bas._rgvarBasic, _rgvarBasic, _rgvarBasic.Length);
			Array.Copy(bas._mpvarivar, _mpvarivar, _mpvarivar.Length);
			return 2;
		}

		/// <summary> Lookup the variable associated with basis index ivar.
		/// </summary>
		public int GetBasicVar(int ivar)
		{
			return _rgvarBasic[ivar];
		}

		/// <summary> Test if variable var is part of the current basis.
		/// </summary>
		public bool IsBasic(int var)
		{
			return _mpvarivar[var] >= 0;
		}

		public static SimplexVarValKind InterpretVvk(int ivar)
		{
			return (SimplexVarValKind)(-(ivar & (ivar >> 31)));
		}

		public SimplexVarValKind GetVvk(int var)
		{
			int num = _mpvarivar[var];
			return (SimplexVarValKind)(-(num & (num >> 31)));
		}

		/// <summary> Lookup the basis index equivalent to the variable var.
		///           -1 is returned if the variable is not in the basis.
		/// </summary>
		public int GetBasisSlot(int var)
		{
			int num = _mpvarivar[var];
			return num | (num >> 31);
		}

		/// <summary> Set this basis to the set of slack variables in the given reduced model.
		/// </summary>
		public void SetToSlacks()
		{
			int num = _mod.VarLim;
			while (--num >= 0)
			{
				_mpvarivar[num] = 0 - DetermineVvk(num);
			}
			int num2 = _mod.RowLim;
			while (--num2 >= 0)
			{
				int slackVarForRow = _mod.GetSlackVarForRow(num2);
				_rgvarBasic[num2] = slackVarForRow;
				_mpvarivar[slackVarForRow] = num2;
			}
			_cvarBasic = _mod.RowLim;
		}

		/// <summary> Choose an initial basis on blended principles of avoiding restrictions,
		///           maximizing basis sparseness, and domains likely to help feasibility.
		/// </summary>
		private void SetToFreedomInner(double slackThreshold, double entryCountWeight)
		{
			int num = _mod.VarLim;
			while (--num >= 0)
			{
				_mpvarivar[num] = 0 - DetermineVvk(num);
			}
			int[] array = new int[_mod.RowLim];
			float[] array2 = new float[_mod.RowLim];
			int num2 = 0;
			int num3 = _mod.RowLim;
			while (--num3 >= 0)
			{
				int slackVarForRow = _mod.GetSlackVarForRow(num3);
				double lowerBoundDbl = _mod.GetLowerBoundDbl(slackVarForRow);
				double upperBoundDbl = _mod.GetUpperBoundDbl(slackVarForRow);
				if (slackThreshold < upperBoundDbl - lowerBoundDbl)
				{
					_rgvarBasic[num3] = slackVarForRow;
					_mpvarivar[slackVarForRow] = num3;
					array[num3] = 1;
				}
				else
				{
					_rgvarBasic[num3] = -1;
					array2[num3] = float.PositiveInfinity;
					num2++;
				}
			}
			if (num2 == 0)
			{
				return;
			}
			double[] mpvarQ = new double[_mod.VarLim];
			int goalRow;
			if (0 < _mod.GoalCount && (goalRow = _mod.GetGoalRow(0)) >= 0)
			{
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mod.Matrix, goalRow);
				while (rowIter.IsValid)
				{
					mpvarQ[rowIter.Column] = Math.Abs(rowIter.Approx) * 0.001;
					rowIter.Advance();
				}
			}
			int[] array3 = new int[_mod.VarLim - _mod.RowLim + num2];
			int num4 = 0;
			for (int i = 0; i < _mod.VarLim; i++)
			{
				if (_mpvarivar[i] < 0)
				{
					double num5 = Math.Min(_mod.GetUpperBoundDbl(i), 1000000000.0);
					double num6 = Math.Max(_mod.GetLowerBoundDbl(i), -1000000000.0);
					mpvarQ[i] += num6 - num5 + entryCountWeight * (double)_mod.Matrix.ColEntryCount(i);
					array3[num4++] = i;
				}
			}
			Array.Sort(array3, (int l, int r) => mpvarQ[l].CompareTo(mpvarQ[r]));
			int[] array4 = array3;
			foreach (int num7 in array4)
			{
				double num8 = 0.0;
				double num9 = 0.0;
				int num10 = -1;
				int num11 = -1;
				bool flag = false;
				CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mod.Matrix, num7);
				while (colIter.IsValid)
				{
					double num12 = Math.Abs(colIter.Approx);
					int row = colIter.Row;
					if (array[row] == 0 && num8 < num12)
					{
						num8 = num12;
						num10 = row;
					}
					if (100.0 * num12 > (double)array2[row])
					{
						flag = true;
					}
					if (_rgvarBasic[row] < 0 && num9 < num12)
					{
						num9 = num12;
						num11 = row;
					}
					colIter.Advance();
				}
				bool flag2 = 0.99 <= num8;
				if (!flag2 && !flag && 0.0 < num9)
				{
					flag2 = true;
					num8 = num9;
					num10 = num11;
				}
				if (flag2)
				{
					_rgvarBasic[num10] = num7;
					_mpvarivar[num7] = num10;
					array2[num10] = (float)num8;
					num2--;
					CoefMatrix.ColIter colIter2 = new CoefMatrix.ColIter(_mod.Matrix, num7);
					while (colIter2.IsValid)
					{
						array[colIter2.Row]++;
						colIter2.Advance();
					}
				}
				if (num2 == 0)
				{
					return;
				}
			}
			for (int k = 0; k < _mod.RowLim; k++)
			{
				if (0 >= num2)
				{
					break;
				}
				if (_rgvarBasic[k] < 0)
				{
					int slackVarForRow2 = _mod.GetSlackVarForRow(k);
					_rgvarBasic[k] = slackVarForRow2;
					_mpvarivar[slackVarForRow2] = k;
					num2--;
				}
			}
		}

		/// <summary> Choose an initial basis using Bixby's idea of maximally free variables.
		/// </summary>
		public void SetToFreedom()
		{
			SetToFreedomInner(10.0, 1.0);
		}

		/// <summary> Use a simple lower triangular symbolic most feasible crash basis [CRASH(LTSF)]
		/// </summary>
		public void SetToCrash()
		{
			SetToSlacks();
			CountList rowCountList = BuildRowCountList();
			CountList colCountList = BuildColCountList();
			int rowChoice;
			int count;
			while ((rowChoice = rowCountList.GetFirst(out count)) != -1)
			{
				if (GetRowSelection(count, rowCountList, out rowChoice))
				{
					GetVarSelection(rowChoice, colCountList, out var varChoice);
					if (varChoice >= 0)
					{
						MarkAsBasic(rowChoice, varChoice);
					}
					rowCountList.Remove(rowChoice);
				}
			}
		}

		private bool GetRowSelection(int count, CountList rowCountList, out int rowChoice)
		{
			bool result = false;
			int num = int.MinValue;
			rowChoice = -1;
			foreach (int item in rowCountList.GetNext(count))
			{
				if (!rowCountList.IsEliminated(item))
				{
					result = true;
					int rowPriority = GetRowPriority(item, count);
					if (rowPriority > num)
					{
						rowChoice = item;
						num = rowPriority;
					}
				}
			}
			return result;
		}

		private void GetVarSelection(int row, CountList colCountList, out int varChoice)
		{
			int num = int.MinValue;
			varChoice = -1;
			CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_mod.Matrix, row);
			while (rowIter.IsValid)
			{
				int column = rowIter.Column;
				if (!colCountList.IsEliminated(column))
				{
					int colPriority = GetColPriority(column, colCountList.Count(column));
					if (colPriority > num)
					{
						varChoice = column;
						num = colPriority;
					}
					colCountList.Remove(column);
				}
				rowIter.Advance();
			}
		}

		private void MarkAsBasic(int row, int varEnter)
		{
			int num = _rgvarBasic[row];
			if (varEnter != num)
			{
				MajorPivot(varEnter, GetBasisSlot(num), num, DetermineVvk(num));
			}
		}

		/// <summary> walk each row to add non-free rows to the row count list 
		/// </summary>
		/// <returns></returns>
		private CountList BuildRowCountList()
		{
			CountList result = new CountList(_mod._rowLim, _mod._varLim);
			for (int i = 0; i < _mod._rowLim; i++)
			{
				if (GetRowType(i) != 3)
				{
					int size = _mod.Matrix.RowEntryCount(i);
					result.Add(i, size);
				}
			}
			return result;
		}

		/// <summary> walk each column escape fixed/slack var
		/// </summary>
		/// <returns></returns>
		private CountList BuildColCountList()
		{
			CountList result = new CountList(_mod._varLim, _mod._rowLim);
			for (int i = 0; i < _mod._varLim; i++)
			{
				if (!_mod.IsSlackVar(i) && GetVarType(i) != 0)
				{
					int size = _mod.Matrix.ColEntryCount(i);
					result.Add(i, size);
				}
			}
			return result;
		}

		private int GetRowPriority(int row, int count)
		{
			return GetRowType(row) - 3 - 10 * count;
		}

		private int GetColPriority(int var, int count)
		{
			return GetVarType(var) - 10 * count;
		}

		private int GetRowType(int row)
		{
			int slackVarForRow = _mod.GetSlackVarForRow(row);
			return GetVarType(slackVarForRow);
		}

		private int GetVarType(int var)
		{
			Rational lowerBound = _mod.GetLowerBound(var);
			Rational upperBound = _mod.GetUpperBound(var);
			if (lowerBound == upperBound)
			{
				return 0;
			}
			if (lowerBound.IsFinite && upperBound.IsFinite)
			{
				return 1;
			}
			if (lowerBound.IsFinite || upperBound.IsFinite)
			{
				return 2;
			}
			return 3;
		}

		protected SimplexVarValKind DetermineVvk(int var)
		{
			Rational lowerBound = _thd.BoundManager.GetLowerBound(var);
			Rational upperBound = _thd.BoundManager.GetUpperBound(var);
			if (lowerBound == upperBound)
			{
				return SimplexVarValKind.Fixed;
			}
			if (upperBound.AbsoluteValue < lowerBound.AbsoluteValue)
			{
				return SimplexVarValKind.Upper;
			}
			if (!lowerBound.IsFinite)
			{
				return SimplexVarValKind.Zero;
			}
			return SimplexVarValKind.Lower;
		}

		protected SimplexVarValKind DetermineVvk(int var, ref Rational numCur, out bool fBetween)
		{
			Rational lowerBound = _thd.BoundManager.GetLowerBound(var);
			Rational upperBound = _thd.BoundManager.GetUpperBound(var);
			fBetween = false;
			if (lowerBound == upperBound)
			{
				return SimplexVarValKind.Fixed;
			}
			if (numCur <= lowerBound)
			{
				return SimplexVarValKind.Lower;
			}
			if (numCur >= upperBound)
			{
				return SimplexVarValKind.Upper;
			}
			fBetween = true;
			if (!upperBound.IsFinite)
			{
				if (!lowerBound.IsFinite)
				{
					return SimplexVarValKind.Zero;
				}
			}
			else if (!lowerBound.IsFinite || numCur - lowerBound > upperBound - numCur)
			{
				return SimplexVarValKind.Upper;
			}
			return SimplexVarValKind.Lower;
		}

		/// <summary> Set this basis according to the set of values in the reduced model
		/// </summary>
		public void SetToVidValues(Rational[] mpvidnum)
		{
			int rowLim = _mod.RowLim;
			int num = rowLim;
			int num2 = rowLim;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = _mod.VarLim;
			while (--num6 >= 0)
			{
				int vid = _mod.GetVid(num6);
				Rational num7 = mpvidnum[vid];
				SimplexVarValKind simplexVarValKind;
				bool fBetween;
				if (!num7.IsFinite)
				{
					simplexVarValKind = DetermineVvk(num6);
					fBetween = false;
				}
				else
				{
					num7 = _mod.MapValueFromVidToVar(num6, num7);
					simplexVarValKind = DetermineVvk(num6, ref num7, out fBetween);
				}
				_mpvarivar[num6] = 0 - simplexVarValKind;
				if (num <= 0)
				{
					continue;
				}
				int b = num6;
				if (!_mod.HasBasicFlag(b))
				{
					if (simplexVarValKind == SimplexVarValKind.Zero)
					{
						if (num2 > 0)
						{
							_rgvarBasic[--num2] = b;
						}
					}
					else if (fBetween && num5 < num2)
					{
						_rgvarBasic[num5++] = b;
					}
				}
				else if (simplexVarValKind == SimplexVarValKind.Zero)
				{
					Statics.Swap(ref _rgvarBasic[--num], ref b);
					if (num2 > num)
					{
						num2 = num;
					}
					else if (num2 > 0)
					{
						_rgvarBasic[--num2] = b;
					}
				}
				else if (fBetween)
				{
					if (num3 < num2)
					{
						Statics.Swap(ref _rgvarBasic[num3++], ref b);
						if (num4 < num3)
						{
							num4 = num3;
						}
						else if (num4 < num2)
						{
							Statics.Swap(ref _rgvarBasic[num4++], ref b);
						}
						if (num5 < num4)
						{
							num5 = num4;
						}
						else if (num5 < num2)
						{
							_rgvarBasic[num5++] = b;
						}
					}
				}
				else if (num4 < num2)
				{
					Statics.Swap(ref _rgvarBasic[num4++], ref b);
					if (num5 < num4)
					{
						num5 = num4;
					}
					else if (num5 < num2)
					{
						_rgvarBasic[num5++] = b;
					}
				}
			}
			if (num4 < num2)
			{
				if (num2 < rowLim)
				{
					Array.Copy(_rgvarBasic, num2, _rgvarBasic, num4, rowLim - num2);
				}
				_cvarBasic = num4 + (rowLim - num2);
				for (int i = _cvarBasic; i < rowLim; i++)
				{
					_rgvarBasic[i] = -1;
				}
			}
			else
			{
				_cvarBasic = rowLim;
			}
			int num8 = _cvarBasic;
			while (--num8 >= 0)
			{
				_mpvarivar[_rgvarBasic[num8]] = num8;
			}
		}

		protected void Permute(Permutation perm)
		{
			perm.ApplyInverse(_rgvarBasic);
			int num = _rgvarBasic.Length;
			while (--num >= 0)
			{
				int num2 = _rgvarBasic[num];
				if (num2 >= 0)
				{
					_mpvarivar[num2] = num;
				}
			}
		}

		protected void MajorPivot(int varEnter, int ivarLeave, int varLeave, SimplexVarValKind vvkLeave)
		{
			_rgvarBasic[ivarLeave] = varEnter;
			_mpvarivar[varEnter] = ivarLeave;
			_mpvarivar[varLeave] = 0 - vvkLeave;
		}

		public void RepairBasis(int varEnter, int ivarLeave, int varLeave, SimplexVarValKind vvkLeave)
		{
			_rgvarBasic[ivarLeave] = varEnter;
			_mpvarivar[varEnter] = ivarLeave;
			if (varLeave >= 0)
			{
				_mpvarivar[varLeave] = 0 - vvkLeave;
			}
			else
			{
				_cvarBasic++;
			}
		}
	}
}
