using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A collection of indexed elements that are used jointly.
	///   The elements contained in this collection will automatically be
	///   labelled with unique IDs from 0 to cardinality-1. This allows to
	///   associate extra information to each element of the collection
	///   using fast (array-based) mappings. 
	/// </summary>
	/// <remarks>
	///   A typical use is to create first the Elements stored in this
	///   collection then to create all dependent data-structures. It is
	///   possible to do things in the opposite order but this will cause
	///   many calls to resize the data-structure - once data-structures are
	///   created add elements sparingly.
	/// </remarks>
	internal class IndexedCollection<Elt> : IIndexedCollection where Elt : Indexed
	{
		private List<Elt> _elements;

		private List<ILookupDatastructure> _dependencies;

		/// <summary>
		///   Number of elements (used in particular to number new instances)
		/// </summary>
		public int Cardinality => _elements.Count;

		/// <summary>
		///   Used to enumerate by index. Use preferably to foreach
		///   whenever the actual indices matter.
		/// </summary>
		public Elt this[int idx] => _elements[idx];

		public IndexedCollection()
		{
			_elements = new List<Elt>();
			_dependencies = new List<ILookupDatastructure>();
		}

		/// <summary>
		///   notifies the collection that a new element that is attached
		///   to it has been created.
		/// </summary>
		int IIndexedCollection.Subscribe(Indexed i)
		{
			Elt item = i as Elt;
			int cardinality = Cardinality;
			_elements.Add(item);
			DispatchSizeChange();
			return cardinality;
		}

		/// <summary>
		///   Returns the set of elements
		/// </summary>
		public IEnumerable<Elt> Enumerate()
		{
			return _elements;
		}

		/// <summary>
		///   True if the element is part of the collection.
		///   Convenient for integrity checks
		/// </summary>
		public bool Contains(Elt e)
		{
			int index = e.Index;
			if (index < _elements.Count)
			{
				return _elements[index] == e;
			}
			return false;
		}

		/// <summary>
		///   Called when a new dependent data-structure is created.
		///   By calling this the data-structure will be informed of any
		///   change in the indexedCollection
		/// </summary>
		void IIndexedCollection.Subscribe(ILookupDatastructure ds)
		{
			_dependencies.Add(ds);
		}

		/// <summary>
		///   Called when an element is added to the collection.
		///   If some data-structures are created that depend on the
		///   collection we have to modify them so that they resize
		///   if needed.
		/// </summary>
		private void DispatchSizeChange()
		{
			if (_dependencies.Count == 0)
			{
				return;
			}
			int count = _elements.Count;
			foreach (ILookupDatastructure dependency in _dependencies)
			{
				dependency.Resize(count);
			}
		}
	}
}
