namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// The algorithm to use for CSP
	/// </summary>
	public enum ConstraintProgrammingAlgorithm
	{
		/// <summary>
		/// Use whatever algorithm the solver thinks is best
		/// </summary>
		Default,
		/// <summary> Use tree search
		/// </summary>
		TreeSearch,
		/// <summary>
		/// Use local search
		/// </summary>
		LocalSearch
	}
}
