#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>Finds a local minimum/maximum for an unconstrained nonlinear function.
	/// </summary>
	/// <remarks>
	/// Uses the L-BFGS algorithm, a limited memory Quasi-Newton method.
	/// </remarks>
	public class CompactQuasiNewtonSolver : CompactQuasiNewtonModel, INonlinearSolver, IRowVariableSolver, ISolver, INonlinearModel, IRowVariableModel, IGoalModel, INonlinearSolution, ISolverSolution, ISolverProperties, IReportProvider
	{
		/// <summary>
		/// Helps with the specific mapping for vid-&gt;solver index of CQN solver 
		/// </summary>
		internal class CqnValueByIndex : ValuesByIndex
		{
			public override double this[int index]
			{
				get
				{
					return base.SolverValues[UnconstrainedNonlinearModel.GetVariableListIndex(index)];
				}
				set
				{
					base.SolverValues[UnconstrainedNonlinearModel.GetVariableListIndex(index)] = value;
				}
			}

			/// <summary>
			/// Creates CqnValueByIndex instance
			/// </summary>
			public CqnValueByIndex()
			{
			}

			internal void SetSolverValues(double[] solverValues)
			{
				base.SolverValues = solverValues;
			}
		}

		private const int MaxItemsToPrint = 10;

		private CqnValueByIndex _valueByIndex;

		private CqnValueByIndex _gradientByIndex;

		private CompactQuasiNewtonSolverParams _params;

		private double[] _startingPoint;

		private MeanImprovementCriterion _terminationCriterion;

		private CompactQuasiNewtonSolverState _state;

		private Func<double[], double[], double> _function;

		private double _requestedTolerance;

		private int _maxIterations;

		private int _iterationCount;

		private long _evaluationCallCount;

		private CompactQuasiNewtonSolutionQuality _quality;

		private bool _localOptimaWhenCriteriaIsNotMet;

		/// <summary>Gets the difference between the solution tolerance and the tolerance
		/// requested by the caller.
		/// </summary>
		/// <remarks>
		/// The solver tolerance is set using CompactQuasiNewtonSolverParams.
		/// If a call to Solve() returns CompactQuasiNewtonSolutionQuality.LocalOptimum
		/// then this value will be zero or less.  If a local optimum is found even
		/// though the stopping criterion is not met, the final tolerance is considered
		/// to be zero and the ToleranceDifference will be the negated version of
		/// the requested tolerance.
		/// </remarks>
		public virtual double ToleranceDifference
		{
			get
			{
				VerifySolveCalled();
				if (_localOptimaWhenCriteriaIsNotMet)
				{
					return 0.0 - _requestedTolerance;
				}
				return _terminationCriterion.CurrentTolerance - _requestedTolerance;
			}
		}

		/// <summary> The number of iterations that have been performed.
		/// </summary>
		public virtual int IterationCount
		{
			get
			{
				VerifySolveCalled();
				return _iterationCount;
			}
		}

		/// <summary> The number of function evaluation calls.
		/// Each call is for both funtion and gradient evaluation.
		/// </summary>
		public virtual long EvaluationCallCount
		{
			get
			{
				VerifySolveCalled();
				return _evaluationCallCount;
			}
		}

		/// <summary> The detailed quality of solution from Compact Quasi Newton solver
		/// </summary>
		public CompactQuasiNewtonSolutionQuality SolutionQuality
		{
			get
			{
				VerifySolveCalled();
				return _quality;
			}
		}

		internal CompactQuasiNewtonSolveState SolveState { get; private set; }

		/// <summary>
		/// The capabilities for this solver
		/// </summary>
		public NonlinearCapabilities NonlinearCapabilities => NonlinearCapabilities.None;

		/// <summary>
		/// Gradient related capability
		/// </summary>
		public DerivativeCapability GradientCapability => DerivativeCapability.Required;

		/// <summary>
		/// Hessian related capability
		/// </summary>
		public DerivativeCapability HessianCapability => DerivativeCapability.NotSupported;

		/// <summary> Number of goals being solved.
		/// </summary>
		int INonlinearSolution.SolvedGoalCount => 1;

		/// <summary>
		/// indicates the type of result (e.g., LocalOptimal) 
		/// </summary>
		public NonlinearResult Result
		{
			get
			{
				if (SolveCalled())
				{
					return CqnSolutionQualityToNonlinearResult(_quality);
				}
				return NonlinearResult.Invalid;
			}
		}

		/// <summary>Creates a new instance.
		/// </summary>
		public CompactQuasiNewtonSolver()
			: this(null)
		{
		}

		/// <summary>Creates a new instance.
		/// </summary>
		/// <param name="comparer">A key comparer</param>
		public CompactQuasiNewtonSolver(IEqualityComparer<object> comparer)
			: base(comparer)
		{
			SolveState = CompactQuasiNewtonSolveState.PreInit;
		}

		/// <summary>Solve the model using the specified parameters.
		/// </summary>
		/// <param name="solverParams">The solver parameters.</param>
		/// <returns>Returns the solution quality.</returns>
		/// <exception cref="T:System.InvalidOperationException"></exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		private CompactQuasiNewtonSolutionQuality SolveCore(CompactQuasiNewtonSolverParams solverParams)
		{
			if (solverParams == null)
			{
				throw new ArgumentNullException("solverParams");
			}
			if (_function == null)
			{
				throw new InvalidOperationException(Resources.DelegateOfFunctionIsNeeded);
			}
			SolveState = CompactQuasiNewtonSolveState.Init;
			_params = solverParams;
			_requestedTolerance = _params.Tolerance;
			_maxIterations = _params.IterationLimit;
			InitializeStartingPoint();
			_state = new CompactQuasiNewtonSolverState(_function, _params, base.TheGoal.Minimize, base.VariableCount, _startingPoint);
			_terminationCriterion = new MeanImprovementCriterion(_requestedTolerance);
			Trace.TraceInformation("");
			Trace.TraceInformation(Resources.CqnTraceIterationHeader);
			Trace.TraceInformation(Resources.CqnDashes);
			Trace.TraceInformation(string.Format(CultureInfo.InvariantCulture, Resources.CqnTraceStarting012, new object[3] { _state.ModifiedValue, _state.EvaluationCount, _terminationCriterion }));
			bool flag = false;
			try
			{
				while (!flag)
				{
					SolveState = CompactQuasiNewtonSolveState.DirectionCalculation;
					CallSolvingEvent();
					_state.UpdateDir();
					SolveState = CompactQuasiNewtonSolveState.LineSearch;
					CallSolvingEvent();
					_state.LineSearch();
					Trace.TraceInformation(string.Format(CultureInfo.InvariantCulture, Resources.CqnTraceIteration0123, _state.Iter, _state.ModifiedValue, _state.EvaluationCount, _terminationCriterion));
					flag = _terminationCriterion.CriterionMet(_state);
					if (!flag)
					{
						if (_state.Iter == _maxIterations)
						{
							throw new CompactQuasiNewtonException(CompactQuasiNewtonErrorType.MaxIterationExceeded);
						}
						_state.Shift();
					}
				}
				_quality = CompactQuasiNewtonSolutionQuality.LocalOptima;
			}
			catch (CompactQuasiNewtonException ex)
			{
				AnalyzeError(ex.Error);
			}
			finally
			{
				SolveState = CompactQuasiNewtonSolveState.PreInit;
			}
			if (IsUsefulSolution(_quality))
			{
				for (int i = 1; i <= base.VariableCount; i++)
				{
					SetValue(i, _state.Point[UnconstrainedNonlinearModel.GetVariableListIndex(i)]);
				}
				if (IsRow(base.TheGoal.Index))
				{
					base.RowValue = _state.ModifiedValue;
				}
			}
			_iterationCount = _state.Iter;
			_evaluationCallCount = _state.EvaluationCount;
			return _quality;
		}

		private void InitializeStartingPoint()
		{
			_startingPoint = new double[base.VariableCount];
			int num = 0;
			foreach (int variableIndex in base.VariableIndices)
			{
				Rational value = GetValue(variableIndex);
				if (value == Rational.Indeterminate)
				{
					_startingPoint[num] = 0.0;
				}
				else
				{
					_startingPoint[num] = value.ToDouble();
				}
				num++;
			}
		}

		private void CallSolvingEvent()
		{
			if (_params.Solving != null)
			{
				_params.Solving();
			}
		}

		private static bool IsUsefulSolution(CompactQuasiNewtonSolutionQuality quality)
		{
			switch (quality)
			{
			case CompactQuasiNewtonSolutionQuality.LocalOptima:
			case CompactQuasiNewtonSolutionQuality.MaxIterationExceeded:
			case CompactQuasiNewtonSolutionQuality.Interrupted:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// If not in solving state and property is one that solver supports, throw
		/// </summary>
		private void ValidateInSolveState(string propertyName)
		{
			if (SolveState == CompactQuasiNewtonSolveState.PreInit && (propertyName == CompactQuasiNewtonProperties.EvaluationCount || propertyName == CompactQuasiNewtonProperties.CurrentTerminationCriterion || propertyName == SolverProperties.GoalValue || propertyName == SolverProperties.IterationCount || propertyName == SolverProperties.SolveState))
			{
				throw new InvalidSolverPropertyException(string.Format(CultureInfo.InvariantCulture, Resources.Property0CanOnlyBeAccessedBySolvingEventHandlers, new object[1] { propertyName }), InvalidSolverPropertyReason.EventDoesNotSupportProperty);
			}
		}

		/// <summary>Returns a string representation of the solver.
		/// </summary>
		/// <returns>Returns a string representation of the solver.</returns>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (base.TheGoal != null)
			{
				string value = (base.TheGoal.Minimize ? Resources.MinimizeProblem : Resources.MaximizeProblem);
				stringBuilder.AppendLine(value);
			}
			stringBuilder.AppendLine(Resources.Dimensions + base.VariableCount.ToString(CultureInfo.InvariantCulture));
			if (base.VariableKeyCount == base.VariableCount)
			{
				stringBuilder.AppendLine("Variable keys: ");
				stringBuilder.AppendLine(Statics.VectorToString(base.VariableKeys, base.VariableCount, 10));
			}
			else
			{
				stringBuilder.AppendLine("Variable indexes: ");
				stringBuilder.AppendLine(Statics.VectorToString(base.VariableIndices, base.VariableCount, 10));
			}
			if (_startingPoint != null)
			{
				stringBuilder.Append(Resources.StartingPoint);
				stringBuilder.Append(Statics.VectorToString(_startingPoint, base.VariableCount, 10));
			}
			if (SolveCalled())
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, Resources.SolutionQualityIs0, new object[1] { _quality }));
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, Resources.NumberOfIterationsPerformed0, new object[1] { IterationCount }));
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, Resources.NumberOfEvaluationCalls0, new object[1] { EvaluationCallCount }));
				if (!double.IsNaN(GetSolutionValue(0)))
				{
					stringBuilder.Append(Resources.FinishingPoint);
					stringBuilder.Append(VariablesToString());
					stringBuilder.AppendLine();
					stringBuilder.Append(Resources.FinishingValue);
					stringBuilder.AppendLine(GetSolutionValue(0).ToString(CultureInfo.InvariantCulture));
				}
			}
			return stringBuilder.ToString();
		}

		private string VariablesToString()
		{
			int num = Math.Min(10, base.VariableCount);
			double[] array = new double[num];
			int num2 = 0;
			foreach (int variableIndex in base.VariableIndices)
			{
				if (num2 != num)
				{
					array[num2] = GetValue(variableIndex).ToDouble();
					num2++;
					continue;
				}
				break;
			}
			return Statics.VectorToString(array, base.VariableCount, 10);
		}

		/// <summary>
		/// Map from detailed internal enum to CompactQuasiNewtonSolutionQuality.
		/// </summary>
		/// <param name="error"></param>
		private void AnalyzeError(CompactQuasiNewtonErrorType error)
		{
			bool flag = _state.IsGradientAlmostZero();
			switch (error)
			{
			case CompactQuasiNewtonErrorType.NonDescentDirection:
			case CompactQuasiNewtonErrorType.InsufficientSteplength:
			case CompactQuasiNewtonErrorType.YIsOrthogonalToS:
				if (flag)
				{
					_quality = CompactQuasiNewtonSolutionQuality.LocalOptima;
					_localOptimaWhenCriteriaIsNotMet = true;
				}
				else
				{
					_quality = CompactQuasiNewtonSolutionQuality.UserCalculationError;
				}
				break;
			case CompactQuasiNewtonErrorType.GradientDeltaIsZero:
				_quality = CompactQuasiNewtonSolutionQuality.LinearObjective;
				break;
			case CompactQuasiNewtonErrorType.NumericLimitExceeded:
				_quality = CompactQuasiNewtonSolutionQuality.Unbounded;
				break;
			case CompactQuasiNewtonErrorType.MaxIterationExceeded:
				_quality = CompactQuasiNewtonSolutionQuality.MaxIterationExceeded;
				break;
			case CompactQuasiNewtonErrorType.Interrupted:
				_quality = CompactQuasiNewtonSolutionQuality.Interrupted;
				break;
			default:
				throw new ArgumentException(Resources.WrongErrorType, "error");
			}
		}

		private void VerifySolveCalled()
		{
			if (!SolveCalled())
			{
				throw new InvalidOperationException(Resources.SolveMethodNeededToBeCalledBeforeCheckingSolution);
			}
		}

		private bool SolveCalled()
		{
			return _state != null;
		}

		/// <summary>
		/// Solve the model using the given parameter instance.
		/// </summary>
		/// <param name="parameters">Should be an instance of CompactQuasiNewtonSolverParams.</param>
		/// <returns>The solution after solving.</returns>
		/// <exception cref="T:System.ArgumentNullException">Parameters should not be null.</exception>
		/// <exception cref="T:System.ArgumentException">parameters should be of CompactQuasiNewtonSolverParams type.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Both FunctionEvaluator and GradientEvaluator must be specified before calling solve.</exception>
		public INonlinearSolution Solve(ISolverParameters parameters)
		{
			if (parameters == null)
			{
				throw new ArgumentNullException("parameters");
			}
			if (!(parameters is CompactQuasiNewtonSolverParams solverParams))
			{
				throw new ArgumentException(Resources.InvalidParams);
			}
			if (base.FunctionEvaluator == null || base.GradientEvaluator == null)
			{
				throw new ModelException(Resources.NmSolverRequiresEvaluatorToBeSpecifiedBeforeCallingSolve);
			}
			_valueByIndex = new CqnValueByIndex();
			_gradientByIndex = new CqnValueByIndex();
			_function = GradientAndValueAtPointCallback;
			SolveCore(solverParams);
			return this;
		}

		/// <summary>
		/// This hooks up calls from the solver core to the callbacks on the user function.
		/// </summary>
		/// <param name="values">Current values of variables at the point.</param>
		/// <param name="gradients">Gradient to fill.</param>
		/// <returns>Function value at the point.</returns>
		private double GradientAndValueAtPointCallback(double[] values, double[] gradients)
		{
			_valueByIndex.SetSolverValues(values);
			_gradientByIndex.SetSolverValues(gradients);
			double result = base.FunctionEvaluator(this, base.TheGoal.Index, _valueByIndex, arg4: true);
			base.GradientEvaluator(this, base.TheGoal.Index, _valueByIndex, arg4: false, _gradientByIndex);
			return result;
		}

		/// <summary> Shutdown the solver instance
		/// </summary>
		/// <remarks>Solver needs to dispose any unmanaged memory used upon this call.</remarks>
		public void Shutdown()
		{
		}

		/// <summary>Set a solver-related property.
		/// </summary>
		/// <param name="propertyName">The name of the property to get.</param>
		/// <param name="vid">An index for the item of interest.</param>
		/// <param name="value">The property value.</param>
		public override void SetProperty(string propertyName, int vid, object value)
		{
			if (SolveState != 0)
			{
				throw new InvalidSolverPropertyException(Resources.ThisSolverDoesNotSupportSettingAPropertyWhileSolving, InvalidSolverPropertyReason.EventDoesNotSupportSetProperty);
			}
			ValidateInSolveState(propertyName);
			base.SetProperty(propertyName, vid, value);
		}

		/// <summary>Get the value of a property.
		/// </summary>
		/// <param name="propertyName">The name of the property to get.</param>
		/// <param name="vid">An index for the item of interest.</param>
		/// <returns>The property value as a System.Object.</returns>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidSolverPropertyException"></exception>
		public override object GetProperty(string propertyName, int vid)
		{
			ValidateInSolveState(propertyName);
			if (propertyName == SolverProperties.IterationCount)
			{
				return _state.Iter;
			}
			if (propertyName == SolverProperties.GoalValue)
			{
				return _state.ModifiedValue;
			}
			if (propertyName == SolverProperties.SolveState)
			{
				return SolveState;
			}
			if (propertyName == CompactQuasiNewtonProperties.CurrentTerminationCriterion)
			{
				return _terminationCriterion.CurrentTolerance;
			}
			if (propertyName == CompactQuasiNewtonProperties.EvaluationCount)
			{
				return _state.EvaluationCount;
			}
			return base.GetProperty(propertyName, vid);
		}

		/// <summary>Return the value of a variable.
		/// </summary>
		/// <param name="vid">A variable id.</param>
		/// <returns>The variable value.</returns>
		double INonlinearSolution.GetValue(int vid)
		{
			return GetValue(vid).ToDouble();
		}

		/// <summary>Get the objective value of a goal.
		/// </summary>
		/// <param name="goalIndex">goal id</param>
		public double GetSolutionValue(int goalIndex)
		{
			if (goalIndex != 0 || !SolveCalled() || !IsUsefulSolution(_quality))
			{
				return double.NaN;
			}
			return GetValue(base.TheGoal.Index).ToDouble();
		}

		/// <summary> Get information about a solved goal.
		/// </summary>
		/// <param name="goalIndex"> 0 &lt;= goal index &lt; SolvedGoalCount </param>
		/// <param name="key">The goal row key</param>
		/// <param name="vid">The goal row vid</param>
		/// <param name="minimize">Whether the goal is to minimize</param>
		/// <param name="optimal">Whether the goal is optimal</param>
		public void GetSolvedGoal(int goalIndex, out object key, out int vid, out bool minimize, out bool optimal)
		{
			if (goalIndex != 0)
			{
				throw new ArgumentOutOfRangeException("goalIndex");
			}
			IGoal theGoal = base.TheGoal;
			key = theGoal.Key;
			vid = theGoal.Index;
			minimize = theGoal.Minimize;
			optimal = SolveCalled() && _quality == CompactQuasiNewtonSolutionQuality.LocalOptima;
		}

		private static NonlinearResult CqnSolutionQualityToNonlinearResult(CompactQuasiNewtonSolutionQuality cqnQuality)
		{
			switch (cqnQuality)
			{
			case CompactQuasiNewtonSolutionQuality.LocalOptima:
				return NonlinearResult.LocalOptimal;
			case CompactQuasiNewtonSolutionQuality.UserCalculationError:
			case CompactQuasiNewtonSolutionQuality.LinearObjective:
			case CompactQuasiNewtonSolutionQuality.MaxIterationExceeded:
				return NonlinearResult.Invalid;
			case CompactQuasiNewtonSolutionQuality.Unbounded:
				return NonlinearResult.Unbounded;
			case CompactQuasiNewtonSolutionQuality.Interrupted:
				return NonlinearResult.Interrupted;
			default:
				throw new NotSupportedException();
			}
		}

		/// <summary>Generate a report
		/// </summary>
		/// <param name="context">The SolverContext.</param>
		/// <param name="solution">The Solution.</param>
		/// <param name="solutionMapping">A SolutionMapping instance.</param>
		/// <returns>Report for model solved by CompactQuasiNewtonSolver</returns>
		public Report GetReport(SolverContext context, Solution solution, SolutionMapping solutionMapping)
		{
			PluginSolutionMapping pluginSolutionMapping = solutionMapping as PluginSolutionMapping;
			if (pluginSolutionMapping == null && solutionMapping != null)
			{
				throw new ArgumentException(Resources.SolutionMappingArgumentIsNotAPluginSolutionMappingObject, "solutionMapping");
			}
			return new CompactQuasiNewtonReport(context, this, solution, pluginSolutionMapping);
		}
	}
}
