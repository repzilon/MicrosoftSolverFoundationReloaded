using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Contains information about the current solution for MILP and quadratic models.
	/// </summary>
	public class LinearReport : Report
	{
		private ILinearSolverSensitivityReport _sensitivity;

		private ILinearSolverInfeasibilityReport _infeasibility;

		private readonly ILinearModel _linearModel;

		private LinearSolutionMapping _lpSolutionMapping;

		/// <summary>Indicates whether sensitivity information is available.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public bool IsSensitivityAvailable
		{
			get
			{
				ValidateSolution();
				return _sensitivity != null;
			}
		}

		/// <summary>Indicates whether infeasibility information is available.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public bool IsInfeasibilityAvailable
		{
			get
			{
				ValidateSolution();
				return _infeasibility != null;
			}
		}

		/// <summary> The ILinearModel associated with this report.
		/// </summary>
		protected ILinearModel LinearModel => _linearModel;

		/// <summary>Nonzero count before presolve.
		/// </summary>
		/// <remarks>
		/// NonzeroCount may not match the number of nonzero constraint terms in the Model. This is because
		/// solvers often convert models into an internal representation by introducing
		/// or removing rows.
		/// </remarks>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public virtual int NonzeroCount
		{
			get
			{
				ValidateSolution();
				return _linearModel.CoefficientCount - _linearModel.RowCount;
			}
		}

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
				return _linearModel.VariableCount;
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
				return _linearModel.RowCount;
			}
		}

		/// <summary>Sensitivity report from the solver. 
		/// </summary>
		/// <remarks>
		/// This property can be overriden by a derived class to provide
		/// sensitivity information.
		/// </remarks>
		protected ILinearSolverSensitivityReport SensitivityReport => _sensitivity;

		/// <summary>Infeasibility report from the solver. 
		/// </summary>
		/// <remarks>
		/// This property can be overriden by a derived class to provide
		/// infeasibility information.
		/// </remarks>
		protected ILinearSolverInfeasibilityReport InfeasibilityReport => _infeasibility;

		/// <summary>Constructor of a report for any Linear (including MILP) or Quadratic model
		/// </summary>
		/// <param name="context">The SolverContext.</param>
		/// <param name="solver">The ISolver that solved the model.</param>
		/// <param name="solution">The Solution.</param>
		/// <param name="solutionMapping">A LinearSolutionMapping instance.</param>
		/// <exception cref="T:System.ArgumentNullException">context, solver and solution must not be null</exception>
		public LinearReport(SolverContext context, ISolver solver, Solution solution, LinearSolutionMapping solutionMapping)
			: base(context, solver, solution, solutionMapping)
		{
			_lpSolutionMapping = solutionMapping;
			_linearModel = solver as ILinearModel;
			if (_linearModel == null)
			{
				throw new ArgumentException(Resources.SolverMustImplementILinearModel, "solver");
			}
			if (solutionMapping != null && !base.IsStochastic)
			{
				try
				{
					_sensitivity = solutionMapping.GetSensitivityReport();
					_infeasibility = solutionMapping.GetInfeasibilityReport();
				}
				catch (NotSupportedException)
				{
				}
			}
		}

		/// <summary>Get a string representation of the report.
		/// </summary>
		/// <param name="verbosity">verbosity options</param>
		/// <param name="format">format provider for the string representation</param>
		/// <returns>A string representation of the report</returns>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		/// <exception cref="T:System.ArgumentNullException">format must not be null</exception>
		public override string ToString(ReportVerbosity verbosity, IFormatProvider format)
		{
			ValidateSolution();
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			base.Context._abortFlag = false;
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
			if (base.IsStochastic)
			{
				GenerateReportStochasticOverview(stringBuilder, format, base.Solution);
			}
			if (SupportsSolutionDetails())
			{
				GenerateReportSolutionDetails(stringBuilder, format, verbosity);
			}
			else
			{
				if (_solverKind == SolverKind.Simplex)
				{
					GenerateReportPartialSolutionDetails(stringBuilder, format);
				}
				if ((_solverKind == SolverKind.Simplex || _solverKind == SolverKind.IPM || _solverKind == SolverKind.PlugIn) && (verbosity & ReportVerbosity.Infeasibility) != 0)
				{
					GenerateReportInfeasibility(stringBuilder);
				}
			}
			base.Context._abortFlag = false;
			string result = stringBuilder.ToString();
			stringBuilder = null;
			return result;
		}

		/// <summary>Gets all shadow prices.
		/// </summary>
		/// <returns>An IEnumerable of pairs, one for each row. The key is a row name, and the is the shadow price for that row.
		/// If sensitivity is not supported by the solver, or GetSensitivity was not specified in the directive, an empty collection is returned.</returns>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public virtual IEnumerable<KeyValuePair<string, Rational>> GetAllShadowPrices()
		{
			ValidateSolution();
			if (IsSensitivityAvailable)
			{
				return _lpSolutionMapping.GetAllShadowPrices(_sensitivity);
			}
			return new KeyValuePair<string, Rational>[0];
		}

		/// <summary>Gets the sensitivity range for all constraints.
		/// A bound can be changed in the sensitivity range without changing the solution vector, i.e. the decision values.
		/// </summary>
		/// <returns>An IEnumerable of pairs, one for each row. The key is a row name, and the is the sensitivity range for that row.
		/// If sensitivity is not supported by the solver, or GetSensitivity was not specified in the directive, an empty collection is returned.</returns>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public virtual IEnumerable<KeyValuePair<string, LinearSolverSensitivityRange>> GetAllConstraintBoundsSensitivity()
		{
			ValidateSolution();
			if (IsSensitivityAvailable)
			{
				return _lpSolutionMapping.GetAllConstraintBoundsSensitivity(_sensitivity);
			}
			return new KeyValuePair<string, LinearSolverSensitivityRange>[0];
		}

		/// <summary>Gets shadow prices for the specified Constraint.
		/// </summary>
		/// <param name="constraint">The constraint for which shadow prices should be returned.</param>
		/// <returns>An IEnumerable of pairs, one for each row. The key is a row name, and the is the shadow price for that row.
		/// If sensitivity is not supported by the solver, or GetSensitivity was not specified in the directive, an empty collection is returned.</returns>
		/// <exception cref="T:System.ArgumentNullException">The constraint was null.</exception>
		/// <exception cref="T:System.ArgumentException">The constraint was not found in model.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public virtual IEnumerable<KeyValuePair<string, Rational>> GetShadowPrices(Constraint constraint)
		{
			ValidateSolution();
			if (constraint == null)
			{
				throw new ArgumentNullException("constraint");
			}
			if (IsSensitivityAvailable)
			{
				return _lpSolutionMapping.GetShadowPrices(_sensitivity, constraint);
			}
			return new KeyValuePair<string, Rational>[0];
		}

		/// <summary>Gets the sensitivity range for a Constraint.
		/// A bound can be changed within the sensitivity range without changing the solution vector, i.e. the decision values.
		/// </summary>
		/// <param name="constraint">The constraint of interest.</param>
		/// <returns>An IEnumerable of pairs, one for each row. The key is a row name, and the is the sensitivity range for that row.
		/// If sensitivity is not supported by the solver, or GetSensitivity was not specified in the directive, an empty collection is returned.</returns>
		/// <exception cref="T:System.ArgumentNullException">The constraint was null.</exception>
		/// <exception cref="T:System.ArgumentException">The constraint was not found in model.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public virtual IEnumerable<KeyValuePair<string, LinearSolverSensitivityRange>> GetConstraintBoundsSensitivity(Constraint constraint)
		{
			ValidateSolution();
			if (constraint == null)
			{
				throw new ArgumentNullException("constraint");
			}
			if (IsSensitivityAvailable)
			{
				return _lpSolutionMapping.GetConstraintBoundsSensitivity(_sensitivity, constraint);
			}
			return new KeyValuePair<string, LinearSolverSensitivityRange>[0];
		}

		/// <summary>Gets the goal coefficient sensitivity range for a Decision with the specified indexes.
		/// </summary>
		/// <param name="decision">The decision of interest.</param>
		/// <param name="indexes">The decision indexes. Use an empty array for a non-indexed decision.</param>
		/// <returns>Goal coefficient range. 
		/// If sensitivity is not supported the result is null.
		/// </returns>
		/// <remarks>
		/// A coefficient can be changed within the range without changing the solution vector, i.e. the decision values.
		/// An indexed Decision has multiple sensitivity ranges, one for each set of indexes. The GetValues method
		/// returns the complete set of indexes that can be passed into this method.
		/// </remarks>
		/// <exception cref="T:System.ArgumentNullException">The decision and its indexes must not be null.</exception>
		/// <exception cref="T:System.ArgumentException">No decision with the specified indexes can be found.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public virtual LinearSolverSensitivityRange? GetGoalCoefficientSensitivity(Decision decision, params object[] indexes)
		{
			ValidateSolution();
			if ((object)decision == null)
			{
				throw new ArgumentNullException("decision");
			}
			if (indexes == null)
			{
				throw new ArgumentNullException("indexes");
			}
			if (IsSensitivityAvailable)
			{
				try
				{
					return _lpSolutionMapping.GetGoalCoefficientSensitivity(_sensitivity, decision, indexes);
				}
				catch (MsfException)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.CannotFindTheDecisionAndIndexesInTheModel0, new object[1] { decision.ToString(indexes) }));
				}
			}
			return null;
		}

		/// <summary>Gets the infeasibility constraint set.
		/// </summary>
		/// <returns>An IEnumerable containing the names of the constraints the infeasibility constraint set.
		/// If infeasibility is not supported in the solver, or GetInfeasibility was not specified in the directive, an empty enumeration is returned.</returns>
		/// <remarks>
		/// The infeasibility set is an irreducible set of constraints that causes the model to be infeasible.
		/// </remarks>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.MsfException">The solution is out of date.</exception>
		public virtual IEnumerable<string> GetInfeasibilitySet()
		{
			ValidateSolution();
			if (IsInfeasibilityAvailable)
			{
				return _lpSolutionMapping.GetInfeasibleSet(_infeasibility);
			}
			return new string[0];
		}

		/// <summary>Adds goals, decision, and sensitivity results. 
		/// </summary>
		protected void GenerateReportSolutionDetails(StringBuilder reportBuilder, IFormatProvider formatProvider, ReportVerbosity verbosity)
		{
			reportBuilder.AppendLine(Resources.ReportHeaderSolutionDetails);
			GenerateReportGoals(reportBuilder, formatProvider);
			if ((verbosity & ReportVerbosity.Decisions) != 0)
			{
				GenerateReportDecisions(reportBuilder, formatProvider);
			}
			if ((verbosity & ReportVerbosity.Sensitivity) != 0)
			{
				GenerateReportSensitivity(reportBuilder, formatProvider);
			}
		}

		/// <summary>Add infeasibility details to the report.
		/// </summary>
		protected void GenerateReportInfeasibility(StringBuilder reportBuilder)
		{
			if (!IsInfeasibilityAvailable)
			{
				return;
			}
			reportBuilder.AppendLine();
			reportBuilder.AppendLine(Resources.ReportLineInfeasibleSet);
			foreach (string item in GetInfeasibilitySet())
			{
				reportBuilder.AppendLine(item);
			}
		}

		/// <summary>Add the stochastic details.
		/// </summary>
		private static void GenerateReportStochasticOverview(StringBuilder report, IFormatProvider formatProvider, Solution solution)
		{
			report.AppendLine();
			report.AppendLine(Resources.StochasticMeasures);
			if (solution.StochasticSolution.SamplingMethod != SamplingMethod.NoSampling)
			{
				report.AppendLine(string.Format(formatProvider, Resources.TypeOfStochasticSolution0, new object[1] { Resources.Sampled }));
				report.AppendLine(string.Format(formatProvider, Resources.SampleMethod0, new object[1] { solution.StochasticSolution.SamplingMethod.ToString() }));
				report.AppendLine(string.Format(formatProvider, Resources.SamplesCount0, new object[1] { solution.StochasticSolution.SampleCount }));
				report.AppendLine(string.Format(formatProvider, Resources.RandomSeed0, new object[1] { solution.StochasticSolution.RandomSeed }));
			}
			else
			{
				report.AppendLine(string.Format(formatProvider, Resources.TypeOfStochasticSolution0, new object[1] { Resources.Complete }));
				report.AppendLine(string.Format(formatProvider, Resources.ScenariosCount0, new object[1] { solution.StochasticSolution.ScenarioCount }));
			}
			report.AppendLine(string.Format(formatProvider, Resources.SolvingMethod0, new object[1] { Resources.DeterministicEquivalent }));
		}

		private void GenerateReportSensitivity(StringBuilder reportBuilder, IFormatProvider formatProvider)
		{
			if (IsSensitivityAvailable)
			{
				reportBuilder.AppendLine();
				reportBuilder.AppendLine(Resources.ReportLineShadowPricing);
				foreach (KeyValuePair<string, Rational> allShadowPrice in _lpSolutionMapping.GetAllShadowPrices(_sensitivity))
				{
					if (base.Context._abortFlag)
					{
						throw new MsfException(Resources.Aborted);
					}
					string key = allShadowPrice.Key;
					Report.PrintMapping(reportBuilder, formatProvider, key, allShadowPrice.Value.GetSignedDouble());
				}
			}
			if (!IsSensitivityAvailable || _solverKind == SolverKind.IPM)
			{
				return;
			}
			reportBuilder.AppendLine();
			reportBuilder.AppendLine(Resources.ReportLineGoalCoefficients);
			foreach (KeyValuePair<Decision, ValueTable<LinearSolverSensitivityRange>> item in _lpSolutionMapping.GetGoalCoefficientsSensitivity(_sensitivity, base.Solution._decisions))
			{
				if (base.Context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				Decision key2 = item.Key;
				ValueTable<LinearSolverSensitivityRange> value = item.Value;
				Report.PrintMapping(reportBuilder, formatProvider, key2, value);
			}
			reportBuilder.AppendLine();
			reportBuilder.AppendLine(Resources.ReportLineConstraintBounds);
			foreach (KeyValuePair<string, LinearSolverSensitivityRange> item2 in _lpSolutionMapping.GetAllConstraintBoundsSensitivity(_sensitivity))
			{
				if (base.Context._abortFlag)
				{
					throw new MsfException(Resources.Aborted);
				}
				string key3 = item2.Key;
				LinearSolverSensitivityRange value2 = item2.Value;
				Report.PrintMapping(reportBuilder, formatProvider, key3, value2);
			}
		}
	}
}
