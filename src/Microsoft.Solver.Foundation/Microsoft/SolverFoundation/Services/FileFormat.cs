namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A format which models can be loaded from or saved to.
	/// </summary>
	public enum FileFormat
	{
		/// <summary>
		/// Optimization Modeling Language format
		/// </summary>
		OML,
		/// <summary>
		/// MPS format
		/// </summary>
		MPS,
		/// <summary>
		/// Free MPS format
		/// </summary>
		FreeMPS,
		/// <summary>
		/// SMPS format (Fixed only)
		/// </summary>
		SMPS
	}
}
