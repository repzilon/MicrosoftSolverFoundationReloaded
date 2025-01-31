namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Indicates how decomposition techniques should be used to solve stochastic models.
	/// </summary>
	public enum DecompositionType
	{
		/// <summary>
		/// Let the solver decide whether to use decomposition.
		/// </summary>
		Automatic,
		/// <summary>
		/// Do not use decomposition. Form the deterministic equivalent instead.
		/// </summary>
		Disabled,
		/// <summary>
		/// Use decomposition.
		/// </summary>
		Enabled
	}
}
