using System;
using System.Text;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Contains information about the current solution for models solved by the NelderMeadSolver.
	/// </summary>
	public class NelderMeadReport : RowVariableReport
	{
		private readonly NelderMeadSolver _solver;

		/// <summary> The number of iterations that have been performed.
		/// </summary>
		public virtual int IterationCount
		{
			get
			{
				ValidateSolution();
				return _solver.IterationCount;
			}
		}

		/// <summary>Number of times the expanded point was accepted.
		/// </summary>
		public int AcceptedExpansionsCount
		{
			get
			{
				ValidateSolution();
				return _solver.AcceptedExpansionsCount;
			}
		}

		/// <summary>Number of times the expanded point was rejected (using the reflected point).
		/// </summary>
		public int RejectedExpansionsCount
		{
			get
			{
				ValidateSolution();
				return _solver.RejectedExpansionsCount;
			}
		}

		/// <summary>Number of times the contracted point was rejected (regenerating the simplex).
		/// </summary>
		public int RejectedContractionsCount
		{
			get
			{
				ValidateSolution();
				return _solver.RejectedContractionsCount;
			}
		}

		/// <summary>Number of times the contracted point was accepted.
		/// </summary>
		public int AcceptedContractionsCount
		{
			get
			{
				ValidateSolution();
				return _solver.AcceptedContractionsCount;
			}
		}

		/// <summary>Number of times the reflected point was accepted.
		/// </summary>
		public int AcceptedReflectionsCount
		{
			get
			{
				ValidateSolution();
				return _solver.AcceptedReflectionsCount;
			}
		}

		/// <summary> The number of function evaluation calls.
		/// </summary>
		public virtual int EvaluationCallCount
		{
			get
			{
				ValidateSolution();
				return _solver.EvaluationCallCount;
			}
		}

		/// <summary>Number of times a small simplex was encountered.
		/// </summary>
		public int SmallSimplexCount
		{
			get
			{
				ValidateSolution();
				return _solver.SmallSimplexCount;
			}
		}

		/// <summary> The type of result of the NelderMead solver.
		/// </summary>
		public NonlinearResult NelderMeadResult
		{
			get
			{
				ValidateSolution();
				return _solver.Result;
			}
		}

		/// <summary>Creates a new instance.
		/// </summary>
		/// <param name="context">The SolverContext.</param>
		/// <param name="solver">The ISolver that solved the model.</param>
		/// <param name="solution">The Solution.</param>
		/// <param name="solutionMapping">A PluginSolutionMapping instance.</param>
		/// <exception cref="T:System.ArgumentNullException">context, solver and solution must not be null</exception>
		internal NelderMeadReport(SolverContext context, ISolver solver, Solution solution, PluginSolutionMapping solutionMapping)
			: base(context, solver, solution, solutionMapping)
		{
			_solver = solver as NelderMeadSolver;
		}

		/// <summary>Adds the solver details to the string builder.
		/// </summary>
		protected override void GenerateReportSolverDetails(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.SolverSolutionQuality0, new object[1] { NelderMeadResult }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.IterationCount0, new object[1] { IterationCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.EvaluationCallCount0, new object[1] { EvaluationCallCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.AcceptedReflectedCount0, new object[1] { AcceptedReflectionsCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.AcceptedExpandedCount0, new object[1] { AcceptedExpansionsCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.RejectedExpandedCount0, new object[1] { RejectedExpansionsCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.AcceptedContractedCount0, new object[1] { AcceptedContractionsCount }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.RejectedContractedCount0, new object[1] { RejectedContractionsCount }));
		}
	}
}
