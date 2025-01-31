using System;
using System.Text;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Contains information about the current solution for models solved by the InteriorPointSolver.
	/// </summary>
	public class InteriorPointReport : LinearReport
	{
		private IInteriorPointStatistics _statistics;

		/// <summary> Interior point algorithm solution metrics.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public IInteriorPointStatistics Statistics
		{
			get
			{
				ValidateSolution();
				return _statistics;
			}
		}

		internal InteriorPointReport(SolverContext context, ISolver solver, Solution solution, LinearSolutionMapping solutionMapping)
			: base(context, solver, solution, solutionMapping)
		{
			_statistics = solver as IInteriorPointStatistics;
			if (_statistics == null)
			{
				throw new ArgumentException(Resources.SolverMustImplementIInteriorPointStatistics, "solver");
			}
		}

		/// <summary>
		/// Determine if solution details can be returned.
		/// </summary>
		protected override bool SupportsSolutionDetails()
		{
			bool result = _statistics is InteriorPointSolver interiorPointSolver && interiorPointSolver._lpResult == LinearResult.Interrupted;
			if (base.Solution._quality != 0 && base.Solution._quality != SolverQuality.Feasible)
			{
				return result;
			}
			return true;
		}

		/// <summary>Adds the solver details to the string builder.
		/// </summary>
		protected override void GenerateReportSolverDetails(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			AddAlgorithm(reportBuilder, formatProvider);
			AddModelInfo(reportBuilder, formatProvider);
		}

		private void AddAlgorithm(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineAlgorithm, new object[1] { Statistics.Algorithm }));
		}

		private void AddModelInfo(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineVariables01, new object[2] { OriginalVariableCount, Statistics.VarCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineRows, new object[2] { OriginalRowCount, Statistics.RowCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineNonzeros, new object[1] { NonzeroCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineIterations, new object[1] { Statistics.IterationCount }));
		}
	}
}
