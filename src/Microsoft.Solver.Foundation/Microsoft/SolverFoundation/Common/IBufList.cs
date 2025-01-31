using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Supports incremental access and direct access.
	/// </summary>
	internal interface IBufList<T> : IEnumerable<T>, IEnumerable
	{
		/// <summary>
		/// Fetch the item at index i. Throw if i is too large.
		/// </summary>
		T this[int i] { get; }

		/// <summary>
		/// A lower bound on the count. Indices less than this are guaranteed
		/// to be valid.
		/// </summary>
		int LowerCount { get; }

		/// <summary>
		/// Trys to fetch the item at index i. Returns false if i is too large.
		/// </summary>
		bool TryGet(int i, out T e);
	}
}
