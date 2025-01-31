using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class LUFactorization<Number>
	{
		protected struct ListInfo
		{
			public int slotFirst;

			public int cslot;

			public int rcNext;

			public int rcPrev;

			public bool Linked => rcNext >= 0;

			public bool Excluded => rcNext == -2;

			public bool Done => rcNext == -3;

			public bool Rejected => rcNext == -4;
		}

		protected const int kcslotMin = 100;

		public const int krcLinkNone = -1;

		public const int krcLinkExcluded = -2;

		public const int krcLinkDone = -3;

		public const int krcLinkRejected = -4;

		protected const int kcslotMinEta = 100;

		protected int[] _rgrow;

		protected int[] _rgcol;

		protected int[] _rgslotNextRow;

		protected int[] _rgslotNextCol;

		protected Number[] _rgnum;

		protected int _slotLim;

		protected int _slotFree;

		protected int _cslotAlive;

		protected int _rcLim;

		protected int _rcLimUser;

		protected Permutation _permRow;

		protected Permutation _permCol;

		protected ListInfo[] _mprowlin;

		protected ListInfo[] _mpcollin;

		protected int[] _mpcslotrowFirst;

		protected int[] _mpcslotcolFirst;

		protected Number[] _rgnumWork;

		protected bool[] _rgfWork;

		protected int[] _rgrowPiv;

		protected int[] _rgcolPiv;

		protected int[] _mprowslotU;

		protected int[] _mpcolslotU;

		protected int[] _mprowslotL;

		protected int[] _mpcolslotL;

		protected List<int> _mpetaslotFirst;

		protected int[] _rgrowEta;

		protected Number[] _rgnumEta;

		protected int[] _rgrcWork;

		protected int[] _rgrcWork2;

		protected int _crcWork;

		protected int[] _rgrcStack;

		protected int[] _rgslotNextStack;

		protected int _crcStack;

		protected Arithmetic<Number> _arith;

		public virtual int EliminatedColumnCount => _rcLim - _rcLimUser;

		public int EtaCount => _mpetaslotFirst.Count - 1;

		protected abstract Number GetValue(ref CoefMatrix.ColIter cit);

		protected abstract Number GetValue(CoefMatrix mat, int row, int col);

		protected abstract int GetBestPivotInCol(int col);

		protected abstract int GetBestPivotInRow(int row);

		protected abstract bool AddToMulPivot(ref Number numDst, Number num1, Number num2);

		protected virtual void NotifyRowRecalc(int row)
		{
		}

		protected abstract bool FuzzyVerify(Number num1, Number num2);

		[Conditional("DEBUG")]
		protected void AssertValidFactoring()
		{
		}

		[Conditional("DEBUG")]
		protected void AssertValidFactored()
		{
		}

		protected LUFactorization()
		{
			_arith = Arithmetic<Number>.Instance;
		}

		public virtual bool Factor(CoefMatrix matSrc, int rowLim, int[] rgcol)
		{
			Load(matSrc, rowLim, rgcol);
			FactorCore();
			return _rcLimUser == _rcLim;
		}

		public virtual void GetEliminatedEntry(int iv, out int row, out int col)
		{
			iv += _rcLimUser;
			row = _permRow.Map(iv);
			col = _permCol.Map(iv);
		}

		public virtual void SetEliminatedEntryValue(int iv, Number num)
		{
			iv += _rcLimUser;
			int num2 = _mprowslotU[iv];
			_rgnum[num2] = num;
		}

		public virtual Permutation GetRowPermutation()
		{
			return _permRow;
		}

		public virtual Permutation GetColumnPermutation()
		{
			return _permCol;
		}

		protected virtual void Load(CoefMatrix matSrc, int rowLim, int[] rgcol)
		{
			int cslotInit = matSrc.EntryCount + 100;
			Init(rowLim, cslotInit);
			ClearCountLists();
			BuildCols(matSrc, rgcol);
			BuildRows();
			CleanCompletedRows();
			BuildCountLists();
		}

		protected virtual void Init(int rowLim, int cslotInit)
		{
			_rcLim = rowLim;
			if (_mprowlin == null || _mprowlin.Length < _rcLim)
			{
				_mpcslotrowFirst = new int[_rcLim + 1];
				_mpcslotcolFirst = new int[_rcLim + 1];
				_rgrowPiv = new int[_rcLim];
				_rgcolPiv = new int[_rcLim];
				_rgnumWork = new Number[_rcLim];
				_rgfWork = new bool[_rcLim];
				_mpcollin = new ListInfo[_rcLim];
				_rgrcStack = new int[rowLim];
				_rgslotNextStack = new int[rowLim];
				_rgrcWork2 = new int[rowLim];
				_rgrcWork = new int[rowLim];
				_mprowlin = new ListInfo[_rcLim];
			}
			else
			{
				Array.Clear(_mprowlin, 0, _rcLim);
				Array.Clear(_mpcollin, 0, _rcLim);
				Array.Clear(_rgnumWork, 0, _rcLim);
				Array.Clear(_rgfWork, 0, _rcLim);
			}
			if (_mpetaslotFirst == null)
			{
				_mpetaslotFirst = new List<int>();
			}
			else
			{
				_mpetaslotFirst.Clear();
			}
			_mpetaslotFirst.Add(0);
			if (_rgrow == null || _rgrow.Length < cslotInit)
			{
				GrowSlotHeap(cslotInit);
			}
			_rgrow[0] = -1;
			_rgcol[0] = -1;
			_rgslotNextRow[0] = 0;
			_rgslotNextCol[0] = 0;
			_slotLim = 1;
			_slotFree = 0;
			_cslotAlive = 0;
			_rcLimUser = 0;
		}

		private void ClearCountLists()
		{
			int num = _rcLim;
			while (--num >= 0)
			{
				_mprowlin[num].rcNext = -2;
				_mprowlin[num].rcPrev = -2;
				_mpcollin[num].rcNext = -2;
				_mpcollin[num].rcPrev = -2;
			}
		}

		private void BuildCols(CoefMatrix matSrc, int[] rgcol)
		{
			for (int i = 0; i < _rcLim; i++)
			{
				int num = ((rgcol == null) ? i : rgcol[i]);
				if (num < 0)
				{
					_mpcollin[i].slotFirst = 0;
					_mpcollin[i].cslot = 0;
					continue;
				}
				int num2 = 0;
				int num3 = 0;
				CoefMatrix.ColIter cit = new CoefMatrix.ColIter(matSrc, num);
				while (cit.IsValid)
				{
					int row = cit.Row;
					if (row < _rcLim)
					{
						int num4 = AllocSlot();
						_rgrow[num4] = row;
						_rgcol[num4] = i;
						_rgnum[num4] = GetValue(ref cit);
						_cslotAlive++;
						if (num3 == 0)
						{
							_mpcollin[i].slotFirst = num4;
						}
						else
						{
							_rgslotNextCol[num3] = num4;
						}
						num3 = num4;
						num2++;
					}
					cit.Advance();
				}
				_rgslotNextCol[num3] = 0;
				_mpcollin[i].cslot = num2;
				if (num2 == 1 && !_mprowlin[_rgrow[num3]].Done)
				{
					int num5 = _rgrow[num3];
					_mpcollin[i].rcNext = (_mpcollin[i].rcPrev = -3);
					_mprowlin[num5].rcNext = (_mprowlin[num5].rcPrev = -3);
					_rgcolPiv[_rcLimUser] = i;
					_rgrowPiv[_rcLimUser] = num5;
					_rcLimUser++;
				}
			}
		}

		private void BuildRows()
		{
			int num = _rcLim;
			while (--num >= 0)
			{
				int num2 = 0;
				int num3 = _mpcollin[num].slotFirst;
				while (num3 > 0)
				{
					int num4 = _rgrow[num3];
					_rgslotNextRow[num3] = _mprowlin[num4].slotFirst;
					_mprowlin[num4].slotFirst = num3;
					_mprowlin[num4].cslot++;
					int num5 = _rgslotNextCol[num3];
					if (!_mprowlin[num4].Done)
					{
						num2 = num3;
					}
					else
					{
						if (num2 == 0)
						{
							_mpcollin[num].slotFirst = num5;
						}
						else
						{
							_rgslotNextCol[num2] = num5;
						}
						_rgslotNextCol[num3] = 0;
						_mpcollin[num].cslot--;
					}
					num3 = num5;
				}
				if (num2 == 0)
				{
					_mpcollin[num].slotFirst = 0;
				}
				else
				{
					_rgslotNextCol[num2] = 0;
				}
			}
		}

		private void CleanCompletedRows()
		{
			for (int i = 0; i < _rcLimUser; i++)
			{
				int num = _rgrowPiv[i];
				int num2 = _rgcolPiv[i];
				int num3 = 0;
				int num4 = _mprowlin[num].slotFirst;
				while (_rgcol[num4] != num2)
				{
					num3 = num4;
					num4 = _rgslotNextRow[num4];
				}
				if (num3 > 0)
				{
					_rgslotNextRow[num3] = _rgslotNextRow[num4];
					_rgslotNextRow[num4] = _mprowlin[num].slotFirst;
					_mprowlin[num].slotFirst = num4;
				}
			}
		}

		private void BuildCountLists()
		{
			int num = _rcLim + 1;
			while (--num >= 0)
			{
				_mpcslotrowFirst[num] = -1;
				_mpcslotcolFirst[num] = -1;
			}
			for (int i = 0; i < _rcLim; i++)
			{
				if (!_mprowlin[i].Done)
				{
					AddToCslotList(i, _mprowlin, _mpcslotrowFirst, fFirst: false);
				}
				if (!_mpcollin[i].Done)
				{
					AddToCslotList(i, _mpcollin, _mpcslotcolFirst, fFirst: false);
				}
			}
		}

		protected void AddToCslotList(int rc, ListInfo[] mprclin, int[] mpcslotrcFirst, bool fFirst)
		{
			int cslot = mprclin[rc].cslot;
			int num = mpcslotrcFirst[cslot];
			if (num == -1)
			{
				mprclin[rc].rcNext = (mprclin[rc].rcPrev = rc);
				mpcslotrcFirst[cslot] = rc;
				return;
			}
			int rcPrev = mprclin[num].rcPrev;
			mprclin[rc].rcNext = num;
			mprclin[num].rcPrev = rc;
			mprclin[rc].rcPrev = rcPrev;
			mprclin[rcPrev].rcNext = rc;
			if (fFirst)
			{
				mpcslotrcFirst[cslot] = rc;
			}
		}

		protected static void RemoveFromCslotList(int rc, ListInfo[] mprclin, int[] mpcslotrcFirst)
		{
			if (mprclin[rc].Excluded)
			{
				return;
			}
			int cslot = mprclin[rc].cslot;
			int rcNext = mprclin[rc].rcNext;
			int rcPrev = mprclin[rc].rcPrev;
			if (rcNext == rc)
			{
				mpcslotrcFirst[cslot] = -1;
			}
			else
			{
				mprclin[rcPrev].rcNext = rcNext;
				mprclin[rcNext].rcPrev = rcPrev;
				if (mpcslotrcFirst[cslot] == rc)
				{
					mpcslotrcFirst[cslot] = rcNext;
				}
			}
			mprclin[rc].rcNext = -2;
			mprclin[rc].rcPrev = -2;
		}

		protected void GrowSlotHeap(int slotLim)
		{
			Array.Resize(ref _rgcol, slotLim);
			Array.Resize(ref _rgslotNextRow, slotLim);
			Array.Resize(ref _rgslotNextCol, slotLim);
			Array.Resize(ref _rgnum, slotLim);
			Array.Resize(ref _rgrow, slotLim);
		}

		protected virtual void AddEntry(int row, int col, Number num)
		{
			int num2 = AllocSlot();
			LinkSlot(num2, row, col);
			_rgnum[num2] = num;
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

		protected void FreeSlot(int slot)
		{
			if (slot == _slotLim - 1)
			{
				_slotLim--;
			}
			else
			{
				_rgslotNextRow[slot] = _slotFree;
				_slotFree = slot;
			}
			CleanEntry(slot);
			_cslotAlive--;
		}

		protected void LinkSlot(int slot, int row, int col)
		{
			_rgrow[slot] = row;
			_rgcol[slot] = col;
			AddSlotToList(slot, ref _mprowlin[row], _rgslotNextRow);
			AddSlotToList(slot, ref _mpcollin[col], _rgslotNextCol);
			_cslotAlive++;
		}

		protected static void AddSlotToList(int slot, ref ListInfo lin, int[] rgslotNext)
		{
			rgslotNext[slot] = lin.slotFirst;
			lin.slotFirst = slot;
			lin.cslot++;
		}

		protected static void RemoveSlotFromList(int slot, ref ListInfo lin, int[] rgslotNext)
		{
			int num = 0;
			for (int num2 = lin.slotFirst; num2 > 0; num2 = rgslotNext[num2])
			{
				if (num2 == slot)
				{
					if (num == 0)
					{
						lin.slotFirst = rgslotNext[slot];
					}
					else
					{
						rgslotNext[num] = rgslotNext[slot];
					}
					rgslotNext[slot] = 0;
					lin.cslot--;
					break;
				}
				num = num2;
			}
		}

		protected void CleanEntry(int slot)
		{
			_rgrow[slot] = -1;
			_rgcol[slot] = -1;
			_rgnum[slot] = default(Number);
		}

		protected bool IsClean(int slot)
		{
			if (_rgcol[slot] < 0)
			{
				return true;
			}
			return false;
		}

		protected static void MarkDone(int rc, ListInfo[] mprclin)
		{
			mprclin[rc].rcNext = -3;
			mprclin[rc].rcPrev = -3;
		}

		protected static void MarkRejected(int rc, ListInfo[] mprclin)
		{
			mprclin[rc].rcNext = -4;
			mprclin[rc].rcPrev = -4;
		}

		protected virtual void FactorCore()
		{
			int slotRet;
			while (_rcLimUser < _rcLim && FindBest(4, out slotRet))
			{
				DoPivot(slotRet);
			}
			if (_rcLimUser < _rcLim)
			{
				int num = _mpcslotrowFirst[0];
				int num2 = _mpcslotcolFirst[0];
				for (int i = _rcLimUser; i < _rcLim; i++)
				{
					_rgrowPiv[i] = num;
					_rgcolPiv[i] = num2;
					int rc;
					num = _mprowlin[rc = num].rcNext;
					int rc2;
					num2 = _mpcollin[rc2 = num2].rcNext;
					MarkRejected(rc, _mprowlin);
					MarkRejected(rc2, _mpcollin);
				}
			}
			if (_permRow == null)
			{
				_permRow = new Permutation(_rcLim);
			}
			if (_permCol == null)
			{
				_permCol = new Permutation(_rcLim);
			}
			_permRow.Set(_rgrowPiv, _rcLim);
			_permCol.Set(_rgcolPiv, _rcLim);
			_mprowslotU = _mpcslotrowFirst;
			_mpcolslotU = _mpcslotcolFirst;
			_mprowslotL = _rgrowPiv;
			_mpcolslotL = _rgcolPiv;
			Array.Clear(_mpcolslotU, 0, _rcLim);
			Array.Clear(_mprowslotL, 0, _rcLim);
			for (int j = 0; j < _rcLimUser; j++)
			{
				_mprowslotU[j] = _mprowlin[_permRow[j]].slotFirst;
				_mpcolslotL[j] = _mpcollin[_permCol[j]].slotFirst;
				int num3 = _mprowslotU[j];
				int num4 = 0;
				while (num3 > 0)
				{
					_rgrow[num3] = j;
					int num5 = _rgcol[num3];
					int num6 = (_rgcol[num3] = _permCol.MapInverse(num5));
					int num7 = _rgslotNextRow[num3];
					if (num6 == j)
					{
						num4 = num3;
					}
					else if (!_mpcollin[num5].Rejected)
					{
						_rgslotNextCol[num3] = _mpcolslotU[num6];
						_mpcolslotU[num6] = num3;
						num4 = num3;
					}
					else
					{
						if (num4 == 0)
						{
							_mprowslotU[j] = num7;
						}
						else
						{
							_rgslotNextRow[num4] = num7;
						}
						FreeSlot(num3);
					}
					num3 = num7;
				}
				for (int num8 = _mpcolslotL[j]; num8 > 0; num8 = _rgslotNextCol[num8])
				{
					_rgcol[num8] = j;
					int num9 = (_rgrow[num8] = _permRow.MapInverse(_rgrow[num8]));
					_rgslotNextRow[num8] = _mprowslotL[num9];
					_mprowslotL[num9] = num8;
				}
			}
			for (int k = _rcLimUser; k < _rcLim; k++)
			{
				int num10 = AllocSlot();
				_rgrow[num10] = k;
				_rgcol[num10] = k;
				_rgnum[num10] = _arith.One;
				_rgslotNextRow[num10] = 0;
				_rgslotNextCol[num10] = 0;
				_cslotAlive++;
				_mprowslotU[k] = num10;
				_mpcolslotL[k] = 0;
			}
		}

		protected virtual bool FindBest(int cpivTry, out int slotRet)
		{
			if (cpivTry > _rcLim - _rcLimUser)
			{
				cpivTry = _rcLim - _rcLimUser;
			}
			if (_mpcslotcolFirst[1] >= 0)
			{
				int num = _mpcslotcolFirst[1];
				slotRet = _mpcollin[num].slotFirst;
				return true;
			}
			if (_mpcslotrowFirst[1] >= 0)
			{
				int num2 = _mpcslotrowFirst[1];
				slotRet = _mprowlin[num2].slotFirst;
				return true;
			}
			double num3 = (double)_rcLim * (double)_rcLim;
			slotRet = 0;
			for (int i = 2; i <= _rcLim; i++)
			{
				int num4 = _mpcslotcolFirst[i];
				if (num4 != -1)
				{
					int num5 = num4;
					do
					{
						int bestPivotInCol = GetBestPivotInCol(num5);
						if (bestPivotInCol == 0)
						{
							int rcNext = _mpcollin[num5].rcNext;
							if (rcNext == _mpcslotcolFirst[i])
							{
								break;
							}
							if (num5 == _mpcslotcolFirst[i])
							{
								_mpcslotcolFirst[i] = rcNext;
							}
							else
							{
								RemoveFromCslotList(num5, _mpcollin, _mpcslotcolFirst);
								AddToCslotList(num5, _mpcollin, _mpcslotcolFirst, fFirst: false);
								if (num4 == _mpcslotcolFirst[i])
								{
									num4 = num5;
								}
							}
							num5 = rcNext;
							continue;
						}
						int num6 = _rgrow[bestPivotInCol];
						double num7 = (double)(i - 1) * (double)(_mprowlin[num6].cslot - 1);
						if (num3 > num7)
						{
							num3 = num7;
							slotRet = bestPivotInCol;
							if (i >= _mprowlin[num6].cslot)
							{
								return true;
							}
						}
						if (--cpivTry <= 0 && slotRet > 0)
						{
							return true;
						}
						num5 = _mpcollin[num5].rcNext;
					}
					while (num5 != num4);
				}
				int num8 = _mpcslotrowFirst[i];
				if (num8 == -1)
				{
					continue;
				}
				int num9 = _mpcslotrowFirst[i];
				do
				{
					int bestPivotInRow = GetBestPivotInRow(num9);
					int num10 = _rgcol[bestPivotInRow];
					double num11 = (double)(i - 1) * (double)(_mpcollin[num10].cslot - 1);
					if (num3 > num11)
					{
						num3 = num11;
						slotRet = bestPivotInRow;
						if (i >= _mpcollin[num10].cslot)
						{
							return true;
						}
					}
					if (--cpivTry <= 0 && slotRet > 0)
					{
						return true;
					}
					num9 = _mprowlin[num9].rcNext;
				}
				while (num9 != num8);
			}
			return slotRet > 0;
		}

		[Conditional("DEBUG")]
		protected void VerifyWorkSpaceCleared()
		{
		}

		/// <summary> The pivot row becomes a row of U.
		/// The pivot column, minus the diagonal entry, is updated inplace
		/// and becomes a column of L.
		/// The work space should already be cleared.
		/// </summary>
		protected virtual void DoPivot(int slotPiv)
		{
			int num = _rgrow[slotPiv];
			int num2 = _rgcol[slotPiv];
			_rgrowPiv[_rcLimUser] = num;
			_rgcolPiv[_rcLimUser] = num2;
			RemoveFromCslotList(num, _mprowlin, _mpcslotrowFirst);
			RemoveFromCslotList(num2, _mpcollin, _mpcslotcolFirst);
			MarkDone(num, _mprowlin);
			MarkDone(num2, _mpcollin);
			RemoveSlotFromList(slotPiv, ref _mpcollin[num2], _rgslotNextCol);
			Number val = _rgnum[slotPiv];
			bool flag = _arith.IsOne(val);
			_rcLimUser++;
			for (int num3 = _mpcollin[num2].slotFirst; num3 > 0; num3 = _rgslotNextCol[num3])
			{
				int num4 = _rgrow[num3];
				RemoveFromCslotList(num4, _mprowlin, _mpcslotrowFirst);
				RemoveSlotFromList(num3, ref _mprowlin[num4], _rgslotNextRow);
				if (!flag)
				{
					_arith.DivTo(ref _rgnum[num3], val);
				}
			}
			int num5 = _mprowlin[num].cslot - 1;
			if (num5 > 0)
			{
				int num6 = 0;
				int num7 = _mprowlin[num].slotFirst;
				while (num7 > 0)
				{
					if (num7 == slotPiv)
					{
						int num8 = _rgslotNextRow[slotPiv];
						if (num6 > 0)
						{
							_rgslotNextRow[num6] = num8;
							_rgslotNextRow[slotPiv] = _mprowlin[num].slotFirst;
							_mprowlin[num].slotFirst = slotPiv;
						}
						num7 = num8;
					}
					else
					{
						int num9 = _rgcol[num7];
						RemoveFromCslotList(num9, _mpcollin, _mpcslotcolFirst);
						RemoveSlotFromList(num7, ref _mpcollin[num9], _rgslotNextCol);
						_rgnumWork[num9] = _rgnum[num7];
						_rgfWork[num9] = true;
						num6 = num7;
						num7 = _rgslotNextRow[num7];
					}
				}
				for (int num10 = _mpcollin[num2].slotFirst; num10 > 0; num10 = _rgslotNextCol[num10])
				{
					int num11 = _rgrow[num10];
					Number num12 = _rgnum[num10];
					_arith.Negate(ref num12);
					num6 = 0;
					int num13 = _mprowlin[num11].slotFirst;
					while (num13 > 0)
					{
						int num14 = _rgcol[num13];
						if (!_arith.IsZero(_rgnumWork[num14]) && AddToMulPivot(ref _rgnum[num13], _rgnumWork[num14], num12))
						{
							int num15 = _rgslotNextRow[num13];
							if (num6 == 0)
							{
								_mprowlin[num11].slotFirst = num15;
							}
							else
							{
								_rgslotNextRow[num6] = num15;
							}
							_rgslotNextRow[num13] = 0;
							_mprowlin[num11].cslot--;
							RemoveSlotFromList(num13, ref _mpcollin[num14], _rgslotNextCol);
							FreeSlot(num13);
							num13 = num15;
						}
						else
						{
							num6 = num13;
							num13 = _rgslotNextRow[num13];
						}
						_rgfWork[num14] = false;
					}
					for (int num16 = _rgslotNextRow[slotPiv]; num16 > 0; num16 = _rgslotNextRow[num16])
					{
						int num17 = _rgcol[num16];
						if (_rgfWork[num17])
						{
							Number numDst = _rgnumWork[num17];
							_arith.MulTo(ref numDst, num12);
							AddEntry(num11, num17, numDst);
						}
						else
						{
							_rgfWork[num17] = true;
						}
					}
				}
				for (int num18 = _mprowlin[num].slotFirst; num18 > 0; num18 = _rgslotNextRow[num18])
				{
					if (num18 != slotPiv)
					{
						int num19 = _rgcol[num18];
						AddToCslotList(num19, _mpcollin, _mpcslotcolFirst, fFirst: true);
						_rgnumWork[num19] = default(Number);
						_rgfWork[num19] = false;
					}
				}
			}
			for (int num20 = _mpcollin[num2].slotFirst; num20 > 0; num20 = _rgslotNextCol[num20])
			{
				int num21 = _rgrow[num20];
				AddToCslotList(num21, _mprowlin, _mpcslotrowFirst, fFirst: true);
				NotifyRowRecalc(num21);
			}
		}

		[Conditional("VERIFY_ALL")]
		protected void Verify(CoefMatrix matSrc, int[] rgcol)
		{
			for (int i = 0; i < _rcLim; i++)
			{
				for (int num = _mprowslotL[i]; num > 0; num = _rgslotNextRow[num])
				{
					int num2 = _rgcol[num];
					_rgnumWork[num2] = _rgnum[num];
				}
				_rgnumWork[i] = _arith.One;
				int row = _permRow[i];
				for (int j = 0; j < _rcLim; j++)
				{
					Number numDst = default(Number);
					_arith.AddToMul(ref numDst, _rgnumWork[j], _rgnum[_mprowslotU[j]]);
					for (int num3 = _mpcolslotU[j]; num3 > 0; num3 = _rgslotNextCol[num3])
					{
						int num4 = _rgrow[num3];
						if (!_arith.IsZero(_rgnumWork[num4]))
						{
							_arith.AddToMul(ref numDst, _rgnumWork[num4], _rgnum[num3]);
						}
					}
					int num5 = _permCol[j];
					Number num6 = (_mpcollin[num5].Rejected ? ((i == j) ? _arith.One : default(Number)) : ((rgcol == null) ? GetValue(matSrc, row, num5) : GetValue(matSrc, row, rgcol[num5])));
					if (!FuzzyVerify(num6, numDst))
					{
						return;
					}
				}
				for (int num7 = _mprowslotL[i]; num7 > 0; num7 = _rgslotNextRow[num7])
				{
					int num8 = _rgcol[num7];
					_rgnumWork[num8] = default(Number);
				}
				_rgnumWork[i] = default(Number);
			}
		}

		protected void UpdateCore(int ivar, Vector<Number> vec)
		{
			int entryCount = vec.EntryCount;
			int etaCount = EtaCount;
			int num = _mpetaslotFirst[etaCount];
			int num2 = num + entryCount;
			if (_rgnumEta == null)
			{
				GrowEtaSlotHeap(Math.Max(num2 * 5, 100));
			}
			else if (num2 > _rgnumEta.Length)
			{
				GrowEtaSlotHeap(num2 + num2 / 2);
			}
			_rgrowEta[num] = ivar;
			_rgnumEta[num] = vec.GetCoef(ivar);
			int num3 = num + 1;
			Vector<Number>.Iter iter = new Vector<Number>.Iter(vec);
			while (iter.IsValid)
			{
				if (iter.Rc != ivar)
				{
					_rgrowEta[num3] = iter.Rc;
					_rgnumEta[num3] = iter.Value;
					num3++;
				}
				iter.Advance();
			}
			_mpetaslotFirst.Add(num2);
		}

		protected void GrowEtaSlotHeap(int cslotMin)
		{
			Array.Resize(ref _rgrowEta, cslotMin);
			Array.Resize(ref _rgnumEta, cslotMin);
		}

		protected void ExecDfs(int[] mprcslotFirst, int[] rgslotNext, int[] rgrc)
		{
			while (_crcStack > 0)
			{
				int num = _crcStack - 1;
				int num2 = _rgrcStack[num];
				int num3 = _rgslotNextStack[num];
				if (num3 == 0)
				{
					_rgrcWork[_crcWork++] = num2;
					_rgfWork[num2] = true;
					_crcStack--;
					continue;
				}
				int num4 = rgrc[num3];
				_rgslotNextStack[num] = rgslotNext[num3];
				if (!_rgfWork[num4])
				{
					int num5 = _crcStack++;
					_rgrcStack[num5] = num4;
					_rgslotNextStack[num5] = mprcslotFirst[num4];
				}
			}
		}

		protected void ExecDfsSkip(int[] mprcslotFirst, int[] rgslotNext, int[] rgrc)
		{
			while (_crcStack > 0)
			{
				int num = _crcStack - 1;
				int num2 = _rgrcStack[num];
				int num3 = _rgslotNextStack[num];
				if (num3 == 0)
				{
					_rgrcWork[_crcWork++] = num2;
					_rgfWork[num2] = true;
					_crcStack--;
					continue;
				}
				_rgslotNextStack[num] = rgslotNext[num3];
				int num4 = rgrc[num3];
				if (!_rgfWork[num4])
				{
					int num5 = _crcStack++;
					_rgrcStack[num5] = num4;
					_rgslotNextStack[num5] = rgslotNext[mprcslotFirst[num4]];
				}
			}
		}
	}
}
