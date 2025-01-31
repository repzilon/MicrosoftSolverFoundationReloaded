namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Data-structures that are implemented using indexing benefit from fast
	///   (look-up based) way to associate extra info to the elements of an
	///   indexedCollection. Then if we modify the indexed collection we should
	///   propagate this info to the data-structure. For that purpose any
	///   such data-structure will inherit from LookupDatastructure and 
	///   implement a Resize method.
	/// </summary>
	internal abstract class ILookupDatastructure
	{
		/// <summary>
		///   Called when the initial collection is modified (typically some
		///   elements are added), so that the data-structure re-allocates
		///   its internal look-up tables if needed
		/// </summary>
		internal abstract void Resize(int newSize);

		/// <summary>
		///   Called when a new dependent data-structure is created.
		///   By calling this the data-structure will be informed of any
		///   change in the indexedCollection
		/// </summary>
		internal ILookupDatastructure(IIndexedCollection set)
		{
			set.Subscribe(this);
		}
	}
}
