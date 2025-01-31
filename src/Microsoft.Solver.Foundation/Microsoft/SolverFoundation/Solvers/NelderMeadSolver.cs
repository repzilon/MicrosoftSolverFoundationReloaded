using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>Finds a local minimum/maximum for an box-constrained nonlinear function.
	/// </summary>
	/// <remarks>
	/// NelderMeadSolver is used to find local minima or maxima for a function whose variables may be constrained to be in a range. 
	/// It does not require computing derivatives so it can be used in cases where other techniques cannot. NelderMeadSolver implements
	/// the INonlinearSolver interface, and the goal function is specified using the FunctionEvaluator property.
	/// NelderMeadSolver implements the method described in Nelder, J.A. and Mead, R., "A Simplex Method for Function Minimization", Computer Journal 7 (4): 308-313 (Jan., 1965)
	/// with the modifications described in Lee, D. and Wiswall, M., 
	/// "A Parallel Implementation of the Simplex Function Minimization Routine".
	/// </remarks>
	public class NelderMeadSolver : UnconstrainedNonlinearModel, INonlinearSolver, IRowVariableSolver, ISolver, INonlinearModel, IRowVariableModel, IGoalModel, INonlinearSolution, ISolverSolution, ISolverProperties, IReportProvider
	{
		/// <summary>Helps to map vid to solver index.
		/// </summary>
		internal class NelderMeadValuesByIndex : ValuesByIndex
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

			/// <summary>Creates a new instance.
			/// </summary>
			public NelderMeadValuesByIndex()
			{
			}

			internal void SetSolverValues(double[] solverValues)
			{
				base.SolverValues = solverValues;
			}
		}

		private const int RetryMax = 10;

		internal static double MinimumToleranceEps = 1E-10;

		private NonlinearResult _result;

		private NelderMeadValuesByIndex[] _valueByIndex;

		private double _alpha = 1.0;

		private double _beta = 0.5;

		private double _gamma = 2.0;

		private double _tau = 0.5;

		private int _iterationCount;

		private int _waitIterations = 1;

		private int _smallSimplexCount;

		private bool _restartOnRetry;

		private double _lastOptimalObjective = double.MaxValue;

		private int _evaluationCallCount;

		private double _objective = double.NaN;

		private double _tolerance;

		private double _goalSign = 1.0;

		private int[] _strategyCount;

		private Rational[] _varLo;

		private Rational[] _varHi;

		private bool _variablesAreConstrained;

		/// <summary>The number of Nelder-Mead iterations.
		/// </summary>
		public int IterationCount => _iterationCount;

		/// <summary>Number of times the expanded point was accepted.
		/// </summary>
		public int AcceptedExpansionsCount => _strategyCount[0];

		/// <summary>Number of times the expanded point was rejected (using the reflected point).
		/// </summary>
		public int RejectedExpansionsCount => _strategyCount[1];

		/// <summary>Number of times the contracted point was rejected (regenerating the simplex).
		/// </summary>
		public int RejectedContractionsCount => _strategyCount[2];

		/// <summary>Number of times the contracted point was accepted.
		/// </summary>
		public int AcceptedContractionsCount => _strategyCount[3];

		/// <summary>Number of times the reflected point was accepted.
		/// </summary>
		/// <remarks>If the reflected point was rejected then we attempt to contract, either
		/// rejecting or accepting the contracted point.
		/// </remarks>
		public int AcceptedReflectionsCount => _strategyCount[4];

		/// <summary>Number of times a small simplex was encountered.
		/// </summary>
		public int SmallSimplexCount => _smallSimplexCount;

		/// <summary>The capabilities for this solver.
		/// </summary>
		public NonlinearCapabilities NonlinearCapabilities => NonlinearCapabilities.SupportsBoundedVariables;

		/// <summary>Gradient capabilities.
		/// </summary>
		public DerivativeCapability GradientCapability => DerivativeCapability.NotSupported;

		/// <summary>Hessian capabilities.
		/// </summary>
		public DerivativeCapability HessianCapability => DerivativeCapability.NotSupported;

		/// <summary>Number of goals being solved.
		/// </summary>
		public int SolvedGoalCount => 1;

		/// <summary>The number of function evaluations performed for the most recent solve.
		/// </summary>
		public int EvaluationCallCount
		{
			get
			{
				if (SolveCalled())
				{
					return _evaluationCallCount;
				}
				return 0;
			}
		}

		/// <summary>Indicates the type of result (e.g., LocalOptimal). 
		/// </summary>
		public NonlinearResult Result
		{
			get
			{
				if (SolveCalled())
				{
					return _result;
				}
				return NonlinearResult.Invalid;
			}
		}

		/// <summary>Creates a new instance.
		/// </summary>
		public NelderMeadSolver()
			: this(null)
		{
		}

		/// <summary>Creates a new instance.
		/// </summary>
		/// <param name="comparer">A key comparer</param>
		public NelderMeadSolver(IEqualityComparer<object> comparer)
			: base(comparer)
		{
			_strategyCount = new int[5];
			_varLo = new Rational[0];
			_varHi = new Rational[0];
		}

		/// <summary>Finds the minimum value of the specified function, using the specified starting point.
		/// </summary>
		/// <param name="f">The function to minimize.</param>
		/// <param name="x0">The initial starting point for the search.</param>
		/// <returns>An INonlinearSolution instance.</returns>
		public static INonlinearSolution Solve(Func<double[], double> f, double[] x0)
		{
			return Solve(f, x0, null, null);
		}

		/// <summary>Finds the minimum value of the specified function, using the specified starting point and variable bounds.
		/// </summary>
		/// <param name="f">The function to minimize.</param>
		/// <param name="x0">The initial starting point for the search.</param>
		/// <param name="xLo">The lower bounds on the variables (optional, default is negative infinity).</param>
		/// <param name="xHi">The upper bounds on the variables (optional, default is infinity).</param>
		/// <returns>An INonlinearSolution instance.</returns>
		public static INonlinearSolution Solve(Func<double[], double> f, double[] x0, double[] xLo, double[] xHi)
		{
			NelderMeadSolver nelderMeadSolver = new NelderMeadSolver(null);
			nelderMeadSolver.AddRow(null, out var vid);
			nelderMeadSolver.AddGoal(vid, 0, minimize: true);
			for (int i = 0; i < x0.Length; i++)
			{
				nelderMeadSolver.AddVariable(null, out var vid2);
				nelderMeadSolver.SetValue(vid2, x0[i]);
				if (xLo != null)
				{
					nelderMeadSolver.SetLowerBound(vid2, xLo[i]);
				}
				if (xHi != null)
				{
					nelderMeadSolver.SetUpperBound(vid2, xHi[i]);
				}
			}
			double[] x1 = (double[])x0.Clone();
			nelderMeadSolver.FunctionEvaluator = delegate(INonlinearModel model, int rowVid, ValuesByIndex values, bool newValues)
			{
				for (int j = 0; j < x1.Length; j++)
				{
					x1[j] = values[j + 1];
				}
				return f(x1);
			};
			NelderMeadSolverParams parameters = new NelderMeadSolverParams();
			return nelderMeadSolver.Solve(parameters);
		}

		/// <summary>Determines if the specified value is a valid solver tolerance.
		/// </summary>
		/// <param name="value">A proposed tolerance value.</param>
		/// <returns>True if the value is within the valid limits.</returns>
		public static bool IsValidTolerance(double value)
		{
			if (value >= MinimumToleranceEps && !double.IsNaN(value))
			{
				return !double.IsPositiveInfinity(value);
			}
			return false;
		}

		/// <summary>Set a property for the specified index.
		/// </summary>
		/// <param name="propertyName">The name of the property to set, see SolverProperties.</param>
		/// <param name="vid">The variable index.</param>
		/// <param name="value">The value.</param>
		/// <exception cref="T:System.ArgumentNullException">The property name is null.</exception>
		/// <exception cref="T:System.ArgumentException">The variable index is invalid.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidSolverPropertyException">The property is not supported. The Reason property indicates why the property is not supported.</exception>
		/// <remarks> This method is typically called by Solver Foundation Services in response to event handler code.
		/// </remarks>
		public void SetProperty(string propertyName, int vid, object value)
		{
			ValidateInSolveState(propertyName);
			if (propertyName == NelderMeadProperties.CurrentTerminationCriterion)
			{
				_tolerance = Rational.ConvertToRational(value).ToDouble();
				if (!IsValidTolerance(_tolerance))
				{
					throw new ArgumentOutOfRangeException(NelderMeadProperties.CurrentTerminationCriterion, _tolerance, Resources.ToleranceTooLow);
				}
				return;
			}
			if (propertyName == NelderMeadProperties.EvaluationCount || propertyName == SolverProperties.IterationCount || propertyName == SolverProperties.GoalValue)
			{
				throw new InvalidSolverPropertyException(Resources.ThisSolverDoesNotSupportSettingAPropertyWhileSolving, InvalidSolverPropertyReason.EventDoesNotSupportSetProperty);
			}
			if (propertyName == SolverProperties.VariableLowerBound)
			{
				if (SolveCalled())
				{
					throw new InvalidSolverPropertyException(Resources.ThisSolverDoesNotSupportSettingAPropertyWhileSolving, InvalidSolverPropertyReason.EventDoesNotSupportSetProperty);
				}
				SetLowerBound(vid, Rational.ConvertToRational(value));
				return;
			}
			if (propertyName == SolverProperties.VariableUpperBound)
			{
				if (SolveCalled())
				{
					throw new InvalidSolverPropertyException(Resources.ThisSolverDoesNotSupportSettingAPropertyWhileSolving, InvalidSolverPropertyReason.EventDoesNotSupportSetProperty);
				}
				SetUpperBound(vid, Rational.ConvertToRational(value));
				return;
			}
			if (propertyName == SolverProperties.VariableStartValue)
			{
				if (SolveCalled())
				{
					throw new InvalidSolverPropertyException(Resources.ThisSolverDoesNotSupportSettingAPropertyWhileSolving, InvalidSolverPropertyReason.EventDoesNotSupportSetProperty);
				}
				SetValue(vid, Rational.ConvertToRational(value));
				return;
			}
			throw new InvalidSolverPropertyException(string.Format(CultureInfo.InvariantCulture, Resources.PropertyNameIsNotSupported0, new object[1] { propertyName }), InvalidSolverPropertyReason.InvalidPropertyName);
		}

		/// <summary>Get a property for the specified index.
		/// </summary>
		/// <param name="propertyName">The name of the property to get, see SolverProperties.</param>
		/// <param name="vid">The variable index.</param>
		/// <returns>The value.</returns>
		/// <exception cref="T:System.ArgumentNullException">The property name is null.</exception>
		/// <exception cref="T:System.ArgumentException">The variable index is invalid.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidSolverPropertyException">The property is not supported. The Reason property indicates why the property is not supported.</exception>
		/// <remarks> This method is typically called by Solver Foundation Services in response to event handler code.
		/// </remarks>
		public object GetProperty(string propertyName, int vid)
		{
			ValidateInSolveState(propertyName);
			Rational numLo;
			Rational numHi;
			if (propertyName == SolverProperties.VariableLowerBound)
			{
				if (IsRow(vid))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
				}
				GetBounds(vid, out numLo, out numHi);
				return numLo;
			}
			if (propertyName == SolverProperties.VariableUpperBound)
			{
				if (IsRow(vid))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
				}
				GetBounds(vid, out numLo, out numHi);
				return numHi;
			}
			if (propertyName == SolverProperties.VariableStartValue)
			{
				if (IsRow(vid))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
				}
				return GetValue(vid);
			}
			if (propertyName == NelderMeadProperties.CurrentTerminationCriterion)
			{
				return _tolerance;
			}
			if (propertyName == NelderMeadProperties.EvaluationCount)
			{
				return _evaluationCallCount;
			}
			if (propertyName == SolverProperties.IterationCount)
			{
				return _iterationCount;
			}
			if (propertyName == SolverProperties.GoalValue)
			{
				return GetSolutionValue(0);
			}
			throw new InvalidSolverPropertyException(string.Format(CultureInfo.InvariantCulture, Resources.PropertyNameIsNotSupported0, new object[1] { propertyName }), InvalidSolverPropertyReason.InvalidPropertyName);
		}

		/// <summary>
		/// If not in solving state and property is one that solver supports, throw
		/// </summary>
		private void ValidateInSolveState(string propertyName)
		{
			if (!SolveCalled() && (propertyName == NelderMeadProperties.EvaluationCount || propertyName == NelderMeadProperties.CurrentTerminationCriterion || propertyName == SolverProperties.GoalValue || propertyName == SolverProperties.IterationCount))
			{
				throw new InvalidSolverPropertyException(string.Format(CultureInfo.InvariantCulture, Resources.Property0CanOnlyBeAccessedBySolvingEventHandlers, new object[1] { propertyName }), InvalidSolverPropertyReason.EventDoesNotSupportProperty);
			}
		}

		/// <summary>
		/// The AddVariable method ensures that a user variable with the given key is in the model.
		/// </summary>
		/// <remarks>
		/// If the model already includes a user variable referenced by key, this sets vid to the variable’s index 
		/// and returns false. Otherwise, if the model already includes a row referenced by key, this sets vid to -1 and returns false. 
		/// Otherwise, this adds a new user variable associated with key to the model, assigns the next available index to the new variable, 
		/// sets vid to this index, and returns true.
		/// By convention variables get indexes from 1 ... VariableCount in the order they were added.
		/// </remarks>
		/// <param name="key">Variable key.</param>
		/// <param name="vid">Variable index.</param>
		/// <returns>True if added successfully, otherwise false.</returns>
		public override bool AddVariable(object key, out int vid)
		{
			bool flag = base.AddVariable(key, out vid);
			if (flag)
			{
				Statics.EnsureArraySize(ref _varLo, base.VariableCount);
				ref Rational reference = ref _varLo[vid - 1];
				reference = Rational.NegativeInfinity;
				Statics.EnsureArraySize(ref _varHi, base.VariableCount);
				ref Rational reference2 = ref _varHi[vid - 1];
				reference2 = Rational.PositiveInfinity;
			}
			return flag;
		}

		/// <summary>Set or adjust upper and lower bounds for a vid.
		/// </summary>
		/// <param name="vid">The variable index.</param>
		/// <param name="numLo">The lower bound.</param>
		/// <param name="numHi">The upper bound.</param>
		/// <remarks>Not supported by unconstrained solvers.  Logically, a vid may have an upper bound of Infinity and/or a lower 
		/// bound of -Infinity. Specifying other non-finite values should be avoided. 
		/// If a vid has a lower bound that is greater than its upper bound, the model is automatically infeasible, 
		/// and ArgumentException is thrown.  
		/// </remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported for goal rows.</exception>
		/// <exception cref="T:System.ArgumentException">Thrown if upper and lower bounds are incompatible.</exception>
		public override void SetBounds(int vid, Rational numLo, Rational numHi)
		{
			PreChange();
			ValidateVid(vid);
			if (!IsRow(vid))
			{
				ValidateBounds(numLo, numHi);
				int variableListIndex = UnconstrainedNonlinearModel.GetVariableListIndex(vid);
				_varLo[variableListIndex] = numLo;
				_varHi[variableListIndex] = numHi;
				_variablesAreConstrained = true;
				return;
			}
			throw new NotSupportedException();
		}

		/// <summary>
		/// Set or adjust the lower bound for a vid.
		/// </summary>
		/// <param name="vid">The variable index.</param>
		/// <param name="numLo">The lower bound.</param>
		/// <remarks>Not supported by unconstrained solvers.</remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported for goal rows.</exception>
		/// <exception cref="T:System.ArgumentException">Thrown if upper and lower bounds are incompatible.</exception>
		public override void SetLowerBound(int vid, Rational numLo)
		{
			PreChange();
			ValidateVid(vid);
			if (!IsRow(vid))
			{
				int variableListIndex = UnconstrainedNonlinearModel.GetVariableListIndex(vid);
				Rational numHi = _varHi[variableListIndex];
				ValidateBounds(numLo, numHi);
				_varLo[variableListIndex] = numLo;
				_variablesAreConstrained = true;
				return;
			}
			throw new NotSupportedException();
		}

		/// <summary>
		/// Set or adjust the upper bound for a vid. 
		/// </summary>
		/// <param name="vid">The variable index.</param>
		/// <param name="numHi">The upper bound.</param>
		/// <remarks>Not supported by unconstrained solvers.</remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported for goal rows.</exception>
		/// <exception cref="T:System.ArgumentException">Thrown if upper and lower bounds are incompatible.</exception>
		public override void SetUpperBound(int vid, Rational numHi)
		{
			PreChange();
			ValidateVid(vid);
			if (!IsRow(vid))
			{
				int variableListIndex = UnconstrainedNonlinearModel.GetVariableListIndex(vid);
				Rational numLo = _varLo[variableListIndex];
				ValidateBounds(numLo, numHi);
				_varHi[variableListIndex] = numHi;
				_variablesAreConstrained = true;
				return;
			}
			throw new NotSupportedException();
		}

		/// <summary>
		/// Return the bounds for a vid.
		/// </summary>
		/// <param name="vid">The variable index.</param>
		/// <param name="numLo">The current lower bound.</param>
		/// <param name="numHi">The current upper bound.</param>
		/// <remarks>Not supported for goal rows.</remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported by unconstrained solvers.</exception>
		public override void GetBounds(int vid, out Rational numLo, out Rational numHi)
		{
			ValidateVid(vid);
			int variableListIndex = UnconstrainedNonlinearModel.GetVariableListIndex(vid);
			if (!IsRow(vid))
			{
				numLo = _varLo[variableListIndex];
				numHi = _varHi[variableListIndex];
				return;
			}
			throw new NotSupportedException();
		}

		/// <summary>Solve the model using the given parameter instance.
		/// </summary>
		/// <param name="parameters">Should be an instance of NelderMeadSolverParams.</param>
		/// <returns>The solution after solving.</returns>
		/// <exception cref="T:System.ArgumentNullException">Parameters should not be null.</exception>
		/// <exception cref="T:System.ArgumentException">Parameters should be of type NelderMeadSolverParams.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">FunctionEvaluator must be specified before calling this method.</exception>
		public INonlinearSolution Solve(ISolverParameters parameters)
		{
			if (!(parameters is NelderMeadSolverParams nelderMeadSolverParams))
			{
				throw new ArgumentNullException(Resources.InvalidParams);
			}
			if (base.FunctionEvaluator == null)
			{
				throw new ModelException(Resources.SolverRequiresFunctionEvaluatorToBeSpecifiedBeforeCallingSolve);
			}
			if (base.TheGoal == null)
			{
				throw new ModelException(Resources.TheModelMustContainAGoal);
			}
			_tolerance = nelderMeadSolverParams.Tolerance;
			_result = NonlinearResult.Feasible;
			Vector startingPoint = GetStartingPoint(nelderMeadSolverParams.StartMethod);
			_strategyCount.ConstantFill(0);
			Tuple<double, double[]> tuple = SolveCore(startingPoint, nelderMeadSolverParams);
			SetGoalValue(tuple.Item1);
			CopyVariableValuesFrom(tuple.Item2);
			return this;
		}

		/// <summary>Shutdown.
		/// </summary>
		public void Shutdown()
		{
		}

		/// <summary>Get the objective value of a goal.
		/// </summary>
		/// <param name="goalIndex">goal id</param>
		public double GetSolutionValue(int goalIndex)
		{
			if (goalIndex != 0 || !SolveCalled())
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
			optimal = SolveCalled() && _result == NonlinearResult.LocalOptimal;
		}

		/// <summary>Return the value of a variable.
		/// </summary>
		/// <param name="vid">A variable id.</param>
		/// <returns>The variable value.</returns>
		double INonlinearSolution.GetValue(int vid)
		{
			return GetValue(vid).ToDouble();
		}

		/// <summary>Generate a report.
		/// </summary>
		/// <param name="context">Solver context.</param>
		/// <param name="solution">The solution.</param>
		/// <param name="solutionMapping">The solution mapping.</param>
		/// <returns>A report object.</returns>
		public Report GetReport(SolverContext context, Solution solution, SolutionMapping solutionMapping)
		{
			PluginSolutionMapping pluginSolutionMapping = solutionMapping as PluginSolutionMapping;
			if (pluginSolutionMapping == null && solutionMapping != null)
			{
				throw new ArgumentException(Resources.SolutionMappingArgumentIsNotAPluginSolutionMappingObject, "solutionMapping");
			}
			return new NelderMeadReport(context, this, solution, pluginSolutionMapping);
		}

		private Tuple<double, double[]> SolveCore(Vector x0, NelderMeadSolverParams nmParams)
		{
			int num = ((nmParams.TerminationSensitivity != NelderMeadTerminationSensitivity.Aggressive) ? 10 : 0);
			int num2 = Math.Max(Math.Min(nmParams.MaximumSearchPoints, x0.Length - 1), 1);
			InitSolve(num2);
			int length = x0.Length;
			Vector vector = new Vector(length + 1);
			Vector vector2 = new Vector(num2);
			Vector[] array = CreateVectors(length + 1, length);
			Vector vector3 = new Vector(length);
			Vector[] array2 = CreateVectors(num2, length);
			Vector[] array3 = CreateVectors(num2, length);
			int num3 = 0;
			GenerateSimplexFromPoint(x0, array, vector);
			while (true)
			{
				_iterationCount++;
				CallSolvingEvent(nmParams);
				SortByAscendingObjectiveValue(array, vector);
				Centroid(array, num2, vector3);
				double num4 = Evaluate(vector3);
				_objective = _goalSign * vector[0];
				SetGoalValue(_objective);
				_waitIterations--;
				if (SimplexIsSmall(vector, num4, array, vector3, _tolerance) && _waitIterations <= 0)
				{
					if (++_smallSimplexCount >= num || !(Math.Abs(_lastOptimalObjective - vector[0]) > _tolerance))
					{
						_result = NonlinearResult.LocalOptimal;
						return new Tuple<double, double[]>(_objective, array[0].ToArray());
					}
					Retry(x0, vector, array);
				}
				if (_iterationCount >= nmParams.IterationLimit || nmParams.ShouldAbort())
				{
					_result = NonlinearResult.Interrupted;
					return new Tuple<double, double[]>(_objective, array[0].ToArray());
				}
				if (double.IsNaN(num4) || double.IsInfinity(num4) || num4 < 0.0 - nmParams.UnboundedTolerance || num4 > nmParams.UnboundedTolerance)
				{
					break;
				}
				num3 = 0;
				SnapshotObjectiveValues(vector, vector2);
				for (int i = 0; i < num2; i++)
				{
					int num5 = length - i;
					Reflect(vector3, array[num5], array3[i]);
					double num6 = Evaluate(array3[i], i);
					if (num6 < vector[0])
					{
						Expand(array3[i], vector3, array2[i]);
						double num7 = Evaluate(array2[i], i);
						if (num7 < num6)
						{
							ReplacePoint(array, vector, num5, array2[i], num7);
							CallSolvingEvent(nmParams);
							_strategyCount[0]++;
						}
						else
						{
							ReplacePoint(array, vector, num5, array3[i], num6);
							CallSolvingEvent(nmParams);
							_strategyCount[1]++;
						}
						continue;
					}
					double num8 = vector2[i];
					if (num6 > num8)
					{
						double num9 = vector[num5];
						if (num9 > num6)
						{
							ReplacePoint(array, vector, num5, array3[i], num6);
							num9 = num6;
						}
						Contract(array[num5], vector3, array2[i]);
						double num10 = Evaluate(array2[i], i);
						if (num10 > num9)
						{
							Interlocked.Increment(ref num3);
							continue;
						}
						ReplacePoint(array, vector, num5, array2[i], num10);
						_strategyCount[3]++;
					}
					else
					{
						ReplacePoint(array, vector, num5, array3[i], num6);
						_strategyCount[4]++;
					}
				}
				if (num3 == num2)
				{
					_strategyCount[2]++;
					for (int j = 1; j < array.Length; j++)
					{
						Vector.ScaledSum(_tau, array[0], 1.0 - _tau, array[j], array[j]);
					}
					MapEvaluate(array, vector, 1);
				}
			}
			_result = NonlinearResult.Unbounded;
			_objective = ((_goalSign > 0.0) ? double.NegativeInfinity : double.PositiveInfinity);
			return new Tuple<double, double[]>(_objective, array[0].ToArray());
		}

		private static void SnapshotObjectiveValues(Vector y, Vector yPrevious)
		{
			int length = y.Length;
			for (int i = 0; i < yPrevious.Length; i++)
			{
				int i2 = length - i - 2;
				yPrevious[i] = y[i2];
			}
		}

		private Vector GetStartingPoint(NelderMeadStartMethod startMethod)
		{
			double[] array = new double[base.VariableCount];
			CopyVariableValuesTo(array, 0.0);
			Vector vector = new Vector(array);
			Project(vector);
			return vector;
		}

		private bool SolveCalled()
		{
			return _result != NonlinearResult.Invalid;
		}

		private void Retry(Vector x0, Vector y, Vector[] p)
		{
			_lastOptimalObjective = y[0];
			int length = x0.Length;
			if (_restartOnRetry)
			{
				x0.CopyFrom(p[0]);
				GenerateSimplexFromPointSimple(x0, p, y);
			}
			else
			{
				_waitIterations = 10 * (int)((double)length * Math.Log(length) + 1.0);
			}
			_restartOnRetry = !_restartOnRetry;
		}

		private static Vector[] CreateVectors(int vectorCount, int vectorSize)
		{
			Vector[] array = new Vector[vectorCount];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new Vector(vectorSize);
			}
			return array;
		}

		private void InitSolve(int searchPoints)
		{
			_goalSign = (base.TheGoal.Minimize ? 1 : (-1));
			_iterationCount = 0;
			_waitIterations = 1;
			_smallSimplexCount = 0;
			_restartOnRetry = false;
			_evaluationCallCount = 0;
			_objective = double.NaN;
			_lastOptimalObjective = double.MaxValue;
			_valueByIndex = new NelderMeadValuesByIndex[searchPoints];
			for (int i = 0; i < _valueByIndex.Length; i++)
			{
				_valueByIndex[i] = new NelderMeadValuesByIndex();
			}
		}

		private static void SortByAscendingObjectiveValue(Vector[] p, Vector y)
		{
			Statics.QuadSort(y.ToArray(), p, 0, y.Length - 1);
		}

		private void GenerateSimplexFromPoint(Vector x0, Vector[] p, Vector y)
		{
			double num = SimplexScaling(x0);
			int length = x0.Length;
			double num2 = num / ((double)length * Math.Sqrt(2.0));
			double num3 = num2 * (Math.Sqrt(length + 1) + (double)length - 1.0);
			double num4 = num2 * (Math.Sqrt(length + 1) - 1.0);
			for (int i = 0; i < length; i++)
			{
				p[i].CopyFrom(x0);
				Vector vector = p[i];
				for (int j = 0; j < length; j++)
				{
					if (j != i)
					{
						vector[j] += num4;
					}
					else
					{
						vector[j] += num3;
					}
				}
			}
			p[length].CopyFrom(x0);
			for (int k = 0; k < p.Length; k++)
			{
				Project(p[k]);
			}
			MapEvaluate(p, y, 0);
		}

		private void GenerateSimplexFromPointSimple(Vector x0, Vector[] p, Vector y)
		{
			double num = SimplexScaling(x0);
			int length = x0.Length;
			for (int i = 0; i < length; i++)
			{
				p[i].CopyFrom(x0);
				p[i][i] += num;
			}
			p[length].CopyFrom(x0);
			for (int j = 0; j < p.Length; j++)
			{
				Project(p[j]);
			}
			MapEvaluate(p, y, 0);
		}

		private static double SimplexScaling(Vector x0)
		{
			double num = x0.Min();
			double num2 = x0.Max();
			int num3 = Math.Sign(num2);
			if (Math.Abs(num2) < Math.Abs(num))
			{
				num3 = Math.Sign(num);
				num2 = num;
			}
			if (num3 == 0)
			{
				num3 = 1;
			}
			return (double)num3 * 0.05 * Math.Max(num2, 0.5);
		}

		private static void Centroid(Vector[] p, int excludeLast, Vector pC)
		{
			int num = p.Length - excludeLast;
			pC.CopyFrom(p[0]);
			for (int i = 1; i < num; i++)
			{
				pC.Add(p[i]);
			}
			pC.ScaleBy(1.0 / (double)num);
		}

		private static void CallSolvingEvent(NelderMeadSolverParams nmParams)
		{
			if (nmParams.Solving != null)
			{
				nmParams.Solving();
			}
		}

		private void Reflect(Vector pC, Vector pH, Vector pR)
		{
			Vector.ScaledSum(1.0 + _alpha, pC, 0.0 - _alpha, pH, pR);
			Project(pR);
		}

		private void Expand(Vector pR, Vector pC, Vector pE)
		{
			Vector.ScaledSum(_gamma, pR, 1.0 - _gamma, pC, pE);
			Project(pE);
		}

		private void Contract(Vector pH, Vector pC, Vector pX)
		{
			Vector.ScaledSum(_beta, pH, 1.0 - _beta, pC, pX);
		}

		/// <summary>
		/// Ensures that the variable vector p is feasible.
		/// </summary>
		/// <param name="p">A vector of variable values.</param>
		private void Project(Vector p)
		{
			if (!_variablesAreConstrained)
			{
				return;
			}
			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] > _varHi[i])
				{
					p[i] = _varHi[i].ToDouble();
				}
				else if (p[i] < _varLo[i])
				{
					p[i] = _varLo[i].ToDouble();
				}
			}
		}

		private static bool SimplexIsSmall(Vector y, double yC, Vector[] p, Vector pC, double tolerance)
		{
			double num = 0.0;
			for (int i = 0; i < y.Length; i++)
			{
				double num2 = y[i] - yC;
				num += num2 * num2;
			}
			return Math.Sqrt(num / (double)y.Length) <= tolerance;
		}

		private static void ReplacePoint(Vector[] p, Vector y, int n, Vector pE, double yE)
		{
			p[n].CopyFrom(pE);
			y[n] = yE;
		}

		/// <summary>
		/// Evaluate the objective function at a series of points.
		/// </summary>
		private void MapEvaluate(Vector[] p, Vector y, int start)
		{
			for (int i = start; i < p.Length; i++)
			{
				y[i] = Evaluate(p[i]);
			}
		}

		/// <summary>
		/// Evaluate the objective function at a point.
		/// </summary>
		/// <param name="x">The point.</param>
		/// <param name="slot">Determines which ValuesByIndex to use (for parallel evaluation - use 0 if N/A).</param>
		/// <returns>The objective value.</returns>
		private double Evaluate(Vector x, int slot = 0)
		{
			_evaluationCallCount++;
			_valueByIndex[slot].SetSolverValues(x.ToArray());
			return _goalSign * base.FunctionEvaluator(this, base.TheGoal.Index, _valueByIndex[slot], arg4: true);
		}

		private static void ValidateBounds(Rational numLo, Rational numHi)
		{
			if (numLo.IsIndeterminate || numHi.IsIndeterminate || numHi < numLo)
			{
				throw new ArgumentException(Resources.InvalidBounds);
			}
		}
	}
}
