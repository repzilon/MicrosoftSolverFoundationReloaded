using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	internal abstract class Task : IDisposable
	{
		/// <summary>
		/// This class serves to hide the semantics of Term.EvaluationContext from users. The effective evaluation
		/// context of a TermWithContext is immutable, even after exit from a CallForEachInput.
		/// </summary>
		protected class TermWithContext
		{
			public Term Term { get; set; }

			public Term.EvaluationContext Context { get; set; }

			public IEnumerable<TermWithContext> AllInputs
			{
				get
				{
					int oldDepth = Context.Depth;
					foreach (Term input in ((OperatorTerm)Term).AllInputs(Context))
					{
						Term.EvaluationContext newContext = Context;
						if (Context.Depth > oldDepth)
						{
							newContext = Context.Clone();
						}
						yield return new TermWithContext(input, newContext);
					}
				}
			}

			public Operator Operation => ((OperatorTerm)Term).Operation;

			/// <summary>
			/// Create a new TermWithContext
			/// </summary>
			/// <param name="term"></param>
			/// <param name="context">The initial evaluation context. It must not change during the lifetime of the TermWithContext.</param>
			public TermWithContext(Term term, Term.EvaluationContext context)
			{
				Term = term;
				Context = context;
			}

			public bool TryEvaluateConstantValue(out Rational value)
			{
				return Term.TryEvaluateConstantValue(out value, Context);
			}

			public bool TryEvaluateConstantValue(out object value)
			{
				return Term.TryEvaluateConstantValue(out value, Context);
			}

			public TermWithContext[] GetInputs()
			{
				List<TermWithContext> list = new List<TermWithContext>();
				if (Term is OperatorTerm)
				{
					foreach (TermWithContext allInput in AllInputs)
					{
						list.Add(allInput);
					}
				}
				else if (Term is IndexTerm)
				{
					Term[] inputs = ((IndexTerm)Term)._inputs;
					foreach (Term term in inputs)
					{
						list.Add(new TermWithContext(term, Context));
					}
				}
				return list.ToArray();
			}

			internal bool TryGetParameterName(out string name)
			{
				name = "";
				if (Term is Parameter parameter)
				{
					name = parameter.Name;
					return true;
				}
				if (!(Term is IndexTerm indexTerm))
				{
					return false;
				}
				if (!(indexTerm._table is Parameter parameter2))
				{
					return false;
				}
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(parameter2.Name);
				if (indexTerm._inputs.Length > 0)
				{
					stringBuilder.Append("(");
					bool flag = true;
					Term[] inputs = indexTerm._inputs;
					foreach (Term term in inputs)
					{
						if (!term.TryEvaluateConstantValue(out object value, Context))
						{
							return false;
						}
						if (!flag)
						{
							stringBuilder.Append(", ");
						}
						flag = false;
						stringBuilder.Append(value.ToString());
					}
				}
				stringBuilder.Append(")");
				name = stringBuilder.ToString();
				return true;
			}
		}

		internal Task<SolverContext.TaskSummary> _task;

		protected SolverContext _context;

		protected ISolverParameters _solverParams;

		internal bool _abort;

		protected Directive _directive;

		protected bool _disposed;

		protected Term.EvaluationContext _evaluationContext;

		protected static object[] _emptyArray = new object[0];

		public Directive Directive => _directive;

		public virtual Term.EvaluationContext EvaluationContext
		{
			get
			{
				return _evaluationContext;
			}
			set
			{
				_evaluationContext = value;
			}
		}

		/// <summary>
		/// The main factory call for creating task
		/// </summary>
		/// <param name="context"></param>
		/// <param name="directive"></param>
		/// <param name="generator"></param>
		/// <param name="mpsWrite"></param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public static Task CreateTask(SolverContext context, Directive directive, ModelGenerator generator, bool mpsWrite = false)
		{
			CheckStochasticDirectiveModelTypeMatching(directive, generator.ModelType);
			Task task = null;
			if (generator.TryGetTask(out task))
			{
				FillTask(generator, task);
				return task;
			}
			ModelType modelType = generator.ModelType;
			bool flag = IsStochastic(modelType);
			bool flag2 = IsSos(modelType);
			List<Exception> list = new List<Exception>();
			Model model = generator._model;
			StochasticDirective stochasticDirective = directive as StochasticDirective;
			if (flag && stochasticDirective != null)
			{
				directive = GetDefaultStochasticLpDirective();
			}
			foreach (SolverCapability capability in generator.GetCapabilities())
			{
				bool flag3 = false;
				int num = 0;
				SolverCapabilityFlags flags = (flag2 ? SolverCapabilityFlags.Sos : ((SolverCapabilityFlags)0));
				foreach (Tuple<ISolver, Type> solver in context.RegisteredSolvers.GetSolvers(capability, flag, directive, context._solverEnv, flags))
				{
					try
					{
						ISolver item = solver.Item1;
						Type item2 = solver.Item2;
						if (item != null)
						{
							num++;
							ISolverParameters solverParams = context.RegisteredSolvers.GetSolverParams(capability, item2, item, directive);
							DebugContracts.NonNull(solverParams);
							if (item2 == typeof(ILinearSolver))
							{
								task = CreateLinearTask(context, model, modelType, directive, capability, flag, mpsWrite, stochasticDirective, item, item2, solverParams);
							}
							else if (item2 == typeof(ITermSolver))
							{
								task = CreateTermTask(context, model, modelType, directive, capability, flag, mpsWrite, stochasticDirective, item, item2, solverParams);
							}
							else if (item2 == typeof(INonlinearSolver))
							{
								task = CreateNonlinearTask(context, model, modelType, directive, capability, flag, mpsWrite, stochasticDirective, item, item2, solverParams);
							}
							else if (item2 == typeof(ConstraintSystem))
							{
								task = CreateCspTask(context, model, modelType, directive, capability, flag, mpsWrite, stochasticDirective, item, item2, solverParams);
							}
							if (task != null)
							{
								FillTask(generator, task);
								flag3 = true;
								return task;
							}
						}
					}
					catch (ModelException item3)
					{
						list.Add(item3);
					}
					catch (NotSupportedException item4)
					{
						list.Add(item4);
					}
					finally
					{
						if (!flag3)
						{
							DisposeTask(ref task);
						}
					}
				}
				if (num == 0 && directive.GetType() != typeof(Directive))
				{
					try
					{
						throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.NoSolverWithCapabilityForDirectiveWithType01, new object[2]
						{
							capability,
							directive.GetType().FullName
						}));
					}
					catch (ModelException item5)
					{
						list.Add(item5);
					}
				}
			}
			throw new UnsolvableModelException(list.ToArray());
		}

		/// <summary>Creates a new instance.
		/// </summary>
		protected Task(SolverContext context, ISolverParameters solverParams, Directive directive)
		{
			_context = context;
			_solverParams = solverParams;
			_directive = directive;
		}

		private static bool IsStochastic(ModelType modelType)
		{
			return (modelType & ModelType.Stochastic) == ModelType.Stochastic;
		}

		private static bool IsSos(ModelType modelType)
		{
			return (modelType & (ModelType.Sos1 | ModelType.Sos2)) != 0;
		}

		private static void FillTask(ModelGenerator generator, Task task)
		{
			DebugContracts.NonNull(task);
			bool flag = false;
			try
			{
				generator.Fill(task);
				flag = true;
			}
			catch (ModelException ex)
			{
				throw new ModelException(Resources.NotLpMilpQpModel, ex);
			}
			catch (NotSupportedException ex2)
			{
				throw new ModelException(Resources.NotLpMilpQpModel, ex2);
			}
			finally
			{
				if (!flag)
				{
					DisposeTask(ref task);
				}
			}
		}

		private static void DisposeTask(ref Task task)
		{
			if (task != null)
			{
				task.Dispose();
				task = null;
			}
		}

		private static Task CreateLinearTask(SolverContext context, Model model, ModelType modelType, Directive directive, SolverCapability solverCapability, bool isStochastic, bool mpsTask, StochasticDirective stochDirective, ISolver solver, Type solverInterface, ISolverParameters solverParams)
		{
			if (!(solver is ILinearSolver ls))
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.SolverDoesNotImplementInterface, new object[2]
				{
					solver.GetType().FullName,
					solverInterface.FullName
				}));
			}
			if (solverCapability != SolverCapability.LP && solverCapability != SolverCapability.MILP && solverCapability != SolverCapability.QP && solverCapability != SolverCapability.MIQP)
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.InterfaceIncompatibleWithProblem, new object[3]
				{
					solver.GetType().FullName,
					solverInterface.FullName,
					solverCapability
				}));
			}
			if (isStochastic && solverCapability == SolverCapability.LP)
			{
				return new LinearStochasticTask(context, model, ls, solverParams, stochDirective ?? directive);
			}
			if (mpsTask)
			{
				return new MpsWriterTask(context, model, ls, solverParams, directive);
			}
			return new LinearTask(context, model, ls, solverParams, directive);
		}

		private static Task CreateTermTask(SolverContext context, Model model, ModelType modelType, Directive directive, SolverCapability solverCapability, bool isStochastic, bool mpsTask, StochasticDirective stochDirective, ISolver solver, Type solverInterface, ISolverParameters solverParams)
		{
			if (!(solver is ITermSolver termSolver))
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.SolverDoesNotImplementInterface, new object[2]
				{
					solver.GetType().FullName,
					solverInterface.FullName
				}));
			}
			TermModelCapabilityAnalyzer termModelCapabilityAnalyzer = new TermModelCapabilityAnalyzer();
			foreach (Goal goal in model.Goals)
			{
				goal.Term.Visit(termModelCapabilityAnalyzer, 0);
			}
			foreach (Constraint constraint in model.Constraints)
			{
				constraint.Term.Visit(termModelCapabilityAnalyzer, 0);
			}
			foreach (TermModelOperation supportedOperation in termSolver.SupportedOperations)
			{
				termModelCapabilityAnalyzer._operations.Remove(supportedOperation);
			}
			if (termModelCapabilityAnalyzer._operations.Count > 0)
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.SolverDoesNotSupportOperation, new object[2]
				{
					solver.GetType().FullName,
					termModelCapabilityAnalyzer._operations.First()
				}));
			}
			return new TermTask(context, model, termSolver, solverParams, directive);
		}

		private static Task CreateNonlinearTask(SolverContext context, Model model, ModelType modelType, Directive directive, SolverCapability solverCapability, bool isStochastic, bool mpsTask, StochasticDirective stochDirective, ISolver solver, Type solverInterface, ISolverParameters solverParams)
		{
			if (!(solver is INonlinearSolver nonlinearSolver))
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.SolverDoesNotImplementInterface, new object[2]
				{
					solver.GetType().FullName,
					solverInterface.FullName
				}));
			}
			NonlinearCapabilities nonlinearCapabilities = nonlinearSolver.NonlinearCapabilities;
			DerivativeCapability gradientCapability = nonlinearSolver.GradientCapability;
			if ((gradientCapability & DerivativeCapability.Required) != 0 && (modelType & ModelType.Differentiable) == 0)
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.SolverDoesNotSupportNonDifferentiableModels, new object[1] { solver.GetType().FullName }));
			}
			if ((nonlinearCapabilities & NonlinearCapabilities.SupportsBoundedRows) == 0 && (modelType & ModelType.Constrained) != 0)
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.SolverDoesNotSupportConstrainedModels, new object[1] { solver.GetType().FullName }));
			}
			if ((nonlinearCapabilities & NonlinearCapabilities.SupportsBoundedVariables) == 0 && (modelType & ModelType.Bounded) != 0)
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.SolverDoesNotSupportBoundedVariables, new object[1] { solver.GetType().FullName }));
			}
			bool doDifferentiate = false;
			if ((gradientCapability & (DerivativeCapability)3) != 0 && (modelType & ModelType.Differentiable) != 0)
			{
				doDifferentiate = true;
			}
			return new NonlinearTask(context, model, nonlinearSolver, solverParams, directive, doDifferentiate);
		}

		private static Task CreateCspTask(SolverContext context, Model model, ModelType modelType, Directive directive, SolverCapability solverCapability, bool isStochastic, bool mpsTask, StochasticDirective stochDirective, ISolver solver, Type solverInterface, ISolverParameters solverParams)
		{
			if (!(solver is ConstraintSystem fs))
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.SolverDoesNotImplementInterface, new object[2]
				{
					solver.GetType().FullName,
					solverInterface.FullName
				}));
			}
			return new CspTask(context, fs, solverParams, directive);
		}

		private static Directive GetDefaultStochasticLpDirective()
		{
			return new Directive();
		}

		private static void CheckStochasticDirectiveModelTypeMatching(Directive directive, ModelType modelType)
		{
			bool flag = (modelType & ModelType.Stochastic) == ModelType.Stochastic;
			bool flag2 = directive is StochasticDirective;
			bool flag3 = flag2 || directive.GetType() == typeof(Directive);
			if (flag2 && !flag)
			{
				throw new ModelException(Resources.ModelIsNotStochasticButStochasticDirectiveWasUsed);
			}
			if (!flag3 && flag)
			{
				throw new ModelException(Resources.ModelIsStochasticButNonStochasticDirectiveWasUsed);
			}
			if (flag && (modelType & ModelType.Lp) == 0)
			{
				throw new ModelException(Resources.StochasticModelsMustBeLinear);
			}
		}

		public virtual void Dispose()
		{
			if (_task != null)
			{
				if (_task.IsCompleted)
				{
					_task.Dispose();
				}
				_task = null;
			}
		}

		public virtual void AbortTask()
		{
			_abort = true;
		}

		public virtual void AbortAllTasks()
		{
			_abort = true;
			_context.AbortAsync();
		}

		public SolverContext.TaskSummary Execute(Func<Task, bool> queryAbort)
		{
			CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
			CultureInfo currentUICulture = Thread.CurrentThread.CurrentUICulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = _context._cultureInfo;
				Thread.CurrentThread.CurrentUICulture = _context._cultureUIInfo;
				return Solve(() => queryAbort(this));
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = currentCulture;
				Thread.CurrentThread.CurrentUICulture = currentUICulture;
			}
		}

		protected void SolvingHook()
		{
			_context.FireSolvingEvent(this);
		}

		protected abstract SolverContext.TaskSummary Solve(Func<bool> queryAbort);

		public abstract void AddGoal(Goal goal, int goalId);

		public abstract void AddConstraints(IEnumerable<Constraint> constraints);

		public virtual void BuildModel()
		{
		}

		protected virtual bool IsDecision(Term term)
		{
			if (!(term is Decision))
			{
				return term is IndexTerm;
			}
			return true;
		}

		protected static Decision GetSubmodelInstanceDecision(Decision decision, Term.EvaluationContext context)
		{
			SubmodelInstance submodelInstance = SubmodelInstance.FollowPath(context);
			if (submodelInstance != null && submodelInstance.TryGetDecision(decision, out var val))
			{
				decision = val;
			}
			return decision;
		}

		protected static RecourseDecision GetSubmodelInstanceDecision(RecourseDecision decision, Term.EvaluationContext context)
		{
			SubmodelInstance submodelInstance = SubmodelInstance.FollowPath(context);
			if (submodelInstance != null && submodelInstance.TryGetRecourseDecision(decision, out var val))
			{
				decision = val;
			}
			return decision;
		}

		protected virtual Decision GetDecision(TermWithContext term)
		{
			Decision decision = term.Term as Decision;
			return GetSubmodelInstanceDecision(decision, term.Context);
		}

		protected virtual Decision GetDecision(TermWithContext term, IndexTerm indexTerm)
		{
			if (!(indexTerm._table is Decision decision))
			{
				throw new InvalidTermException(Resources.CannotIndexNonDecision, indexTerm);
			}
			return GetSubmodelInstanceDecision(decision, term.Context);
		}

		protected static ValueSet[] DecisionValueSets(Decision decision)
		{
			ValueSet[] array = new ValueSet[decision._indexSets.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = decision._indexSets[i].ValueSet;
			}
			return array;
		}

		internal abstract SolutionMapping GetSolutionMapping(ISolverSolution solution);

		internal abstract IEnumerable<object[]> GetIndexes(Decision decision);

		internal abstract ISolverProperties GetSolverPropertiesInstance();

		internal abstract int FindDecisionVid(Decision decision, object[] indexes);

		/// <summary>Get the value of a model- or decision-level property.
		/// </summary>
		/// <param name="property">The name of the property to get, see SolverProperties.</param>
		/// <param name="decision">The decision. In the case of a solver-level property this argument should be null.</param>
		/// <param name="indexes">The decision indexes. In the case of a solver-level property this argument should be an empty array.</param>
		/// <returns>The value.</returns>
		internal virtual object GetSolverProperty(string property, Decision decision, object[] indexes)
		{
			ISolverProperties solverPropertiesInstance = GetSolverPropertiesInstance();
			if (property == SolverProperties.SolverName)
			{
				return solverPropertiesInstance.GetType().Name;
			}
			int vid = FindDecisionVid(decision, indexes);
			try
			{
				return solverPropertiesInstance.GetProperty(property, vid);
			}
			catch (ArgumentException)
			{
				if ((object)decision == null)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.PropertyRequiresADecision0, new object[1] { property }));
				}
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.CannotFindTheDecisionAndIndexesInTheModel0, new object[1] { decision.ToString(indexes) }));
			}
		}

		/// <summary>Set the value of a model- or decision-level property.
		/// </summary>
		/// <param name="property">The name of the property to get, see SolverProperties.</param>
		/// <param name="decision">The decision. In the case of a solver-level property this argument should be null.</param>
		/// <param name="indexes">The decision indexes. In the case of a solver-level property this argument should be an empty array.</param>
		/// <param name="value">The value.</param>
		/// <returns>The value.</returns>
		internal virtual void SetSolverProperty(string property, Decision decision, object[] indexes, object value)
		{
			ISolverProperties solverPropertiesInstance = GetSolverPropertiesInstance();
			int vid = FindDecisionVid(decision, indexes);
			try
			{
				solverPropertiesInstance.SetProperty(property, vid, value);
			}
			catch (ArgumentException)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.CannotFindTheDecisionAndIndexesInTheModel0, new object[1] { ((object)decision == null) ? "" : decision.ToString(indexes) }));
			}
		}

		protected static void EnsureArraySize<T>(ref T[] array, int size)
		{
			if (array == null)
			{
				array = new T[Math.Max(100, size * 2)];
			}
			else if (array.Length <= size)
			{
				Array.Resize(ref array, size * 2);
			}
		}

		protected static ISolverProperties GetSolverPropertiesInstance(ISolver solver)
		{
			if (!(solver is ISolverProperties result))
			{
				throw new InvalidSolverPropertyException(Resources.SolverDoesNotSupportGettingOrSettingProperties, InvalidSolverPropertyReason.SolverDoesNotSupportEvents);
			}
			return result;
		}
	}
}
