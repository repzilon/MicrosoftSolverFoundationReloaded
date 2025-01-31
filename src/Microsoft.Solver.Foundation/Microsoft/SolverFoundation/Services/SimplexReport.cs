using System;
using System.Text;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Contains information about the current solution for models solved by SimplexSolver.
	/// </summary>
	public class SimplexReport : LinearReport
	{
		private ILinearSimplexStatistics _statistics;

		private SimplexSolver _simplexSolver;

		/// <summary> Simplex algorithm solution metrics.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public ILinearSimplexStatistics Statistics
		{
			get
			{
				ValidateSolution();
				return _statistics;
			}
		}

		/// <summary>The kind of initial basis used
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public SimplexBasisKind InitialBasisUsed
		{
			get
			{
				ValidateSolution();
				return _simplexSolver.InitialBasisUsed;
			}
		}

		internal SimplexReport(SolverContext context, ISolver solver, Solution solution, LinearSolutionMapping solutionMapping)
			: base(context, solver, solution, solutionMapping)
		{
			_statistics = solver as ILinearSimplexStatistics;
			if (_statistics == null)
			{
				throw new ArgumentException(Resources.SolverMustImplementILinearSimplexStatistics, "solver");
			}
			_simplexSolver = solver as SimplexSolver;
			if (_simplexSolver == null)
			{
				throw new ArgumentException(Resources.SolverMustBeSimplexSolver, "solver");
			}
		}

		/// <summary>Adds the solver details to the string builder
		/// </summary>
		protected override void GenerateReportSolverDetails(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			AddSimplexAlgorithm(reportBuilder, formatProvider);
			AddSimplexNumericFormat(reportBuilder);
			AddSimplexModelInfo(reportBuilder, formatProvider);
			AddSimplexCostingDetails(reportBuilder, formatProvider);
			AddSimplexInitialBasis(reportBuilder, formatProvider);
			AddSimplexSolveDetails(reportBuilder, formatProvider);
		}

		private void AddSimplexInitialBasis(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineBasis, new object[1] { InitialBasisUsed }));
		}

		private void AddSimplexCostingDetails(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			if (Statistics.UseExact)
			{
				reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLinePricingExact, new object[1] { Statistics.CostingUsedExact }));
			}
			if (Statistics.UseDouble)
			{
				reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLinePricingDouble, new object[1] { Statistics.CostingUsedDouble }));
			}
		}

		private void AddSimplexSolveDetails(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLinePivotCount, new object[1] { Statistics.PivotCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLinePhase1Pivots, new object[2] { Statistics.PivotCountDoublePhaseOne, Statistics.PivotCountExactPhaseOne }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLinePhase2Pivots, new object[2] { Statistics.PivotCountDoublePhaseTwo, Statistics.PivotCountExactPhaseTwo }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineFactorings, new object[2] { Statistics.FactorCountDouble, Statistics.FactorCountExact }));
			double num = ((Statistics.PivotCount > 0) ? (100.0 * (double)Statistics.PivotCountDegenerate / (double)Statistics.PivotCount) : 0.0);
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineDegeneratePivots, new object[2] { Statistics.PivotCountDegenerate, num }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineBranches, new object[1] { Statistics.BranchCount }));
		}

		private void AddSimplexNumericFormat(StringBuilder reportBuilder)
		{
			if (Statistics.UseExact && Statistics.UseDouble)
			{
				reportBuilder.AppendLine(Resources.ReportLineNumericFormatHybrid);
			}
			else if (Statistics.UseDouble)
			{
				reportBuilder.AppendLine(Resources.ReportLineNumericFormatDouble);
			}
			else
			{
				reportBuilder.AppendLine(Resources.ReportLineNumericFormatExact);
			}
		}

		private void AddSimplexAlgorithm(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineAlgorithm, new object[1] { Statistics.AlgorithmUsed }));
		}

		private void AddSimplexModelInfo(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineVariables, new object[3]
			{
				OriginalVariableCount,
				Statistics.InnerIndexCount - Statistics.InnerSlackCount,
				Statistics.InnerSlackCount
			}));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineRows, new object[2] { OriginalRowCount, Statistics.InnerRowCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineNonzeros, new object[1] { NonzeroCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineEliminatedSlackVariables, new object[1] { Statistics.InnerRowCount - Statistics.InnerSlackCount }));
		}
	}
}
