namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A generic Report from any solver.
	/// </summary>
	public sealed class GenericReport : RowVariableReport
	{
		/// <summary>
		/// Instantiates a GenericReport
		/// </summary>
		/// <param name="context"></param>
		/// <param name="solver"></param>
		/// <param name="solution"></param>
		/// <param name="solutionMapping"></param>
		internal GenericReport(SolverContext context, ISolver solver, Solution solution, PluginSolutionMapping solutionMapping)
			: base(context, solver, solution, solutionMapping)
		{
		}
	}
}
