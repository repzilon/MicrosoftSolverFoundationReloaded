using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A min-heap designed for Indexed objects, and allowing to insert
	///   elements accompanied with a numerical (real) score. The heap will
	///   be constantly re-ordered so that the top is the element with lowest
	///   score. 
	/// </summary>
	/// <remarks>
	///   A key, non-standard functionality (and the one that justifies the
	///   requirement that contents be Indexed) is that this implementation
	///   allows to change the score assigned to a contained object. It is
	///   also possible to remove inserted objects, whatever their position.
	///   All operations take logarithmic time
	/// </remarks>
	internal class BinaryHeap<Elt> : ILookupDatastructure where Elt : Indexed
	{
		internal struct ElementScorePair
		{
			public Elt _value;

			public double _score;

			public ElementScorePair(Elt val, double score)
			{
				_score = score;
				_value = val;
			}
		}

		/// <summary>
		///   For each inserted element we keep index where
		///   inserted (0 if NOT inserted)
		/// </summary>
		private int[] _positions;

		/// <summary>
		///   The heap binary tree, implemented as array.
		///   root is at position 1, so that children of index i are found
		///   at indexes 2*i and 2*i + 1.
		/// </summary>
		private ElementScorePair[] _array;

		/// <summary>
		///    Simulates extendible array
		/// </summary>
		private int _firstFreePosition;

		/// <summary>
		///   True if the heap contains no element
		/// </summary>
		public bool Empty => _firstFreePosition == 1;

		/// <summary>
		///   Number of elements inserted in the heap
		/// </summary>
		public int Count => _firstFreePosition - 1;

		public BinaryHeap(IndexedCollection<Elt> set)
			: base(set)
		{
			int cardinality = set.Cardinality;
			_positions = new int[cardinality];
			_array = new ElementScorePair[cardinality + 1];
			_firstFreePosition = 1;
		}

		/// <summary>
		///   Removes an element contained in the heap
		/// </summary>
		/// <remarks>
		///   This is done by clobbering the element on top with
		///   the right-most leaf and reordering along the branch.
		/// </remarks>
		public void Remove(Elt e)
		{
			int index = e.Index;
			int num = _positions[index];
			_positions[index] = 0;
			_firstFreePosition--;
			if (_firstFreePosition != num)
			{
				double score = _array[num]._score;
				ElementScorePair p = _array[_firstFreePosition];
				Write(p, num);
				Reorder(num, p._score < score);
			}
		}

		/// <summary>
		///   Removes the element with minimum score and returns it 
		/// </summary>
		/// <remarks>
		///   This is done by clobbering the element on top with
		///   the right-most leaf and reordering-down. 
		/// </remarks>
		public Elt Pop()
		{
			Elt value = _array[1]._value;
			_positions[value.Index] = 0;
			_firstFreePosition--;
			if (_firstFreePosition != 1)
			{
				Write(_array[_firstFreePosition], 1);
				ReorderDown(1);
			}
			return value;
		}

		/// <summary>
		///   Inserts a non-contained element with indicated score
		/// </summary>
		public void Insert(Elt e, double score)
		{
			Write(new ElementScorePair(e, score), _firstFreePosition);
			ReorderUp(_firstFreePosition);
			_firstFreePosition++;
		}

		/// <summary>
		///   Change the score of an element contained in the heap. 
		///   This will cause re-ordering
		/// </summary>
		public void ChangeScore(Elt e, double newscore)
		{
			int num = _positions[e.Index];
			double score = _array[num]._score;
			_array[num]._score = newscore;
			Reorder(num, newscore < score);
		}

		/// <summary>
		///   returns true if the element is in the heap
		/// </summary>
		public bool Contains(Elt e)
		{
			return _positions[e.Index] != 0;
		}

		/// <summary>
		///   Returns the score attached to an included element
		/// </summary>
		public double Score(Elt e)
		{
			return _array[_positions[e.Index]]._score;
		}

		/// <summary>
		///   Access the element with lowest score, without removint it
		/// </summary>
		public Elt Top()
		{
			return _array[1]._value;
		}

		private void Reorder(int position, bool scoreDecreased)
		{
			if (scoreDecreased)
			{
				ReorderUp(position);
			}
			else
			{
				ReorderDown(position);
			}
		}

		/// <summary>
		///   Reordering operation, upward: from a node of binary tree
		///   if order w.r.t parent not ok we swap and repeat the
		///   operation for the parent
		/// </summary>
		private void ReorderUp(int position)
		{
			ElementScorePair p = _array[position];
			int num = position;
			while (HasParent(num))
			{
				int num2 = IndexParent(num);
				ElementScorePair p2 = _array[num2];
				if (!(p2._score > p._score))
				{
					break;
				}
				Write(p2, num);
				num = num2;
			}
			Write(p, num);
		}

		/// <summary>
		///   Reordering operation, downward: from a node of binary tree
		///   if order w.r.t parent not ok we swap and repeat the
		///   operation for the parent
		/// </summary>
		private void ReorderDown(int position)
		{
			ElementScorePair p = _array[position];
			int num = position;
			while (HasChildren(num))
			{
				int num2 = IndexDown(num);
				ElementScorePair p2 = _array[num2];
				if (!(p2._score < p._score))
				{
					break;
				}
				Write(p2, num);
				num = num2;
			}
			Write(p, num);
		}

		private void Write(ElementScorePair p, int pos)
		{
			_array[pos] = p;
			_positions[p._value.Index] = pos;
		}

		private static int IndexLeftChild(int i)
		{
			return 2 * i;
		}

		private static int IndexRightChild(int i)
		{
			return 2 * i + 1;
		}

		private static bool HasParent(int pos)
		{
			return pos > 1;
		}

		private bool HasChildren(int pos)
		{
			return IndexLeftChild(pos) < _firstFreePosition;
		}

		private static int IndexParent(int i)
		{
			return i / 2;
		}

		private int IndexDown(int i)
		{
			int num = 2 * i;
			int num2 = num + 1;
			if (num2 >= _firstFreePosition || _array[num]._score <= _array[num2]._score)
			{
				return num;
			}
			return num2;
		}

		/// <summary>
		///   Called if new elements are added to the original
		///   IndexedCollection while this datastructure is in use.
		/// </summary>
		internal override void Resize(int newSize)
		{
			int num = _positions.Length;
			if (newSize > num)
			{
				int num2 = Math.Max(newSize, 2 * num);
				int[] array = new int[num2];
				ElementScorePair[] array2 = new ElementScorePair[num2 + 1];
				Array.Copy(_array, array2, _array.Length);
				Array.Copy(_positions, array, _positions.Length);
				_array = array2;
				_positions = array;
			}
		}
	}
}
