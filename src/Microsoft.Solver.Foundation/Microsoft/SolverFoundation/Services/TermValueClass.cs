namespace Microsoft.SolverFoundation.Services
{
	internal enum TermValueClass
	{
		/// <summary>
		/// A numeric value.
		/// </summary>
		Numeric,
		/// <summary>
		/// An enumerated value. Uses a numeric value as an index into a set of strings.
		/// Enumerated values can't be used in mathematical operations (except comparisons).
		/// </summary>
		Enumerated,
		/// <summary>
		/// A string.
		/// </summary>
		String,
		/// <summary>
		/// Either an arbitrary number or an arbitrary string.
		/// "Any" values can only be used as an index.
		/// </summary>
		Any,
		/// <summary>
		/// A multidimensional parameter or decision.
		/// Table values can only be used by indexing into them.
		/// </summary>
		Table,
		/// <summary>
		/// An instance of a submodel
		/// </summary>
		Submodel,
		/// <summary>
		/// A random distribution
		/// </summary>
		Distribution
	}
}
