namespace Microsoft.SolverFoundation.Services
{
	/// <summary>solver result 
	/// </summary>
	public enum SolverQuality
	{
		/// <summary>The result is optimal.
		/// </summary>
		Optimal,
		/// <summary>The model is feasible, but the result found was not proven optimal.
		/// </summary>
		Feasible,
		/// <summary>The model is infeasible.
		/// </summary>
		Infeasible,
		/// <summary>The model is feasible, but there is no optimal solution because the optimal goal value
		/// is infinite.
		/// </summary>
		Unbounded,
		/// <summary>The model was proved to have no optimum solution, but the solver was unable to determine
		/// whether or not it is feasible.
		/// </summary>
		InfeasibleOrUnbounded,
		/// <summary>The solver was unable to find a solution or prove the model infeasible.
		/// This could be because the solver was interrupted, or because the model has no solution but the solver
		/// was unable to prove it.
		/// </summary>
		Unknown,
		/// <summary>The solver found a solution which is locally optimal, but may not be globally optimal.
		/// </summary>
		LocalOptimal,
		/// <summary>The solver found a solution which is locally infeasible, but can not determine if the model is globally infeasible.
		/// </summary>
		LocalInfeasible
	}
}
