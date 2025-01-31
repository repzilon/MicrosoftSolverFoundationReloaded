namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> The kind of initial basis used.
	/// </summary>
	public enum SimplexBasisKind
	{
		/// <summary> A basis using all the slack variables (default).
		/// </summary>
		Slack,
		/// <summary> Use basis currently specified.
		/// </summary>
		Current,
		/// <summary> A basis using unfixed slacks and other maximally free variables
		/// </summary>
		Freedom,
		/// <summary> Crash basis.
		/// </summary>
		Crash
	}
}
