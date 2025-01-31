namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// The result of solving a nonlinear model.
	/// </summary>
	public enum NonlinearResult
	{
		/// <summary> incomplete: solver cannot finish solving. 
		/// This can be caused by some inconsistent functions, max iteration exceeded or other causes
		/// </summary>
		Invalid,
		/// <summary> incomplete: the solver is interrupted 
		/// </summary>
		Interrupted,
		/// <summary> complete: local optimal value is found 
		/// </summary>
		LocalOptimal,
		/// <summary>complete: global optimal value is found
		/// </summary>
		Optimal,
		/// <summary> complete: model is feasible but may not be optimal
		/// </summary>
		Feasible,
		/// <summary> complete: the model form is unbounded 
		/// </summary>
		Unbounded,
		/// <summary> complete: the model is infeasible 
		/// </summary>
		Infeasible,
		/// <summary> complete: the model is either infeasible or unbounded
		/// </summary>
		InfeasibleOrUnbounded,
		/// <summary> complete: local infeasible value is found, but solver can not determine if the model is globally infeasible.
		/// </summary>
		LocalInfeasible
	}
}
