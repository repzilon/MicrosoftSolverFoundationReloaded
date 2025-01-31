namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Interface for defining a linear programming solver
	/// </summary>
	public interface ILinearSolver : ISolver, ILinearModel
	{
		/// <summary>
		/// indicates the result of the solve attempt
		/// </summary>
		LinearResult Result { get; }

		/// <summary> 
		/// Solve the model using the given parameter instance
		/// </summary>
		ILinearSolution Solve(ISolverParameters parameters);

		/// <summary> Get sensitivity report  
		/// </summary>
		/// <param name="reportType">simplex report type</param>
		/// <returns>a linear solver report</returns>
		ILinearSolverReport GetReport(LinearSolverReportType reportType);
	}
}
