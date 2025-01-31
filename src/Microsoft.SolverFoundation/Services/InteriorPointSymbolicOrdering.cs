namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Select the manner in which the IPM solver performs symbolic factorizations of
	/// matrices.
	/// </summary>
	public enum InteriorPointSymbolicOrdering
	{
		/// <summary> Automatic (let the solver choose).
		/// </summary>
		Automatic = -1,
		/// <summary> Attempt to minimize estimated fill.
		/// </summary>
		MinimizeFill,
		/// <summary> Approximate minimum degree.
		/// </summary>
		AMD
	}
}
