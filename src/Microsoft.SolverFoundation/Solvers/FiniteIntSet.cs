namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Class of finite subsets of the integers. 
	///   The integer values that can be stored within this set has to be
	///   bounded between a lower and upper bounds, and these bounds should
	///   not be unreasonably large (underlying is an array representation).
	/// </summary>
	/// <remarks>
	///   Representation is mixed:
	///
	///   - one vector ("_content" fields) containing the elements effectively
	///     present in the set (these are stored from index 0 to 
	///     _firstNonWrittenPosition-1)
	///
	///   - one basic array ("_position" field) such that _position[i] == -1
	///     if i is not contained in the array, and position[i] gives the
	///     position at which i is stored in the _content vector otherwise.
	///
	///   These two vectors are in fact stored as a unique array of Cells.
	/// </remarks>
	///
	/// <remarks>
	///   Good in terms of worst-case time for each operation (everything
	///   constant-time) BUT not the most memory-efficient representation;
	///   consider bit-vectors as well?
	/// </remarks>
	internal class FiniteIntSet
	{
		internal struct Enumerator
		{
			private FiniteIntSet _set;

			private int _position;

			private int _end;

			public int Current => _set.Content(_position);

			public Enumerator(FiniteIntSet s)
			{
				_set = s;
				_position = -1;
				_end = s._firstNonWrittenPosition;
			}

			public bool MoveNext()
			{
				_position++;
				return _position < _end;
			}

			public void Reset()
			{
				_position = -1;
			}
		}

		internal struct Cell
		{
			public int _position;

			public int _content;

			public Cell(int pos, int c)
			{
				_position = pos;
				_content = c;
			}
		}

		private int _firstNonWrittenPosition;

		private readonly int _minValue;

		private Cell[] _tab;

		/// <summary>
		///   Cardinal of the set, i.e. nb of elements
		/// </summary>
		public int Cardinal => _firstNonWrittenPosition;

		/// <summary>
		///   Lowest integer value whose storage in the set is allowed
		/// </summary>
		public int MinValue => _minValue;

		/// <summary>
		///   Highest integer value whose storage in the set is allowed
		/// </summary>
		public int MaxValue => _minValue + _tab.Length - 1;

		/// <summary>
		///   get or set the i-th element
		/// </summary>
		public int this[int pos] => Content(pos);

		/// <summary>
		///   Construction of a set that can contain integer elements
		///   ranging over minvalue .. maxvalue. The set is initially empty.
		/// </summary>
		public FiniteIntSet(int minvalue, int maxvalue)
		{
			int num = maxvalue - minvalue + 1;
			_minValue = minvalue;
			_tab = new Cell[num];
			for (int i = 0; i < num; i++)
			{
				ref Cell reference = ref _tab[i];
				reference = new Cell(-1, -1234567890);
			}
		}

		/// <summary>
		///   Adds an element to the set.
		///   Element is assumed NOT already present
		/// </summary>
		public void AddNotContainedElement(int elt)
		{
			SetPosition(elt, _firstNonWrittenPosition);
			SetContent(_firstNonWrittenPosition, elt);
			_firstNonWrittenPosition++;
		}

		/// <summary>
		///   Adds an element in the set 
		/// </summary>
		public void Add(int elt)
		{
			if (!Contains(elt))
			{
				AddNotContainedElement(elt);
			}
		}

		/// <summary>
		///   Removes an element from the set.
		///   Element is assumed present.
		/// </summary>
		public void RemoveContainedElement(int elt)
		{
			_firstNonWrittenPosition--;
			int num = Content(_firstNonWrittenPosition);
			if (elt != num)
			{
				int num2 = Position(elt);
				SetPosition(num, num2);
				SetContent(num2, num);
			}
			SetPosition(elt, -1);
		}

		/// <summary>
		///   Removes an element from the set.
		/// </summary>
		public void Remove(int elt)
		{
			if (Contains(elt))
			{
				RemoveContainedElement(elt);
			}
		}

		/// <summary>
		///   Removes all elements from the set
		/// </summary>
		public void Clear()
		{
			for (int i = 0; i < _firstNonWrittenPosition; i++)
			{
				int elt = Content(i);
				SetPosition(elt, -1);
			}
			_firstNonWrittenPosition = 0;
		}

		/// <summary>
		///   True if the element is contained in the set
		/// </summary>
		public bool Contains(int elt)
		{
			return Position(elt) >= 0;
		}

		/// <summary>
		///   True iff the set is empty
		/// </summary>
		public bool IsEmpty()
		{
			return _firstNonWrittenPosition == 0;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		private int Position(int elt)
		{
			return _tab[elt - _minValue]._position;
		}

		private void SetPosition(int elt, int newpos)
		{
			_tab[elt - _minValue]._position = newpos;
		}

		private int Content(int pos)
		{
			return _tab[pos]._content;
		}

		private void SetContent(int pos, int newcontent)
		{
			_tab[pos]._content = newcontent;
		}
	}
}
