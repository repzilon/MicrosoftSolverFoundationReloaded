#define TRACE
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal class LinearStochasticTask : LinearTask
	{
		/// <summary>The type of stochastic task.
		/// </summary>
		internal enum TaskType
		{
			DeterministicEquivalent
		}

		private Rational _currentScenarioProbability;

		/// <summary>Stochastic directive (if any).
		/// </summary>
		public StochasticDirective StochasticDirective => _directive as StochasticDirective;

		/// <summary>NonzeroCount of the linear model
		/// </summary>
		public int NonzeroCount => _linearSolver.CoefficientCount;

		/// <summary>RowCount of the linear model
		/// </summary>
		public int RowCount => _linearSolver.RowCount;

		/// <summary>Create a new instance.
		/// </summary>
		public LinearStochasticTask(SolverContext context, Model model, ILinearSolver ls, ISolverParameters solverParams, Directive directive)
			: base(context, model, ls, solverParams, directive)
		{
		}

		/// <summary>
		/// Add the new second stage decisions with their coefficients to the goal, and update the first stage decisions coefficients
		/// </summary>
		/// <param name="goal">the only goal</param>
		/// <param name="id">the goal id</param>
		/// <param name="currentScenarioProbability"></param>
		public void UpdateGoal(Goal goal, int id, Rational currentScenarioProbability)
		{
			DebugContracts.NonNull(_evaluationContext);
			_currentScenarioProbability = currentScenarioProbability;
			_evaluationContext.Constraint = null;
			_evaluationContext.Goal = goal;
			if (_linearSolver.GoalCount == 0)
			{
				AddGoal(goal, id);
				return;
			}
			int vidRow = _goalVids[id];
			Rational boundsAdjustment = 0;
			AddTermToRow(goal.Term, vidRow, ref boundsAdjustment, _currentScenarioProbability);
		}

		/// <summary>
		/// REVIEW shahark: might abstract that more, as just one line is different from the base impl
		/// </summary>
		/// <param name="goal">the only goal</param>
		/// <param name="priority">always 0</param>
		public override void AddGoal(Goal goal, int priority)
		{
			Rational currentScenarioProbability = _currentScenarioProbability;
			AddGoal(priority, goal, currentScenarioProbability);
		}

		/// <summary>Called by the model generator to add a goal
		/// </summary>
		/// <param name="goal"></param>
		/// <param name="priority"></param>
		/// <param name="currentProbability">When adding goal to master this will be 1
		/// when setting the goals of the slaves this will be the actual probability of the scenario</param>
		public void AddGoal(Goal goal, int priority, Rational currentProbability)
		{
			_currentScenarioProbability = currentProbability;
			AddGoal(goal, priority);
		}

		protected override SolverContext.TaskSummary Solve(Func<bool> queryAbort)
		{
			_solverParams.QueryAbort = queryAbort;
			MsfException exception = null;
			ILinearSolution linearSolution = null;
			try
			{
				linearSolution = _linearSolver.Solve(_solverParams);
			}
			catch (MsfException ex)
			{
				exception = ex;
			}
			SolverContext.TaskSummary taskSummary = new SolverContext.TaskSummary();
			taskSummary.SolutionMapping = GetSolutionMapping(linearSolution);
			taskSummary.Solution = new Solution(_context, LinearTask.GetSolverQuality(linearSolution));
			taskSummary.Directive = _directive;
			taskSummary.Solver = _linearSolver;
			taskSummary.Exception = exception;
			return taskSummary;
		}

		/// <summary>Stochastic considers RecourseDecision as well as Decision.
		/// </summary>
		/// <param name="term"></param>
		/// <returns></returns>
		protected override bool IsDecision(Term term)
		{
			if (!base.IsDecision(term))
			{
				return term is RecourseDecision;
			}
			return true;
		}

		private static RecourseDecision GetSubmodelInstanceRecourseDecision(TermWithContext term, RecourseDecision decision)
		{
			SubmodelInstance submodelInstance = SubmodelInstance.FollowPath(term.Context);
			if (submodelInstance != null && submodelInstance.TryGetRecourseDecision(decision, out var val))
			{
				decision = val;
			}
			return decision;
		}

		/// <summary>Get a Decision, taking into account RecourseDecisions.
		/// </summary>
		protected override Decision GetDecision(TermWithContext term)
		{
			if (!(term.Term is RecourseDecision decision))
			{
				return base.GetDecision(term);
			}
			return GetSubmodelInstanceRecourseDecision(term, decision).CurrentSecondStageDecision;
		}

		/// <summary>Get a Decision, taking into account RecourseDecisions.
		/// </summary>
		protected override Decision GetDecision(TermWithContext term, IndexTerm indexTerm)
		{
			if (!(indexTerm._table is RecourseDecision decision))
			{
				return base.GetDecision(term, indexTerm);
			}
			return GetSubmodelInstanceRecourseDecision(term, decision).CurrentSecondStageDecision;
		}

		public void TraceModelDetails()
		{
			_context.TraceSource.TraceInformation("{0} model has {1} columns, {2} rows and {3} nonzeroes", TaskType.DeterministicEquivalent, _linearSolver.VariableCount, RowCount, NonzeroCount);
		}

		/// <summary>Called by the model generator before starting to populate the task
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Body")]
		public void Init(StochasticModelGenerator generator, int scenariosCount)
		{
			if (scenariosCount > 0)
			{
				_recourseDecisionVids = new int[generator._model._maxRecourseDecisionId, scenariosCount];
				_recourseDecisionIndexedVids = new ValueTable<int>[generator._model._maxRecourseDecisionId, scenariosCount];
			}
		}
	}
}
