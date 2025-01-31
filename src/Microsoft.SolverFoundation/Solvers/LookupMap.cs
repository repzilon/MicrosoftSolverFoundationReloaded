using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Mapping that associates information of a certain type (Value) to the
	///   elements of an indexedCollection. Use similar to Dictionary but
	///   implementation uses fast (constant-time) index-based look-up.
	/// </summary>
	/// <remarks>
	///   Trying hard to implement semantics strictly equivalent to Dictionary
	///   even for value types (which I find best implemented with extra boolean
	///   flag - default is not necessarily invalid value and having trouble
	///   with generic equality.)
	/// </remarks>
	internal sealed class LookupMap<Elt, Value> : ILookupDatastructure where Elt : Indexed
	{
		private Value[] _lookupTable;

		/// <summary>
		///   Gets or sets the value associated to a key.
		/// </summary>
		public Value this[Elt key]
		{
			get
			{
				return _lookupTable[key.Index];
			}
			set
			{
				_lookupTable[key.Index] = value;
			}
		}

		/// <summary>
		///   Creates a Map that allows to associate extra info of type Value
		///   to any member of the indexed collection.
		/// </summary>
		public LookupMap(IndexedCollection<Elt> elts)
			: base(elts)
		{
			int cardinality = elts.Cardinality;
			_lookupTable = new Value[cardinality];
		}

		/// <summary>
		///   Called if new elements are added to the original
		///   IndexedCollection while this datastructure is in use.
		/// </summary>
		internal override void Resize(int newSize)
		{
			int num = _lookupTable.Length;
			if (newSize > num)
			{
				int num2 = Math.Max(newSize, 2 * num);
				Value[] array = new Value[num2];
				Array.Copy(_lookupTable, array, num);
				_lookupTable = array;
			}
		}
	}
}
