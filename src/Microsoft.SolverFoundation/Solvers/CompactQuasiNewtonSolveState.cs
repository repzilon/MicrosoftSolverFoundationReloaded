namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Indicates the operation about to be performed by the CQN solver.
	/// </summary>
	/// <remarks>All states changed right before the action.</remarks>
	internal enum CompactQuasiNewtonSolveState
	{
		/// <summary>
		/// Before solve has been started.
		/// </summary>
		PreInit,
		/// <summary>
		/// Start. 
		/// </summary>
		Init,
		/// <summary>
		/// Calculating new direction.
		/// </summary>
		DirectionCalculation,
		/// <summary>
		/// Get the right step size in the calculated direction.
		/// </summary>
		LineSearch
	}
}
