namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Indicates the quality of the "solution".
	/// </summary>
	public enum LinearSolutionQuality
	{
		/// <summary> Not run, or the solver is interrupted 
		/// </summary>
		None,
		/// <summary> Double precision 
		/// </summary>
		Approximate,
		/// <summary> Exact arithmetic   
		/// </summary>
		Exact
	}
}
