using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Subset of an indexed collection
	/// </summary>
	internal sealed class SubSet<Elt> : ILookupDatastructure where Elt : Indexed
	{
		private FiniteIntSet _set;

		private IndexedCollection<Elt> _collection;

		/// <summary>
		///   Number of elements currently in the subset
		/// </summary>
		public int Cardinal => _set.Cardinal;

		/// <summary>
		///   Get the i-th element of the set
		/// </summary>
		public Elt this[int i] => _collection[_set[i]];

		/// <summary>
		///   Creates a subset of the IndexedCollection;
		///   initially empty
		/// </summary>
		public SubSet(IndexedCollection<Elt> elts)
			: base(elts)
		{
			_set = new FiniteIntSet(0, Math.Max(elts.Cardinality - 1, 0));
			_collection = elts;
		}

		/// <summary>
		///   Adds an element to the subset
		/// </summary>
		public void Add(Elt elt)
		{
			_set.Add(elt.Index);
		}

		/// <summary>
		///   Removes an element for the set
		/// </summary>
		public void Remove(Elt elt)
		{
			_set.Remove(elt.Index);
		}

		/// <summary>
		///   Removes all emlements from the set
		/// </summary>
		public void Clear()
		{
			_set.Clear();
		}

		/// <summary>
		///   Returns true iff a value exists for the key
		/// </summary>
		public bool Contains(Elt elt)
		{
			return _set.Contains(elt.Index);
		}

		/// <summary>
		///   Enumerates the keys for which a value has been defined
		/// </summary>
		public IEnumerable<Elt> Enumerate()
		{
			FiniteIntSet.Enumerator enumerator = _set.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					int i = enumerator.Current;
					yield return _collection[i];
				}
			}
			finally
			{
			}
		}

		/// <summary>
		///   Called if new elements are added to the original
		///   IndexedCollection while this datastructure is in use.
		/// </summary>
		internal override void Resize(int newSize)
		{
			int maxValue = _set.MaxValue;
			if (newSize > maxValue)
			{
				int maxvalue = Math.Max(newSize, 2 * maxValue);
				FiniteIntSet finiteIntSet = new FiniteIntSet(0, maxvalue);
				FiniteIntSet.Enumerator enumerator = _set.GetEnumerator();
				while (enumerator.MoveNext())
				{
					int current = enumerator.Current;
					finiteIntSet.Add(current);
				}
				_set = finiteIntSet;
			}
		}
	}
}
