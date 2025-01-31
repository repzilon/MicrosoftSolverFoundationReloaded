namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// IRowVariableSolver represents a solver for optimization models with goals.
	/// </summary>
	public interface IRowVariableSolver : ISolver, IRowVariableModel
	{
		/// <summary> 
		/// Solve the model using the given parameters.
		/// </summary>
		INonlinearSolution Solve(ISolverParameters parameters);
	}
}
