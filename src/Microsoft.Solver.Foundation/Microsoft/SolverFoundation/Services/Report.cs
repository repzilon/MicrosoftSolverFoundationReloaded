using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Contains information about the current solution.
	/// </summary>
	/// <remarks>
	/// A Report instance is obtained from a Solution using the GetReport method. Depending on the solver
	/// that was used to solve the model, GetReport may return a subclass of Report with solver-specific information.
	/// </remarks>
	public abstract class Report
	{
		internal enum SolverKind
		{
			None,
			Simplex,
			IPM,
			CSP,
			PlugIn
		}

		private readonly SolverContext _context;

		private readonly Solution _solution;

		private readonly ISolver _solver;

		private readonly SolutionMapping _solutionMapping;

		/// <summary>This is needed for internal access from Solution for setter instead of using the ctr
		/// </summary>
		internal IFormatProvider _defaultFormatProvider;

		/// <summary>This is needed for internal access from Solution for setter instead of using the ctr
		/// </summary>
		internal ReportVerbosity _defaultVerbosity;

		private static readonly object[] _emptyArray = new object[0];

		internal readonly SolverKind _solverKind;

		private readonly SolverCapability _solverCapability;

		/// <summary>The SolverContext
		/// </summary>
		protected SolverContext Context => _context;

		/// <summary>The solution this report is built for
		/// </summary>
		protected Solution Solution => _solution;

		/// <summary>The solver that solved the model.
		/// </summary>
		protected ISolver Solver => _solver;

		/// <summary>An object that maps between model and solver level terms.
		/// </summary>
		protected SolutionMapping SolutionMapping => _solutionMapping;

		/// <summary>The default format provider for building the string representation of the report
		/// </summary>
		protected IFormatProvider DefaultFormatProvider => _defaultFormatProvider;

		/// <summary>The default verbosity for building the string representation of the report
		/// </summary>
		protected ReportVerbosity DefaultVerbosity => _defaultVerbosity;

		/// <summary>Solver result quality
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public SolverQuality SolutionQuality
		{
			get
			{
				ValidateSolution();
				return Solution._quality;
			}
		}

		/// <summary>Time spent solving the model (in milliseconds).
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public long SolveTime
		{
			get
			{
				ValidateSolution();
				return Solution._solveTimeMilliseconds;
			}
		}

		/// <summary>Total time spent (in milliseconds)
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public long TotalTime
		{
			get
			{
				ValidateSolution();
				return Solution._totalTimeMilliseconds;
			}
		}

		/// <summary>The name of the model.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public string ModelName
		{
			get
			{
				ValidateSolution();
				return Solution._model.Name;
			}
		}

		/// <summary>Gets the directive passed to the solver that found the solution.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public Directive SolutionDirective => Solution.Directive;

		/// <summary>All directives that were used.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public IEnumerable<Directive> Directives
		{
			get
			{
				ValidateSolution();
				return Solution._directives;
			}
		}

		/// <summary>The System.Type of the solver that found the solution.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public Type SolverType
		{
			get
			{
				ValidateSolution();
				if (_solverKind != 0)
				{
					return Solver.GetType();
				}
				return null;
			}
		}

		/// <summary>Capability of the solver that found this solution
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public SolverCapability SolverCapability
		{
			get
			{
				ValidateSolution();
				return _solverCapability;
			}
		}

		/// <summary>
		/// Determine if the model is stochastic
		/// </summary>
		protected bool IsStochastic => Solution.StochasticSolution != null;

		/// <summary>Creates a new instance.
		/// </summary>
		/// <param name="context">The SolverContext.</param>
		/// <param name="solver">The solver instance.</param>
		/// <param name="solution">The solution.</param>
		/// <param name="solutionMapping">A SolutionMapping object.</param>
		/// <exception cref="T:System.ArgumentNullException">The context, solver and solution must not be null.</exception>
		protected Report(SolverContext context, ISolver solver, Solution solution, SolutionMapping solutionMapping)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			if (solver == null)
			{
				throw new ArgumentNullException("solver");
			}
			if (solution == null)
			{
				throw new ArgumentNullException("solution");
			}
			_context = context;
			_solver = solver;
			_solution = solution;
			_solutionMapping = solutionMapping;
			GetSolverKindAndCapability(Solver, out _solverKind, out _solverCapability);
		}

		/// <summary>Writes the string representation of the report to the destination.
		/// </summary>
		/// <param name="destination">A TextWriter where the output is directed.</param>
		/// <exception cref="T:System.ArgumentNullException">The destination must not be null.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public void WriteTo(TextWriter destination)
		{
			if (destination == null)
			{
				throw new ArgumentNullException("destination");
			}
			destination.Write(ToString());
		}

		/// <summary>Get a string representation of the report.
		/// </summary>
		/// <remarks>Uses the verbosity supplied on GetReport(), or the default Verbosity (All) if none was supplied. 
		/// Uses the formatProvider given to GetReport(), or the default IFormatProvider (CultureInfo.CurrentCulture) if none was supplied.</remarks>
		/// <returns>A string representation of the report</returns>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public override string ToString()
		{
			return ToString(DefaultVerbosity, DefaultFormatProvider);
		}

		/// <summary>Get a string representation of the report
		/// </summary>
		/// <remarks>Uses the verbosity supplied on GetReport(), or the default Verbosity (All) if none was supplied</remarks>
		/// <param name="format">format provider for the string representation</param>
		/// <returns>A string representation of the report</returns>
		/// <exception cref="T:System.ArgumentNullException">The format must not be null</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public string ToString(IFormatProvider format)
		{
			return ToString(DefaultVerbosity, format);
		}

		/// <summary>Get a string representation of the report
		/// </summary>
		/// <remarks>Uses the formatProvider given to GetReport(), or the default IFormatProvider (CultureInfo.CurrentCulture) if none was supplied</remarks>
		/// <param name="verbosity">verbosity options</param>
		/// <returns>A string representation of the report</returns>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public string ToString(ReportVerbosity verbosity)
		{
			return ToString(verbosity, DefaultFormatProvider);
		}

		/// <summary>Get a string representation of the report
		/// </summary>
		/// <param name="verbosity">verbosity options</param>
		/// <param name="format">format provider for the string representation</param>
		/// <returns>A string representation of the report</returns>
		/// <exception cref="T:System.ArgumentNullException">The format must not be null</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public virtual string ToString(ReportVerbosity verbosity, IFormatProvider format)
		{
			ValidateSolution();
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			Context._abortFlag = false;
			StringBuilder stringBuilder = new StringBuilder();
			GenerateReportOverview(stringBuilder, format);
			if ((verbosity & ReportVerbosity.Directives) != 0)
			{
				GenerateReportDirectives(stringBuilder);
			}
			if ((verbosity & ReportVerbosity.SolverDetails) != 0)
			{
				GenerateReportSolverDetails(stringBuilder, format);
			}
			if (SupportsSolutionDetails())
			{
				stringBuilder.AppendLine(Resources.ReportHeaderSolutionDetails);
				GenerateReportGoals(stringBuilder, format);
				if ((verbosity & ReportVerbosity.Decisions) != 0)
				{
					GenerateReportDecisions(stringBuilder, format);
				}
			}
			Context._abortFlag = false;
			return stringBuilder.ToString();
		}

		/// <summary>
		/// Determine if solution details can be returned.
		/// </summary>
		protected virtual bool SupportsSolutionDetails()
		{
			if (Solution._quality != 0 && Solution._quality != SolverQuality.Feasible && Solution._quality != SolverQuality.LocalOptimal)
			{
				return Solution._quality == SolverQuality.LocalInfeasible;
			}
			return true;
		}

		/// <summary>Make sure the solution is a valid one. 
		/// </summary>
		/// <remarks>Every pulic API should call this verification method</remarks>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		protected void ValidateSolution()
		{
			Solution.ValidateSolution();
		}

		/// <summary>Adds the solver details to the string builder
		/// </summary>
		protected virtual void GenerateReportSolverDetails(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
		}

		/// <summary>Add the overview to the report
		/// </summary>
		protected void GenerateReportOverview(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			DateTime now = DateTime.Now;
			reportBuilder.AppendLine(Resources.ReportHeaderReportOverview);
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineDatetime0, new object[1] { now }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineVersion0, new object[1] { License.VersionToString() }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineModelName, new object[1] { ModelName }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportCapability, new object[1] { SolverCapability }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineSolveTimeMs, new object[1] { SolveTime }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineTotalTimeMs, new object[1] { TotalTime }));
			reportBuilder.AppendLine(string.Format(formatProvider, Resources.ReportLineSolveCompletionStatus, new object[1] { SolutionQuality }));
			if (_solverKind != 0)
			{
				reportBuilder.AppendLine(string.Format(CultureInfo.InstalledUICulture, Resources.ReportLineSolverSelected, new object[1] { SolverType.FullName }));
			}
			else
			{
				reportBuilder.AppendLine(Resources.NoSolverFoundASolutionWithinTheTimeLimit);
			}
		}

		/// <summary>Add the decision results to the report
		/// </summary>
		protected void GenerateReportDecisions(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine();
			reportBuilder.AppendLine(Resources.ReportLineDecisions);
			foreach (Decision decision in Solution._decisions)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				PrintMapping(reportBuilder, formatProvider, decision, decision._valueTable);
			}
			if (!IsStochastic)
			{
				return;
			}
			reportBuilder.AppendLine();
			reportBuilder.AppendLine(Resources.SecondStageDecisionsValuesAverageMinimalMaximal);
			foreach (RecourseDecision recourseDecision in Solution._recourseDecisions)
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				PrintMapping(reportBuilder, formatProvider, recourseDecision, recourseDecision._secondStageResults);
			}
		}

		/// <summary>Add the goals results to the report's string representation
		/// </summary>
		protected void GenerateReportGoals(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			reportBuilder.AppendLine(Resources.ReportLineGoals);
			int maxGoalCount = GetMaxGoalCount();
			int num = 0;
			foreach (Goal item in Solution._goals.OrderBy((Goal goal) => goal.Order))
			{
				if (_context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				if (maxGoalCount != num)
				{
					num++;
					if (item.Enabled)
					{
						reportBuilder.AppendLine(string.Format(formatProvider, "{0}: {1}", new object[2]
						{
							item.Name,
							item.ToDouble()
						}));
					}
					continue;
				}
				break;
			}
		}

		/// <summary>Returns the MaximumGoalCount of the winning task if exists
		/// </summary>
		/// <remarks>The default number for MaximumGoalCount is -1 which means no limit</remarks>
		/// <returns></returns>
		private int GetMaxGoalCount()
		{
			int result = -1;
			if (Context.WinningTask != null && Context.WinningTask.Directive != null)
			{
				result = Context.WinningTask.Directive.MaximumGoalCount;
			}
			return result;
		}

		internal static void PrintMapping(StringBuilder report, IFormatProvider formatProvider, string key, double value)
		{
			report.AppendLine(string.Format(formatProvider, "{0}: {1}", new object[2] { key, value }));
		}

		internal static void PrintMapping(StringBuilder report, IFormatProvider formatProvider, string key, LinearSolverSensitivityRange value)
		{
			report.AppendLine(string.Format(formatProvider, "{0}: {1} [{2} {3}]", key, (double)value.Current, (double)value.Lower, (double)value.Upper));
		}

		internal static void PrintMapping(StringBuilder report, IFormatProvider formatProvider, Decision decision, ValueTable<double> values)
		{
			if (values.IndexCount == 0)
			{
				if (values.TryGetValue(out var value))
				{
					string text = decision.EnumeratedDomain.FormatValue(formatProvider, value);
					report.AppendLine(string.Format(formatProvider, "{0}: {1}", new object[2] { decision._name, text }));
				}
				return;
			}
			string listSeperator = GetListSeperator(formatProvider);
			listSeperator += " ";
			foreach (object[] key in values.Keys)
			{
				string text2 = string.Join(listSeperator, key.Select((object o) => o.ToString()).ToArray());
				if (values.TryGetValue(out var value2, key))
				{
					string text3 = decision.EnumeratedDomain.FormatValue(formatProvider, value2);
					report.AppendLine(string.Format(formatProvider, "{0}({1}): {2}", new object[3] { decision._name, text2, text3 }));
				}
			}
		}

		/// <summary>
		/// prints the second stage results
		/// </summary>
		/// <param name="report">The report</param>
		/// <param name="formatProvider">The format provider</param>
		/// <param name="decision">Currunt RecourseDecision</param>
		/// <param name="values">ValueTable which holds array of doubles, 
		/// first is avaerage, second is minimal value and the third is maximal</param>
		private static void PrintMapping(StringBuilder report, IFormatProvider formatProvider, RecourseDecision decision, ValueTable<double[]> values)
		{
			if (decision.EnumeratedDomain.ValueClass != 0 || values == null)
			{
				return;
			}
			string listSeperator = GetListSeperator(formatProvider);
			double[] value;
			if (values.IndexCount == 0)
			{
				values.TryGetValue(out value);
				report.AppendLine(string.Format(formatProvider, "{0}: {1} [{2}{3} {4}]", decision._name, value[0], value[1], listSeperator, value[2]));
				return;
			}
			listSeperator += " ";
			foreach (object[] key in values.Keys)
			{
				string text = string.Join(listSeperator, key.Select((object o) => o.ToString()).ToArray());
				values.TryGetValue(out value, key);
				report.AppendLine(string.Format(formatProvider, "{0}({1}): {2} [{3}{4} {5}]", decision._name, text, value[0], value[1], listSeperator, value[2]));
			}
		}

		private static string GetListSeperator(IFormatProvider formatProvider)
		{
			if (!(formatProvider is CultureInfo cultureInfo))
			{
				return CultureInfo.CurrentCulture.TextInfo.ListSeparator;
			}
			return cultureInfo.TextInfo.ListSeparator;
		}

		internal static void PrintMapping(StringBuilder report, IFormatProvider formatProvider, Decision decision, ValueTable<LinearSolverSensitivityRange> values)
		{
			if (values.IndexCount == 0)
			{
				values.TryGetValue(out var value);
				report.AppendLine(string.Format(formatProvider, "{0}: {1} [{2} {3}]", decision._name, (double)value.Current, (double)value.Lower, (double)value.Upper));
				return;
			}
			string listSeperator = GetListSeperator(formatProvider);
			listSeperator += " ";
			foreach (object[] key in values.Keys)
			{
				string text = string.Join(listSeperator, key.Select((object o) => o.ToString()).ToArray());
				values.TryGetValue(out var value2, key);
				report.AppendLine(string.Format(formatProvider, "{0}({1}): {2} [{3} {4}]", decision._name, text, (double)value2.Current, (double)value2.Lower, (double)value2.Upper));
			}
		}

		private static void GetSolverKindAndCapability(ISolver solver, out SolverKind solverKind, out SolverCapability solverCapability)
		{
			ILinearModel model = solver as ILinearModel;
			if (solver == null)
			{
				solverKind = SolverKind.None;
				solverCapability = SolverCapability.Undefined;
				return;
			}
			if (solver is SimplexSolver)
			{
				solverKind = SolverKind.Simplex;
				solverCapability = GetRequiredCapabilityFromLinearModel(model);
				return;
			}
			if (solver is InteriorPointSolver)
			{
				solverKind = SolverKind.IPM;
				solverCapability = GetRequiredCapabilityFromLinearModel(model);
				return;
			}
			if (solver is ConstraintSystem)
			{
				solverKind = SolverKind.CSP;
				solverCapability = SolverCapability.CP;
				return;
			}
			if (solver is ILinearSolver)
			{
				solverKind = SolverKind.PlugIn;
				solverCapability = GetRequiredCapabilityFromLinearModel(model);
				return;
			}
			if (solver is ITermSolver)
			{
				solverKind = SolverKind.PlugIn;
				solverCapability = SolverCapability.NLP;
				return;
			}
			if (solver is INonlinearSolver)
			{
				solverKind = SolverKind.PlugIn;
				solverCapability = SolverCapability.NLP;
				return;
			}
			throw new NotSupportedException(Resources.ReportingIsOnlySupportedForSimplexIPMAndCSPModels);
		}

		private static SolverCapability GetRequiredCapabilityFromLinearModel(ILinearModel model)
		{
			if (model != null)
			{
				if (model.IntegerIndexCount > 0)
				{
					if (model.IsQuadraticModel)
					{
						return SolverCapability.MIQP;
					}
					return SolverCapability.MILP;
				}
				if (model.IsQuadraticModel)
				{
					return SolverCapability.QP;
				}
				return SolverCapability.LP;
			}
			return SolverCapability.Undefined;
		}

		/// <summary>Adds goal results to the report's string representation. Works for suboptimal solutions as well 
		/// </summary>
		protected void GenerateReportPartialSolutionDetails(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			if (SolutionMapping == null)
			{
				return;
			}
			bool flag = false;
			int num = 0;
			int maxGoalCount = GetMaxGoalCount();
			int num2 = 0;
			foreach (Goal item in Solution._goals.OrderBy((Goal goal) => goal.Order))
			{
				if (Context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				if (maxGoalCount == num2)
				{
					break;
				}
				num2++;
				if (!item.Enabled)
				{
					continue;
				}
				double num3 = (double)SolutionMapping.GetValue(item, _emptyArray);
				if (SolutionMapping.IsGoalOptimal(num))
				{
					if (!flag)
					{
						reportBuilder.AppendLine(Resources.ReportHeaderSolutionDetails);
						reportBuilder.AppendLine(Resources.ReportLineGoals);
						flag = true;
					}
					reportBuilder.AppendLine(string.Format(formatProvider, "{0}: {1}", new object[2] { item.Name, num3 }));
				}
				num++;
			}
		}

		/// <summary>Adds a section about all directives being used to the report string representation
		/// </summary>
		protected void GenerateReportDirectives(StringBuilder reportBuilder)
		{
			reportBuilder.AppendLine(Resources.ReportLineDirectives);
			foreach (Directive directive in Directives)
			{
				reportBuilder.AppendLine(directive.ToString());
			}
		}
	}
}
