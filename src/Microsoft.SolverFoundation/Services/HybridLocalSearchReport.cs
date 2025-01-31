using System;
using System.Text;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Contains information about the current solution for models solved by the CompactQuasiNewtonSolver.
	/// </summary>
	public class HybridLocalSearchReport : RowVariableReport
	{
		private readonly HybridLocalSearchSolver _localSearchSolver;

		/// <summary>Step count of the search when it is running
		/// </summary>
		public long StepCount
		{
			get
			{
				ValidateSolution();
				return _localSearchSolver.Step;
			}
		}

		/// <summary>Constraints violation. Zero stands for no violation (feasible solution), 
		/// and the smaller Violation is the more solution tends toward feasibility.
		/// </summary>
		public double Violation
		{
			get
			{
				ValidateSolution();
				return _localSearchSolver.Violation;
			}
		}

		/// <summary>Constructor of a report for any  HybridLocalSearch model
		/// </summary>
		/// <param name="context">The SolverContext.</param>
		/// <param name="solver">The ISolver that solved the model.</param>
		/// <param name="solution">The Solution.</param>
		/// <param name="solutionMapping">A PluginSolutionMapping instance.</param>
		/// <exception cref="T:System.ArgumentNullException">context, solver and solution must not be null</exception>
		internal HybridLocalSearchReport(SolverContext context, ISolver solver, Solution solution, PluginSolutionMapping solutionMapping)
			: base(context, solver, solution, solutionMapping)
		{
			_localSearchSolver = solver as HybridLocalSearchSolver;
		}

		/// <summary>Adds the solver details to the string builder.
		/// </summary>
		protected override void GenerateReportSolverDetails(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.StepCount0, new object[1] { StepCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.Violation0, new object[1] { Violation }));
		}

		/// <summary>
		/// Determine if solution details can be returned.
		/// </summary>
		/// <returns>true if can, false otherwize</returns>
		protected override bool SupportsSolutionDetails()
		{
			if (_localSearchSolver.Result == NonlinearResult.Interrupted)
			{
				return true;
			}
			return base.SupportsSolutionDetails();
		}
	}
}
