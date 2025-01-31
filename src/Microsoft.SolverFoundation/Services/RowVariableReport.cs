namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Contains information about the current solution for Linear/Nonlinear/Term base  models.
	/// </summary>
	public abstract class RowVariableReport : Report
	{
		private readonly IRowVariableModel _rowVariableModel;

		/// <summary>Variable count before presolve, as represented by the solver.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		/// <remarks>
		/// OriginalVariableCount may not match the decision count of the Model. This is because
		/// solvers often convert models into an internal representation by introducing
		/// or removing variables.
		/// </remarks>
		public virtual int OriginalVariableCount
		{
			get
			{
				ValidateSolution();
				return _rowVariableModel.VariableCount;
			}
		}

		/// <summary>Row count before presolve.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		/// <remarks>
		/// OriginalRowCount may not match the constraint count of the Model. This is because
		/// solvers often convert models into an internal representation by introducing
		/// or removing rows.
		/// </remarks>
		public virtual int OriginalRowCount
		{
			get
			{
				ValidateSolution();
				return _rowVariableModel.RowCount;
			}
		}

		/// <summary>Constructor of a report for any Linear/Nonlinear/Term base model
		/// </summary>
		/// <param name="context">The SolverContext.</param>
		/// <param name="solver">The ISolver that solved the model.</param>
		/// <param name="solution">The Solution.</param>
		/// <param name="solutionMapping">A PluginSolutionMapping instance.</param>
		/// <exception cref="T:System.ArgumentNullException">context, solver and solution must not be null</exception>
		protected RowVariableReport(SolverContext context, ISolver solver, Solution solution, PluginSolutionMapping solutionMapping)
			: base(context, solver, solution, solutionMapping)
		{
			_rowVariableModel = solver as IRowVariableModel;
		}
	}
}
