using System;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Implements a heap of integers that is indexed, to track whether a value
	/// is in the heap.
	/// </summary>
	internal class IndexedHeap : Heap<int>
	{
		private readonly int[] _mpviv;

		public IndexedHeap(int vLim, Func<int, int, bool> fnReverse)
			: base(fnReverse)
		{
			_mpviv = new int[vLim];
		}

		public bool InHeap(int v)
		{
			return _mpviv[v] > 0;
		}

		public void MoveUp(int v)
		{
			BubbleUp(_mpviv[v]);
		}

		public void MoveDown(int v)
		{
			BubbleDown(_mpviv[v]);
		}

		public void Remove(int v)
		{
			int num = _mpviv[v];
			Delete(v);
			base.Elements[num] = Statics.PopList(base.Elements);
			if (num > 1 && base.FnReverse(base.Elements[Heap<int>.Parent(num)], base.Elements[num]))
			{
				BubbleUp(num);
			}
			else
			{
				BubbleDown(num);
			}
		}

		protected override void MoveTo(int v, int iv)
		{
			base.Elements[iv] = v;
			_mpviv[v] = iv;
		}

		protected override void Delete(int v)
		{
			_mpviv[v] = 0;
		}
	}
}
