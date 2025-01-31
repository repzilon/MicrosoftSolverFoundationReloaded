namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Interface for defining solvers which can provide a Solver Foundation Services report
	/// </summary>
	public interface IReportProvider
	{
		/// <summary>Generate a report.
		/// </summary>
		/// <param name="context">Solver context.</param>
		/// <param name="solution">The solution.</param>
		/// <param name="solutionMapping">The solution mapping.</param>
		/// <returns>A report object.</returns>
		Report GetReport(SolverContext context, Solution solution, SolutionMapping solutionMapping);
	}
}
