namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// The basis to use for simplex.
	/// </summary>
	public enum SimplexBasis
	{
		/// <summary> Use whatever basis the solver thinks is best
		/// </summary>
		Default,
		/// <summary> Use crash basis
		/// </summary>
		Crash,
		/// <summary> Use slack basis
		/// </summary>
		Slack,
		/// <summary> Use freedom basis
		/// </summary>
		Freedom
	}
}
