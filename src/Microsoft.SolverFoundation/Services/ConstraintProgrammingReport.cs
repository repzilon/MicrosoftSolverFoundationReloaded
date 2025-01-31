using System;
using System.Text;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Constraint solver report.
	/// </summary>
	public sealed class ConstraintProgrammingReport : Report
	{
		private ConstraintSystem _cspSolver;

		/// <summary>The CSP search algorithm.
		/// </summary>
		private ConstraintSolverParams.CspSearchAlgorithm Algorithm
		{
			get
			{
				ValidateSolution();
				return _cspSolver.Parameters.Algorithm;
			}
		}

		/// <summary>Move selection heuristic for local search.
		/// </summary>
		private ConstraintSolverParams.LocalSearchMove MoveSelection
		{
			get
			{
				ValidateSolution();
				return _cspSolver.Parameters.MoveSelection;
			}
		}

		/// <summary>Value ordering heuristic for tree search.
		/// </summary>
		private ConstraintSolverParams.TreeSearchValueOrdering ValueSelection
		{
			get
			{
				ValidateSolution();
				return _cspSolver.Parameters.ValueSelection;
			}
		}

		/// <summary>Variable ordering heuristic for tree search.
		/// </summary>
		private ConstraintSolverParams.TreeSearchVariableOrdering VariableSelection
		{
			get
			{
				ValidateSolution();
				return _cspSolver.Parameters.VariableSelection;
			}
		}

		/// <summary>Number of backtracks during solve for tree search.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public int BacktrackCount
		{
			get
			{
				ValidateSolution();
				return _cspSolver.BacktrackCount;
			}
		}

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="context">The SolverContext.</param>
		/// <param name="solver">The solver object.</param>
		/// <param name="solution">The solution.</param>
		/// <param name="solutionMapping">The solution mapping.</param>
		internal ConstraintProgrammingReport(SolverContext context, ISolver solver, Solution solution, CspSolutionMapping solutionMapping)
			: base(context, solver, solution, solutionMapping)
		{
			_cspSolver = solver as ConstraintSystem;
			if (_cspSolver == null)
			{
				throw new ArgumentException(Resources.SolverMustBeConstraintSystem);
			}
		}

		/// <summary>Adds the solver details to the string builder.
		/// </summary>
		protected override void GenerateReportSolverDetails(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineAlgorithm, new object[1] { Algorithm }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineVariableSelection, new object[1] { VariableSelection }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineValueSelection, new object[1] { ValueSelection }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineMoveSelection, new object[1] { MoveSelection }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineBacktrackCount, new object[1] { BacktrackCount }));
		}
	}
}
