namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// The result of solving a linear model.
	/// Note that if the LinearSolutionQuality is not Exact, the LinearResult may not
	/// be definitive (ie, correct).
	/// </summary>
	public enum LinearResult
	{
		/// <summary> incomplete: model was found to be invalid
		/// </summary>
		Invalid,
		/// <summary> incomplete: the solver is interrupted 
		/// </summary>
		Interrupted,
		/// <summary> complete: optimal value is found 
		/// </summary>
		Optimal,
		/// <summary> complete: model is feasible but may not be optimal
		/// </summary>
		Feasible,
		/// <summary> complete: the primal form is unbounded 
		/// </summary>
		UnboundedPrimal,
		/// <summary> complete: the dual form is unbounded 
		/// </summary>
		UnboundedDual,
		/// <summary> complete: the primal form is infeasible 
		/// </summary>
		InfeasiblePrimal,
		/// <summary> complete: the dual form is infeasible
		/// </summary>
		InfeasibleOrUnbounded
	}
}
