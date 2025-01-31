namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Classes where each instance belongs to an "indexed collection".
	///   All instances of each collection are labelled by construction with 
	///   indexes that range from 0 to the cardinal of the collection minus 1. 
	///   This allows to associate extra information to each element of the
	///   collection using fast (array-based) mappings. 
	/// </summary>
	internal class Indexed
	{
		/// <summary>
		///   Gets the ID of the object. Within the collection this ID 
		///   uniquely denotes the object. Moreover, the ids range from 0 to size 
		///   of collection - 1, allowing to use it for fast array look-up.
		/// </summary>
		public readonly int Index;

		public Indexed(IIndexedCollection c)
		{
			Index = c.Subscribe(this);
		}
	}
}
