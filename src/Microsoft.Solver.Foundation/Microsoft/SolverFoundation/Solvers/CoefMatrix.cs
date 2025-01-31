using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class CoefMatrix
	{
		protected struct ListInfo
		{
			public int slotFirst;

			public int slotLast;

			public int cslot;

			public override string ToString()
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}: {1}..{2}", new object[3] { cslot, slotFirst, slotLast });
			}
		}

		public struct RowIter
		{
			private CoefMatrix _mat;

			private int _slotCur;

			public bool IsValid => _slotCur > 0;

			public int Column => _mat._rgcol[_slotCur];

			public Rational Exact => _mat._rgrat[_slotCur];

			public double Approx => _mat._rgdbl[_slotCur];

			/// <summary>
			/// Creates a new instance.
			/// </summary>
			/// <param name="matrix">The matrix whose row is iterated.</param>
			/// <param name="row">The row to iterate.</param>
			public RowIter(CoefMatrix matrix, int row)
			{
				_mat = matrix;
				_slotCur = matrix._mprowlin[row].slotFirst;
			}

			public void Advance()
			{
				_slotCur = _mat._rgslotNextRow[_slotCur];
			}

			public double ApproxAndColumn(out int col)
			{
				col = _mat._rgcol[_slotCur];
				return _mat._rgdbl[_slotCur];
			}
		}

		public struct ColIter
		{
			private CoefMatrix _mat;

			private int _slotCur;

			public bool IsValid => _slotCur > 0;

			public int Row => _mat._rgrow[_slotCur];

			public Rational Exact => _mat._rgrat[_slotCur];

			public double Approx => _mat._rgdbl[_slotCur];

			/// <summary>
			/// Creates a new instance.
			/// </summary>
			/// <param name="matrix">The matrix whose row is iterated.</param>
			/// <param name="column">The column to iterate.</param>
			public ColIter(CoefMatrix matrix, int column)
			{
				_mat = matrix;
				_slotCur = matrix._mpcollin[column].slotFirst;
			}

			public void Advance()
			{
				_slotCur = _mat._rgslotNextCol[_slotCur];
			}
		}

		protected const int kcslotMin = 100;

		protected const int kcrcMin = 10;

		private int[] _rgrow;

		private int[] _rgcol;

		private int[] _rgslotNextRow;

		private int[] _rgslotNextCol;

		private double[] _rgdbl;

		private Rational[] _rgrat;

		private bool _fExact;

		private bool _fDouble;

		private int _slotLim;

		private int _cslotAlive;

		private int _slotFree;

		private int _rowLim;

		private int _colLim;

		private BitArray _rowUsageCache;

		private int _rowUsageCacheRow;

		private int _switchColumnCount;

		private int _switchRowCount;

		private int _lastAddedRow;

		private int _lastAddedColumn;

		private ListInfo[] _mprowlin;

		private ListInfo[] _mpcollin;

		private string _display;

		public int RowCount => _rowLim;

		public int ColCount => _colLim;

		public int EntryCount => _cslotAlive;

		[Conditional("DEBUG")]
		protected void AssertValid()
		{
		}

		public CoefMatrix(int rowLim, int colLim, int cslotInit, bool fExact, bool fDouble)
		{
			_rowLim = rowLim;
			_colLim = colLim;
			_mprowlin = new ListInfo[Math.Max(10, _rowLim)];
			_mpcollin = new ListInfo[Math.Max(10, _colLim)];
			cslotInit++;
			if (cslotInit < 100)
			{
				cslotInit = 100;
			}
			_fExact = fExact;
			_fDouble = fDouble;
			GrowSlotHeap(cslotInit);
			_rgrow[0] = -1;
			_rgcol[0] = -1;
			_rgslotNextRow[0] = 0;
			_rgslotNextCol[0] = 0;
			if (_fExact)
			{
				ref Rational reference = ref _rgrat[0];
				reference = Rational.Indeterminate;
			}
			if (_fDouble)
			{
				_rgdbl[0] = double.NaN;
			}
			_slotLim = 1;
			_rowUsageCacheRow = -1;
			_switchColumnCount = (_switchRowCount = 0);
			_lastAddedColumn = (_lastAddedRow = -1);
		}

		protected static void EnsureInfoSize(ref ListInfo[] rglin, int rcLim)
		{
			if (rcLim > rglin.Length)
			{
				rcLim = Math.Max(rcLim, rglin.Length + rglin.Length / 2);
				Array.Resize(ref rglin, rcLim);
			}
		}

		protected void GrowSlotHeap(int slotLim)
		{
			Array.Resize(ref _rgcol, slotLim);
			Array.Resize(ref _rgslotNextRow, slotLim);
			Array.Resize(ref _rgslotNextCol, slotLim);
			if (_fDouble)
			{
				Array.Resize(ref _rgdbl, slotLim);
			}
			if (_fExact)
			{
				Array.Resize(ref _rgrat, slotLim);
			}
			Array.Resize(ref _rgrow, slotLim);
		}

		public int RowEntryCount(int row)
		{
			return _mprowlin[row].cslot;
		}

		public int ColEntryCount(int col)
		{
			return _mpcollin[col].cslot;
		}

		public void ResizeRows(int rowLim)
		{
			Resize(rowLim, _colLim);
		}

		public void ResizeCols(int colLim)
		{
			Resize(_rowLim, colLim);
		}

		public void Resize(int rowLim, int colLim)
		{
			ClearRowCache();
			if (rowLim == _rowLim && colLim == _colLim)
			{
				return;
			}
			if (rowLim > _rowLim)
			{
				EnsureInfoSize(ref _mprowlin, rowLim);
				Array.Clear(_mprowlin, _rowLim, rowLim - _rowLim);
			}
			else if (rowLim < _rowLim)
			{
				int num = _rowLim;
				while (--num >= rowLim)
				{
					int num2 = _mprowlin[num].slotFirst;
					if (num2 <= 0)
					{
						continue;
					}
					for (int num3 = num2; num3 > 0; num3 = _rgslotNextRow[num3])
					{
						if (!IsClean(num3))
						{
							int num4 = _rgcol[num3];
							if (num4 < colLim)
							{
								TrimColumnAtRow(num4, rowLim);
							}
							else
							{
								CleanEntry(num3);
							}
						}
						num2 = num3;
					}
					_rgslotNextRow[num2] = _slotFree;
					_slotFree = _mprowlin[num].slotFirst;
					SubCheck(ref _cslotAlive, _mprowlin[num].cslot);
				}
			}
			_rowLim = rowLim;
			if (colLim > _colLim)
			{
				EnsureInfoSize(ref _mpcollin, colLim);
				Array.Clear(_mpcollin, _colLim, colLim - _colLim);
			}
			else if (colLim < _colLim)
			{
				int num5 = _colLim;
				while (--num5 >= colLim)
				{
					int num6 = _mpcollin[num5].slotFirst;
					if (num6 <= 0)
					{
						continue;
					}
					while (num6 > 0)
					{
						int num7 = _rgrow[num6];
						if (num7 >= 0)
						{
							TrimRowAtColumn(num7, colLim);
						}
						num6 = _rgslotNextCol[num6];
					}
				}
			}
			_colLim = colLim;
		}

		protected static void SubCheck(ref int cv, int cvSub)
		{
			cv -= cvSub;
		}

		protected void TrimColumnAtRow(int col, int rowLim)
		{
			int num = _mpcollin[col].slotFirst;
			int num2 = 0;
			int num3 = 0;
			int[] rgslotNextCol = _rgslotNextCol;
			while (true)
			{
				int num4 = ((num2 == 0) ? num : rgslotNextCol[num2]);
				if (num4 <= 0)
				{
					break;
				}
				if (_rgrow[num4] < rowLim)
				{
					num2 = num4;
					continue;
				}
				if (num2 == 0)
				{
					num = rgslotNextCol[num4];
				}
				else
				{
					rgslotNextCol[num2] = rgslotNextCol[num4];
				}
				CleanEntry(num4);
				num3++;
			}
			if (num3 > 0)
			{
				_mpcollin[col].slotFirst = num;
				_mpcollin[col].slotLast = num2;
				SubCheck(ref _mpcollin[col].cslot, num3);
			}
		}

		protected void TrimRowAtColumn(int row, int colLim)
		{
			int num = 0;
			int num2 = 0;
			int num3 = _mprowlin[row].slotFirst;
			int num4 = 0;
			int num5 = 0;
			int[] rgslotNextRow = _rgslotNextRow;
			while (true)
			{
				int num6 = ((num4 == 0) ? num3 : _rgslotNextRow[num4]);
				if (num6 <= 0)
				{
					break;
				}
				if (_rgcol[num6] < colLim)
				{
					num4 = num6;
					continue;
				}
				if (num4 == 0)
				{
					num3 = rgslotNextRow[num6];
				}
				else
				{
					rgslotNextRow[num4] = rgslotNextRow[num6];
				}
				CleanEntry(num6);
				if (num == 0)
				{
					num = num6;
				}
				else
				{
					rgslotNextRow[num2] = num6;
				}
				num2 = num6;
				num5++;
			}
			if (num5 > 0)
			{
				rgslotNextRow[num2] = _slotFree;
				_slotFree = num;
				_mprowlin[row].slotFirst = num3;
				_mprowlin[row].slotLast = num4;
				SubCheck(ref _mprowlin[row].cslot, num5);
				SubCheck(ref _cslotAlive, num5);
			}
		}

		protected void CleanEntry(int slot)
		{
			_rgrow[slot] = -1;
			_rgcol[slot] = -1;
		}

		protected bool IsClean(int slot)
		{
			if (_rgcol[slot] < 0)
			{
				return true;
			}
			return false;
		}

		private void ClearRowCache()
		{
			_rowUsageCacheRow = -1;
		}

		private bool MakeRowCacheEntry(int row, int col)
		{
			if (row != _lastAddedRow)
			{
				_switchRowCount++;
			}
			if (col != _lastAddedColumn)
			{
				_switchColumnCount++;
			}
			_lastAddedRow = row;
			_lastAddedColumn = col;
			if (_rowUsageCacheRow != row)
			{
				if (_switchRowCount < _switchColumnCount * 2)
				{
					return false;
				}
				if (_cslotAlive / _rowLim < _colLim / 100)
				{
					return false;
				}
				_rowUsageCacheRow = row;
				if (_rowUsageCache != null && _rowUsageCache.Length == _colLim)
				{
					_rowUsageCache.SetAll(value: false);
				}
				else
				{
					_rowUsageCache = new BitArray(_colLim);
				}
				for (int num = _mprowlin[_rowUsageCacheRow].slotFirst; num > 0; num = _rgslotNextRow[num])
				{
					_rowUsageCache[_rgcol[num]] = true;
				}
			}
			if (_rowUsageCache[col])
			{
				return false;
			}
			_rowUsageCache[col] = true;
			return true;
		}

		public void SetCoefExact(int row, int col, Rational rat)
		{
			if (MakeRowCacheEntry(row, col) || !FindRowCol(row, col, out var slot))
			{
				slot = AllocSlot();
				LinkSlot(slot, row, col);
			}
			_rgrat[slot] = rat;
		}

		public void SetCoefDouble(int row, int col, double dbl)
		{
			if (MakeRowCacheEntry(row, col) || !FindRowCol(row, col, out var slot))
			{
				slot = AllocSlot();
				LinkSlot(slot, row, col);
			}
			_rgdbl[slot] = dbl;
		}

		public void SetCoef(int row, int col, Rational rat, double dbl)
		{
			if (MakeRowCacheEntry(row, col) || !FindRowCol(row, col, out var slot))
			{
				slot = AllocSlot();
				LinkSlot(slot, row, col);
			}
			_rgrat[slot] = rat;
			_rgdbl[slot] = dbl;
		}

		public void CopyExactToApprox()
		{
			for (int i = 0; i < _slotLim; i++)
			{
				_rgdbl[i] = (double)_rgrat[i];
			}
		}

		public Rational GetCoefExact(int row, int col)
		{
			if (FindRowCol(row, col, out var slot))
			{
				return _rgrat[slot];
			}
			return default(Rational);
		}

		public double GetCoefDouble(int row, int col)
		{
			if (FindRowCol(row, col, out var slot))
			{
				return _rgdbl[slot];
			}
			return 0.0;
		}

		public void RemoveCoef(int row, int col)
		{
			ClearRowCache();
			if (FindRowCol(row, col, out var slot, out var slotPrevInRow, out var slotPrevInCol))
			{
				int num = _rgslotNextRow[slot];
				if (num == 0)
				{
					_mprowlin[row].slotLast = slotPrevInRow;
				}
				if (slotPrevInRow == 0)
				{
					_mprowlin[row].slotFirst = num;
				}
				else
				{
					_rgslotNextRow[slotPrevInRow] = num;
				}
				num = _rgslotNextCol[slot];
				if (num == 0)
				{
					_mpcollin[col].slotLast = slotPrevInCol;
				}
				if (slotPrevInCol == 0)
				{
					_mpcollin[col].slotFirst = num;
				}
				else
				{
					_rgslotNextCol[slotPrevInCol] = num;
				}
				SubCheck(ref _mprowlin[row].cslot, 1);
				SubCheck(ref _mpcollin[col].cslot, 1);
				SubCheck(ref _cslotAlive, 1);
				CleanEntry(slot);
				if (slot == _slotLim - 1)
				{
					_slotLim--;
					return;
				}
				_rgslotNextRow[slot] = _slotFree;
				_slotFree = slot;
			}
		}

		public void ClearColumn(int col)
		{
			ClearRowCache();
			ClearFile(col, _mpcollin, _mprowlin, _rgslotNextCol, _rgslotNextRow, _rgrow);
		}

		public void ClearRow(int row)
		{
			ClearRowCache();
			ClearFile(row, _mprowlin, _mpcollin, _rgslotNextRow, _rgslotNextCol, _rgcol);
		}

		protected void ClearFile(int rc, ListInfo[] mprclin, ListInfo[] mprclinOther, int[] rgslotNext, int[] rgslotNextOther, int[] rgcr)
		{
			int num = 0;
			int num2 = mprclin[rc].slotFirst;
			while (num2 > 0)
			{
				int num3 = rgcr[num2];
				FindPrev(rgslotNextOther, mprclinOther[num3].slotFirst, num2, out var slotPrev);
				int num4 = rgslotNextOther[num2];
				if (num4 == 0)
				{
					mprclinOther[num3].slotLast = slotPrev;
				}
				if (slotPrev == 0)
				{
					mprclinOther[num3].slotFirst = num4;
				}
				else
				{
					rgslotNextOther[slotPrev] = num4;
				}
				SubCheck(ref mprclinOther[num3].cslot, 1);
				int num5 = rgslotNext[num2];
				CleanEntry(num2);
				if (num2 == _slotLim - 1)
				{
					_slotLim--;
				}
				else
				{
					_rgslotNextRow[num2] = _slotFree;
					_slotFree = num2;
				}
				num++;
				num2 = num5;
			}
			mprclin[rc].cslot = 0;
			mprclin[rc].slotFirst = 0;
			mprclin[rc].slotLast = 0;
			SubCheck(ref _cslotAlive, num);
		}

		protected bool FindRowCol(int row, int col, out int slot)
		{
			int cslot;
			int cslot2;
			if ((cslot = _mprowlin[row].cslot) <= 0 || (cslot2 = _mpcollin[col].cslot) <= 0)
			{
				slot = 0;
				return false;
			}
			if (cslot <= cslot2)
			{
				return FindInList(col, _mprowlin[row].slotFirst, _rgcol, _rgslotNextRow, out slot);
			}
			return FindInList(row, _mpcollin[col].slotFirst, _rgrow, _rgslotNextCol, out slot);
		}

		protected bool FindRowCol(int row, int col, out int slot, out int slotPrevInRow, out int slotPrevInCol)
		{
			int cslot;
			int cslot2;
			if ((cslot = _mprowlin[row].cslot) <= 0 || (cslot2 = _mpcollin[col].cslot) <= 0)
			{
				slot = 0;
				slotPrevInRow = 0;
				slotPrevInCol = 0;
				return false;
			}
			if (cslot <= cslot2)
			{
				if (!FindInList(col, _mprowlin[row].slotFirst, _rgcol, _rgslotNextRow, out slot, out slotPrevInRow))
				{
					slotPrevInCol = 0;
					return false;
				}
				FindPrev(_rgslotNextCol, _mpcollin[col].slotFirst, slot, out slotPrevInCol);
			}
			else
			{
				if (!FindInList(row, _mpcollin[col].slotFirst, _rgrow, _rgslotNextCol, out slot, out slotPrevInCol))
				{
					slotPrevInRow = 0;
					return false;
				}
				FindPrev(_rgslotNextRow, _mprowlin[row].slotFirst, slot, out slotPrevInRow);
			}
			return true;
		}

		protected static bool FindInList(int rc, int slotFirst, int[] rgrc, int[] rgslotNext, out int slot)
		{
			for (slot = slotFirst; slot > 0; slot = rgslotNext[slot])
			{
				if (rgrc[slot] == rc)
				{
					return true;
				}
			}
			return false;
		}

		protected static bool FindInList(int rc, int slotFirst, int[] rgrc, int[] rgslotNext, out int slot, out int slotPrev)
		{
			slotPrev = 0;
			for (slot = slotFirst; slot > 0; slot = rgslotNext[slot])
			{
				if (rgrc[slot] == rc)
				{
					return true;
				}
				slotPrev = slot;
			}
			return false;
		}

		protected static bool FindPrev(int[] rgslotNext, int slotFirst, int slotFind, out int slotPrev)
		{
			slotPrev = 0;
			for (int num = slotFirst; num > 0; num = rgslotNext[num])
			{
				if (num == slotFind)
				{
					return true;
				}
				slotPrev = num;
			}
			return false;
		}

		protected int AllocSlot()
		{
			if (_slotFree > 0)
			{
				int slotFree = _slotFree;
				_slotFree = _rgslotNextRow[_slotFree];
				return slotFree;
			}
			if (_slotLim >= _rgrow.Length)
			{
				GrowSlotHeap(_rgrow.Length + _rgrow.Length / 2);
			}
			return _slotLim++;
		}

		protected void LinkSlot(int slot, int row, int col)
		{
			_rgrow[slot] = row;
			_rgcol[slot] = col;
			int slotLast = _mprowlin[row].slotLast;
			_mprowlin[row].slotLast = slot;
			if (slotLast == 0)
			{
				_mprowlin[row].slotFirst = slot;
			}
			else
			{
				_rgslotNextRow[slotLast] = slot;
			}
			_rgslotNextRow[slot] = 0;
			_mprowlin[row].cslot++;
			slotLast = _mpcollin[col].slotLast;
			_mpcollin[col].slotLast = slot;
			if (slotLast == 0)
			{
				_mpcollin[col].slotFirst = slot;
			}
			else
			{
				_rgslotNextCol[slotLast] = slot;
			}
			_rgslotNextCol[slot] = 0;
			_mpcollin[col].cslot++;
			_cslotAlive++;
		}

		public void ScaleRowExact(int row, Rational num)
		{
			for (int num2 = _mprowlin[row].slotFirst; num2 > 0; num2 = _rgslotNextRow[num2])
			{
				_rgrat[num2] *= num;
			}
		}

		public void ScaleColExact(int col, Rational num)
		{
			for (int num2 = _mpcollin[col].slotFirst; num2 > 0; num2 = _rgslotNextCol[num2])
			{
				_rgrat[num2] *= num;
			}
		}

		public void ScaleRowApprox(int row, double dbl)
		{
			for (int num = _mprowlin[row].slotFirst; num > 0; num = _rgslotNextRow[num])
			{
				_rgdbl[num] *= dbl;
			}
		}

		public void ScaleColApprox(int col, double dbl)
		{
			for (int num = _mpcollin[col].slotFirst; num > 0; num = _rgslotNextCol[num])
			{
				_rgdbl[num] *= dbl;
			}
		}

		public void ScaleRowDivApprox(int row, double dbl)
		{
			for (int num = _mprowlin[row].slotFirst; num > 0; num = _rgslotNextRow[num])
			{
				_rgdbl[num] /= dbl;
			}
		}

		public void ScaleColDivApprox(int col, double dbl)
		{
			for (int num = _mpcollin[col].slotFirst; num > 0; num = _rgslotNextCol[num])
			{
				_rgdbl[num] /= dbl;
			}
		}

		private bool ValidatePrincipalDiagnoals(int row)
		{
			double num = 0.0;
			double num2 = 1E-08;
			while (row < RowCount)
			{
				if (FindRowCol(row, row, out var slot))
				{
					num = _rgdbl[slot];
				}
				if (Math.Abs(num) < num2)
				{
					for (slot = _mprowlin[row].slotFirst; slot > 0; slot = _rgslotNextRow[slot])
					{
						if (Math.Abs(_rgdbl[slot]) > num2)
						{
							return false;
						}
					}
					ColIter colIter = new ColIter(this, row);
					while (colIter.IsValid)
					{
						if (colIter.Row > row && Math.Abs(colIter.Approx) > num2)
						{
							return false;
						}
						colIter.Advance();
					}
				}
				else if (num < 0.0)
				{
					return false;
				}
				row++;
			}
			return true;
		}

		public void RemoveEmptyRows()
		{
			ClearRowCache();
			RemoveEmptyFile(null, ref _rowLim, _mprowlin, _rgslotNextRow, _rgrow);
		}

		public void RemoveEmptyColumns(Func<int, bool> fn)
		{
			ClearRowCache();
			RemoveEmptyFile(fn, ref _colLim, _mpcollin, _rgslotNextCol, _rgcol);
		}

		protected static void RemoveEmptyFile(Func<int, bool> fn, ref int rcLim, ListInfo[] mprclin, int[] rgslotNext, int[] rgrc)
		{
			int num = 0;
			for (int i = 0; i < rcLim; i++)
			{
				if (mprclin[i].cslot == 0 && (fn == null || fn(i)))
				{
					continue;
				}
				if (num == i)
				{
					num++;
					continue;
				}
				for (int num2 = mprclin[i].slotFirst; num2 > 0; num2 = rgslotNext[num2])
				{
					rgrc[num2] = num;
				}
				ref ListInfo reference = ref mprclin[num];
				reference = mprclin[i];
				num++;
			}
			rcLim = num;
		}

		[Conditional("DEBUG")]
		private static void DumpMatrix(CoefMatrix mat, string name)
		{
		}

		/// <summary>
		/// This is a simple algorithm to test whether a matrix is PSD
		///  step 1. make SymMat = Mat + Mat^T 
		///  step 2. starting from the first row of SymMat, stop if the last row 
		///  step 3. check if any diagnoals are positive, continue. If negative, terminate not PSD. If zero, terminate if exist any other non-zero entries, otherwise continue. 
		///  step 4. Eliminate the column corresponding to the current row number, go to step 2 for the next row
		/// </summary>
		/// <returns></returns>
		public static bool IsPositiveSemiDefinite(CoefMatrix mat, bool flipSign, Func<bool> queryAbort)
		{
			if (!mat._fDouble || mat.ColCount != mat.RowCount)
			{
				throw new NotSupportedException();
			}
			CoefMatrix coefMatrix = new CoefMatrix(mat.RowCount, mat.ColCount, mat.EntryCount, fExact: false, fDouble: true);
			for (int i = 0; i < mat.RowCount; i++)
			{
				RowIter rowIter = new RowIter(mat, i);
				while (rowIter.IsValid)
				{
					coefMatrix.SetCoefDouble(i, rowIter.Column, flipSign ? (0.0 - rowIter.Approx) : rowIter.Approx);
					rowIter.Advance();
				}
			}
			if (queryAbort != null && queryAbort())
			{
				throw new TimeLimitReachedException();
			}
			int num = coefMatrix.RowCount * coefMatrix.ColCount;
			double num2 = (double)coefMatrix.EntryCount * 1.0 / (double)num;
			if (num > 1000000 && num2 > 0.01)
			{
				return true;
			}
			for (int j = 0; j < coefMatrix.RowCount; j++)
			{
				double num3 = (double)coefMatrix.EntryCount * 1.0 / (double)num;
				if (j % 100 == 0 && queryAbort != null && queryAbort())
				{
					throw new TimeLimitReachedException();
				}
				if (num3 > 10.0 * num2)
				{
					return true;
				}
				double num4 = 0.0;
				if (!coefMatrix.ValidatePrincipalDiagnoals(j))
				{
					return false;
				}
				if (coefMatrix.FindRowCol(j, j, out var slot))
				{
					num4 = coefMatrix._rgdbl[slot];
				}
				if (num4 == 0.0)
				{
					num4 = 1E-08;
				}
				coefMatrix.ScaleRowDivApprox(j, num4);
				for (int k = j + 1; k < coefMatrix.RowCount; k++)
				{
					if (k % 100 == 0 && queryAbort != null && queryAbort())
					{
						throw new TimeLimitReachedException();
					}
					if (!coefMatrix.FindRowCol(k, j, out slot))
					{
						continue;
					}
					double num5 = coefMatrix._rgdbl[slot];
					RowIter rowIter2 = new RowIter(coefMatrix, k);
					while (rowIter2.IsValid)
					{
						if (rowIter2.Column >= j)
						{
							double approx = rowIter2.Approx;
							if (coefMatrix.FindRowCol(j, rowIter2.Column, out slot))
							{
								approx -= num5 * coefMatrix._rgdbl[slot];
								coefMatrix.SetCoefDouble(k, rowIter2.Column, approx);
							}
						}
						rowIter2.Advance();
					}
					RowIter rowIter3 = new RowIter(coefMatrix, j);
					while (rowIter3.IsValid)
					{
						if (rowIter3.Column >= j && coefMatrix.FindRowCol(j, rowIter3.Column, out slot))
						{
							double approx2 = rowIter3.Approx;
							if (!coefMatrix.FindRowCol(k, rowIter3.Column, out var _))
							{
								approx2 = num5 * coefMatrix._rgdbl[slot];
								approx2 = 0.0 - approx2;
								coefMatrix.SetCoefDouble(k, rowIter3.Column, approx2);
							}
						}
						rowIter3.Advance();
					}
				}
			}
			return true;
		}

		[Conditional("DEBUG")]
		private void Display()
		{
		}

		[Conditional("DEBUG")]
		private void DisplayExact()
		{
		}

		public override string ToString()
		{
			_display = null;
			_ = _fExact;
			if (_display == null)
			{
				return base.ToString();
			}
			string display = _display;
			_display = null;
			return display;
		}
	}
}
