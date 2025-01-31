using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> A Solution represents the result of solving a model.
	/// </summary>
	public sealed class Solution
	{
		internal SolverContext _context;

		internal long _dataBindTimeMilliseconds;

		internal IEnumerable<Decision> _decisions;

		internal Directive[] _directives;

		internal IEnumerable<Goal> _goals;

		internal long _hydrateTimeMilliseconds;

		internal Model _model;

		internal SolverQuality _quality;

		internal IEnumerable<RecourseDecision> _recourseDecisions;

		internal long _solveTimeMilliseconds;

		private StochasticSolution _stochasticSolutionDetails;

		internal long _totalTimeMilliseconds;

		internal Directive _winningDirective;

		internal bool _validSolution;

		/// <summary>
		/// The quality of this solution (optimal, feasible, infeasible, or unknown).
		/// </summary>
		public SolverQuality Quality
		{
			get
			{
				ValidateSolution();
				return _quality;
			}
			internal set
			{
				_quality = value;
			}
		}

		/// <summary>
		/// All the goals of the model with their solution values.
		/// </summary>
		public IEnumerable<Goal> Goals
		{
			get
			{
				ValidateSolution();
				return _goals;
			}
		}

		/// <summary>
		/// All the decisions of the model, with their values for this solution.
		/// </summary>
		public IEnumerable<Decision> Decisions
		{
			get
			{
				ValidateSolution();
				return _decisions;
			}
		}

		/// <summary>
		/// Gets the directive passed to the solver that found this solution.
		/// </summary>
		public Directive Directive
		{
			get
			{
				ValidateSolution();
				return _winningDirective;
			}
		}

		/// <summary>
		/// Encapsulates details about the stochastic solution
		/// </summary>
		public StochasticSolution StochasticSolution
		{
			get
			{
				ValidateSolution();
				return _stochasticSolutionDetails;
			}
		}

		internal Solution(SolverContext context, SolverQuality quality)
		{
			_model = context.CurrentModel;
			if (_model != null)
			{
				_decisions = _model._decisions;
				_recourseDecisions = _model.RecourseDecisions;
				_goals = _model._goals;
				if (_model.IsStochastic)
				{
					_stochasticSolutionDetails = new StochasticSolution(context.ScenarioGenerator);
				}
			}
			_quality = quality;
			_context = context;
			_validSolution = true;
		}

		/// <summary>
		/// Get the next solution.
		/// </summary>
		public void GetNext()
		{
			ValidateSolution();
			if (_context.CurrentTimeLimit >= 0)
			{
				Stopwatch getNextTimer = new Stopwatch();
				Func<bool> newTimer = () => getNextTimer.ElapsedMilliseconds >= _context.CurrentTimeLimit;
				getNextTimer.Start();
				_quality = _context.FinalSolution.SolutionMapping.GetNext(_model, newTimer);
				getNextTimer.Stop();
			}
			else
			{
				_quality = _context.FinalSolution.SolutionMapping.GetNext(_model, null);
			}
		}

		/// <summary>Gets a report on the solution to the current model.
		/// </summary>
		/// <remarks>Uses ReportVerbosity.All and CultureInfo.CurrentCulture as the default Verbosity and IFormatProvider</remarks>
		public Report GetReport()
		{
			return GetReport(ReportVerbosity.All, CultureInfo.CurrentCulture);
		}

		/// <summary>Gets a report on the solution to the current model.
		/// </summary>
		/// <remarks>Uses CultureInfo.CurrentCulture as the default IFormatProvider</remarks>
		/// <param name="verbosity">The default verbosity to be used in string representation</param>
		/// <returns></returns>
		public Report GetReport(ReportVerbosity verbosity)
		{
			return GetReport(verbosity, CultureInfo.CurrentCulture);
		}

		/// <summary>Gets a report on the solution to the current model.
		/// </summary>
		/// <param name="verbosity">The default verbosity to be used in string representation</param>
		/// <param name="format">The default format to be used in string representation</param>
		/// <returns></returns>
		public Report GetReport(ReportVerbosity verbosity, IFormatProvider format)
		{
			ValidateSolution();
			_context._abortFlag = false;
			Report report;
			try
			{
				if (_context.FinalSolution.Solver is IReportProvider reportProvider)
				{
					report = reportProvider.GetReport(_context, this, _context.FinalSolution.SolutionMapping);
				}
				else if (_context.FinalSolution.Solver is ILinearSolver)
				{
					LinearSolutionMapping solutionMapping = _context.FinalSolution.SolutionMapping as LinearSolutionMapping;
					report = new LinearReport(_context, _context.FinalSolution.Solver, this, solutionMapping);
				}
				else
				{
					PluginSolutionMapping solutionMapping2 = _context.FinalSolution.SolutionMapping as PluginSolutionMapping;
					report = new GenericReport(_context, _context.FinalSolution.Solver, this, solutionMapping2);
				}
			}
			finally
			{
				_context._abortFlag = false;
			}
			report._defaultFormatProvider = format;
			report._defaultVerbosity = verbosity;
			return report;
		}

		internal void ValidateSolution()
		{
			if (!_validSolution)
			{
				throw new MsfException(Resources.SolutionIsOutOfDate);
			}
		}
	}
}
