using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Implements a heap - for implementing priority queues.
	/// </summary>
	internal class Heap<T>
	{
		private readonly List<T> _rgv;

		private readonly Func<T, T, bool> _fnReverse;

		protected List<T> Elements => _rgv;

		/// <summary> Func tests true if first element should be after the second
		/// </summary>
		public Func<T, T, bool> FnReverse => _fnReverse;

		/// <summary> Current count of elements remaining in the heap
		/// </summary>
		public int Count => _rgv.Count - 1;

		/// <summary> Peek at the first element in the heap
		/// </summary>
		public T Top
		{
			get
			{
				if (_rgv.Count <= 1)
				{
					return default(T);
				}
				return _rgv[1];
			}
		}

		/// <summary> A Heap structure gives efficient access to the ordered next element.
		/// </summary>
		/// <param name="fnReverse"> tests true if first element should be after the second </param>
		public Heap(Func<T, T, bool> fnReverse)
		{
			_rgv = new List<T>();
			_rgv.Add(default(T));
			_fnReverse = fnReverse;
		}

		/// <summary> A Heap structure gives efficient access to the ordered next element.
		/// </summary>
		/// <param name="fnReverse"> tests true if first element should be after the second </param>
		/// <param name="capacity"> the maximum capacity of the heap </param>
		public Heap(Func<T, T, bool> fnReverse, int capacity)
		{
			_rgv = new List<T>(capacity);
			_rgv.Add(default(T));
			_fnReverse = fnReverse;
		}

		protected static int Parent(int iv)
		{
			return iv >> 1;
		}

		protected static int Left(int iv)
		{
			return iv + iv;
		}

		protected static int Right(int iv)
		{
			return iv + iv + 1;
		}

		protected virtual void MoveTo(T v, int iv)
		{
			_rgv[iv] = v;
		}

		protected virtual void Delete(T v)
		{
		}

		/// <summary> Discard all elements currently in the heap
		/// </summary>
		public void Clear()
		{
			_rgv.RemoveRange(1, _rgv.Count - 1);
		}

		/// <summary> Remove and return the first element in the heap
		/// </summary>
		public T Pop()
		{
			int count = _rgv.Count;
			if (count <= 1)
			{
				throw new InvalidOperationException(Resources.EmptyHeap);
			}
			T val = _rgv[1];
			Delete(val);
			_rgv[1] = _rgv[--count];
			_rgv.RemoveAt(count);
			if (count > 1)
			{
				BubbleDown(1);
			}
			return val;
		}

		/// <summary> Add a new element to the heap
		/// </summary>
		public void Add(T v)
		{
			int count = _rgv.Count;
			_rgv.Add(v);
			BubbleUp(count);
		}

		/// <summary> Returns all the current elements of the heap in
		///           an array, with no guaranteed order.
		/// </summary>
		public T[] ToArrayUnsorted()
		{
			T[] array = new T[Count];
			_rgv.CopyTo(1, array, 0, Count);
			return array;
		}

		protected void BubbleUp(int iv)
		{
			T val = _rgv[iv];
			int num;
			while ((num = Parent(iv)) > 0 && _fnReverse(_rgv[num], val))
			{
				MoveTo(_rgv[num], iv);
				iv = num;
			}
			MoveTo(val, iv);
		}

		protected void BubbleDown(int iv)
		{
			int count = _rgv.Count;
			T val = _rgv[iv];
			int num;
			while ((num = Left(iv)) < count)
			{
				if (num + 1 < count && _fnReverse(_rgv[num], _rgv[num + 1]))
				{
					num++;
				}
				if (!_fnReverse(val, _rgv[num]))
				{
					break;
				}
				MoveTo(_rgv[num], iv);
				iv = num;
			}
			MoveTo(val, iv);
		}
	}
}
