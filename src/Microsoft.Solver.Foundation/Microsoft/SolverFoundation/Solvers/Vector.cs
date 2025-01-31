using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class Vector<Number>
	{
		/// <summary> Iterate the non-zero elements of a vector in efficient but
		///           arbitrary order.
		/// </summary>
		public struct Iter
		{
			private Vector<Number> _vec;

			private int _slotCur;

			/// <summary> True if the iterator is at a defined element, false if finished.
			/// </summary>
			public bool IsValid => _slotCur > 0;

			public int Rc => _vec._rgrc[_slotCur];

			public Number Value => _vec._rgnum[_slotCur];

			internal int Slot => _slotCur;

			/// <summary> Iterate the non-zero elements of a vector by declining order
			///           of slot number.  You can use OrderForward() to cause this to
			///           be guaranteed right-to-left order, else order is arbitrary.
			/// </summary>
			public Iter(Vector<Number> vec)
			{
				_vec = vec;
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
			public void RemoveAndAdvance()
			{
				_vec.RemoveSlot(_slotCur);
				_slotCur--;
			}

			public void RemoveSeen(int rc)
			{
				_vec.RemoveCoef(rc);
			}
		}

		protected const int kcslotMin = 100;

		internal int[] _mprcslot;

		internal int[] _rgrc;

		internal Number[] _rgnum;

		internal int _slotLim;

		private int _rcLim;

		private string _display;

		internal int Capacity => _rgrc.Length - 1;

		/// <summary> The count of defined row/column positions (including empties).
		/// </summary>
		public int RcCount => _rcLim;

		/// <summary> The count of non-zero elements currently in use.
		/// </summary>
		public int EntryCount => _slotLim - 1;

		public Number this[int rc]
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

		[Conditional("DEBUG")]
		protected void AssertValid()
		{
		}

		protected Vector(int rcLim, int cslotInit)
		{
			_rcLim = rcLim;
			_mprcslot = new int[_rcLim];
			GrowSlotHeap(Math.Max(cslotInit, 100));
			_rgrc[0] = -1;
			_slotLim = 1;
		}

		/// <summary> Copy the contents of vecSrc into this vector.
		/// </summary>
		public void CopyFrom(Vector<Number> vecSrc)
		{
			if (_mprcslot.Length < vecSrc._rcLim)
			{
				_mprcslot = new int[vecSrc._rcLim];
			}
			else
			{
				EraseRcSlotMap();
			}
			_rcLim = vecSrc._rcLim;
			_slotLim = 1;
			if (Capacity < vecSrc.EntryCount)
			{
				GrowSlotHeap(Math.Min(Capacity + vecSrc.EntryCount, vecSrc.Capacity));
			}
			_slotLim = vecSrc._slotLim;
			Array.Copy(vecSrc._rgrc, _rgrc, _slotLim);
			Array.Copy(vecSrc._rgnum, _rgnum, _slotLim);
			int num = _slotLim;
			while (--num > 0)
			{
				_ = _rgrc[num];
				_mprcslot[_rgrc[num]] = num;
			}
		}

		protected void GrowSlotHeap(int cslot)
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
			EraseRcSlotMap();
			_slotLim = 1;
		}

		/// <summary> Erase the _mprcslot entries.
		/// </summary>
		protected void EraseRcSlotMap()
		{
			int num = _slotLim;
			while (--num > 0)
			{
				int num2 = _rgrc[num];
				_mprcslot[num2] = 0;
			}
		}

		/// <summary> Set the slots in rising order of column number.
		/// </summary>
		public void OrderForward()
		{
			Array.Sort(_rgrc, _rgnum, 1, _slotLim - 1);
			int num = _slotLim;
			while (0 < --num)
			{
				_mprcslot[_rgrc[num]] = num;
			}
		}

		/// <summary> Multiply every element of the vector by a.
		/// </summary>
		public abstract void ScaleBy(Number a);

		/// <summary> Return the value at the defined row/column position, element [rc].
		///           0 &lt;= rc &lt; RcCount
		///           Empty positions return zero.
		/// </summary>
		public Number GetCoef(int rc)
		{
			return _rgnum[_mprcslot[rc]];
		}

		/// <summary> Get the value at the defined row/column position, element [rc].
		///           0 &lt;= rc &lt; RcCount
		///           Empty positions return false.
		/// </summary>
		public bool TryGetCoef(int rc, out Number value)
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
			}
		}

		protected void RemoveSlot(int slot)
		{
			_mprcslot[_rgrc[slot]] = 0;
			_slotLim--;
			if (slot < _slotLim)
			{
				int num = _rgrc[_slotLim];
				_mprcslot[num] = slot;
				_rgrc[slot] = num;
				_rgnum[slot] = _rgnum[_slotLim];
			}
		}

		protected void SetCoefCore(int rc, Number num)
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
			}
			_rgnum[num2] = num;
		}

		public abstract void SetCoef(int rc, Number value);

		protected void GrowSlotHeap(int newLength, ref double[] shadows)
		{
			GrowSlotHeap(newLength);
			if (shadows.Length < _rgrc.Length)
			{
				Array.Resize(ref shadows, _rgrc.Length);
			}
		}

		private void Display()
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
				stringBuilder.Append(GetCoef(i).ToString());
			}
			_display = stringBuilder.ToString();
		}

		public override string ToString()
		{
			_display = null;
			Display();
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
