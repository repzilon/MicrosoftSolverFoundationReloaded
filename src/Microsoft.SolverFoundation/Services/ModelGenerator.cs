using System.Collections.Generic;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	internal class ModelGenerator
	{
		internal readonly Model _model;

		internal readonly SolverContext _context;

		private ModelAnalyzer _analyzer;

		protected Term.EvaluationContext _evaluationContext = new Term.EvaluationContext();

		public ModelType ModelType { get; private set; }

		public IEnumerable<Operator> ModelOperators
		{
			get
			{
				_analyzer.AnalyzeOperators();
				return _analyzer.Operators;
			}
		}

		/// <summary> Create a new instance.
		/// </summary>
		public static ModelGenerator Create(SolverContext context, Model model)
		{
			ModelType modelType = ModelType.Unknown;
			ModelAnalyzer modelAnalyzer;
			try
			{
				modelAnalyzer = new ModelAnalyzer(model);
				modelAnalyzer.Analyze();
				modelType = modelAnalyzer.GetModelType();
			}
			catch (ModelException innerException)
			{
				throw new MsfException(Resources.ModelCannotBeAnalyzed, innerException);
			}
			if ((modelType & ModelType.Stochastic) == ModelType.Stochastic)
			{
				return new StochasticModelGenerator(context, model, modelType, modelAnalyzer);
			}
			return new ModelGenerator(context, model, modelType, modelAnalyzer);
		}

		/// <summary> Create a new instance.
		/// </summary>
		protected ModelGenerator(SolverContext context, Model model, ModelType modelType, ModelAnalyzer analyzer)
		{
			_context = context;
			_model = model;
			_analyzer = analyzer;
			ModelType = modelType;
		}

		internal IEnumerable<SolverCapability> GetCapabilities()
		{
			return _analyzer.GetCapabilities();
		}

		public static void BindData(DataBinder dataBinder)
		{
			dataBinder.BindData(boundIfAlreadyBound: true);
		}

		public void RewriteModel()
		{
		}

		/// <summary> Fill task with current model.
		/// This will be called even from task returned by TryGetTask
		/// </summary>
		public virtual void Fill(Task task)
		{
			task.EvaluationContext = _evaluationContext;
			AddConstraints(task);
			AddGoals(task);
			BuildModel(task);
			if (!_context.HasDataBindingEvent)
			{
				return;
			}
			foreach (Decision decision in _model._decisions)
			{
				_context.FireDataBindingEvent(decision, task);
			}
		}

		/// <summary> Check to see if there is an available task.
		/// </summary>
		public virtual bool TryGetTask(out Task task)
		{
			task = null;
			return false;
		}

		protected void AddGoals(Task task)
		{
			int num = 0;
			_evaluationContext.Constraint = null;
			int maximumGoalCount = task.Directive.MaximumGoalCount;
			foreach (Goal item in _model.AllGoals.OrderBy((Goal goal) => goal.Order))
			{
				if (num != maximumGoalCount)
				{
					_evaluationContext.Goal = item;
					AddGoal(task, item, num);
					num++;
					continue;
				}
				break;
			}
		}

		protected virtual void AddGoal(Task task, Goal goal, int goalId)
		{
			if (goal.Enabled)
			{
				goal._id = goalId;
				task.AddGoal(goal, goalId);
			}
		}

		private void AddConstraints(Task task)
		{
			task.AddConstraints(_model.AllConstraints);
		}

		private static void BuildModel(Task task)
		{
			task.BuildModel();
		}
	}
}
