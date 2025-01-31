using System;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> This class allows accumulation of a predictably bounded
	///           count of unique values from an unpredictable and likely
	///           much longer sequence of values in unknown order.
	/// </summary>
	/// <typeparam name="T"> the type of values to be accumulated </typeparam>
	internal class AccumulateUnique<T>
	{
		private T[] _V;

		private int _next;

		private bool _unique;

		/// <summary> Indexed access to the accumulation.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public T this[int index]
		{
			get
			{
				if (index < Count())
				{
					return _V[index];
				}
				throw new IndexOutOfRangeException();
			}
		}

		/// <summary> A bounded array of unique values to be filterred out
		///           of an unbounded, unordered sequence.
		/// </summary>
		/// <param name="capacity"> At least the maximum count of unique values ever possible </param>
		public AccumulateUnique(int capacity)
		{
			_V = new T[capacity];
			_next = 0;
			_unique = true;
		}

		/// <summary> Return a count of the unique values.
		/// </summary>
		/// <returns> a count of the distinct values </returns>
		public int Count()
		{
			if (!_unique)
			{
				Array.Sort(_V, 0, _next);
				int num = 0;
				for (int i = 1; i < _next; i++)
				{
					if (!_V[num].Equals(_V[i]))
					{
						num++;
						if (num < i)
						{
							_V[num] = _V[i];
						}
					}
				}
				_next = num + 1;
				_unique = true;
			}
			return _next;
		}

		/// <summary> Add another value to the accumulation
		/// </summary>
		/// <param name="val"> A value to be considered </param>
		public void Add(T val)
		{
			if (_next == _V.Length)
			{
				Count();
			}
			_V[_next++] = val;
			_unique = false;
		}

		/// <summary> Remove all contents.
		/// </summary>
		public void Clear()
		{
			_next = 0;
			_unique = true;
		}
	}
}
