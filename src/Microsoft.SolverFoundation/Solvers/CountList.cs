using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> a double link list to link row/col with the same
	/// number of non-zeros
	/// </summary>
	internal struct CountList
	{
		private const int Eliminated = -1;

		private const int Rejected = -2;

		private int _dimension;

		private int _maxsize;

		/// <summary> row count link list 
		/// </summary>
		public int[] _next;

		/// <summary> row count link list 
		/// </summary>
		public int[] _prev;

		/// <summary> reverse mapping from index to count
		/// </summary>
		private int[] _count;

		public CountList(int dimension, int maxsize)
		{
			_dimension = dimension;
			_maxsize = maxsize;
			int num = _dimension + _maxsize + 1;
			_next = new int[num];
			_prev = new int[num];
			_count = new int[_dimension];
			for (int i = 0; i < _dimension; i++)
			{
				_next[i] = (_prev[i] = -1);
			}
			for (int j = 0; j <= _maxsize; j++)
			{
				_next[_dimension + j] = (_prev[_dimension + j] = _dimension + j);
			}
		}

		/// <summary>
		/// Add this row/col index to the list of #size 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="size"></param>
		public void Add(int index, int size)
		{
			int num = _next[_dimension + size];
			_next[_dimension + size] = index;
			_next[index] = num;
			_prev[num] = index;
			_prev[index] = _dimension + size;
			_count[index] = size;
		}

		/// <summary>
		/// Remove this row/col
		/// </summary>
		/// <param name="index"></param>
		public void Remove(int index)
		{
			_next[_prev[index]] = _next[index];
			_prev[_next[index]] = _prev[index];
			_next[index] = -1;
			_prev[index] = -1;
			_count[index] = -1;
		}

		public bool IsEliminated(int index)
		{
			return _next[index] == -1;
		}

		public int Count(int index)
		{
			return _count[index];
		}

		public bool IsRejected(int index)
		{
			return _next[index] == -2;
		}

		public void Reject(int index)
		{
			_next[_prev[index]] = _next[index];
			_prev[_next[index]] = _prev[index];
			_next[index] = -2;
			_prev[index] = -2;
			_count[index] = -1;
		}

		public bool IsEmpty(int size)
		{
			return _next[_dimension + size] == _dimension + size;
		}

		public int GetFirst(out int count)
		{
			count = -1;
			for (int i = 0; i < _maxsize; i++)
			{
				int num = _next[_dimension + i];
				if (num != _dimension + i)
				{
					count = i;
					return num;
				}
			}
			return -1;
		}

		public IEnumerable<int> GetNext(int size)
		{
			if (!IsEmpty(size))
			{
				int head = ListHead(size);
				int firstRow = _next[_dimension + size];
				int nextRow = firstRow;
				do
				{
					yield return nextRow;
					nextRow = _next[nextRow];
				}
				while (nextRow != head);
			}
		}

		private int ListHead(int size)
		{
			return _dimension + size;
		}
	}
}
