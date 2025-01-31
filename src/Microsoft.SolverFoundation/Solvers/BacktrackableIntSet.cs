namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Class of finite subsets of the integers, that are backtrackable.
	///
	///   The integer values that can be stored within this set have to be
	///   bounded between a lower and upper bounds, and these bounds should
	///   not be unreasonably large (underlying is an array representation).
	///
	///   A BacktrackableIntSet int is connected to a TrailOfIntSets,
	///   so that any save operation on the trail allows to later change back
	///   to the previous state of all the backtrackable sets connected to it.
	/// </summary>
	internal class BacktrackableIntSet
	{
		internal FiniteIntSet _set;

		private TrailOfIntSets _trail;

		/// <summary>
		///   Get the i-th element of the set
		/// </summary>
		public int this[int i] => _set[i];

		/// <summary>
		///   Number of elements effectively stored in the vector
		/// </summary>
		public int Cardinal => _set.Cardinal;

		/// <summary>
		///   Construction; the set is initially empty
		/// </summary>
		/// <param name="t">trail to which the set is connected</param>
		/// <param name="minvalue">min value storable in the set</param>
		/// <param name="maxvalue">max value storable in the set</param>
		public BacktrackableIntSet(TrailOfIntSets t, int minvalue, int maxvalue)
		{
			_set = new FiniteIntSet(minvalue, maxvalue);
			_trail = t;
			t.Register(this);
		}

		/// <summary>
		///   Adds an integer to the set.
		/// </summary>
		public void Add(int elt)
		{
			if (!_set.Contains(elt))
			{
				_set.AddNotContainedElement(elt);
				_trail.RecordInsertion(this, elt);
			}
		}

		/// <summary>
		///   Removes an integer from the set.
		/// </summary>
		public void Remove(int elt)
		{
			if (_set.Contains(elt))
			{
				_set.RemoveContainedElement(elt);
				_trail.RecordRemoval(this, elt);
			}
		}

		/// <summary>
		///   Adds all values to the set (this is typically used for a 
		///   freshly constructed set, to have it initially full, because
		///   the constructor makes it empty by default)
		/// </summary>
		public void Fill()
		{
			int minValue = _set.MinValue;
			int maxValue = _set.MaxValue;
			for (int i = minValue; i <= maxValue; i++)
			{
				Add(i);
			}
		}

		/// <summary>
		///   Enumeration
		/// </summary>
		public FiniteIntSet.Enumerator GetEnumerator()
		{
			return _set.GetEnumerator();
		}

		/// <summary>
		///   Returns true iff elt included in the set.
		/// </summary>
		public bool Contains(int elt)
		{
			return _set.Contains(elt);
		}
	}
}
