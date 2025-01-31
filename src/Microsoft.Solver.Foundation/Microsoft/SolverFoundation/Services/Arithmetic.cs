namespace Microsoft.SolverFoundation.Services
{
	/// <summary>This specifies an arithmetic to use for numeric solving. The choice of arithmetic trades speed for accuracy.
	/// </summary>
	public enum Arithmetic
	{
		/// <summary>Let the system pick an arithmetic
		/// </summary>
		Default,
		/// <summary>Exact arithmetic
		/// </summary>
		Exact,
		/// <summary>Double floating-point arithmetic
		/// </summary>
		Double
	}
}
