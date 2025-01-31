namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>The termination policy for NelderMead.
	/// </summary>
	public enum NelderMeadTerminationSensitivity
	{
		/// <summary>Terminate when the simplex is sufficiently small and the goal value has remained stable.
		/// </summary>
		Conservative,
		/// <summary>Terminate when the simplex is sufficiently small, without checking the change in goal value.
		/// </summary>
		Aggressive
	}
}
