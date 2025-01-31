using System;
using System.Text;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Contains information about the current solution for models solved by the CompactQuasiNewtonSolver.
	/// </summary>
	public class CompactQuasiNewtonReport : RowVariableReport
	{
		private readonly CompactQuasiNewtonSolver _cqnSolver;

		/// <summary>Gets the difference between the solution tolerance and the tolerance
		/// requested by the caller.
		/// </summary>
		/// <remarks>
		/// The solver tolerance is set using CompactQuasiNewtonSolverParams.
		/// If a call to Solve() returns CompactQuasiNewtonSolutionQuality.LocalOptimum
		/// then this value will be zero or less.  If a local optimum is found even
		/// though the stopping criterion is not met, the final tolerance is considered
		/// to be zero and the ToleranceDifference will be the negated version of
		/// the requested tolerance.
		/// </remarks>
		public virtual double ToleranceDifference
		{
			get
			{
				ValidateSolution();
				return _cqnSolver.ToleranceDifference;
			}
		}

		/// <summary> The number of iterations that have been performed.
		/// </summary>
		public virtual int IterationCount
		{
			get
			{
				ValidateSolution();
				return _cqnSolver.IterationCount;
			}
		}

		/// <summary> The number of function evaluation calls.
		/// Each call is for both funtion and gradient evaluation.
		/// </summary>
		public virtual long EvaluationCallCount
		{
			get
			{
				ValidateSolution();
				return _cqnSolver.EvaluationCallCount;
			}
		}

		/// <summary> The detailed quality of solution from Compact Quasi Newton solver
		/// </summary>
		public CompactQuasiNewtonSolutionQuality CompactQuasiNewtonSolutionQuality
		{
			get
			{
				ValidateSolution();
				return _cqnSolver.SolutionQuality;
			}
		}

		/// <summary>Constructor of a report for any CompactQuasiNewton model
		/// </summary>
		/// <param name="context">The SolverContext.</param>
		/// <param name="solver">The ISolver that solved the model.</param>
		/// <param name="solution">The Solution.</param>
		/// <param name="solutionMapping">A PluginSolutionMapping instance.</param>
		/// <exception cref="T:System.ArgumentNullException">context, solver and solution must not be null</exception>
		internal CompactQuasiNewtonReport(SolverContext context, ISolver solver, Solution solution, PluginSolutionMapping solutionMapping)
			: base(context, solver, solution, solutionMapping)
		{
			_cqnSolver = solver as CompactQuasiNewtonSolver;
		}

		/// <summary>Adds the solver details to the string builder.
		/// </summary>
		protected override void GenerateReportSolverDetails(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.SolverSolutionQuality0, new object[1] { CompactQuasiNewtonSolutionQuality }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.IterationCount0, new object[1] { IterationCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.EvaluationCallCount0, new object[1] { EvaluationCallCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ToleranceDifference0, new object[1] { ToleranceDifference }));
		}
	}
}
