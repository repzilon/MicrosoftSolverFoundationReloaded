#define TRACE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Stochastic model generator.
	/// </summary>
	internal class StochasticModelGenerator : ModelGenerator
	{
		/// <summary> Solution phase.
		/// </summary>
		private enum Phase
		{
			Solve,
			SolveComplete,
			Finished
		}

		/// <summary>This class maintains the stages of the stochastic solution
		/// </summary>
		private class StochasticState
		{
			/// <summary> Number of all slaves/scenarios/samples
			/// </summary>
			public int SecondStageCount;
		}

		private StochasticDirective _stochasticDirective;

		private readonly List<Constraint> _secondStageConstraints = new List<Constraint>();

		private ScenarioGenerator _generator;

		private StochasticState _state;

		private Goal TheOnlyGoal => _model.AllGoals.First();

		/// <summary> Create a new instance.
		/// </summary>
		public StochasticModelGenerator(SolverContext context, Model model, ModelType modelType, ModelAnalyzer analyzer)
			: base(context, model, modelType, analyzer)
		{
			if (model.AllGoals.Count() > 1)
			{
				throw new ModelException(Resources.StochasticModelCannotContainMoreThanOneGoal);
			}
			if (model.AllGoals.Count() == 0 || !TheOnlyGoal.Enabled)
			{
				throw new ModelException(Resources.StochasticModelMustContainAGoal);
			}
			if (!TheOnlyGoal.IsValidStochastic())
			{
				throw new ModelException(Resources.GoalsCannotContainDecisionsMultipliedByRandomParameters);
			}
			_stochasticDirective = new StochasticDirective();
			_state = new StochasticState();
		}

		/// <summary>Called from the task to see if there is already task ready which
		/// we should work with. The master task should not be rebuilt every iteration.
		/// This also return the task of a slave if a feasibility task is needed
		/// </summary>
		/// <param name="task">The task to be set.</param>
		/// <returns>True if there is task is set.</returns>
		public override bool TryGetTask(out Task task)
		{
			task = null;
			return false;
		}

		/// <summary>Called from the Task factory.
		/// Fill in the master of slave (in the case of decomposition) or the DE
		/// </summary>
		/// <param name="task"></param>
		public override void Fill(Task task)
		{
			if (!(task is LinearStochasticTask stochasticTask))
			{
				throw new NotSupportedException();
			}
			if (_generator == null)
			{
				Init(stochasticTask);
			}
			task.EvaluationContext = _evaluationContext;
			FillDeterministicEquivalent(stochasticTask);
		}

		private void Init(LinearStochasticTask stochasticTask)
		{
			if (stochasticTask.Directive.MaximumGoalCount == 0)
			{
				throw new ModelException(Resources.StochasticModelMustContainAGoal);
			}
			if (stochasticTask.StochasticDirective != null)
			{
				InitDirective(stochasticTask.StochasticDirective);
			}
			TuneStochasticOptions();
			InitScenarioGenerator();
			_state.SecondStageCount = (_generator.SamplingNeeded ? _generator.SampleCount : _generator.ScenarioCount);
		}

		private void InitDirective(StochasticDirective directive)
		{
			_stochasticDirective.MaximumScenarioCountBeforeSampling = directive.MaximumScenarioCountBeforeSampling;
			_stochasticDirective.DecompositionType = directive.DecompositionType;
		}

		private void TuneStochasticOptions()
		{
			if (_stochasticDirective.MaximumScenarioCountBeforeSampling < 0)
			{
				_stochasticDirective.MaximumScenarioCountBeforeSampling = 500;
			}
		}

		/// <summary>Fills the task with the Deterministic Equivalent model
		/// </summary>
		/// <param name="stochasticTask"></param>
		///
		/// <returns>True if the deteministic Equivalent met the criteria 
		/// or if (checkCriteria == false)</returns>
		private bool FillDeterministicEquivalent(LinearStochasticTask stochasticTask)
		{
			stochasticTask.Init(this, _state.SecondStageCount);
			_context.TraceSource.TraceInformation("Start to build the deterministic equivalent model");
			AddFirstStageConstraints(stochasticTask);
			IEnumerable<Rational> allScenarios = _generator.GetAllScenarios();
			int num = 1;
			List<double> list = new List<double>();
			foreach (Rational item in allScenarios)
			{
				list.Add(item.ToDouble());
				stochasticTask._currentScenario = num - 1;
				foreach (RecourseDecision allRecourseDecision in _model.AllRecourseDecisions)
				{
					allRecourseDecision.CurrentSecondStageDecision = new Decision(allRecourseDecision._domain, GetNameForSecondStageDecision(allRecourseDecision.Name, num), allRecourseDecision._indexSets);
					allRecourseDecision.InitCurrentDecision();
				}
				AddSecondStageConstraints(stochasticTask);
				stochasticTask.UpdateGoal(TheOnlyGoal, 0, item);
				num++;
			}
			double[] secondStageProbabilities = list.ToArray();
			foreach (RecourseDecision allRecourseDecision2 in _model.AllRecourseDecisions)
			{
				allRecourseDecision2._secondStageProbabilities = secondStageProbabilities;
			}
			_context.TraceSource.TraceInformation("Finish to build the deterministic equivalent model");
			stochasticTask.TraceModelDetails();
			return true;
		}

		private void InitScenarioGenerator()
		{
			List<DistributedValue> list = new List<DistributedValue>();
			foreach (RandomParameter allRandomParameter in _model.AllRandomParameters)
			{
				list.AddRange(allRandomParameter.ValueTable.Values);
			}
			SamplingParameters samplingParameters = new SamplingParameters(_context.SamplingParameters);
			TuneSampleParameters(samplingParameters);
			_generator = new ScenarioGenerator(_context, samplingParameters, list, _stochasticDirective);
			_context.ScenarioGenerator = _generator;
			foreach (RecourseDecision allRecourseDecision in _model.AllRecourseDecisions)
			{
				allRecourseDecision.Reset();
			}
		}

		/// <summary>This will use some heuristics to determine the sampling parameters
		/// </summary>
		/// <param name="samplingParameters"></param>
		private static void TuneSampleParameters(SamplingParameters samplingParameters)
		{
			if (samplingParameters.SamplingMethod == SamplingMethod.Automatic)
			{
				samplingParameters.SamplingMethod = SamplingMethod.LatinHypercube;
			}
			if (samplingParameters.SampleCount == 0)
			{
				if (samplingParameters.SamplingMethod == SamplingMethod.LatinHypercube)
				{
					samplingParameters.SampleCount = 100;
				}
				else
				{
					samplingParameters.SampleCount = 300;
				}
			}
		}

		private void AddSecondStageConstraints(Task task)
		{
			task.AddConstraints(_secondStageConstraints);
		}

		private void AddFirstStageConstraints(Task task)
		{
			List<Constraint> list = new List<Constraint>();
			_secondStageConstraints.Clear();
			_evaluationContext.Goal = null;
			foreach (Constraint allConstraint in _model.AllConstraints)
			{
				_evaluationContext.Constraint = allConstraint;
				if (!allConstraint.IsSecondStage())
				{
					list.Add(allConstraint);
				}
				else
				{
					_secondStageConstraints.Add(allConstraint);
				}
			}
			task.AddConstraints(list);
		}

		/// <summary> Get name for second stage decision.
		/// </summary>
		/// <remarks> For now just use the number of scenarios.
		/// No need to check for duplicates (as for example if user called some other decision "decision_1" 
		/// as there is no restriction of names duplication anyway in Model
		/// </remarks>
		private static string GetNameForSecondStageDecision(string recourseDecisionName, int scenarioCount)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}_{1}", new object[2] { recourseDecisionName, scenarioCount });
		}
	}
}
