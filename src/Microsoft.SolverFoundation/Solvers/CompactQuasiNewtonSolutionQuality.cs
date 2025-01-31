namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>Represents the solution quality.
	/// </summary>
	public enum CompactQuasiNewtonSolutionQuality
	{
		/// <summary>An optimal local minimum was found.
		/// </summary>
		LocalOptima,
		/// <summary>A failure likely due to error in information provided by the caller.
		/// </summary>
		/// <remarks>
		/// The failure relates to one of the following: a step was taken in 
		/// a non-descent direction; the steplength was too short, the "y" and "s"
		/// vectors are orthogonal.  These conditions usually happen when the user-provided
		/// objective or gradient are incorrect or ill-conditioned, or when the termination criteria 
		/// is too strict.
		/// </remarks>
		UserCalculationError,
		/// <summary>The difference between sequential gradients is zero.
		/// </summary>
		/// <remarks>
		/// This condition should occur only with linear objective functions.
		/// </remarks>
		LinearObjective,
		/// <summary>The solution exceeded the range of System.Double.
		/// This condition usually means that the function is unbounded.
		/// </summary>
		Unbounded,
		/// <summary>The maximum number of iterations was exceeded.
		/// </summary>
		MaxIterationExceeded,
		/// <summary>The solver was aborted by the caller.
		/// </summary>
		Interrupted
	}
}
