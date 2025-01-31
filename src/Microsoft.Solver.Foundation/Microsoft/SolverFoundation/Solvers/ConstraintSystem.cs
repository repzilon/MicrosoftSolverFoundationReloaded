#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Main class that contains model construction and solving APIs
	/// </summary>
	public sealed class ConstraintSystem : ISolver, IReportProvider
	{
		/// <summary>
		/// The model construction mode
		/// </summary>
		public enum ModelConstructionMode
		{
			/// <summary>
			/// Neither propagation nor search is performed. Load the model in full speed
			/// </summary>
			FullSpeed,
			/// <summary>
			/// Perform propagations after each constraint is added. Does not guarantee there is no conflicts
			/// </summary>
			DebugLight,
			/// <summary>
			/// Search for one solution after each constraint is added. Guarantees there is no conflicts
			/// </summary>
			DebugHeavy
		}

		[Flags]
		internal enum SolverKind
		{
			CspSolver = 1,
			IntegerSolver = 2
		}

		/// <summary> A composite key for a Symbol which is an element in a vector or an array.
		/// </summary>
		internal class ElementKey
		{
			private object _key;

			private int _index;

			/// <summary>
			/// Get the key for the variable vector or array
			/// </summary>
			public object Key => _key;

			/// <summary>
			/// Get the index that the key is associated with
			/// </summary>
			public int Index => _index;

			internal ElementKey(object aggregate, int index)
			{
				_key = aggregate;
				_index = index;
			}

			/// <summary> Generate a name for this object upon demand
			/// </summary>
			public override string ToString()
			{
				return _key.ToString() + Index.ToString(CultureInfo.InvariantCulture);
			}
		}

		internal const int _MinFinite = -1073741823;

		internal const int _MaxFinite = 1073741823;

		private bool _isPureIntegerModel;

		internal ModelConstructionMode _mode;

		internal static TraceSource TraceSource;

		internal static int TraceCnt;

		internal static bool IsTracing;

		private Dictionary<object, int> _elementKeySizes;

		private bool _isInterrupted;

		private int _baseChangeCount;

		internal bool _fIsInModelingPhase;

		internal IEqualityComparer<object> _comparer;

		internal List<KeyValuePair<CspSolverTerm, int>> _changes;

		internal List<CspSolverTerm> _variables;

		internal List<CspVariable> _variablesExcludingConstants;

		internal List<CspTerm> _allTerms;

		internal List<CspComposite> _composites;

		internal List<CspCompositeVariable> _compositeVariables;

		internal Dictionary<object, CspSolverTerm> _varDict;

		internal Dictionary<object, CspComposite> _compositeDict;

		internal Dictionary<object, CspVariable> _numConstants;

		internal Dictionary<double, CspVariable> _decConstants;

		internal Dictionary<CspSymbolDomain, Dictionary<string, CspSolverTerm>> _symbolConstants;

		internal Dictionary<object, CspCompositeVariable> _setConstants;

		internal Dictionary<object, CspCompositeVariable> _listConstants;

		internal List<CspSolverTerm> _constraints;

		internal List<CspSolverTerm> _minimizationGoals;

		internal List<CspUserVariable> _userVars;

		internal CspBooleanVariable _TFalse;

		internal CspBooleanVariable _TTrue;

		internal List<int> _scaleBuffer;

		internal bool _verbose;

		internal bool _fAbortDomainNarrowing;

		internal static CspSolverDomain DFinite;

		internal static CspSolverDomain DBoolean;

		internal static CspSolverDomain DFalse;

		internal static CspSolverDomain DTrue;

		internal static sEmptyDomain DEmpty;

		private Thread _domainNarrowingThread;

		private readonly int _defaultPrecision;

		private ConstraintSolverParams _params;

		internal Stopwatch _stopwatch;

		/// <summary>
		/// Propagator that propagates value changes in variables through the constraint network.
		/// </summary>
		internal AdaptiveLocalPropagation _propagator;

		/// <summary>
		/// Decider that maintains the decision stack during the backtrack search.
		/// </summary>
		internal CspSolver _baseSolver;

		internal TreeSearchStatistics _integerSolverStatistics;

		/// <summary>
		/// Term map that maps model terms to IntegerSolver terms. Created only when a clone ctor is called
		/// </summary>
		private Dictionary<CspTerm, CspTerm> _termMap;

		/// <summary> Counts the number of all terms created in this solver, including the ones created in Composites and Composites variables
		/// </summary>
		internal int _allTermCount;

		internal int _numUnequalTerms;

		internal int _numBooleanAndTerms;

		internal int _numBooleanOrTerms;

		private int _prngSeed;

		/// <summary> Callback for ending the solve
		/// </summary>
		public Func<bool> QueryAbort
		{
			get
			{
				return _params.QueryAbort;
			}
			set
			{
				_params.QueryAbort = value;
			}
		}

		/// <summary>
		/// Minimal integer value that CspSolver can use.
		/// </summary>
		public static int MinFinite => -1073741823;

		/// <summary>
		/// Maximal integer value that CspSolver can use.
		/// </summary>
		public static int MaxFinite => 1073741823;

		/// <summary> An CspIntervalDomain from the minimum to the maximum supported Finite value of the underlying Solver.
		/// </summary>
		public CspDomain DefaultInterval
		{
			[DebuggerStepThrough]
			get
			{
				return DFinite;
			}
		}

		/// <summary>
		/// A Boolean Domain that contains 0(false) and 1(true).
		/// </summary>
		public CspDomain DefaultBoolean
		{
			[DebuggerStepThrough]
			get
			{
				return DBoolean;
			}
		}

		/// <summary> The Domain with Count zero and any attempt to get its first or last values will
		///             throw InvalidOperationException.  Enumerations Forward or Backwards will be empty.
		/// </summary>
		public CspDomain Empty
		{
			[DebuggerStepThrough]
			get
			{
				return DEmpty;
			}
		}

		/// <summary>
		/// Get the global precision for real values in this solver obeject.
		/// </summary>
		public int Precision => _defaultPrecision;

		/// <summary> Create a set of IntegerSolverParams with default values and the specified
		///           function to use for asynchronous querying of Abort status.
		/// </summary>
		public ConstraintSolverParams Parameters => _params;

		/// <summary> Check if the Solver currently has been stopped by external request or such a request is pending.
		///           Set to True to request an asynchronous stop.  The stop request is checked at frequent safe points
		///             within the solver.
		///           Setting to False when the solver is running is undefined: the solver may already have seen the request
		///             but not finished acting on it.
		/// </summary>
		/// <remarks> The only guaranteed correct time to set False is prior to starting first solve. </remarks>
		public bool AbortDomainNarrowing
		{
			get
			{
				return _fAbortDomainNarrowing;
			}
			set
			{
				_fAbortDomainNarrowing = value;
			}
		}

		/// <summary> Enumerate the Variables that have been instantiated for this Solver.
		/// </summary>
		public IEnumerable<CspTerm> Variables
		{
			get
			{
				foreach (CspSolverTerm variable in _variables)
				{
					yield return variable;
				}
			}
		}

		/// <summary> Enumerate the Functions which have been added to this Solver as Constraints.
		/// </summary>
		public IEnumerable<CspTerm> Constraints
		{
			get
			{
				foreach (CspSolverTerm constraint in _constraints)
				{
					yield return constraint;
				}
			}
		}

		/// <summary> Enumerate the Functions which have been added to this Solver as Constraints.
		/// </summary>
		public IEnumerable<CspTerm> MinimizationGoals
		{
			get
			{
				foreach (CspSolverTerm minimizationGoal in _minimizationGoals)
				{
					yield return minimizationGoal;
				}
			}
		}

		/// <summary>
		/// Return true if and only if the model in the solver is empty
		/// </summary>
		public bool IsEmpty => AllTerms.Count == 0;

		/// <summary>
		/// Get the number of backtracks during search
		/// </summary>
		public int BacktrackCount
		{
			get
			{
				if (_integerSolverStatistics != null)
				{
					return _integerSolverStatistics.NbFails;
				}
				if (_baseSolver == null || !(_baseSolver is TreeSearchSolver))
				{
					return 0;
				}
				return (_baseSolver as TreeSearchSolver)._cBacktracks;
			}
		}

		/// <summary>
		/// Getter for solver version as a string.
		/// </summary>
		public static string Version => "M5";

		/// <summary> The Boolean Term {false} which is immutable and can be used anywhere you need a false constant.
		/// </summary>
		public CspTerm False => _TFalse;

		/// <summary> The Boolean Term {true} which is immutable and can be used anywhere you need a true constant.
		/// </summary>
		public CspTerm True => _TTrue;

		/// <summary>
		/// Get the number of minimization goals (&gt;= 0)
		/// </summary>
		public int GoalCount => _minimizationGoals.Count;

		/// <summary>
		/// Return if the last solve is interrupted or not
		/// </summary>
		/// <returns></returns>
		public bool IsInterrupted => _isInterrupted;

		/// <summary>
		/// Get or set the mode of the solver. If Debug* mode is set, then the solver will search for conflicts whenever a
		/// constraint is added.
		/// </summary>
		public ModelConstructionMode Mode
		{
			get
			{
				return _mode;
			}
			set
			{
				_mode = value;
			}
		}

		/// <summary>
		/// Get/set the flag that indicates whether the model is pure integer model (thus suitable for IntegerSolver)
		/// </summary>
		internal bool IsPureIntegerModel
		{
			get
			{
				return _isPureIntegerModel;
			}
			set
			{
				_isPureIntegerModel = value;
			}
		}

		internal Dictionary<CspTerm, CspTerm> TermMap => _termMap;

		/// <summary>
		/// Enumerate all terms that are constructed, including constants, variables (decisive and indecisive), and functions.
		/// </summary>
		internal List<CspTerm> AllTerms => _allTerms;

		internal bool Verbose
		{
			[DebuggerStepThrough]
			get
			{
				return _verbose;
			}
		}

		/// <summary> A Solver for Constraint problems on variables which have finite domains.
		/// </summary>
		static ConstraintSystem()
		{
			TraceSource = new TraceSource("CspSolverTrace");
			TraceCnt = 0;
			IsTracing = TraceSource.Switch.ShouldTrace(TraceEventType.Verbose);
			DFinite = CspIntervalDomain.Create(MinFinite, MaxFinite) as CspIntervalDomain;
			DBoolean = CspIntervalDomain.Create(0, 1);
			DFalse = CspIntervalDomain.Create(0, 0);
			DTrue = CspIntervalDomain.Create(1, 1);
			DEmpty = new sEmptyDomain();
		}

		/// <summary> A general purpose finite solver implementation capable of handling a mix of logic and
		///           non-linear math over domains up to +- 1 billion.  Create an IntegerSolverParams with
		///           default values and the specified function for asynchronous querying of Abort status.
		/// </summary>
		internal ConstraintSystem()
			: this(null, null, 100)
		{
		}

		/// <summary> A general purpose finite solver implementation capable of handling a mix of logic and
		///           non-linear math over domains up to +- 1 billion.  Create an IntegerSolverParams with
		///           default values and the specified function for asynchronous querying of Abort status.
		/// </summary>
		internal ConstraintSystem(Func<bool> fnQueryAbort, IEqualityComparer<object> cmp)
			: this(fnQueryAbort, cmp, 100)
		{
		}

		/// <summary> A general purpose finite solver implementation capable of handling a mix of logic and
		///           non-linear math over domains up to +- 1 billion.  Create an IntegerSolverParams with
		///           default values and the specified function for asynchronous querying of Abort status.
		/// </summary>
		internal ConstraintSystem(Func<bool> fnQueryAbort, IEqualityComparer<object> cmp, int precision)
		{
			ValidatePrecision(precision);
			_isPureIntegerModel = true;
			_mode = ModelConstructionMode.FullSpeed;
			_isInterrupted = false;
			_baseChangeCount = 0;
			_fIsInModelingPhase = true;
			_elementKeySizes = new Dictionary<object, int>();
			_comparer = cmp;
			_variables = new List<CspSolverTerm>();
			_variablesExcludingConstants = new List<CspVariable>();
			_allTerms = new List<CspTerm>();
			_compositeVariables = new List<CspCompositeVariable>();
			_varDict = new Dictionary<object, CspSolverTerm>(_comparer);
			_composites = new List<CspComposite>();
			_compositeDict = new Dictionary<object, CspComposite>(_comparer);
			_numConstants = new Dictionary<object, CspVariable>();
			_decConstants = new Dictionary<double, CspVariable>();
			_symbolConstants = new Dictionary<CspSymbolDomain, Dictionary<string, CspSolverTerm>>();
			_setConstants = new Dictionary<object, CspCompositeVariable>();
			_listConstants = new Dictionary<object, CspCompositeVariable>();
			_constraints = new List<CspSolverTerm>();
			_minimizationGoals = new List<CspSolverTerm>();
			_userVars = new List<CspUserVariable>();
			_changes = new List<KeyValuePair<CspSolverTerm, int>>();
			_scaleBuffer = new List<int>();
			_params = new ConstraintSolverParams();
			_params.QueryAbort = fnQueryAbort;
			_params.RestartEnabled = false;
			_domainNarrowingThread = null;
			_defaultPrecision = precision;
			_stopwatch = new Stopwatch();
			_TFalse = new CspBooleanVariable(this, CspSolverTerm.TermKinds.Constant, "ConstraintSystem.False");
			_TFalse.Force(choice: false, out var conflict);
			_TTrue = new CspBooleanVariable(this, CspSolverTerm.TermKinds.Constant, "ConstraintSystem.True");
			_TTrue.Force(choice: true, out conflict);
		}

		internal ConstraintSystem(ConstraintSystem solver)
			: this(solver.Parameters.QueryAbort, solver._comparer, solver._defaultPrecision)
		{
			_termMap = AppendModel(solver, this);
			_params = new ConstraintSolverParams(solver.Parameters, _termMap);
		}

		/// <summary> Shutdown the ConstraintSystem instance
		/// </summary>
		public void Shutdown()
		{
		}

		/// <summary>Generate a report
		/// </summary>
		/// <param name="context"></param>
		/// <param name="solution"></param>
		/// <param name="solutionMapping"></param>
		/// <returns></returns>
		public Report GetReport(SolverContext context, Solution solution, SolutionMapping solutionMapping)
		{
			if (!(solutionMapping is CspSolutionMapping solutionMapping2))
			{
				throw new ArgumentException("solutionMapping is not a CspSolutionMapping", "solutionMapping");
			}
			return new ConstraintProgrammingReport(context, this, solution, solutionMapping2);
		}

		/// <summary> Get a Term for the immutable value k
		/// </summary>
		public CspTerm Constant(int k)
		{
			CheckInModelingPhase();
			if (!_numConstants.TryGetValue(k, out var value))
			{
				value = new CspVariable(this, CspIntervalDomain.Create(k, k), CspSolverTerm.TermKinds.Constant, null);
				_numConstants.Add(k, value);
			}
			return value;
		}

		/// <summary> Get a Term for the immutable real value k with the default precision
		/// </summary>
		public CspTerm Constant(double k)
		{
			return Constant(Precision, k);
		}

		/// <summary> Get a Term for the immutable real value k
		/// <param name="precision">Only allows 1, 10, 100, 1000, and 1000</param>
		/// <param name="k">Value</param>
		/// </summary>
		public CspTerm Constant(int precision, double k)
		{
			CheckInModelingPhase();
			MarkNonIntegerModel();
			IsPureIntegerModel = false;
			ValidatePrecision(precision);
			if (!_decConstants.TryGetValue(k, out var value))
			{
				value = new CspVariable(this, CspIntervalDomain.Create(precision, k, k), CspSolverTerm.TermKinds.Constant, null);
				_decConstants.Add(k, value);
			}
			return value;
		}

		/// <summary>
		/// Get a Term for the immutable symbol value k
		/// </summary>
		/// <param name="valueSet">The domain that the constant belongs to</param>
		/// <param name="k">the value of the constant</param>
		/// <returns></returns>
		public CspTerm Constant(CspDomain valueSet, string k)
		{
			CheckInModelingPhase();
			MarkNonIntegerModel();
			if (valueSet.Kind != CspDomain.ValueKind.Symbol)
			{
				throw new InvalidOperationException(Resources.UnknownString + k);
			}
			CspSymbolDomain cspSymbolDomain = valueSet as CspSymbolDomain;
			if (!_symbolConstants.TryGetValue(cspSymbolDomain, out var value) || !value.TryGetValue(k, out var value2))
			{
				int integerValue = cspSymbolDomain.GetIntegerValue(k);
				if (integerValue < 0)
				{
					throw new InvalidOperationException(Resources.InvalidStringConstant + k);
				}
				CspVariable cspVariable = new CspVariable(this, cspSymbolDomain, k, integerValue);
				value2 = cspVariable;
				if (!_symbolConstants.TryGetValue(cspSymbolDomain, out value))
				{
					value = new Dictionary<string, CspSolverTerm>();
					_symbolConstants.Add(cspSymbolDomain, value);
				}
				value.Add(k, value2);
			}
			return value2;
		}

		/// <summary> This function is a Boolean inverse of its Boolean input.
		/// </summary>
		public CspTerm Not(CspTerm input)
		{
			CheckInModelingPhase();
			ValidateInputs(input);
			return new BooleanNot(this, AsBoolean(input));
		}

		/// <summary> This function is a Boolean OR of its Boolean inputs.
		/// </summary>
		public CspTerm Or(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new BooleanOr(this, AsBooleans(inputs));
		}

		/// <summary> This function is true iff input is a member of domain.
		/// </summary>
		public CspTerm IsElementOf(CspTerm input, CspDomain domain)
		{
			CheckInModelingPhase();
			ValidateInputs(input);
			return new IsElementOf(this, input as CspSolverTerm, domain as CspSolverDomain);
		}

		/// <summary> This function is a Boolean implication of the form antecedent -&gt; consequent
		/// </summary>
		public CspTerm Implies(CspTerm antecedent, CspTerm consequent)
		{
			CheckInModelingPhase();
			ValidateInputs(antecedent);
			ValidateInputs(consequent);
			return new BooleanImplies(this, AsBoolean(antecedent), AsBoolean(consequent));
		}

		/// <summary> This function is a Boolean AND of its Boolean inputs.
		/// </summary>
		public CspTerm And(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new BooleanAnd(this, AsBooleans(inputs));
		}

		/// <summary> This function is true iff exactly m of its Boolean inputs are true.
		/// </summary>
		public CspTerm ExactlyMofN(int M, params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new ExactlyMOfN(this, M, AsBooleans(inputs));
		}

		/// <summary> This function is true iff at most m of its Boolean inputs are true.
		/// </summary>
		public CspTerm AtMostMofN(int M, params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new AtMostMOfN(this, M, AsBooleans(inputs));
		}

		/// <summary> This function is true iff all of its inputs are equal.
		/// </summary>
		public CspTerm Equal(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			if (IsInputComposite(inputs, out var domain))
			{
				return CompositeEqual(inputs, domain);
			}
			ValidateInputs(inputs);
			CspSolverTerm[] array = AsTerms(inputs);
			bool flag = true;
			int num = 0;
			while (flag && num < array.Length)
			{
				flag = array[num].IsBoolean;
				num++;
			}
			if (!flag)
			{
				return new Equal(this, array);
			}
			return new BooleanEqual(this, array);
		}

		/// <summary> This function is true iff every pairing of its inputs is unequal.
		/// </summary>
		public CspTerm Unequal(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			CspSolverTerm[] array = AsTerms(inputs);
			bool flag = true;
			int num = 0;
			while (flag && num < array.Length)
			{
				flag = array[num].IsBoolean;
				num++;
			}
			if (!flag)
			{
				return new Unequal(this, array);
			}
			return new BooleanUnequal(this, array);
		}

		/// <summary> This function is true iff each of its inputs is less than the following input.
		/// </summary>
		public CspTerm Less(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new Less(this, AsTerms(inputs));
		}

		/// <summary> This function is true iff each of its inputs is less than or equal to the following input.
		/// </summary>
		public CspTerm LessEqual(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new LessEqual(this, AsTerms(inputs));
		}

		/// <summary> This function is true iff each of its inputs is greater than the following input.
		/// </summary>
		public CspTerm Greater(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new Greater(this, AsTerms(inputs));
		}

		/// <summary> This function is true iff each of its inputs is greater than or equal to the following input.
		/// </summary>
		public CspTerm GreaterEqual(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new GreaterEqual(this, AsTerms(inputs));
		}

		/// <summary> This function is true iff all of its inputs are equal to the constant.
		/// </summary>
		public CspTerm Equal(int constant, params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new Equal(this, AsTerms(Constant(constant) as CspSolverTerm, inputs));
		}

		/// <summary> This function is true iff every pairing of its inputs is unequal.
		/// </summary>
		public CspTerm Unequal(int constant, params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new Unequal(this, AsTerms(Constant(constant) as CspSolverTerm, inputs));
		}

		/// <summary> This function is true iff each of its inputs is less than the following input.
		/// </summary>
		public CspTerm Less(int constant, params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new Less(this, AsTerms(Constant(constant) as CspSolverTerm, inputs));
		}

		/// <summary> This function is true iff each of its inputs is less than or equal to the following input.
		/// </summary>
		public CspTerm LessEqual(int constant, params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new LessEqual(this, AsTerms(Constant(constant) as CspSolverTerm, inputs));
		}

		/// <summary> This function is true iff each of its inputs is greater than the following input.
		/// </summary>
		public CspTerm Greater(int constant, params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new Greater(this, AsTerms(Constant(constant) as CspSolverTerm, inputs));
		}

		/// <summary> This function is true iff each of its inputs is greater than or equal to the following input.
		/// </summary>
		public CspTerm GreaterEqual(int constant, params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new GreaterEqual(this, AsTerms(Constant(constant) as CspSolverTerm, inputs));
		}

		/// <summary> This function is the absolute value of its input.
		/// </summary>
		public CspTerm Abs(CspTerm input)
		{
			CheckInModelingPhase();
			ValidateInputs(input);
			return new Abs(this, AsTerm(input));
		}

		/// <summary> This function is the negation of its input.
		/// </summary>
		public CspTerm Neg(CspTerm input)
		{
			CheckInModelingPhase();
			ValidateInputs(input);
			return new Negate(this, AsTerm(input));
		}

		/// <summary> This function is its input raised to the power.
		/// </summary>
		public CspTerm Power(CspTerm x, int power)
		{
			CheckInModelingPhase();
			ValidateInputs(x);
			if (power < 0)
			{
				throw new ArgumentException(Resources.NegativePower);
			}
			return new Power(this, AsTerm(x), power);
		}

		/// <summary> This function is the sum of its inputs.
		/// </summary>
		public CspTerm Sum(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new Sum(this, AsTerms(inputs));
		}

		/// <summary> This function is the minimum of its inputs
		/// </summary>
		public CspTerm Min(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new Min(this, AsTerms(inputs));
		}

		/// <summary> This function is the minimum of its inputs
		/// </summary>
		public CspTerm Max(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return new Max(this, AsTerms(inputs));
		}

		/// <summary> This function is the product of its inputs.
		/// </summary>
		public CspTerm Product(params CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			int num = CheckForPower(inputs);
			if (num > 1)
			{
				return Power(inputs[0], num);
			}
			return new Product(this, AsTerms(inputs));
		}

		/// <summary> This function is the sum of the pairwise-product of its inputs.
		/// </summary>
		/// <remarks> The input vectors must be of equal length </remarks>
		public CspTerm SumProduct(CspTerm[] inputs1, CspTerm[] inputs2)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs1);
			ValidateInputs(inputs2);
			int num = inputs1.Length;
			CspSolverTerm[] array = new CspSolverTerm[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = new Product(this, AsTerm(inputs1[i]), AsTerm(inputs2[i]));
			}
			return new Sum(this, array);
		}

		/// <summary> This function is the sum of the conditional inputs.
		/// </summary>
		/// <remarks> The input vectors must be of equal length </remarks>
		public CspTerm FilteredSum(CspTerm[] conditions, CspTerm[] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(conditions);
			ValidateInputs(inputs);
			int num = conditions.Length;
			CspSolverTerm[] array = new CspSolverTerm[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = new Product(this, AsBoolean(conditions[i]), AsTerm(inputs[i]));
			}
			return new Sum(this, array);
		}

		/// <summary> This function is the value of the [index] input.
		/// </summary>
		public CspTerm Index(CspTerm[] inputs, CspTerm index)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			return Index(inputs, index, CreateIntegerInterval(0, inputs.Length - 1));
		}

		/// <summary> This function is the value of the [index] input.
		/// </summary>
		public CspTerm Index(CspTerm[] inputs, CspTerm index, CspDomain indexBase)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			ValidateInputs(index);
			ValidateVarDomain(indexBase);
			if (!StandardIndexConstraint(indexBase, inputs.Length))
			{
				MarkNonIntegerModel();
			}
			CspSolverTerm cspSolverTerm = index as CspSolverTerm;
			CspSolverDomain cspSolverDomain = indexBase as CspSolverDomain;
			int num = inputs.Length;
			CspSolverTerm[] array = new CspSolverTerm[num + 1];
			for (int i = 0; i < num; i++)
			{
				array[i] = inputs[i] as CspSolverTerm;
			}
			array[num] = cspSolverTerm;
			return new IntMap(this, array, cspSolverDomain);
		}

		/// <summary> This function is the value of the [row][column] input
		/// </summary>
		public CspTerm Index(CspTerm[][] inputs, CspTerm row, CspTerm column)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			int num = inputs.Length;
			int num2 = inputs[0].Length;
			return Index(inputs, row, column, CreateIntegerInterval(0, num - 1), CreateIntegerInterval(0, num2 - 1));
		}

		/// <summary> This function is the value of the [row][column] input
		/// </summary>
		public CspTerm Index(CspTerm[][] inputs, CspTerm row, CspTerm column, CspDomain rowIndexBase, CspDomain columnIndexBase)
		{
			CheckInModelingPhase();
			MarkNonIntegerModel();
			ValidateInputs(inputs);
			ValidateIndexArrayShape(inputs);
			ValidateInputs(row);
			ValidateInputs(column);
			ValidateVarDomain(rowIndexBase);
			ValidateVarDomain(columnIndexBase);
			CspSolverTerm cspSolverTerm = row as CspSolverTerm;
			CspSolverTerm cspSolverTerm2 = column as CspSolverTerm;
			CspSolverDomain cspSolverDomain = rowIndexBase as CspSolverDomain;
			CspSolverDomain cspSolverDomain2 = columnIndexBase as CspSolverDomain;
			int num = inputs.Length;
			int num2 = inputs[0].Length;
			int num3 = num * num2;
			CspSolverTerm[] array = new CspSolverTerm[num3 + 2];
			int num4 = 0;
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < num2; j++)
				{
					array[num4++] = inputs[i][j] as CspSolverTerm;
				}
			}
			array[num3] = cspSolverTerm;
			array[num3 + 1] = cspSolverTerm2;
			return new IntMap(this, array, cspSolverDomain, cspSolverDomain2);
		}

		/// <summary> This function is the value of the [row][column] input
		/// </summary>
		public CspTerm Index(CspTerm[][] inputs, CspTerm row, int column)
		{
			CheckInModelingPhase();
			return Index(GetColumn(inputs, column), row);
		}

		/// <summary> This function is the value of the [row][column] input
		/// </summary>
		public CspTerm Index(CspTerm[][] inputs, int row, CspTerm column)
		{
			CheckInModelingPhase();
			if (inputs.Length <= row)
			{
				throw new ArgumentOutOfRangeException(Resources.IndexConstraintOutOfRange);
			}
			return Index(inputs[row], column);
		}

		/// <summary> This function is the value of the input selected by the keys,
		///           which map into the data with the first key being most major.
		/// </summary>
		public CspTerm Index(CspTerm[] inputs, CspTerm[] keys)
		{
			CheckInModelingPhase();
			ValidateInputs(keys);
			CspSolverDomain[] array = new CspSolverDomain[keys.Length];
			for (int i = 0; i < keys.Length; i++)
			{
				CspSolverTerm cspSolverTerm = AsTerm(keys[i]);
				array[i] = cspSolverTerm.BaseValueSet;
			}
			return Index(inputs, keys, array);
		}

		/// <summary> This function is the value of the input selected by the keys,
		///           which map into the data with the first key being most major.
		/// </summary>
		public CspTerm Index(CspTerm[] inputs, CspTerm[] keys, CspDomain[] baseDomains)
		{
			CheckInModelingPhase();
			ValidateInputs(inputs);
			ValidateInputs(keys);
			ValidateVarDomains(baseDomains);
			if (keys.Length != baseDomains.Length)
			{
				throw new ModelException(Resources.IndexVariableRangesDoNotMatchArrayShape);
			}
			if (keys.Length == 1)
			{
				return Index(inputs, keys[0], baseDomains[0]);
			}
			CspSolverTerm[] array = new CspSolverTerm[inputs.Length + keys.Length];
			for (int i = 0; i < inputs.Length; i++)
			{
				array[i] = AsTerm(inputs[i]);
			}
			CspSolverDomain[] array2 = new CspSolverDomain[keys.Length];
			for (int j = 0; j < keys.Length; j++)
			{
				CspSolverTerm cspSolverTerm = AsTerm(keys[j]);
				array2[j] = baseDomains[j] as CspSolverDomain;
				array[j + inputs.Length] = cspSolverTerm;
			}
			return new IntMap(this, array, array2);
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// value combinations of column variables.
		/// </summary>
		public CspTerm TableInteger(CspTerm[] colVars, params int[][] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(colVars);
			CspTerm[][] array = new CspTerm[inputs.Length][];
			int num = colVars.Length;
			HashSet<int>[] array2 = new HashSet<int>[num];
			for (int i = 0; i < num; i++)
			{
				array2[i] = new HashSet<int>();
			}
			for (int j = 0; j < inputs.Length; j++)
			{
				if (inputs[j] == null || num != inputs[j].Length)
				{
					throw new ArgumentException(Resources.TableMismachedDimension);
				}
				array[j] = new CspTerm[num];
				for (int k = 0; k < num; k++)
				{
					array[j][k] = Constant(inputs[j][k]);
					array2[k].Add(inputs[j][k]);
				}
			}
			CspDomain[][] array3 = new CspDomain[1][] { new CspDomain[num] };
			for (int l = 0; l < num; l++)
			{
				int[] array4 = new int[array2[l].Count];
				array2[l].CopyTo(array4);
				Array.Sort(array4);
				array3[0][l] = CreateIntegerSet(array4);
			}
			return And(TableCore(colVars, array), TableDomain(colVars, array3));
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// value combinations of column variables.
		/// </summary>
		public CspTerm TableDecimal(CspTerm[] colVars, params double[][] inputs)
		{
			CheckInModelingPhase();
			MarkNonIntegerModel();
			ValidateInputs(colVars);
			CspTerm[][] array = new CspTerm[inputs.Length][];
			int num = colVars.Length;
			for (int i = 0; i < inputs.Length; i++)
			{
				if (inputs[i] == null || num != inputs[i].Length)
				{
					throw new ArgumentException(Resources.TableMismachedDimension);
				}
				array[i] = new CspTerm[num];
				for (int j = 0; j < num; j++)
				{
					array[i][j] = Constant(inputs[i][j]);
				}
			}
			return TableCore(colVars, array);
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// value combinations of column variables.
		/// </summary>
		public CspTerm TableSymbol(CspTerm[] colVars, params string[][] inputs)
		{
			CheckInModelingPhase();
			MarkNonIntegerModel();
			ValidateInputs(colVars);
			CspTerm[][] array = new CspTerm[inputs.Length][];
			int num = colVars.Length;
			for (int i = 0; i < inputs.Length; i++)
			{
				if (inputs[i] == null || num != inputs[i].Length)
				{
					throw new ArgumentException(Resources.TableMismachedDimension);
				}
				array[i] = new CspTerm[num];
				for (int j = 0; j < num; j++)
				{
					if (!(colVars[j] is CspVariable))
					{
						throw new ArgumentException(Resources.InvalidColumnVar + colVars[j].ToString());
					}
					CspVariable cspVariable = colVars[j] as CspVariable;
					if (cspVariable.Kind != CspDomain.ValueKind.Symbol)
					{
						throw new ArgumentException(Resources.InvalidColumnVar + cspVariable.ToString());
					}
					array[i][j] = Constant(cspVariable.Symbols, inputs[i][j]);
				}
			}
			return TableCore(colVars, array);
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// Domain combinations of column variables.
		/// </summary>
		public CspTerm TableTerm(CspTerm[] colVars, params CspTerm[][] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(colVars);
			ValidateInputs(inputs);
			return TableCore(colVars, inputs);
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// Domain combinations of column variables.
		/// </summary>
		public CspTerm TableDomain(CspTerm[] colVars, params CspDomain[][] inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(colVars);
			CspTerm[] array = new CspSolverTerm[inputs.Length];
			int num = colVars.Length;
			for (int i = 0; i < inputs.Length; i++)
			{
				CspSolverTerm[] array2 = new CspSolverTerm[num];
				for (int j = 0; j < num; j++)
				{
					ValidateVarDomain(inputs[i][j]);
					CspSolverDomain domain = inputs[i][j] as CspSolverDomain;
					CspSolverTerm input = colVars[j] as CspSolverTerm;
					array2[j] = new IsElementOf(this, input, domain);
				}
				array[i] = And(array2);
			}
			if (array.Length == 1)
			{
				return array[0];
			}
			return Or(array);
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// value combinations of column variables.
		/// </summary>
		public CspTerm TableTerm(CspTerm[] colVars, IEnumerable<IEnumerable<CspTerm>> inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(colVars);
			IEnumerable<CspTerm>[] array = Statics.EnumerableToArray(inputs);
			CspTerm[][] array2 = new CspTerm[array.Length][];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = Statics.EnumerableToArray(array[i]);
			}
			return TableTerm(colVars, array2);
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// Domain combinations of column variables.
		/// </summary>
		public CspTerm TableDomain(CspTerm[] colVars, IEnumerable<IEnumerable<CspDomain>> inputs)
		{
			CheckInModelingPhase();
			ValidateInputs(colVars);
			IEnumerable<CspDomain>[] array = Statics.EnumerableToArray(inputs);
			CspDomain[][] array2 = new CspDomain[array.Length][];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = Statics.EnumerableToArray(array[i]);
			}
			return TableDomain(colVars, array2);
		}

		/// <summary>
		/// Core method to creat a table. We assume all input validation has been done prior to the call to this method.
		/// </summary>
		private CspTerm TableCore(CspTerm[] colVars, CspTerm[][] inputs)
		{
			CheckInModelingPhase();
			CspTerm[] array = new CspTerm[inputs.Length];
			int num = colVars.Length;
			for (int i = 0; i < inputs.Length; i++)
			{
				CspTerm[] array2 = new CspTerm[colVars.Length];
				for (int j = 0; j < num; j++)
				{
					if (colVars[j].Model != inputs[i][j].Model)
					{
						throw new ArgumentException(Resources.IncompatibleInputTerms);
					}
					array2[j] = Equal(colVars[j], inputs[i][j]);
				}
				array[i] = And(array2);
			}
			if (array.Length == 1)
			{
				return array[0];
			}
			return Or(array);
		}

		/// <summary> 
		/// A constraint imposing that the start times of a number of tasks be 
		/// scheduled so that at no time instant their total consumption of
		/// a shared resource is larger than the availability of this resource
		/// </summary>
		/// <param name="starts">start time of every task</param>
		/// <param name="durations">duration of each task</param>
		/// <param name="consumptions">consumptio of each task</param>
		/// <param name="capacity">availability of the resource</param>
		internal CspTerm Packing(CspTerm[] starts, int[] durations, int[] consumptions, int capacity)
		{
			CheckInModelingPhase();
			ValidateInputs(starts);
			if (starts.Length != durations.Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "starts", "durations" }));
			}
			if (starts.Length != consumptions.Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "starts", "consumptions" }));
			}
			return new DisjointPacking(this, AsTerms(starts), durations, consumptions, capacity);
		}

		/// <summary> Specify which Function Terms are to be considered Constraints.  Not all Functions
		///             automatically become constraints, indeed only Boolean-valued functions are
		///             eligable to be Constraints since a Constraint "must be true".
		///           All the other Functions are interior nodes in expression-trees which come in as
		///             inputs to the Constraints.  Please consult examples in the user guide if this is unclear.
		/// </summary>
		/// <returns> True if and only if all constraints are valid.
		/// </returns>
		/// <remarks> There is no "AddVariables" method because all Variables are automatically known to the Solver.
		///             It would just be extra clutter to ask a user to list them again.
		/// </remarks>
		[SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "loops")]
		public bool AddConstraints(params CspTerm[] constraints)
		{
			CheckInModelingPhase();
			if (constraints == null)
			{
				throw new ArgumentNullException(Resources.NullConstraints);
			}
			foreach (CspTerm cspTerm in constraints)
			{
				if (cspTerm == null)
				{
					throw new ArgumentNullException(Resources.NullConstraints);
				}
				if (!(cspTerm is CspSolverTerm cspSolverTerm))
				{
					throw new ArgumentException(Resources.UnknowTermType + cspTerm.ToString());
				}
				CspFunction cspFunction = cspSolverTerm as CspFunction;
				CspVariable cspVariable = cspSolverTerm as CspVariable;
				if (cspFunction != null)
				{
					cspFunction.Restrain(DTrue);
					cspFunction.ChangeCount = 0;
				}
				else
				{
					if (cspVariable == null)
					{
						throw new ModelException(Resources.NonBooleanConstraints + cspSolverTerm.ToString());
					}
					if (cspVariable.Count >= 2)
					{
						cspVariable.Restrain(DTrue);
					}
					else if (!cspVariable.IsTrue)
					{
						throw new ModelException(Resources.AddingFalseAsConstraint);
					}
					cspVariable.ChangeCount = 0;
				}
				_constraints.Add(cspSolverTerm);
				cspSolverTerm.IsConstraint = true;
				CspSolverTerm conflict;
				switch (_mode)
				{
				case ModelConstructionMode.DebugLight:
					CheckDomainNarrowing();
					if (_propagator == null)
					{
						_propagator = new AdaptiveLocalPropagation(this);
					}
					if (!_propagator.Propagate(out conflict))
					{
						return false;
					}
					break;
				case ModelConstructionMode.DebugHeavy:
				{
					CheckDomainNarrowing();
					if (_propagator == null)
					{
						_propagator = new AdaptiveLocalPropagation(this);
					}
					if (!_propagator.Propagate(out conflict))
					{
						return false;
					}
					int count = _changes.Count;
					bool flag = false;
					using (IEnumerator<int> enumerator = InnerSolve(yieldSuboptimals: true).GetEnumerator())
					{
						if (enumerator.MoveNext())
						{
							_ = enumerator.Current;
							flag = true;
							_stopwatch.Stop();
						}
					}
					if (!flag)
					{
						return false;
					}
					Backtrack(count);
					_baseSolver = null;
					_fIsInModelingPhase = true;
					break;
				}
				}
			}
			return true;
		}

		/// <summary>
		/// Remove constraints which were added by AddConstraints.
		/// </summary>
		/// <param name="constraints"></param>
		public void RemoveConstraints(params CspTerm[] constraints)
		{
			CheckInModelingPhase();
			if (constraints == null)
			{
				throw new ArgumentNullException(Resources.NullConstraints);
			}
			foreach (CspTerm cspTerm in constraints)
			{
				if (cspTerm == null)
				{
					throw new ArgumentNullException(Resources.NullConstraints);
				}
				if (!(cspTerm is CspSolverTerm cspSolverTerm))
				{
					throw new ArgumentException(Resources.UnknowTermType + cspTerm.ToString());
				}
				if (cspSolverTerm.IsConstraint)
				{
					_constraints.Remove(cspSolverTerm);
					cspSolverTerm.IsConstraint = false;
					cspSolverTerm.ChangeCount = 0;
					if (_baseChangeCount > 0)
					{
						Backtrack(_baseChangeCount);
					}
					cspSolverTerm.Backtrack(1);
				}
			}
		}

		/// <summary> Specify Terms which are to be minimized as the optimization goal of the solver.
		///           If the solver is not given any goal, then the solver will find all feasible solutions.
		///           If one Term is specified, the solver will find all solutions equalling the best value of the goal.
		///           If more than one goal Term is supplied, then each Term is considered only within the set of
		///             solutions satisfying the optimum of all preceding goals.
		///           If TryAddMinimizationGoals is called separately then Terms will be appended to prior registered goals.
		/// </summary>
		/// <returns> false if the underlying solver cannot TryAddMinimizationGoals the given terms. </returns>
		/// <remarks> To maximize a goal, TryAddMinimizationGoals its negative.
		/// </remarks>
		public bool TryAddMinimizationGoals(params CspTerm[] goals)
		{
			CheckInModelingPhase();
			if (goals == null)
			{
				throw new ArgumentNullException(Resources.NullGoals);
			}
			foreach (CspTerm cspTerm in goals)
			{
				if (cspTerm == null)
				{
					throw new ArgumentNullException(Resources.NullGoals);
				}
				if (!(cspTerm is CspSolverTerm item))
				{
					throw new ArgumentException(Resources.UnknowTermType + cspTerm.ToString());
				}
				_minimizationGoals.Add(item);
			}
			return true;
		}

		/// <summary>
		/// Removes all minimization goals.
		/// </summary>
		public void RemoveAllMinimizationGoals()
		{
			CheckInModelingPhase();
			_minimizationGoals.Clear();
		}

		/// <summary>
		/// Create a Domain of the allowed integer values you provide (which must be in strictly ascending order)
		/// </summary>
		/// <param name="orderedUniqueSet">The ordered sequences of values to include in the set</param>
		/// <returns></returns>
		public CspDomain CreateIntegerSet(params int[] orderedUniqueSet)
		{
			ValidateSet(orderedUniqueSet, 0, orderedUniqueSet.Length);
			return CspSetDomain.Create(orderedUniqueSet);
		}

		/// <summary>
		/// Create a Domain of the allowed integer values you provide (which must be in strictly ascending order)
		/// </summary>
		/// <param name="orderedUniqueSet">The ordered sequences of values to include in the set</param>
		/// <param name="from">The starting position in orderedUniqueSet</param>
		/// <param name="count">The number of elements from the starting position that should be included into the set</param>
		/// <returns></returns>
		public CspDomain CreateIntegerSet(int[] orderedUniqueSet, int from, int count)
		{
			ValidateSet(orderedUniqueSet, from, count);
			return CspSetDomain.Create(orderedUniqueSet, from, count);
		}

		/// <summary>
		/// Create a Domain of the allowed real values with the default precision (which must be in strictly ascending order)
		/// </summary>
		/// <param name="orderedUniqueSet">The ordered sequences of values to include in the set</param>
		/// <returns></returns>
		public CspDomain CreateDecimalSet(params double[] orderedUniqueSet)
		{
			MarkNonIntegerModel();
			ValidateSet(Precision, orderedUniqueSet, 0, orderedUniqueSet.Length);
			return CspSetDomain.Create(Precision, orderedUniqueSet);
		}

		/// <summary>
		/// Create a Domain of the allowed real values you provide (which must be in strictly ascending order)
		/// </summary>
		/// <param name="precision">The precision for this real value set</param>
		/// <param name="orderedUniqueSet">The ordered sequences of values to include in the set</param>
		/// <returns></returns>
		internal CspDomain CreateDecimalSet(int precision, params double[] orderedUniqueSet)
		{
			MarkNonIntegerModel();
			ValidatePrecision(precision);
			ValidateSet(precision, orderedUniqueSet, 0, orderedUniqueSet.Length);
			return CspSetDomain.Create(precision, orderedUniqueSet);
		}

		/// <summary>
		/// Create a Domain of the allowed real values with the default precision (which must be in strictly ascending order)
		/// </summary>
		/// <param name="orderedUniqueSet">The ordered sequences of values to include in the set</param>
		/// <param name="from">The starting position in orderedUniqueSet</param>
		/// <param name="count">The number of elements from the starting position that should be included into the set</param>
		/// <returns></returns>
		public CspDomain CreateDecimalSet(double[] orderedUniqueSet, int from, int count)
		{
			MarkNonIntegerModel();
			ValidateSet(Precision, orderedUniqueSet, from, count);
			return CspSetDomain.Create(Precision, orderedUniqueSet, from, count);
		}

		/// <summary>
		/// Create a Domain of the allowed real values you provide (which must be in strictly ascending order)
		/// </summary>
		/// <param name="precision">The precision for this real value set</param>
		/// <param name="orderedUniqueSet">The ordered sequences of values to include in the set</param>
		/// <param name="from">The starting position in orderedUniqueSet</param>
		/// <param name="count">The number of elements from the starting position that should be included into the set</param>
		/// <returns></returns>
		internal CspDomain CreateDecimalSet(int precision, double[] orderedUniqueSet, int from, int count)
		{
			MarkNonIntegerModel();
			ValidatePrecision(precision);
			ValidateSet(precision, orderedUniqueSet, from, count);
			return CspSetDomain.Create(precision, orderedUniqueSet, from, count);
		}

		/// <summary>
		/// Create a Domain of symbols.
		/// </summary>
		/// <param name="uniqueSymbols">The symbols in the Domain. Must contain no duplicates. We respect the order of the symbols.</param>
		/// <returns></returns>
		public CspDomain CreateSymbolSet(params string[] uniqueSymbols)
		{
			MarkNonIntegerModel();
			ValidateSymbols(uniqueSymbols);
			return CspSymbolDomain.Create(uniqueSymbols);
		}

		/// <summary> Create a Domain of the integer interval [first .. last]
		/// </summary>
		public CspDomain CreateIntegerInterval(int first, int last)
		{
			ValidateInterval(first, last);
			return CspIntervalDomain.Create(first, last);
		}

		/// <summary> Create a Domain of the real interval [first .. last] with the default precision
		/// </summary>
		public CspDomain CreateDecimalInterval(double first, double last)
		{
			MarkNonIntegerModel();
			ValidateInterval(first, last);
			return CspIntervalDomain.Create(Precision, first, last);
		}

		/// <summary> Create a Domain of the real interval [first .. last]
		/// </summary>
		internal CspDomain CreateDecimalInterval(int precision, double first, double last)
		{
			MarkNonIntegerModel();
			ValidatePrecision(precision);
			ValidateInterval(first, last);
			return CspIntervalDomain.Create(precision, first, last);
		}

		/// <summary>
		/// Create an empty composite to which fields can be added.
		/// </summary>
		/// <returns></returns>
		public CspComposite CreateComposite(object key)
		{
			CheckInModelingPhase();
			MarkNonIntegerModel();
			return CspComposite.CreateComposite(this, key);
		}

		/// <summary>
		/// Create an empty composite with an auto-generated key
		/// </summary>
		/// <returns></returns>
		public CspComposite CreateComposite()
		{
			return CreateComposite(null);
		}

		/// <summary> Create a finite variable with the specified domain.
		/// </summary>
		public CspTerm CreateVariable(CspDomain domain, object key)
		{
			if (domain == DefaultBoolean)
			{
				return CreateBoolean(key);
			}
			CheckInModelingPhase();
			ValidateVarDomain(domain);
			return new CspVariable(this, domain as CspSolverDomain, CspSolverTerm.TermKinds.DecisionVariable, key);
		}

		/// <summary>
		/// Create a composite variable
		/// </summary>
		public CspTerm CreateVariable(CspComposite domain, object key)
		{
			CheckInModelingPhase();
			MarkNonIntegerModel();
			ValidateVarDomain(domain);
			return new CspCompositeVariable(this, domain, key);
		}

		/// <summary> Create a finite variable with the given domain and an auto-generated key
		/// </summary>
		public CspTerm CreateVariable(CspDomain domain)
		{
			if (domain == DefaultBoolean)
			{
				return CreateBoolean();
			}
			CheckInModelingPhase();
			ValidateVarDomain(domain);
			return new CspVariable(this, (CspSolverDomain)domain, CspSolverTerm.TermKinds.DecisionVariable);
		}

		/// <summary> Create a composite variable with an auto-generated key
		/// </summary>
		public CspTerm CreateVariable(CspComposite domain)
		{
			CheckInModelingPhase();
			MarkNonIntegerModel();
			ValidateVarDomain(domain);
			return new CspCompositeVariable(this, domain);
		}

		/// <summary> Create a boolean variable initially either false or true
		/// </summary>
		public CspTerm CreateBoolean(object key)
		{
			CheckInModelingPhase();
			return new CspBooleanVariable(this, CspSolverTerm.TermKinds.DecisionVariable, key);
		}

		/// <summary>
		/// Create a boolean variable with an auto-generated key
		/// </summary>
		/// <returns></returns>
		public CspTerm CreateBoolean()
		{
			CheckInModelingPhase();
			return new CspBooleanVariable(this, CspSolverTerm.TermKinds.DecisionVariable);
		}

		/// <summary>
		/// Create a user variable whose value can be interactively set/unset
		/// </summary>
		internal CspTerm CreateUserVariable(CspDomain domain, object key)
		{
			if (domain == DefaultBoolean)
			{
				return CreateUserBoolean(key);
			}
			CheckInModelingPhase();
			MarkNonIntegerModel();
			ValidateVarDomain(domain);
			CspUserVariable cspUserVariable = new CspUserVariable(this, domain as CspSolverDomain, key);
			_userVars.Add(cspUserVariable);
			return cspUserVariable;
		}

		/// <summary>
		/// Create a user variable with auto-generated key
		/// </summary>
		internal CspTerm CreateUserVariable(CspDomain domain)
		{
			if (domain == DefaultBoolean)
			{
				return CreateUserBoolean();
			}
			CheckInModelingPhase();
			MarkNonIntegerModel();
			ValidateVarDomain(domain);
			CspUserVariable cspUserVariable = new CspUserVariable(this, domain as CspSolverDomain);
			_userVars.Add(cspUserVariable);
			return cspUserVariable;
		}

		/// <summary>
		/// Create a user Boolean variable whose value can be interactively set/unset
		/// </summary>
		internal CspTerm CreateUserBoolean(object key)
		{
			CheckInModelingPhase();
			MarkNonIntegerModel();
			CspUserVariable cspUserVariable = new CspUserVariable(this, DefaultBoolean as CspSolverDomain, key);
			_userVars.Add(cspUserVariable);
			return cspUserVariable;
		}

		/// <summary>
		/// Create a user Boolean variable with auto-generated key
		/// </summary>
		internal CspTerm CreateUserBoolean()
		{
			CheckInModelingPhase();
			MarkNonIntegerModel();
			CspUserVariable cspUserVariable = new CspUserVariable(this, DefaultBoolean as CspSolverDomain);
			_userVars.Add(cspUserVariable);
			return cspUserVariable;
		}

		/// <summary> Create a vector of Finite Variable Terms all of the same initial domain.
		/// </summary>
		public CspTerm[] CreateVariableVector(CspDomain domain, object key, int length)
		{
			return CreateVariableVectorCore(CspSolverTerm.TermKinds.DecisionVariable, domain, key, length);
		}

		/// <summary>
		/// Create a vector of Finite Variable Terms all of the same composite domain.
		/// </summary>
		public CspTerm[] CreateVariableVector(CspComposite domain, object key, int length)
		{
			if (_elementKeySizes.ContainsKey(key))
			{
				throw new ArgumentException(Resources.DuplicateKey + ((key != null) ? key.ToString() : "null"));
			}
			_elementKeySizes.Add(key, -length);
			CspTerm[] array = new CspTerm[length];
			for (int i = 0; i < length; i++)
			{
				ElementKey key2 = new ElementKey(key, i);
				array[i] = CreateVariable(domain, key2);
			}
			return array;
		}

		/// <summary> Create an array of Finite Variable Terms all of the same initial domain.
		///           It is defined as two-level to facilitate use of rows as inputs to the functions.
		///           See the GetColumn helper method, below, for slicing the array a column at a time.
		/// </summary>
		public CspTerm[][] CreateVariableArray(CspDomain domain, object key, int rows, int columns)
		{
			if (_elementKeySizes.ContainsKey(key))
			{
				throw new ArgumentException(Resources.DuplicateKey + ((key != null) ? key.ToString() : "null"));
			}
			_elementKeySizes.Add(key, columns);
			CspTerm[][] array = new CspTerm[rows][];
			for (int i = 0; i < rows; i++)
			{
				array[i] = new CspTerm[columns];
				for (int j = 0; j < columns; j++)
				{
					ElementKey key2 = new ElementKey(key, i * columns + j);
					array[i][j] = CreateVariable(domain, key2);
				}
			}
			return array;
		}

		/// <summary> Create a vector of Boolean Variable Terms all of the same initial domain.
		/// </summary>
		public CspTerm[] CreateBooleanVector(object key, int length)
		{
			return CreateBooleanVectorCore(CspSolverTerm.TermKinds.DecisionVariable, key, length);
		}

		/// <summary> Create an array of Boolean Variable Terms all of the same initial domain.
		///           It is defined as two-level to facilitate use of rows as inputs to the functions.
		///           See the GetColumn helper method, below, for slicing the array a column at a time.
		/// </summary>
		public CspTerm[][] CreateBooleanArray(object key, int rows, int columns)
		{
			if (_elementKeySizes.ContainsKey(key))
			{
				throw new ArgumentException(Resources.DuplicateKey + ((key != null) ? key.ToString() : "null"));
			}
			_elementKeySizes.Add(key, columns);
			CspTerm[][] array = new CspTerm[rows][];
			for (int i = 0; i < rows; i++)
			{
				array[i] = new CspTerm[columns];
				for (int j = 0; j < columns; j++)
				{
					ElementKey key2 = new ElementKey(key, i * columns + j);
					array[i][j] = CreateBoolean(key2);
				}
			}
			return array;
		}

		/// <summary> If key corresponds to a variable, set term to the variable and return true.
		/// Otherwise, return false.
		/// </summary>
		public bool TryGetVariableFromKey(object key, out CspTerm term)
		{
			if (key != null && _varDict.TryGetValue(key, out var value))
			{
				term = value;
				return true;
			}
			term = null;
			return false;
		}

		/// <summary>
		/// Start the domain narrowing thread. This method should be invoked only once. All constraints must
		/// be added to the solver prior to the invocation of this method.
		/// </summary>
		internal void StartNarrowingDomains()
		{
			if (!_fIsInModelingPhase)
			{
				throw new InvalidOperationException(Resources.AlreadySolving);
			}
			_fIsInModelingPhase = false;
			_baseChangeCount = _changes.Count;
			_verbose = false;
			_propagator = new AdaptiveLocalPropagation(this);
			if (_propagator.Propagate(out var _))
			{
				_baseSolver = new DomainNarrowingWithACS(this);
				_domainNarrowingThread = new Thread(NarrowingDomains);
				_domainNarrowingThread.Start();
			}
		}

		/// <summary>
		/// Stop the domain narrowing thread. This method must be called after StartNarrowingDomains().
		/// </summary>
		internal void StopNarrowingDomains()
		{
			ResetSolver();
		}

		/// <summary>
		/// Test if the domain narrowing is finished.
		/// </summary>
		internal bool IsDomainNarrowingFinished()
		{
			foreach (CspUserVariable userVar in _userVars)
			{
				if (!userVar.IsFinished())
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Reset the solver state for another round of solver from scratch
		/// </summary>
		public void ResetSolver()
		{
			if (_fIsInModelingPhase)
			{
				return;
			}
			_fIsInModelingPhase = true;
			lock (this)
			{
				if (_domainNarrowingThread != null)
				{
					if (_baseSolver != null && _baseSolver is DomainNarrowingWithACS domainNarrowingWithACS)
					{
						domainNarrowingWithACS.AbortDomainNarrowing();
					}
					_domainNarrowingThread.Join();
					_baseSolver = null;
					_domainNarrowingThread = null;
				}
			}
			if (_baseChangeCount > 0)
			{
				Backtrack(_baseChangeCount);
			}
			_baseChangeCount = 0;
			_propagator = null;
			_baseSolver = null;
			_isInterrupted = false;
			_params.Abort = false;
		}

		/// <summary>
		/// Create a solver instance
		/// </summary>
		public static ConstraintSystem CreateSolver()
		{
			return new ConstraintSystem();
		}

		/// <summary>
		/// Create a solver instance
		/// </summary>
		public static ConstraintSystem CreateSolver(ISolverEnvironment context)
		{
			return new ConstraintSystem();
		}

		/// <summary>
		/// Create a solver instance with the given parameters
		/// </summary>
		public static ConstraintSystem CreateSolver(Func<bool> fnQueryAbort, IEqualityComparer<object> cmp, int precision)
		{
			return new ConstraintSystem(fnQueryAbort, cmp, precision);
		}

		/// <summary>
		/// Solve the model using the default solver parameter.  
		/// </summary>
		public ConstraintSolverSolution Solve()
		{
			return Solve(null);
		}

		/// <summary>
		/// Solve the model using given solver parameter.  
		/// </summary>
		public ConstraintSolverSolution Solve(ISolverParameters param)
		{
			ConstraintSolverParams constraintSolverParams = param as ConstraintSolverParams;
			if (constraintSolverParams == null)
			{
				constraintSolverParams = Parameters;
			}
			if (!constraintSolverParams.EnumerateInterimSolutions && constraintSolverParams.Algorithm == ConstraintSolverParams.CspSearchAlgorithm.LocalSearch)
			{
				constraintSolverParams.EnumerateInterimSolutions = true;
			}
			if (constraintSolverParams != null && constraintSolverParams != _params)
			{
				Func<bool> queryAbort = _params.QueryAbort;
				_params = new ConstraintSolverParams(constraintSolverParams, _termMap);
				if (_params.QueryAbort == null && queryAbort != null)
				{
					_params.QueryAbort = queryAbort;
				}
			}
			_params.Abort = false;
			_isInterrupted = false;
			SolverKind solverKind = DecideSolverKind(constraintSolverParams);
			ConstraintSolverSolution result;
			if ((solverKind & SolverKind.CspSolver) != SolverKind.CspSolver || constraintSolverParams._forceIntegerSolver)
			{
				if ((solverKind & SolverKind.IntegerSolver) != SolverKind.IntegerSolver || (constraintSolverParams.Algorithm != ConstraintSolverParams.CspSearchAlgorithm.TreeSearch && constraintSolverParams.Algorithm != 0))
				{
					throw new ModelException(Resources.CannotSolve);
				}
				Func<bool> abortCallBack = ((constraintSolverParams.QueryAbort != null) ? constraintSolverParams.QueryAbort : ((Parameters.QueryAbort == null) ? ((Func<bool>)(() => Parameters.Abort)) : Parameters.QueryAbort));
				IntegerSolver integerSolver = new IntegerSolver(this, abortCallBack);
				result = integerSolver.SolveCore(_params);
				_integerSolverStatistics = integerSolver.Statistics;
			}
			else
			{
				result = SolveCore(constraintSolverParams);
				if (constraintSolverParams.Algorithm != ConstraintSolverParams.CspSearchAlgorithm.LocalSearch)
				{
					_params.ValueSelection = ConstraintSolverParams.TreeSearchValueOrdering.ForwardOrder;
					_params.VariableSelection = ConstraintSolverParams.TreeSearchVariableOrdering.DomainOverWeightedDegree;
					_params.Algorithm = ConstraintSolverParams.CspSearchAlgorithm.TreeSearch;
				}
			}
			return result;
		}

		/// <summary>
		/// Append the model from the source solver to the target solver
		/// </summary>
		/// <param name="source">The solver that contains the model to be cloned</param>
		/// <param name="target">The solver that will contain the cloned model</param>
		internal static Dictionary<CspTerm, CspTerm> AppendModel(ConstraintSystem source, ConstraintSystem target)
		{
			return AppendModel(source, target, null);
		}

		/// <summary>
		/// Append the model from the source solver to the target solver
		/// </summary>
		/// <param name="source">The solver that contains the model to be appended</param>
		/// <param name="target">The solver whose model will be appended</param>
		/// <param name="preClonedTermMap">Record a set of terms that have been created in the target for the appending operation. 
		/// Maps the term in the source to the term in the target.</param>
		internal static Dictionary<CspTerm, CspTerm> AppendModel(ConstraintSystem source, ConstraintSystem target, Dictionary<CspTerm, CspTerm> preClonedTermMap)
		{
			if (source.IsEmpty)
			{
				return null;
			}
			Dictionary<CspTerm, CspTerm> dictionary = preClonedTermMap ?? new Dictionary<CspTerm, CspTerm>();
			AppendModelTerms(source, target, dictionary);
			AppendModelConstraints(source, target, dictionary);
			AppendModelGoals(source, target, dictionary);
			return dictionary;
		}

		private static void AppendModelTerms(ConstraintSystem source, ConstraintSystem target, Dictionary<CspTerm, CspTerm> termMap)
		{
			List<CspTerm> allTerms = source.AllTerms;
			CspTerm[] array = new CspTerm[allTerms.Count];
			for (int i = 0; i < allTerms.Count; i++)
			{
				CspSolverTerm cspSolverTerm = allTerms[i] as CspSolverTerm;
				CspVariable cspVariable = cspSolverTerm as CspVariable;
				switch (cspSolverTerm.TermKind)
				{
				case CspSolverTerm.TermKinds.DecisionVariable:
					if (!termMap.ContainsKey(cspSolverTerm))
					{
						CspTerm value;
						if (cspVariable.BaseValueSet == source.DefaultBoolean || cspVariable.BaseValueSet == target.DefaultBoolean)
						{
							value = target.CreateBoolean(cspVariable.Key);
						}
						else
						{
							CspDomain baseValueSet = cspVariable.BaseValueSet;
							value = target.CreateVariable(baseValueSet, cspVariable.Key);
						}
						termMap.Add(cspSolverTerm, value);
					}
					break;
				case CspSolverTerm.TermKinds.Constant:
					if (cspSolverTerm == source.True)
					{
						array[i] = target.True;
					}
					else if (cspSolverTerm == source.False)
					{
						array[i] = target.False;
					}
					else
					{
						switch (cspVariable.Kind)
						{
						case CspDomain.ValueKind.Integer:
							array[i] = target.Constant((int)cspVariable.GetValue());
							break;
						case CspDomain.ValueKind.Decimal:
							array[i] = target.Constant(cspVariable.OutputScale, (double)cspVariable.GetValue());
							break;
						case CspDomain.ValueKind.Symbol:
							array[i] = target.Constant(cspVariable.Symbols, (string)cspVariable.GetValue());
							break;
						default:
							throw new ArgumentException(Resources.InvalidValueType + cspVariable.ToString());
						}
					}
					termMap.Add(cspSolverTerm, array[i]);
					break;
				case CspSolverTerm.TermKinds.Function:
				{
					CspFunction cspFunction = cspSolverTerm as CspFunction;
					DebugContracts.NonNull(cspFunction);
					CspSolverTerm[] args = cspSolverTerm.Args;
					CspTerm[] array2 = new CspTerm[cspSolverTerm.Width];
					for (int j = 0; j < args.Length; j++)
					{
						CspSolverTerm cspSolverTerm2 = args[j];
						if (termMap.ContainsKey(cspSolverTerm2))
						{
							array2[j] = termMap[cspSolverTerm2];
							continue;
						}
						throw new ArgumentException(Resources.CloneInvalidTerm + cspSolverTerm2.ToString());
					}
					array[i] = cspFunction.Clone(target, array2);
					termMap.Add(cspSolverTerm, array[i]);
					break;
				}
				default:
					throw new ArgumentException(Resources.CloneInvalidTerm + cspSolverTerm.ToString());
				case CspSolverTerm.TermKinds.TemplateVariable:
					break;
				}
			}
		}

		private static void AppendModelConstraints(ConstraintSystem source, ConstraintSystem target, Dictionary<CspTerm, CspTerm> termMap)
		{
			foreach (CspTerm constraint in source.Constraints)
			{
				CspSolverTerm key = constraint as CspSolverTerm;
				target.AddConstraints(termMap[key]);
			}
		}

		private static void AppendModelGoals(ConstraintSystem source, ConstraintSystem target, Dictionary<CspTerm, CspTerm> termMap)
		{
			foreach (CspTerm minimizationGoal in source.MinimizationGoals)
			{
				CspSolverTerm key = minimizationGoal as CspSolverTerm;
				target.TryAddMinimizationGoals(termMap[key]);
			}
		}

		internal static CspDomain CloneDomain(ConstraintSystem target, CspDomain source)
		{
			return source;
		}

		/// <summary>
		/// Append the model from the source solver to the target solver
		/// </summary>
		/// <param name="source">The solver that contains the model to be cloned</param>
		/// <param name="target">The solver that will contain the cloned model</param>
		internal static Dictionary<CspTerm, CspTerm> AppendModel(ConstraintSystem source, IntegerSolver target)
		{
			return AppendModel(source, target, null);
		}

		/// <summary>
		/// Append the model from the source solver to the target solver
		/// </summary>
		/// <param name="source">The solver that contains the model to be appended</param>
		/// <param name="target">The solver whose model will be appended</param>
		/// <param name="preClonedTermMap">Record a set of terms that have been created in the target for the appending operation. 
		/// Maps the term in the source to the term in the target.</param>
		internal static Dictionary<CspTerm, CspTerm> AppendModel(ConstraintSystem source, IntegerSolver target, Dictionary<CspTerm, CspTerm> preClonedTermMap)
		{
			if (source.IsEmpty)
			{
				return null;
			}
			Dictionary<CspTerm, CspTerm> dictionary = preClonedTermMap ?? new Dictionary<CspTerm, CspTerm>();
			AppendModelTerms(source, target, dictionary);
			AppendModelConstraints(source, target, dictionary);
			AppendModelGoals(source, target, dictionary);
			return dictionary;
		}

		private static void AppendModelTerms(ConstraintSystem source, IntegerSolver target, Dictionary<CspTerm, CspTerm> termMap)
		{
			List<CspTerm> allTerms = source.AllTerms;
			CspTerm[] array = new CspTerm[allTerms.Count];
			for (int i = 0; i < allTerms.Count; i++)
			{
				CspSolverTerm cspSolverTerm = allTerms[i] as CspSolverTerm;
				CspVariable cspVariable = cspSolverTerm as CspVariable;
				switch (cspSolverTerm.TermKind)
				{
				case CspSolverTerm.TermKinds.DecisionVariable:
					if (!termMap.ContainsKey(cspSolverTerm))
					{
						CspTerm value;
						if (cspVariable.BaseValueSet == source.DefaultBoolean || cspVariable.BaseValueSet == target.DefaultBoolean)
						{
							value = target.CreateBoolean(cspVariable.Key);
						}
						else
						{
							CspDomain domain = CloneDomain(target, cspVariable.BaseValueSet);
							value = target.CreateVariable(domain, cspVariable.Key);
						}
						termMap.Add(cspSolverTerm, value);
					}
					break;
				case CspSolverTerm.TermKinds.Constant:
					if (cspSolverTerm == source.True)
					{
						array[i] = target.True;
					}
					else if (cspSolverTerm == source.False)
					{
						array[i] = target.False;
					}
					else
					{
						CspDomain.ValueKind kind = cspVariable.Kind;
						if (kind != CspDomain.ValueKind.Integer)
						{
							throw new ArgumentException(Resources.InvalidValueType + cspVariable.ToString());
						}
						array[i] = target.Constant((int)cspVariable.GetValue());
					}
					termMap.Add(cspSolverTerm, array[i]);
					break;
				case CspSolverTerm.TermKinds.Function:
				{
					CspFunction cspFunction = cspSolverTerm as CspFunction;
					DebugContracts.NonNull(cspFunction);
					CspSolverTerm[] args = cspSolverTerm.Args;
					CspTerm[] array2 = new CspTerm[cspSolverTerm.Width];
					for (int j = 0; j < args.Length; j++)
					{
						CspSolverTerm cspSolverTerm2 = args[j];
						if (termMap.ContainsKey(cspSolverTerm2))
						{
							array2[j] = termMap[cspSolverTerm2];
							continue;
						}
						throw new ArgumentException(Resources.CloneInvalidTerm + cspSolverTerm2.ToString());
					}
					array[i] = cspFunction.Clone(target, array2);
					termMap.Add(cspSolverTerm, array[i]);
					break;
				}
				default:
					throw new ArgumentException(Resources.CloneInvalidTerm + cspSolverTerm.ToString());
				case CspSolverTerm.TermKinds.TemplateVariable:
					break;
				}
			}
		}

		private static void AppendModelConstraints(ConstraintSystem source, IntegerSolver target, Dictionary<CspTerm, CspTerm> termMap)
		{
			foreach (CspTerm constraint in source.Constraints)
			{
				CspSolverTerm key = constraint as CspSolverTerm;
				target.AddConstraints(termMap[key]);
			}
		}

		private static void AppendModelGoals(ConstraintSystem source, IntegerSolver target, Dictionary<CspTerm, CspTerm> termMap)
		{
			foreach (CspTerm minimizationGoal in source.MinimizationGoals)
			{
				CspSolverTerm key = minimizationGoal as CspSolverTerm;
				target.TryAddMinimizationGoals(termMap[key]);
			}
		}

		internal static CspDomain CloneDomain(IntegerSolver target, CspDomain source)
		{
			CspDomain result = source;
			if (target != null)
			{
				CspIntervalDomain cspIntervalDomain = source as CspIntervalDomain;
				CspSetDomain cspSetDomain = source as CspSetDomain;
				if (cspIntervalDomain != null)
				{
					result = target.CreateIntegerInterval(cspIntervalDomain.First, cspIntervalDomain.Last);
				}
				else
				{
					if (cspSetDomain == null)
					{
						throw new InvalidOperationException(Resources.CloneUnsupportedDomain);
					}
					result = target.CreateIntegerSet(cspSetDomain.Set);
				}
			}
			return result;
		}

		/// <summary> Invoke propagation only to eliminate infeasible values from variable domains
		/// </summary>
		internal bool Presolve(Func<bool> queryAbort, out CspSolverTerm conflict)
		{
			if (!_fIsInModelingPhase)
			{
				throw new InvalidOperationException(Resources.AlreadySolving);
			}
			Func<bool> queryAbort2 = QueryAbort;
			QueryAbort = queryAbort;
			_params.Abort = false;
			_isInterrupted = false;
			_fIsInModelingPhase = false;
			_verbose = false;
			_baseChangeCount = _changes.Count;
			if (_propagator == null)
			{
				_propagator = new AdaptiveLocalPropagation(this);
			}
			bool result = _propagator.Propagate(out conflict);
			QueryAbort = queryAbort2;
			return result;
		}

		/// <summary>
		///   Solve the model; constructs a solution enumerator 
		///   specific to a particular directive
		/// </summary>
		internal ConstraintSolverSolution SolveCore(ConstraintSolverParams param)
		{
			ConstraintSolverSolution constraintSolverSolution = new ConstraintSolverSolution(this);
			constraintSolverSolution.SolverParams = param;
			constraintSolverSolution.GetNext();
			return constraintSolverSolution;
		}

		private SolverKind DecideSolverKind(ConstraintSolverParams directives)
		{
			if (!IsPureIntegerModel || directives.Algorithm == ConstraintSolverParams.CspSearchAlgorithm.LocalSearch)
			{
				return SolverKind.CspSolver;
			}
			if (directives == null)
			{
				return SolverKind.CspSolver | SolverKind.IntegerSolver;
			}
			if ((directives.VariableSelection == ConstraintSolverParams.TreeSearchVariableOrdering.DynamicWeighting || directives.VariableSelection == ConstraintSolverParams.TreeSearchVariableOrdering.Any) && (directives.ValueSelection == ConstraintSolverParams.TreeSearchValueOrdering.ForwardOrder || directives.ValueSelection == ConstraintSolverParams.TreeSearchValueOrdering.Any))
			{
				return SolverKind.CspSolver | SolverKind.IntegerSolver;
			}
			return SolverKind.IntegerSolver;
		}

		/// <summary> Get a Term for the immutable value k.  Give it a key for convenience,
		///           though this will NOT (as a constant) be visible in the Variables dictionary.
		/// </summary>
		/// <remarks> For OML compatibility only. Deprecated.</remarks>
		internal CspTerm SymbolicConstant(int k, object key)
		{
			return new CspVariable(this, CspIntervalDomain.Create(k, k), CspSolverTerm.TermKinds.Constant, key);
		}

		/// <summary>
		/// Public interface for saving a model.
		/// </summary>
		/// <param name="textWriter"></param>
		/// <returns></returns>
		public SerializationStatus Save(TextWriter textWriter)
		{
			CspNativeSaveVisitor cspNativeSaveVisitor = new CspNativeSaveVisitor();
			return cspNativeSaveVisitor.Save(this, textWriter);
		}

		internal CspTerm CreateVariableWithKind(CspSolverDomain domain, CspSolverTerm.TermKinds kind, object key)
		{
			ValidateVarDomain(domain);
			return new CspVariable(this, domain, kind, key);
		}

		internal CspTerm CreateVariableWithKind(CspSolverDomain domain, CspSolverTerm.TermKinds kind)
		{
			ValidateVarDomain(domain);
			return new CspVariable(this, domain, kind);
		}

		internal CspTerm CreateBooleanWithKind(CspSolverTerm.TermKinds kind, object key)
		{
			return new CspBooleanVariable(this, kind, key);
		}

		internal CspTerm CreateBooleanWithKind(CspSolverTerm.TermKinds kind)
		{
			return new CspBooleanVariable(this, kind);
		}

		internal void Accept(IVisitor visitor)
		{
			foreach (CspTerm allTerm in _allTerms)
			{
				CspSolverTerm cspSolverTerm = allTerm as CspSolverTerm;
				if (cspSolverTerm is CspVariable variable)
				{
					if (variable != _TTrue && variable != _TFalse && !variable.IsConstant)
					{
						visitor.Visit(variable.Values[0]);
						visitor.VisitDefinition(ref variable);
					}
				}
				else
				{
					cspSolverTerm.Accept(visitor);
				}
			}
			foreach (CspSolverTerm constraint2 in _constraints)
			{
				CspSolverTerm constraint = constraint2;
				visitor.VisitConstraint(ref constraint);
			}
			foreach (CspSolverTerm minimizationGoal in _minimizationGoals)
			{
				CspSolverTerm goal = minimizationGoal;
				visitor.VisitGoal(ref goal);
			}
		}

		/// <summary> This is a convenience function.  It will compose a Vector from any column of a Term array.
		/// </summary>
		internal static CspTerm[] GetColumn(CspTerm[][] termArray, int column)
		{
			int num = termArray.Length;
			if (num < 1)
			{
				return null;
			}
			CspTerm[] array = new CspTerm[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = termArray[i][column];
			}
			return array;
		}

		/// <summary> Get a random generator
		/// </summary>
		/// <remarks> This is for local search in order to guarantee
		///           that we explore the same initial assignments and
		///           make the same "random" moves from this assignment
		///           independently of the number of threads. Each time
		///           we do a restart we pick the next generator,
		///           which guarantees that if N restarts happen they
		///           will explore the same points, independently on the
		///           number fo threads.
		/// </remarks>
		internal Random GetNextRandomGenerator()
		{
			int seed;
			lock (this)
			{
				seed = _prngSeed++;
			}
			return new Random(seed);
		}

		internal CspTerm CreateTemplateVariable(CspDomain domain, object key)
		{
			ValidateVarDomain(domain);
			return new CspVariable(this, domain as CspSolverDomain, CspSolverTerm.TermKinds.TemplateVariable, key);
		}

		internal CspTerm CreateTemplateBoolean(object key)
		{
			return new CspBooleanVariable(this, CspSolverTerm.TermKinds.TemplateVariable, key);
		}

		internal CspTerm[] CreateTemplateVariableVector(CspDomain domain, object key, int length)
		{
			return CreateVariableVectorCore(CspSolverTerm.TermKinds.TemplateVariable, domain, key, length);
		}

		internal CspTerm[] CreateTemplateBooleanVector(object key, int length)
		{
			return CreateBooleanVectorCore(CspSolverTerm.TermKinds.TemplateVariable, key, length);
		}

		internal CspTerm[] CreateVariableVectorCore(CspSolverTerm.TermKinds kind, CspDomain domain, object key, int length)
		{
			if (_elementKeySizes.ContainsKey(key))
			{
				throw new ArgumentException(Resources.DuplicateKey + ((key != null) ? key.ToString() : "null"));
			}
			_elementKeySizes.Add(key, -length);
			CspTerm[] array = new CspTerm[length];
			for (int i = 0; i < length; i++)
			{
				ElementKey key2 = new ElementKey(key, i);
				if (kind == CspSolverTerm.TermKinds.DecisionVariable)
				{
					array[i] = CreateVariable(domain, key2);
				}
				else
				{
					array[i] = CreateTemplateVariable(domain, key2);
				}
			}
			return array;
		}

		internal CspTerm[] CreateBooleanVectorCore(CspSolverTerm.TermKinds kind, object key, int length)
		{
			if (_elementKeySizes.ContainsKey(key))
			{
				throw new ArgumentException(Resources.DuplicateKey + ((key != null) ? key.ToString() : "null"));
			}
			_elementKeySizes.Add(key, -length);
			CspTerm[] array = new CspTerm[length];
			for (int i = 0; i < length; i++)
			{
				ElementKey key2 = new ElementKey(key, i);
				if (kind == CspSolverTerm.TermKinds.DecisionVariable)
				{
					array[i] = CreateBoolean(key2);
				}
				else
				{
					array[i] = CreateTemplateBoolean(key2);
				}
			}
			return array;
		}

		/// <summary> Get a sequence of solutions (or optimal solutions when there are goals) to the model.  Each solution is copied into a Dictionary.
		///           The solution values are immutable.  You may overlap reading and using a solution with
		///           solving for subsequent solutions. In case there are optimization goals, only optimal
		///           solutions will be returned.
		/// </summary>
		internal IEnumerable<Dictionary<CspTerm, object>> EnumerateSolutions()
		{
			CheckDomainNarrowing();
			_stopwatch.Reset();
			_stopwatch.Start();
			foreach (int item in InnerSolve(yieldSuboptimals: false))
			{
				_ = item;
				_stopwatch.Stop();
				_params.SetElapsed((int)_stopwatch.ElapsedMilliseconds);
				yield return _baseSolver.SnapshotVariablesValues();
				_stopwatch.Start();
			}
			_stopwatch.Stop();
			_params.SetElapsed((int)_stopwatch.ElapsedMilliseconds);
		}

		/// <summary> Get a sequence of interim solutions to the model when doing optimization.  Each solution is copied into a Dictionary.
		///           The solution values are immutable.  You may overlap reading and using a solution with
		///           solving for subsequent solutions. In case there are optimization goals, both optimal
		///           and suboptimal solutions will be returned.
		/// </summary>
		internal IEnumerable<Dictionary<CspTerm, object>> EnumerateInterimSolutions()
		{
			CheckDomainNarrowing();
			_stopwatch.Reset();
			_stopwatch.Start();
			foreach (int item in InnerSolve(yieldSuboptimals: true))
			{
				_ = item;
				_stopwatch.Stop();
				_params.SetElapsed((int)_stopwatch.ElapsedMilliseconds);
				yield return _baseSolver.SnapshotVariablesValues();
				_stopwatch.Start();
			}
			_stopwatch.Stop();
			_params.SetElapsed((int)_stopwatch.ElapsedMilliseconds);
		}

		/// <summary> Get a sequence of solutions to the model.  Each solution is copied into a Dictionary.
		///           The solution values are immutable.  You may overlap reading and using a solution with
		///           solving for subsequent solutions. In case there are optimization goals, only optimal
		///           solutions will be returned.
		/// </summary>
		internal IEnumerable<Dictionary<CspTerm, int>> EnumerateIntegerSolutions()
		{
			CheckDomainNarrowing();
			_stopwatch.Reset();
			_stopwatch.Start();
			foreach (int item in InnerSolve(yieldSuboptimals: false))
			{
				_ = item;
				_stopwatch.Stop();
				_params.SetElapsed((int)_stopwatch.ElapsedMilliseconds);
				yield return _baseSolver.SnapshotVariablesIntegerValues();
				_stopwatch.Start();
			}
			_stopwatch.Stop();
			_params.SetElapsed((int)_stopwatch.ElapsedMilliseconds);
		}

		/// <summary>
		/// Fix/Unfix the given user variable's value based on var.IsFixing(). 
		/// </summary>
		/// <param name="var">The user variable to fix/unfix</param>
		internal void FixUserVariable(CspTerm var)
		{
			CspUserVariable cspUserVariable = var as CspUserVariable;
			DebugContracts.NonNull(cspUserVariable);
			if (_baseSolver != null && _baseSolver is DomainNarrowingWithACS domainNarrowingWithACS)
			{
				domainNarrowingWithACS.TryFixUserVariable(cspUserVariable);
			}
		}

		internal CspSolverTerm AsBoolean(CspTerm input)
		{
			if (input == null)
			{
				throw new ArgumentNullException(Resources.NullInput);
			}
			if (!(input is CspSolverTerm cspSolverTerm))
			{
				throw new ArgumentException(Resources.NotBoolean + input.ToString());
			}
			if (!cspSolverTerm.IsBoolean)
			{
				return (CspSolverTerm)Unequal(input, Constant(0));
			}
			return cspSolverTerm;
		}

		internal CspSolverTerm[] AsBooleans(CspTerm[] inputs)
		{
			if (inputs == null)
			{
				throw new ArgumentNullException(Resources.NullInput);
			}
			if (inputs.Length < 1)
			{
				throw new ArgumentNullException(Resources.NullInput);
			}
			CspSolverTerm[] array = new CspSolverTerm[inputs.Length];
			for (int i = 0; i < inputs.Length; i++)
			{
				array[i] = AsBoolean(inputs[i]);
			}
			return array;
		}

		internal static CspSolverTerm AsTerm(CspTerm input)
		{
			if (input == null)
			{
				throw new ArgumentNullException(Resources.NullInput);
			}
			if (!(input is CspSolverTerm result))
			{
				throw new ArgumentException(Resources.UnknowTermType + input.ToString());
			}
			return result;
		}

		internal static CspSolverTerm[] AsTerms(CspSolverTerm solo, CspTerm[] inputs)
		{
			if (inputs == null)
			{
				throw new ArgumentNullException(Resources.NullInput);
			}
			if (inputs.Length < 1)
			{
				throw new ArgumentNullException(Resources.NullInput);
			}
			int num = ((solo != null) ? 1 : 0);
			CspSolverTerm[] array = new CspSolverTerm[num + inputs.Length];
			if (0 < num)
			{
				array[0] = solo;
			}
			for (int i = 0; i < inputs.Length; i++)
			{
				array[num + i] = AsTerm(inputs[i]);
			}
			return array;
		}

		internal static CspSolverTerm[] AsTerms(CspTerm[] inputs)
		{
			return AsTerms(null, inputs);
		}

		internal static void UpdateAllTermCount(ConstraintSystem solver, int newlyAddedTermCount)
		{
			solver._allTermCount += newlyAddedTermCount;
			if (License.CspTermLimit != 0 && solver._allTermCount >= License.CspTermLimit)
			{
				throw new MsfLicenseException();
			}
		}

		internal void AddVariable(CspVariable variable)
		{
			_variables.Add(variable);
			if (variable.BaseValueSet.Count > 1)
			{
				_variablesExcludingConstants.Add(variable);
			}
			if (variable.Key != null)
			{
				_varDict.Add(variable.Key, variable);
			}
			AddChange(variable, variable.Values.Count);
		}

		internal void AddTerm(CspSolverTerm variable)
		{
			_allTerms.Add(variable);
		}

		internal void AddComposite(CspComposite comp)
		{
			_composites.Add(comp);
			if (comp.Key != null)
			{
				_compositeDict.Add(comp.Key, comp);
			}
		}

		internal void AddCompositeVariable(CspCompositeVariable compVar)
		{
			_compositeVariables.Add(compVar);
		}

		/// <summary> Record the Term which changed and how deep its prior value stack was
		/// </summary>
		internal void AddChange(CspSolverTerm change, int valueCount)
		{
			if (IsTracing)
			{
				TraceSource.TraceEvent(TraceEventType.Verbose, 0, string.Format(CultureInfo.InvariantCulture, Resources.AddChange01, new object[2]
				{
					change.Ordinal.ToString(CultureInfo.InvariantCulture),
					change.Count
				}));
			}
			if (0 < _changes.Count)
			{
				KeyValuePair<CspSolverTerm, int> keyValuePair = _changes[_changes.Count - 1];
				if (keyValuePair.Key == change && keyValuePair.Value == valueCount)
				{
					throw new InvalidOperationException(Resources.UnnecessaryChange);
				}
			}
			_changes.Add(new KeyValuePair<CspSolverTerm, int>(change, valueCount));
			change.ChangeCount = _changes.Count;
		}

		/// <summary>
		/// Backtrack to an earlier consistent point in the decision stack.
		/// </summary>
		internal void Backtrack(int backtrackCount)
		{
			for (int i = backtrackCount; i < _changes.Count; i++)
			{
				KeyValuePair<CspSolverTerm, int> keyValuePair = _changes[i];
				keyValuePair.Key.Backtrack(keyValuePair.Value - 1);
			}
			_changes.RemoveRange(backtrackCount, _changes.Count - backtrackCount);
			if (_propagator != null)
			{
				_propagator.Backtrack(backtrackCount);
			}
		}

		internal IEnumerable<int> InnerSolve(bool yieldSuboptimals)
		{
			if (!_fIsInModelingPhase)
			{
				throw new InvalidOperationException(Resources.AlreadySolving);
			}
			_fIsInModelingPhase = false;
			_verbose = false;
			_isInterrupted = false;
			_params.Abort = false;
			_baseChangeCount = _changes.Count;
			if (_propagator == null)
			{
				_propagator = new AdaptiveLocalPropagation(this);
			}
			if (!_propagator.Propagate(out var _))
			{
				yield break;
			}
			if (_params.Algorithm == ConstraintSolverParams.CspSearchAlgorithm.LocalSearch)
			{
				_baseSolver = new LocalSearchSolver(this, _params.MoveSelection);
			}
			else if (_minimizationGoals.Count <= 0)
			{
				_baseSolver = new AdaptiveConflictSeeking(this);
			}
			else
			{
				_baseSolver = new LinearSearchOnGoalsWithACS(this);
			}
			foreach (int item in _baseSolver.Search(yieldSuboptimals))
			{
				yield return item;
			}
		}

		internal object GetValue(CspTerm variable)
		{
			if (_baseSolver == null)
			{
				throw new InvalidOperationException(Resources.ModelNotSolved);
			}
			return _baseSolver.GetValue(variable);
		}

		/// <summary>
		/// Check to see if the solving should be aborted
		/// </summary>
		/// <remarks>Note that the method has a side-effect that caches the value into _isInterrupted</remarks>
		internal bool CheckAbort()
		{
			_isInterrupted = _isInterrupted || _params.Abort || _stopwatch.ElapsedMilliseconds >= _params.TimeLimitMilliSec;
			return _isInterrupted;
		}

		private static bool ValidateIndexArrayShape(CspTerm[][] array)
		{
			if (array == null || array.Length == 0)
			{
				return false;
			}
			int num = array[0].Length;
			foreach (CspTerm[] array2 in array)
			{
				if (array2.Length != num)
				{
					return false;
				}
			}
			return true;
		}

		private static bool StandardIndexConstraint(CspDomain baseDomain, int cardinality)
		{
			if (baseDomain == null)
			{
				return false;
			}
			if (baseDomain.Count == cardinality && baseDomain.First == 0)
			{
				return baseDomain.Last == cardinality - 1;
			}
			return false;
		}

		private void MarkNonIntegerModel()
		{
			IsPureIntegerModel = false;
		}

		private static void ValidateSet(int[] orderedUniqueSet, int from, int count)
		{
			if (!CspSetDomain.IsOrderedUniqueSet(orderedUniqueSet, from, count))
			{
				throw new ArgumentException(Resources.InvalidSetDomainDefinition);
			}
		}

		private static void ValidateSet(int precision, double[] orderedUniqueSet, int from, int count)
		{
			if (!CspSetDomain.IsOrderedUniqueSet(precision, orderedUniqueSet, from, count))
			{
				throw new ArgumentException(Resources.InvalidSetDomainDefinition);
			}
		}

		private static void ValidateSymbols(string[] uniqueSymbols)
		{
			if (!CspSymbolDomain.IsUniqueSet(uniqueSymbols))
			{
				throw new ArgumentException(Resources.InvalidSymbolDomainDefinition);
			}
		}

		private static void ValidateInterval(int min, int max)
		{
			if (min > max)
			{
				throw new ArgumentException(Resources.InvalidIntervalDomainDefinition);
			}
		}

		private static void ValidateInterval(double min, double max)
		{
			if (min > max)
			{
				throw new ArgumentException(Resources.InvalidIntervalDomainDefinition);
			}
		}

		internal static void ValidatePrecision(int precision)
		{
			if (precision != 1 && precision != 10 && precision != 100 && precision != 1000 && precision != 10000)
			{
				throw new ArgumentException(Resources.InvalidDecimalPrecision);
			}
		}

		private static void ValidateVarDomain(CspDomain domain)
		{
			if (domain == null || !(domain is CspSolverDomain))
			{
				throw new ArgumentNullException(Resources.NullDomain);
			}
		}

		private static void ValidateVarDomains(CspDomain[] domains)
		{
			if (domains == null || domains.Length == 0)
			{
				throw new ArgumentNullException(Resources.NullDomain);
			}
			foreach (CspDomain domain in domains)
			{
				ValidateVarDomain(domain);
			}
		}

		private static void ValidateVarDomain(CspComposite domain)
		{
			if (domain == null)
			{
				throw new ArgumentNullException(Resources.NullDomain);
			}
		}

		private void CheckInModelingPhase()
		{
			if (!_fIsInModelingPhase)
			{
				throw new InvalidOperationException(Resources.AlreadySolving);
			}
		}

		private void ValidateInputs(params CspTerm[] inputs)
		{
			if (inputs == null || inputs.Length <= 0)
			{
				throw new ArgumentNullException(Resources.NullInput);
			}
			foreach (CspTerm cspTerm in inputs)
			{
				if (cspTerm == null)
				{
					throw new ArgumentNullException(Resources.NullInput);
				}
				if (cspTerm.Model != this)
				{
					throw new InvalidOperationException(Resources.IncompatibleInputTerms + cspTerm.ToString());
				}
				if (cspTerm is CspCompositeVariable)
				{
					throw new InvalidOperationException(Resources.CompositeDomainNotSupported + cspTerm.ToString());
				}
			}
		}

		private void ValidateInputs(CspTerm[][] inputs)
		{
			if (inputs == null || inputs.Length <= 0)
			{
				throw new ArgumentNullException(Resources.NullInput);
			}
			foreach (CspTerm[] inputs2 in inputs)
			{
				ValidateInputs(inputs2);
			}
		}

		/// <summary> Check to see if all inputs are the same term. If so, the product can be rewritten to a power.
		/// </summary>
		/// <returns>The exponent of the power if &gt; 0. Otherwise, inputs are not the same term.</returns>
		private static int CheckForPower(CspTerm[] inputs)
		{
			if (inputs.Length == 0)
			{
				return -1;
			}
			CspTerm cspTerm = inputs[0];
			for (int i = 1; i < inputs.Length; i++)
			{
				if (cspTerm != inputs[i])
				{
					return -1;
				}
			}
			return inputs.Length;
		}

		private void NarrowingDomains()
		{
			try
			{
				(_baseSolver as DomainNarrowingWithACS).SearchForValidValues();
			}
			catch (ThreadAbortException)
			{
			}
		}

		private void CheckDomainNarrowing()
		{
			if (_userVars == null || _userVars.Count <= 0)
			{
				return;
			}
			StopNarrowingDomains();
			foreach (CspUserVariable userVar in _userVars)
			{
				if (userVar.IsFixing())
				{
					int fixedValue = userVar.GetFixedValue();
					if (userVar.FiniteValue.Count != 1 && userVar.FiniteValue.Contains(fixedValue))
					{
						userVar.Restrain(CspIntervalDomain.Create(fixedValue, fixedValue));
					}
				}
			}
		}

		private static bool IsInputComposite(CspTerm[] inputs, out CspComposite domain)
		{
			domain = null;
			bool flag = true;
			for (int i = 0; i < inputs.Length; i++)
			{
				CspSolverTerm cspSolverTerm = (CspSolverTerm)inputs[i];
				if (cspSolverTerm is CspCompositeVariable cspCompositeVariable)
				{
					if (flag)
					{
						domain = cspCompositeVariable.DomainComposite;
					}
					if (cspCompositeVariable.DomainComposite != domain)
					{
						throw new ArgumentException(Resources.CompositeDomainIncompatible);
					}
				}
				else if (domain != null)
				{
					throw new ArgumentException(Resources.CompositeDomainIncompatible);
				}
				flag = false;
			}
			return domain != null;
		}

		private CspTerm CompositeEqual(CspTerm[] inputs, CspComposite domain)
		{
			List<CspTerm> list = new List<CspTerm>();
			foreach (CspComposite.Tuple allField in domain.AllFields)
			{
				object key = allField.Key;
				for (int i = 0; i < allField.Arity; i++)
				{
					List<CspTerm> list2 = new List<CspTerm>();
					for (int j = 0; j < inputs.Length; j++)
					{
						CspSolverTerm cspSolverTerm = (CspSolverTerm)inputs[j];
						CspCompositeVariable cspCompositeVariable = cspSolverTerm as CspCompositeVariable;
						list2.Add(cspCompositeVariable.FieldInternal(key, i));
					}
					list.Add(Equal(list2.ToArray()));
				}
			}
			return And(list.ToArray());
		}
	}
}
