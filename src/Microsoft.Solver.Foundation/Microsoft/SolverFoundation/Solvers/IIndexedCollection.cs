namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Interface exporting methods of class IndexedCollection to non-generic
	///   class Indexed. (tedious, but needed to allow the Indexed objects and
	///   the LookupDatastructures to subscribe themselves at construction-time
	///   auto-magically, 
	/// </summary>
	internal interface IIndexedCollection
	{
		/// <summary>
		///   Subscribe a new indexed to collection; 
		///   returns the ID that the index must take
		/// </summary>
		int Subscribe(Indexed e);

		/// <summary>
		///   Called when a new dependent data-structure is created.
		///   By calling this the data-structure will be informed of any
		///   change in the indexedCollection
		/// </summary>
		void Subscribe(ILookupDatastructure ds);
	}
}
