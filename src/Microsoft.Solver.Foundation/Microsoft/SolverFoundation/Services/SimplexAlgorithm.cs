namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// The algorithm to use for simplex.
	/// </summary>
	public enum SimplexAlgorithm
	{
		/// <summary> Use whatever algorithm the solver thinks is best
		/// </summary>
		Default,
		/// <summary> Use primal simplex
		/// </summary>
		Primal,
		/// <summary> Use dual simplex
		/// </summary>
		Dual
	}
}
