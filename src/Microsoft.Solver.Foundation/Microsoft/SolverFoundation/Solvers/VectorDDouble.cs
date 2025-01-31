using System;
using System.Globalization;
using System.Text;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class VectorDDouble
	{
		/// <summary> Iterate the non-zero elements of a vector in efficient but
		///           arbitrary order.
		/// </summary>
		public struct Iter
		{
			private int _slotCur;

			/// <summary> True if the iterator is at a defined element, false if finished.
			/// </summary>
			public bool IsValid => _slotCur > 0;

			/// <summary> Iterate the non-zero elements of a vector by declining order
			///           of slot double.  You can use OrderForward() to cause this to
			///           be guaranteed right-to-left order, else order is arbitrary.
			/// </summary>
			public Iter(ref VectorDDouble vec)
			{
				_slotCur = vec._slotLim - 1;
			}

			/// <summary> Move to the next defined element.
			/// </summary>
			public void Advance()
			{
				_slotCur--;
			}

			/// <summary> Set current element to zero and remove from active slot list,
			///           then advance to next element.
			/// </summary>
			public void RemoveAndAdvance(ref VectorDDouble vec)
			{
				vec.RemoveSlot(_slotCur);
				_slotCur--;
			}

			public int Rc(ref VectorDDouble vec)
			{
				return vec._rgrc[_slotCur];
			}

			public double Value(ref VectorDDouble vec)
			{
				return vec._rgnum[_slotCur];
			}
		}

		private const int kcslotMin = 100;

		private int[] _mprcslot;

		private int[] _rgrc;

		private double[] _rgnum;

		private int _slotLim;

		private int _rcLim;

		internal int Capacity => _rgrc.Length - 1;

		/// <summary> The count of defined row/column positions (including empties).
		/// </summary>
		public int RcCount => _rcLim;

		/// <summary> The count of non-zero elements currently in use.
		/// </summary>
		public int EntryCount => _slotLim - 1;

		public double this[int rc]
		{
			get
			{
				return GetCoef(rc);
			}
			set
			{
				SetCoef(rc, value);
			}
		}

		private void AssertValid()
		{
		}

		public VectorDDouble(int rcLim, int cslotInit)
		{
			_rcLim = rcLim;
			_mprcslot = new int[_rcLim];
			_slotLim = 0;
			GrowSlotHeap(Math.Max(cslotInit, 100));
			_rgrc[0] = -1;
			_slotLim = 1;
			AssertValid();
		}

		private void GrowSlotHeap(int cslot)
		{
			if (cslot > _rcLim)
			{
				cslot = _rcLim;
			}
			cslot++;
			Array.Resize(ref _rgnum, cslot);
			Array.Resize(ref _rgrc, cslot);
		}

		/// <summary> Set all vector positions to zero (optimized as empty).
		/// </summary>
		public void Clear()
		{
			AssertValid();
			EraseRcSlotMap();
			_slotLim = 1;
			AssertValid();
		}

		/// <summary> Erase the _mprcslot entries.
		/// </summary>
		private void EraseRcSlotMap()
		{
			int num = _slotLim;
			while (--num > 0)
			{
				int num2 = _rgrc[num];
				_mprcslot[num2] = 0;
			}
		}

		/// <summary> Return the value at the defined row/column position, element [rc].
		///           0 &lt;= rc &lt; RcCount
		///           Empty positions return zero.
		/// </summary>
		public double GetCoef(int rc)
		{
			return _rgnum[_mprcslot[rc]];
		}

		/// <summary> Get the value at the defined row/column position, element [rc].
		///           0 &lt;= rc &lt; RcCount
		///           Empty positions return false.
		/// </summary>
		public bool TryGetCoef(int rc, out double value)
		{
			int num = _mprcslot[rc];
			value = _rgnum[num];
			return 0 != num;
		}

		/// <summary> Set element [rc] to zero, sparsely represented.
		///           0 &lt;= rc &lt; RcCount
		/// </summary>
		public void RemoveCoef(int rc)
		{
			AssertValid();
			int num = _mprcslot[rc];
			if (num != 0)
			{
				_mprcslot[rc] = 0;
				_slotLim--;
				if (num < _slotLim)
				{
					int num2 = _rgrc[_slotLim];
					_mprcslot[num2] = num;
					_rgrc[num] = num2;
					_rgnum[num] = _rgnum[_slotLim];
				}
				AssertValid();
			}
		}

		private void RemoveSlot(int slot)
		{
			AssertValid();
			_mprcslot[_rgrc[slot]] = 0;
			_slotLim--;
			if (slot < _slotLim)
			{
				int num = _rgrc[_slotLim];
				_mprcslot[num] = slot;
				_rgrc[slot] = num;
				_rgnum[slot] = _rgnum[_slotLim];
			}
			AssertValid();
		}

		public void SetCoefNonZero(int rc, double num)
		{
			int num2 = _mprcslot[rc];
			if (num2 == 0)
			{
				num2 = _slotLim;
				if (num2 > Capacity)
				{
					GrowSlotHeap(num2 + num2 / 2);
				}
				_rgrc[num2] = rc;
				_mprcslot[rc] = num2;
				_slotLim++;
				AssertValid();
			}
			_rgnum[num2] = num;
		}

		public void SetCoef(int rc, double num)
		{
			if (0.0 == num)
			{
				RemoveCoef(rc);
			}
			else
			{
				SetCoefNonZero(rc, num);
			}
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < RcCount; i++)
			{
				if (0 < i)
				{
					stringBuilder.Append(", ");
					if (2048 < stringBuilder.Length)
					{
						stringBuilder.Append("...");
						break;
					}
				}
				stringBuilder.Append(GetCoef(i).ToString(NumberFormatInfo.InvariantInfo));
			}
			return stringBuilder.ToString();
		}
	}
}
