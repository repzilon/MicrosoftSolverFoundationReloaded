using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Main class for creating and solving problems using Disolver.
	/// </summary>
	internal class IntegerSolver
	{
		private static DisolverDiscreteDomain _defaultDomain = new ConvexDomain(ConstraintSystem.MinFinite, ConstraintSystem.MaxFinite);

		private List<DisolverTerm> _variables;

		private List<DisolverIntegerTerm> _objectives;

		private List<DisolverTerm> _constraints;

		private List<DisolverTerm> _allTerms;

		private List<DisolverBooleanTerm> _allBooleanTerms;

		private List<DisolverIntegerTerm> _allIntegerTerms;

		private Dictionary<object, DisolverTerm> _namedVariables;

		private Dictionary<DisolverBooleanTerm, DisolverBooleanAsInteger> _booleanToIntegerConversions;

		private DisolverBooleanTerm _constantTrue;

		private DisolverBooleanTerm _constantFalse;

		private Dictionary<long, DisolverIntegerTerm> _integerConstants;

		private Dictionary<DisolverBooleanTerm, DisolverBooleanTerm> _negations;

		/// <summary>
		///   Time for resolution
		/// </summary>
		internal Stopwatch _watch;

		private VariableEnumerationStrategy _variableStrategy;

		private ValueEnumerationStrategy _valueStrategy;

		/// <summary>
		///   If set to true, Disolver will regularly display info
		/// </summary>
		/// <remarks>
		///   Should use a stream in which to inject the comments instead
		/// </remarks>
		private bool _verbose;

		/// <summary>
		///   If set to true, Disolver will use a preprocessing simplifying
		///   the problem by "singleton-arc-consistency"
		/// </summary>
		private bool _useshaving;

		/// <summary>
		///   Copy of the statistics produced by the last tree search
		/// </summary>
		private TreeSearchStatistics _treeSearchStatistics = new TreeSearchStatistics();

		/// <summary>
		///   parameters of CspSolverBase root class
		/// </summary>
		private ConstraintSolverParams _parameters;

		/// <summary>
		///   set to true when aborting 
		/// </summary>
		private bool _hasAborted;

		/// <summary>
		/// Term map that maps model terms to IntegerSolver terms. Created only when a clone ctor is called
		/// </summary>
		private Dictionary<CspTerm, CspTerm> _termMap;

		/// <summary>
		/// Stores the base model from which this IntegerSolver instance is cloned
		/// </summary>
		private ConstraintSystem _baseModel;

		/// <summary>
		///   Term representing the Boolean constant false   
		/// </summary>
		public CspTerm False => _constantFalse;

		/// <summary>
		///  Term representing the Boolean constant true   
		/// </summary>
		public CspTerm True => _constantTrue;

		/// <summary>
		///   Get the domain that variables take by default
		/// </summary>
		public CspDomain DefaultInterval => _defaultDomain;

		public CspDomain DefaultBoolean => new ConvexDomain(0L, 1L);

		public CspDomain Empty => ConvexDomain.Empty();

		/// <summary>
		///   enumerates all terms created by this solver
		/// </summary>
		internal List<CspTerm> AllTerms
		{
			get
			{
				List<CspTerm> list = new List<CspTerm>();
				foreach (DisolverTerm allTerm in _allTerms)
				{
					list.Add(allTerm);
				}
				return list;
			}
		}

		/// <summary>
		///   returns all variables of the problem
		/// </summary>
		public IEnumerable<CspTerm> Variables
		{
			get
			{
				foreach (DisolverTerm variable in _variables)
				{
					yield return variable;
				}
			}
		}

		/// <summary>
		///   returns all constraints of the problem
		/// </summary>
		public IEnumerable<CspTerm> Constraints
		{
			get
			{
				foreach (DisolverTerm constraint in _constraints)
				{
					yield return constraint;
				}
			}
		}

		/// <summary>
		///   returns all enumeration goals of the problem
		/// </summary>
		public IEnumerable<CspTerm> MinimizationGoals
		{
			get
			{
				foreach (DisolverIntegerTerm objective in _objectives)
				{
					yield return objective;
				}
			}
		}

		/// <summary>
		///   True of we are doing optimization, i.e. the 
		///   solver has at least one optimization goal
		/// </summary>
		public bool HasMinimizationGoals => (_objectives != null) & (_objectives.Count != 0);

		public bool IsEmpty => _allTerms.Count == 0;

		/// <summary>Does the problem have optimization goals? </summary>
		private bool IsOptimizing
		{
			get
			{
				if (_objectives != null)
				{
					return _objectives.Count != 0;
				}
				return false;
			}
		}

		public int GoalCount => _objectives.Count;

		public bool IsInterrupted => _hasAborted;

		public ConstraintSolverParams Parameters => _parameters;

		public int BacktrackCount => _treeSearchStatistics.NbFails;

		/// <summary>
		///   Gives time since current search was run
		///   (in general called during the search)
		/// </summary>
		internal long ElapsedMilliSec => _watch.ElapsedMilliseconds;

		/// <summary>
		///   Option saying whether the solver should display
		///   regular activity messages. Set or get.
		/// </summary>
		public bool Verbose
		{
			get
			{
				return _verbose;
			}
			set
			{
				_verbose = value;
			}
		}

		/// <summary>
		///   option saying whether shaving should be used
		///   as pre-process
		/// </summary>
		public bool UseShaving
		{
			get
			{
				return _useshaving;
			}
			set
			{
				_useshaving = value;
			}
		}

		/// <summary>
		///   True if solver has timed out
		/// </summary>
		public bool HasAborted => _hasAborted;

		/// <summary>
		///   Gets the statistics of the latest called tree-search.
		/// </summary>
		public TreeSearchStatistics Statistics => _treeSearchStatistics;

		/// <summary>
		///   Option telling the solver whether it should 
		///   diversify its search by restarting every now and then
		/// </summary>
		public bool UseRestarts
		{
			get
			{
				return _parameters.RestartEnabled;
			}
			set
			{
				_parameters.RestartEnabled = value;
			}
		}

		/// <summary>
		///   Search strategy of the solver - kept as string
		///   for simplicity
		/// </summary>
		private CspSearchStrategy Strategy => new CspSearchStrategy(_variableStrategy, _valueStrategy, UseRestarts);

		internal Dictionary<CspTerm, CspTerm> TermMap => _termMap;

		/// <summary>
		///   Construction
		/// </summary>
		/// <param name="abort">call back for search interruption</param>
		public IntegerSolver(Func<bool> abort)
		{
			_parameters = new ConstraintSolverParams();
			_parameters.QueryAbort = abort;
			_watch = new Stopwatch();
			_namedVariables = new Dictionary<object, DisolverTerm>();
			_allBooleanTerms = new List<DisolverBooleanTerm>();
			_allIntegerTerms = new List<DisolverIntegerTerm>();
			_allTerms = new List<DisolverTerm>();
			_variables = new List<DisolverTerm>();
			_objectives = new List<DisolverIntegerTerm>();
			_constraints = new List<DisolverTerm>();
			_integerConstants = new Dictionary<long, DisolverIntegerTerm>();
			_negations = new Dictionary<DisolverBooleanTerm, DisolverBooleanTerm>();
			_booleanToIntegerConversions = new Dictionary<DisolverBooleanTerm, DisolverBooleanAsInteger>();
			_variableStrategy = VariableEnumerationStrategy.MinDom;
			_valueStrategy = ValueEnumerationStrategy.Random;
			UseRestarts = true;
			_constantFalse = new DisolverBooleanConstant(this);
			_constantTrue = new DisolverBooleanConstant(this);
			_constantFalse.SetInitialValue(b: false);
			_constantTrue.SetInitialValue(b: true);
			_useshaving = false;
			_termMap = null;
		}

		/// <summary>
		///   Default constructor.
		/// </summary>
		public IntegerSolver()
			: this(Utils.AlwaysFalse)
		{
		}

		/// <summary>
		///   Construction by copy of an initial solver
		/// </summary>
		public IntegerSolver(ConstraintSystem source, Func<bool> abortCallBack)
			: this()
		{
			_termMap = ConstraintSystem.AppendModel(source, this);
			_parameters = new ConstraintSolverParams(source.Parameters, _termMap);
			_parameters.QueryAbort = abortCallBack;
			_baseModel = source;
		}

		/// <summary>
		///    Call implicitly any time a Term is created 
		///    - do not call anywhere else.
		/// </summary>
		internal void AddTerm(DisolverTerm t)
		{
			_allTerms.Add(t);
		}

		/// <summary>
		///   Call implicitly any time a Boolean Term is created
		///   - do not call anywhere else.
		/// </summary>
		internal void AddBooleanTerm(DisolverBooleanTerm t)
		{
			_allBooleanTerms.Add(t);
		}

		/// <summary>
		///   Call implicitly any time a Integer Term is created
		///   - do not call anywhere else.
		/// </summary>
		internal void AddIntegerTerm(DisolverIntegerTerm t)
		{
			_allIntegerTerms.Add(t);
		}

		/// <summary>
		///   Creation of a term representing an integer constant
		/// </summary>
		/// <param name="k">constant</param>
		public CspTerm Constant(long k)
		{
			if (!_integerConstants.TryGetValue(k, out var value))
			{
				value = new DisolverIntegerConstant(this, k);
				_variables.Add(value);
				_integerConstants.Add(k, value);
			}
			return value;
		}

		/// <summary>
		///   Creates a variable with the given domain and 
		///   an auto-generated key
		/// </summary>
		public CspTerm CreateVariable(CspDomain domain)
		{
			return CreateVariable(domain, "@I" + _variables.Count);
		}

		/// <summary>
		///   creates a variable with the default range and
		///   a key of type string
		/// </summary>
		/// <remarks>
		///   This method is essentially for backward compatibility;
		///   we have old versions 
		/// </remarks>
		public CspTerm CreateVariable(string name)
		{
			return CreateVariable(DefaultInterval, name);
		}

		/// <summary>
		///   Create an integer variable ranging over the
		///   indicated domain. 
		/// </summary>
		/// <param name="domain">initial domain</param>
		/// <param name="key">key by which the variable is designated</param>
		public CspTerm CreateVariable(CspDomain domain, object key)
		{
			if (key == null || _namedVariables.ContainsKey(key))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.WrongVariableIdentifier0KeysShouldBeNonNullAndUnique, new object[1] { key.ToString() }));
			}
			DisolverDiscreteDomain dom = DisolverDiscreteDomain.SubCast(domain);
			DisolverIntegerVariable disolverIntegerVariable = new DisolverIntegerVariable(this, dom, key, userDefined: true);
			_variables.Add(disolverIntegerVariable);
			return disolverIntegerVariable;
		}

		/// <summary>
		///   Creates a Boolean variable with an auto-generated key
		/// </summary>
		public CspTerm CreateBoolean()
		{
			return CreateBoolean("@B" + _variables.Count);
		}

		/// <summary>
		///   Creates a Boolean variable
		/// </summary>
		/// <param name="key">key by which the variable is designated</param>
		public CspTerm CreateBoolean(object key)
		{
			if (key == null || _namedVariables.ContainsKey(key))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.WrongVariableIdentifier0KeysShouldBeNonNullAndUnique, new object[1] { key.ToString() }));
			}
			DisolverBooleanTerm disolverBooleanTerm = new DisolverBooleanVariable(this, key, userDefined: true);
			_variables.Add(disolverBooleanTerm);
			return disolverBooleanTerm;
		}

		/// <summary>
		///   method aimed at being used exclusively by DisolverTerms;
		///   whenever a term receives a key (at creation time or by direct use
		///   of the Key property), this ensures that necessary updates are done.
		///   the "_named" fields should not be taken care of anywhere else.
		/// </summary>
		internal void RecordKey(object key, DisolverTerm s)
		{
			_namedVariables.Add(key, s);
		}

		/// <summary>
		///   Creates an array of variables with homogeneous domains
		/// </summary>
		/// <param name="dom">domain, common to all variables</param>
		/// <param name="name">
		///   name of the array; each position will be given a 
		///   name of the form "name[0]", etc.
		/// </param>
		/// <param name="nb">size of the array</param>
		/// <returns></returns>
		public CspTerm[] CreateVariableVector(CspDomain dom, string name, int nb)
		{
			CspTerm[] array = new CspTerm[nb];
			for (int i = 0; i < nb; i++)
			{
				array[i] = CreateVariable(dom, name + "[" + i + "]");
			}
			return array;
		}

		/// <summary>
		///   Creates a bi-dimensional array of variables of homogeneous domains
		/// </summary>
		public CspTerm[][] CreateVariableArray(CspDomain dom, string name, int nb1, int nb2)
		{
			CspTerm[][] array = new CspTerm[nb1][];
			for (int i = 0; i < nb1; i++)
			{
				array[i] = new CspTerm[nb2];
				for (int j = 0; j < nb2; j++)
				{
					array[i][j] = CreateVariable(dom, name + "[" + i + "][" + j + "]");
				}
			}
			return array;
		}

		/// <summary>
		///   Creates an array of Boolean variables
		/// </summary>
		/// <param name="name">
		///   name of the array; each position will be given a 
		///   name of the form "name[0]", etc.
		/// </param>
		/// <param name="nb">size of the array</param>
		/// <returns></returns>
		public CspTerm[] CreateBooleanVector(string name, int nb)
		{
			CspTerm[] array = new CspTerm[nb];
			for (int i = 0; i < nb; i++)
			{
				array[i] = CreateBoolean(name + "[" + i + "]");
			}
			return array;
		}

		/// <summary>
		///   Creates a domain described in extension by its
		///   set of values
		/// </summary>
		/// <param name="values">list of values in the domain</param>
		public CspDomain CreateIntegerSet(params int[] values)
		{
			return new SparseDomain(values);
		}

		public CspDomain CreateIntegerSet(int[] orderedUniqueSet, int from, int count)
		{
			return new SparseDomain(orderedUniqueSet, from, count);
		}

		/// <summary>
		///   Creates a domain described in extension by its
		///   bounds (the domain contains all values between these bounds)
		/// </summary>
		/// <param name="first">lower bound</param>
		/// <param name="last">upper bound</param>
		public CspDomain CreateIntegerInterval(int first, int last)
		{
			return new ConvexDomain(first, last);
		}

		/// <summary>
		///   Creates a domain whose values are symbols
		/// </summary>
		public CspDomain CreateSymbolSet(params string[] uniqueSymbols)
		{
			return new DisolverSymbolSet(uniqueSymbols);
		}

		/// <summary>
		///   get the term that has the corresponding key
		/// </summary>
		/// <param name="key">identifier of the term</param>
		/// <param name="term">resulting term</param>
		/// <returns>true iff the key is found</returns>
		public bool TryGetVariableFromKey(object key, out CspTerm term)
		{
			DisolverTerm value;
			bool result = _namedVariables.TryGetValue(key, out value);
			term = value;
			return result;
		}

		/// <summary>
		///   Negation: term that is true if subterm is false
		/// </summary>
		/// <param name="input">subterm</param>
		public CspTerm Not(CspTerm input)
		{
			DisolverBooleanTerm disolverBooleanTerm = ToBooleanTerm(input);
			if (!_negations.TryGetValue(disolverBooleanTerm, out var value))
			{
				value = new DisolverNot(this, disolverBooleanTerm);
				_negations.Add(disolverBooleanTerm, value);
				_negations.Add(value, disolverBooleanTerm);
			}
			return value;
		}

		/// <summary>
		///   Disjunction: term that is true iff one of the subterms is
		/// </summary>
		/// <param name="inputs">list of subterms</param>
		public CspTerm Or(params CspTerm[] inputs)
		{
			switch (inputs.Length)
			{
			case 0:
				throw new ArgumentException(Resources.AtLeast1ArgumentExpected);
			case 1:
				return ToBooleanTerm(inputs[0]);
			default:
				return new DisolverOr(this, ToDisolverTermArray(inputs));
			}
		}

		/// <summary>
		///   Logical implication (A implies B)
		/// </summary>
		/// <param name="antecedent">left-hand side of the implication</param>
		/// <param name="consequent">right-hand side of the implication</param>
		public CspTerm Implies(CspTerm antecedent, CspTerm consequent)
		{
			return Or(Not(antecedent), consequent);
		}

		/// <summary> 
		///   Conjunction: term that is true iff all the subterms are
		/// </summary>
		/// <param name="inputs">list of subterms</param>
		public CspTerm And(params CspTerm[] inputs)
		{
			switch (inputs.Length)
			{
			case 0:
				throw new ArgumentException(Resources.AtLeast1ArgumentExpected);
			case 1:
				return ToBooleanTerm(inputs[0]);
			default:
				return new DisolverAnd(this, ToBooleanTermArray(inputs));
			}
		}

		/// <summary>
		///   Term that is true if all subterms are equal
		/// </summary>
		/// <param name="inputs">list of subterms</param>
		public CspTerm Equal(params CspTerm[] inputs)
		{
			if (inputs.Length == 0)
			{
				throw new ArgumentException(Resources.EmptyArgListInEqual);
			}
			if (inputs.Length == 1)
			{
				return _constantTrue;
			}
			return new DisolverEqual(this, ToDisolverTermArray(inputs));
		}

		/// <summary>
		///   Disequality: Term that is true iff subterms are all different
		/// </summary>
		/// <param name="inputs">list of subterms</param>
		public CspTerm Unequal(params CspTerm[] inputs)
		{
			if (inputs.Length == 0)
			{
				throw new ArgumentException(Resources.EmptyArgListInUnequal);
			}
			if (inputs.Length == 1)
			{
				return _constantTrue;
			}
			return new DisolverDifferent(this, ToDisolverTermArray(inputs));
		}

		/// <summary>
		///   Term that is true if subterms are ordered in non-strictly increasing
		///   fashion (each one less equal to follower in subterm list)
		/// </summary>
		/// <param name="inputs">list of subterms</param>
		public CspTerm LessEqual(params CspTerm[] inputs)
		{
			int num = inputs.Length;
			if (num < 2)
			{
				throw new ArgumentException(Resources.ComparisonsShouldHave2Arguments);
			}
			if (num == 2)
			{
				return new DisolverLessEqual(this, ToDisolverTermArray(inputs));
			}
			CspTerm[] array = new CspTerm[num - 1];
			for (int i = 0; i < num - 1; i++)
			{
				array[i] = LessEqual(inputs[i], inputs[i + 1]);
			}
			return And(array);
		}

		/// <summary>
		///   Term that is true if subterms are ordered in strictly increasing
		///   fashion (each one less strict to follower in subterm list)
		/// </summary>
		/// <param name="inputs">list of subterms</param>
		public CspTerm Less(params CspTerm[] inputs)
		{
			int num = inputs.Length;
			if (num < 2)
			{
				throw new ArgumentException(Resources.ComparisonsShouldHave2Arguments);
			}
			if (num == 2)
			{
				return Not(LessEqual(inputs[1], inputs[0]));
			}
			CspTerm[] array = new CspTerm[num - 1];
			for (int i = 0; i < num - 1; i++)
			{
				array[i] = Less(inputs[i], inputs[i + 1]);
			}
			return And(array);
		}

		/// <summary>
		///   Term that is true if subterms are ordered in non-strictly decreasing
		///   fashion (each one greater equal to follower in subterm list)
		/// </summary>
		/// <param name="inputs">list of subterms</param>
		public CspTerm GreaterEqual(params CspTerm[] inputs)
		{
			return LessEqual(ReversedTermArray(inputs));
		}

		/// <summary>
		///   Term that is true if subterms are ordered in strictly decreasing
		///   fashion (each one greater strict to follower in subterm list)
		/// </summary>
		/// <param name="inputs">list of subterms</param>
		public CspTerm Greater(params CspTerm[] inputs)
		{
			return Less(ReversedTermArray(inputs));
		}

		/// <summary>
		///   Term that is true if subterms are ordered in strictly increasing
		///   fashion (each one less strict to follower in subterm list)
		/// </summary>
		/// <param name="constant">first subterm</param>
		/// <param name="inputs">list of other subterms</param>
		public CspTerm Less(long constant, params CspTerm[] inputs)
		{
			DisolverTerm[] inputs2 = ToDisolverTermArray(constant, inputs);
			return Less(inputs2);
		}

		/// <summary>
		///   Term that is true if subterms are ordered in non-strictly increasing
		///   fashion (each one less equal to follower in subterm list)
		/// </summary>
		/// <param name="constant">first subterm</param>
		/// <param name="inputs">list of other subterms</param>
		public CspTerm LessEqual(long constant, params CspTerm[] inputs)
		{
			DisolverTerm[] inputs2 = ToDisolverTermArray(constant, inputs);
			return LessEqual(inputs2);
		}

		/// <summary>
		///   Term that is true if subterms are ordered in strictly decreasing
		///   fashion (each one greater strict to follower in subterm list)
		/// </summary>
		/// <param name="constant">first subterm</param>
		/// <param name="inputs">list of other subterms</param>
		public CspTerm Greater(long constant, params CspTerm[] inputs)
		{
			DisolverTerm[] inputs2 = ToDisolverTermArray(constant, inputs);
			return Greater(inputs2);
		}

		/// <summary>
		///   Term that is true if subterms are ordered in non-strictly decreasing
		///   fashion (each one greater equal to follower in subterm list)
		/// </summary>
		/// <param name="constant">first subterm</param>
		/// <param name="inputs">list of other subterms</param>
		public CspTerm GreaterEqual(long constant, params CspTerm[] inputs)
		{
			DisolverTerm[] inputs2 = ToDisolverTermArray(constant, inputs);
			return GreaterEqual(inputs2);
		}

		/// <summary>
		///   Equality: Term that is true iff subterms are all equal
		/// </summary>
		/// <param name="constant">first subterm</param>
		/// <param name="inputs">list of other subterms</param>
		public CspTerm Equal(long constant, params CspTerm[] inputs)
		{
			DisolverTerm[] inputs2 = ToDisolverTermArray(constant, inputs);
			return Equal(inputs2);
		}

		/// <summary>
		///   Disequality: Term that is true iff subterms are all different
		/// </summary>
		/// <param name="constant">first subterm</param>
		/// <param name="inputs">list of other subterms</param>
		public CspTerm Unequal(long constant, params CspTerm[] inputs)
		{
			DisolverTerm[] inputs2 = ToDisolverTermArray(constant, inputs);
			return Unequal(inputs2);
		}

		/// <summary>
		///   Term representing the minimum of a list of subterms
		/// </summary>
		/// <param name="args">list of subterms</param>
		public CspTerm Min(params CspTerm[] args)
		{
			DisolverTerm[] array = ToDisolverTermArray(args);
			if (array.Length == 1)
			{
				return array[0];
			}
			Interval interval = UnionDomain(array);
			return new DisolverMin(this, interval.Lower, interval.Upper, array);
		}

		/// <summary>
		///   Term representing the maximum of a list of subterms
		/// </summary>
		/// <param name="args">list of subterms</param>
		public CspTerm Max(params CspTerm[] args)
		{
			DisolverTerm[] array = ToDisolverTermArray(args);
			if (array.Length == 1)
			{
				return array[0];
			}
			Interval interval = UnionDomain(array);
			return new DisolverMax(this, interval.Lower, interval.Upper, array);
		}

		/// <summary>
		///   Term that is true iff a variable belongs to a
		///   certain domain of values
		/// </summary>
		/// <param name="x">the variable</param>
		/// <param name="d">the set of values</param>
		public CspTerm Member(CspTerm x, CspDomain d)
		{
			return new DisolverMember(this, ToDisolverTerm(x), d);
		}

		/// <summary>
		///   Term representing the construct "cond ? a : b" of the
		///   C language. 
		/// </summary>
		/// <param name="condition">Boolean Term for the condition</param>
		/// <param name="ifValue">value returned if condition true</param>
		/// <param name="elseValue">value returned if condition false</param>
		public CspTerm IfThenElse(CspTerm condition, CspTerm ifValue, CspTerm elseValue)
		{
			DisolverBooleanTerm cond = ToBooleanTerm(condition);
			DisolverIntegerTerm disolverIntegerTerm = ToIntegerTerm(ifValue);
			DisolverIntegerTerm disolverIntegerTerm2 = ToIntegerTerm(elseValue);
			long l = Math.Min(disolverIntegerTerm.InitialLowerBound, disolverIntegerTerm2.InitialLowerBound);
			long r = Math.Max(disolverIntegerTerm.InitialUpperBound, disolverIntegerTerm2.InitialUpperBound);
			return new DisolverIfThenElse(this, l, r, cond, disolverIntegerTerm, disolverIntegerTerm2);
		}

		/// <summary>
		///   Boolean Term that has a list of Boolean subterms and that is true
		///   iff the exact indicated number of them are true
		/// </summary>
		/// <param name="m">number of subterms that should be true</param>
		/// <param name="inputs">list of Boolean subterms</param>
		public CspTerm ExactlyMofN(int m, params CspTerm[] inputs)
		{
			if (!Utils.TrueForAll(inputs, (CspTerm t) => t is DisolverBooleanTerm))
			{
				throw new ArgumentException(Resources.ExactlyMofNExpectsBooleanTerms);
			}
			return Equal(m, Sum(inputs));
		}

		/// <summary>
		///   Boolean Term that has a list of Boolean subterms and that is true
		///   iff at most the indicated number of them are true
		/// </summary>
		/// <param name="m">maximum number of true subterms</param>
		/// <param name="inputs">list of Boolean subterms</param>
		public CspTerm AtMostMofN(int m, params CspTerm[] inputs)
		{
			if (!Utils.TrueForAll(inputs, (CspTerm t) => t is DisolverBooleanTerm))
			{
				throw new ArgumentException(Resources.AtMostMofNExpectsBooleanTerms);
			}
			return GreaterEqual(m, Sum(inputs));
		}

		/// <summary>
		///   Term representing the absolute value of a term
		/// </summary>
		/// <param name="input">unique subterm</param>
		public CspTerm Abs(CspTerm input)
		{
			DisolverIntegerTerm disolverIntegerTerm = ToIntegerTerm(input);
			long r = Math.Max(Math.Abs(disolverIntegerTerm.InitialLowerBound), Math.Abs(disolverIntegerTerm.InitialUpperBound));
			return new DisolverAbs(this, 0L, r, disolverIntegerTerm);
		}

		/// <summary>
		///   Opposite (beware the confusing name!!!): 
		///   Term representing the opposite of an integer term x, 
		///   i.e.  -x
		/// </summary>
		/// <param name="input">unique subterm</param>
		public CspTerm Neg(CspTerm input)
		{
			DisolverIntegerTerm disolverIntegerTerm = ToIntegerTerm(input);
			return new DisolverUnaryMinus(this, -disolverIntegerTerm.InitialUpperBound, -disolverIntegerTerm.InitialLowerBound, disolverIntegerTerm);
		}

		/// <summary>
		///   Term representing the result of raising the input
		///   term to the given power
		/// </summary>
		/// <param name="x">unique subterm</param>
		/// <param name="power">power, i.e. exponent</param>
		public CspTerm Power(CspTerm x, int power)
		{
			DisolverIntegerTerm disolverIntegerTerm = ToIntegerTerm(x);
			if (power <= 0)
			{
				throw new ArgumentException(Resources.WrongArgumentPowerExpectsExponent1);
			}
			switch (power)
			{
			case 1:
				return disolverIntegerTerm;
			case 2:
			{
				Interval initialRange = disolverIntegerTerm.InitialRange;
				Interval interval = initialRange * initialRange;
				return new DisolverSquare(this, Math.Max(0L, interval.Lower), interval.Upper, disolverIntegerTerm);
			}
			default:
				return Product(x, Power(x, power - 1));
			}
		}

		/// <summary>
		///   Addition: term representing the sum of its subterms
		/// </summary>
		/// <param name="inputs">list of subterms</param>
		public CspTerm Sum(params CspTerm[] inputs)
		{
			DisolverTerm[] array = ToDisolverTermArray(inputs);
			if (array.Length == 1)
			{
				return array[0];
			}
			long num = 0L;
			long num2 = 0L;
			DisolverTerm[] array2 = array;
			foreach (DisolverTerm disolverTerm in array2)
			{
				num += disolverTerm.InitialLowerBound;
				if (num <= -4611686018427387903L)
				{
					num = -4611686018427387903L;
					break;
				}
			}
			DisolverTerm[] array3 = array;
			foreach (DisolverTerm disolverTerm2 in array3)
			{
				num2 += disolverTerm2.InitialUpperBound;
				if (num2 >= 4611686018427387903L)
				{
					num2 = 4611686018427387903L;
					break;
				}
			}
			return new DisolverSum(this, num, num2, array);
		}

		/// <summary>
		///   Given two vectors of subterms of identical size, the created 
		///   term represents the sum of the pairwise products of these
		///   terms, i.e. inputs1[0]*inputs2[0] + inputs1[1]*inputs2[1] + ...
		/// </summary>
		/// <param name="inputs1">first vector of subterms</param>
		/// <param name="inputs2">second vector of subterms</param>
		public CspTerm SumProduct(CspTerm[] inputs1, CspTerm[] inputs2)
		{
			int num = inputs1.Length;
			if (num != inputs2.Length)
			{
				throw new ArgumentException(Resources.UnmatchedSizesOfListsInSumProduct);
			}
			CspTerm[] array = new CspTerm[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = Product(inputs1[i], inputs2[i]);
			}
			return Sum(array);
		}

		/// <summary>
		///   Given a vector of Boolean terms and a vector of integer terms
		///   of identical size, creates a term representing the sum of 
		///   the integer terms for every position where the corresponding
		///   Boolean term is true. This is equivalent to a SumProduct 
		///   where the Boolean is considered as a 0/1 value, i.e.
		///   conditions[0]*inputs[0] + conditions[1]*inputs[1] + ...
		/// </summary>
		/// <param name="conditions">list of conditions</param>
		/// <param name="inputs">list of integer terms</param>
		public CspTerm FilteredSum(CspTerm[] conditions, CspTerm[] inputs)
		{
			return SumProduct(conditions, inputs);
		}

		/// <summary>
		///   Product: term representing the multiplication of all subterms
		/// </summary>
		/// <param name="inputs">list of subterms</param>
		public CspTerm Product(params CspTerm[] inputs)
		{
			int num = inputs.Length;
			switch (num)
			{
			case 0:
				throw new ArgumentException(Resources.EmptyArgListInProduct);
			case 1:
				return inputs[0];
			case 2:
			{
				Interval initialRange = ToDisolverTerm(inputs[0]).InitialRange;
				Interval initialRange2 = ToDisolverTerm(inputs[1]).InitialRange;
				Interval interval = initialRange * initialRange2;
				return new DisolverProduct(this, interval.Lower, interval.Upper, ToDisolverTermArray(inputs));
			}
			default:
			{
				CspTerm cspTerm = inputs[0];
				for (int i = 1; i < num; i++)
				{
					CspTerm cspTerm2 = inputs[i];
					cspTerm = Product(cspTerm, cspTerm2);
				}
				return cspTerm;
			}
			}
		}

		public CspTerm IsElementOf(CspTerm input, CspDomain domain)
		{
			return new DisolverMember(this, ToDisolverTerm(input), domain);
		}

		/// <summary>
		///   Given an array of terms T and a term I used for index, 
		///   creates a term representing T[i], i.e. the value of the term will
		///   be the value of the subterm contained at the position given by the
		///   value of the index term.
		/// </summary>
		/// <param name="inputs">array</param>
		/// <param name="index">term whose value will give the index</param>
		public CspTerm Index(CspTerm[] inputs, CspTerm index)
		{
			DisolverIntegerTerm disolverIntegerTerm = ToIntegerTerm(index);
			if (disolverIntegerTerm.IsInstantiated())
			{
				try
				{
					return inputs[disolverIntegerTerm.GetValue()];
				}
				catch (IndexOutOfRangeException)
				{
				}
			}
			DisolverTerm[] array = ToDisolverTermArray(inputs);
			Interval interval = UnionDomain(array);
			return new DisolverIndex(this, interval.Lower, interval.Upper, array, disolverIntegerTerm);
		}

		/// <summary>
		///   Given a matrix of terms T and 2 terms I and J used for index, 
		///   creates a term representing T[i][j], i.e. the value of the term 
		///   will be the value of the subterm contained at the position given 
		///   by the pair of values of the index terms.
		/// </summary>
		/// <param name="inputs">matrix</param>
		/// <param name="row">first index</param>
		/// <param name="column">second index</param>
		public CspTerm Index(CspTerm[][] inputs, CspTerm row, CspTerm column)
		{
			DisolverIntegerTerm disolverIntegerTerm = ToIntegerTerm(row);
			DisolverIntegerTerm disolverIntegerTerm2 = ToIntegerTerm(column);
			if (disolverIntegerTerm.IsInstantiated() && disolverIntegerTerm2.IsInstantiated())
			{
				try
				{
					return inputs[disolverIntegerTerm.GetValue()][disolverIntegerTerm2.GetValue()];
				}
				catch (IndexOutOfRangeException)
				{
				}
			}
			DisolverTerm[][] array = new DisolverTerm[inputs.Length][];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = ToDisolverTermArray(inputs[i]);
			}
			Interval interval = UnionDomain(array);
			return new DisolverMatrixIndex(this, interval.Lower, interval.Upper, array, disolverIntegerTerm, disolverIntegerTerm2);
		}

		/// <summary>
		///   The only form of indexing we export is with
		///   one index, 0-based
		/// </summary>
		public CspTerm Index(CspTerm[] inputs, params CspTerm[] keys)
		{
			if (keys != null && 1 == keys.Length)
			{
				return Index(inputs, keys[0]);
			}
			throw new NotImplementedException();
		}

		/// <summary>
		///   Creates a Term that is true if the tuple of values for
		///   colVars can be found in the array of integer tuples
		///   given in extension as second argument
		/// </summary>
		/// <param name="colVars">tuple of terms</param>
		/// <param name="inputs">
		///   list of integer tuples, each of which specifies an
		///   allowed combination for the tuple of Terms
		/// </param>
		public CspTerm TableInteger(CspTerm[] colVars, params int[][] inputs)
		{
			DisolverIntegerTerm[] vars = ToIntegerTermArray(colVars);
			return new DisolverPositiveTableTerm(this, vars, inputs);
		}

		/// <summary>
		///   Table containing elements that can be arbitrary complex terms
		/// </summary>
		internal CspTerm TableTerm(CspTerm[] colVars, params CspTerm[][] inputs)
		{
			_ = inputs.Length;
			int num = inputs.Length;
			int num2 = inputs[0].Length;
			List<CspTerm> list = new List<CspTerm>();
			for (int i = 0; i < num; i++)
			{
				CspTerm[] array = new CspTerm[num2];
				for (int j = 0; j < num2; j++)
				{
					array[j] = Equal(colVars[j], inputs[i][j]);
				}
				list.Add(And(array));
			}
			return Or(list.ToArray());
		}

		/// <summary>
		///   Table containing elements that can be arbitrary complex sets of values
		/// </summary>
		internal CspTerm TableDomain(CspTerm[] colVars, params CspDomain[][] inputs)
		{
			_ = inputs.Length;
			int num = inputs.Length;
			int num2 = inputs[0].Length;
			List<CspTerm> list = new List<CspTerm>();
			for (int i = 0; i < num; i++)
			{
				CspTerm[] array = new CspTerm[num2];
				for (int j = 0; j < num2; j++)
				{
					array[j] = Member(colVars[j], inputs[i][j]);
				}
				list.Add(And(array));
			}
			return Or(list.ToArray());
		}

		/// <summary>
		///   Adds constraint to the problem; each of these constraints is
		///   a Boolean Term, adding it has a constraint will force it to be
		///   true in any solution
		/// </summary>
		/// <param name="constraints">list of Boolean Terms</param>
		public bool AddConstraints(params CspTerm[] constraints)
		{
			for (int i = 0; i < constraints.Length; i++)
			{
				DisolverTerm item = (DisolverTerm)constraints[i];
				_constraints.Add(item);
			}
			return true;
		}

		/// <summary>
		///   Adds minimization goals. The solver will try to optimize these
		///   goals in lexicographic order, i.e. the first objective is
		///   minimized, then for equal values of this first objective, a 
		///   minimal value of the second objective is seeked, etc.
		/// </summary>
		/// <param name="goals">list of integer terms</param>
		public bool TryAddMinimizationGoals(params CspTerm[] goals)
		{
			for (int i = 0; i < goals.Length; i++)
			{
				DisolverIntegerTerm item = (DisolverIntegerTerm)goals[i];
				_objectives.Add(item);
			}
			return true;
		}

		/// <summary>
		/// Enumerate the solutions of the problem; 
		/// if the problem has minimization goals then every time a solution
		/// is found we restrict the search to strictly improved solutions
		/// </summary>
		private IEnumerable<Dictionary<CspTerm, object>> EnumerateAndImprove()
		{
			_watch.Reset();
			_watch.Start();
			TreeSearchAlgorithm s = new TreeSearchAlgorithm(p: Compile(), goalsToMinimize: IsOptimizing ? _objectives.ToArray() : null, s: this, strat: Strategy, stop: CheckAbortion);
			foreach (Dictionary<CspTerm, object> item in Search(s))
			{
				yield return item;
			}
			_watch.Stop();
			SetBaseModelParameterElapsedMilliseconds();
		}

		/// <summary>
		/// Enumerate the solutions of the problem in an any-time 
		/// fashion: Solutions are returned as they are found, before global 
		/// optimality is reached.
		/// </summary>
		public IEnumerable<Dictionary<CspTerm, object>> EnumerateInterimSolutions()
		{
			Dictionary<CspTerm, object> best = null;
			foreach (Dictionary<CspTerm, object> sol in EnumerateAndImprove())
			{
				best = sol;
				yield return sol;
			}
			if (best == null || !IsOptimizing)
			{
				yield break;
			}
			FixObjectives(best);
			_watch.Start();
			Problem p = Compile();
			TreeSearchAlgorithm s = new TreeSearchAlgorithm(this, p, Strategy, null, CheckAbortion);
			bool bestFound = false;
			foreach (Dictionary<CspTerm, object> sol2 in Search(s))
			{
				if (bestFound)
				{
					yield return sol2;
				}
				else if (IsSameSolution(sol2, best))
				{
					bestFound = true;
				}
				else
				{
					yield return sol2;
				}
			}
			_watch.Stop();
			SetBaseModelParameterElapsedMilliseconds();
		}

		/// <summary>
		/// Proves optimality and enumerates all globally optimal solutions
		/// </summary>
		public IEnumerable<Dictionary<CspTerm, object>> EnumerateSolutions()
		{
			_watch.Reset();
			if (IsOptimizing)
			{
				Dictionary<CspTerm, object> dictionary = null;
				foreach (Dictionary<CspTerm, object> item in EnumerateAndImprove())
				{
					dictionary = item;
				}
				if (dictionary == null)
				{
					yield break;
				}
				FixObjectives(dictionary);
			}
			_watch.Start();
			Problem p = Compile();
			TreeSearchAlgorithm s = new TreeSearchAlgorithm(this, p, Strategy, null, CheckAbortion);
			foreach (Dictionary<CspTerm, object> item2 in Search(s))
			{
				yield return item2;
			}
			_watch.Stop();
			SetBaseModelParameterElapsedMilliseconds();
		}

		/// <summary>
		/// Enumeration of the solutions of a treeSearchAlgorithm 
		/// </summary>
		/// <remarks>
		/// Factors code in several solution enumeration methods
		/// </remarks>
		private IEnumerable<Dictionary<CspTerm, object>> Search(TreeSearchAlgorithm s)
		{
			bool again = s.FindFirstSolution();
			while (again)
			{
				Dictionary<CspTerm, object> result = s.Problem.GetSolution();
				_watch.Stop();
				SetBaseModelParameterElapsedMilliseconds();
				yield return result;
				_watch.Start();
				if (_baseModel != null && _baseModel._fIsInModelingPhase)
				{
					throw new InvalidOperationException(Resources.SolverResetDuringSolve);
				}
				again = s.FindNextSolution();
			}
		}

		/// <summary>Are the two solutions the same? </summary>
		private bool IsSameSolution(Dictionary<CspTerm, object> sol1, Dictionary<CspTerm, object> sol2)
		{
			foreach (DisolverTerm variable in _variables)
			{
				if (sol1[variable] != sol2[variable])
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Constrains the objectives to be equal to a given
		/// (usually, optimal) solution
		/// </summary>
		private void FixObjectives(Dictionary<CspTerm, object> best)
		{
			foreach (DisolverIntegerTerm objective in _objectives)
			{
				long constant = (int)best[objective];
				AddConstraints(Equal(constant, objective));
			}
		}

		private void SetBaseModelParameterElapsedMilliseconds()
		{
			if (_baseModel != null)
			{
				_baseModel.Parameters.SetElapsed((int)_watch.ElapsedMilliseconds);
			}
			_parameters.SetElapsed((int)_watch.ElapsedMilliseconds);
		}

		internal ConstraintSolverSolution SolveCore(ConstraintSolverParams dir)
		{
			if (_baseModel != null)
			{
				if (!_baseModel._fIsInModelingPhase)
				{
					throw new InvalidOperationException(Resources.AlreadySolving);
				}
				_baseModel._fIsInModelingPhase = false;
			}
			ConstraintSolverParams constraintSolverParams;
			if (dir != null)
			{
				constraintSolverParams = dir;
				_parameters.TimeLimitMilliSec = dir.TimeLimitMilliSec;
			}
			else
			{
				constraintSolverParams = _parameters;
			}
			if (constraintSolverParams.ValueSelection == ConstraintSolverParams.TreeSearchValueOrdering.Any)
			{
				constraintSolverParams.ValueSelection = ConstraintSolverParams.TreeSearchValueOrdering.RandomOrder;
			}
			if (constraintSolverParams.VariableSelection == ConstraintSolverParams.TreeSearchVariableOrdering.Any)
			{
				constraintSolverParams.VariableSelection = ConstraintSolverParams.TreeSearchVariableOrdering.DomainOverWeightedDegree;
			}
			constraintSolverParams.Algorithm = ConstraintSolverParams.CspSearchAlgorithm.TreeSearch;
			switch (constraintSolverParams.ValueSelection)
			{
			case ConstraintSolverParams.TreeSearchValueOrdering.SuccessPrediction:
				_valueStrategy = ValueEnumerationStrategy.Dicho;
				break;
			case ConstraintSolverParams.TreeSearchValueOrdering.ForwardOrder:
				_valueStrategy = ValueEnumerationStrategy.Lex;
				break;
			default:
				_valueStrategy = ValueEnumerationStrategy.Random;
				break;
			}
			switch (constraintSolverParams.VariableSelection)
			{
			case ConstraintSolverParams.TreeSearchVariableOrdering.MinimalDomainFirst:
				_variableStrategy = VariableEnumerationStrategy.MinDom;
				break;
			case ConstraintSolverParams.TreeSearchVariableOrdering.DeclarationOrder:
				_variableStrategy = VariableEnumerationStrategy.Lex;
				break;
			case ConstraintSolverParams.TreeSearchVariableOrdering.DynamicWeighting:
			case ConstraintSolverParams.TreeSearchVariableOrdering.ConflictDriven:
				_variableStrategy = VariableEnumerationStrategy.Vsids;
				break;
			case ConstraintSolverParams.TreeSearchVariableOrdering.ImpactPrediction:
				_variableStrategy = VariableEnumerationStrategy.Impact;
				break;
			default:
				_variableStrategy = VariableEnumerationStrategy.DomWdeg;
				break;
			}
			UseRestarts = constraintSolverParams.RestartEnabled;
			ConstraintSolverSolution constraintSolverSolution = new ConstraintSolverSolution(this);
			constraintSolverSolution.SolverParams = dir;
			constraintSolverSolution.GetNext();
			return constraintSolverSolution;
		}

		/// <summary>
		///   Checks if an abortion condition has been met and
		///   if so throws an exception
		/// </summary>
		/// <remarks>
		///   other solution is method returning bool but then caller
		///   is responsible for setting abortion flag and interrputing
		/// </remarks>
		public void CheckAbortion()
		{
			if (_parameters.Abort || _watch.ElapsedMilliseconds >= _parameters.TimeLimitMilliSec)
			{
				_hasAborted = true;
				throw new TimeLimitReachedException();
			}
		}

		/// <summary>
		///   Computes the union of the intervals of values for all
		///   the elements in the enumeration
		/// </summary>
		internal static Interval UnionDomain(IEnumerable<DisolverTerm> list)
		{
			long num = long.MaxValue;
			long num2 = long.MinValue;
			foreach (DisolverTerm item in list)
			{
				num = Math.Min(num, item.InitialLowerBound);
				num2 = Math.Max(num2, item.InitialUpperBound);
			}
			return new Interval(num, num2);
		}

		/// <summary>
		///   Computes the union of the intervals of values for all
		///   the elements in the enumeration
		/// </summary>
		internal static Interval UnionDomain(IEnumerable<IEnumerable<DisolverTerm>> list)
		{
			long num = long.MaxValue;
			long num2 = long.MinValue;
			foreach (DisolverTerm[] item in list)
			{
				DisolverTerm[] array2 = item;
				foreach (DisolverTerm disolverTerm in array2)
				{
					num = Math.Min(num, disolverTerm.InitialLowerBound);
					num2 = Math.Max(num2, disolverTerm.InitialUpperBound);
				}
			}
			return new Interval(num, num2);
		}

		/// <summary>
		///   Down-casting of a Boolean Term.
		/// </summary>
		private static DisolverBooleanTerm ToBooleanTerm(CspTerm t)
		{
			string disolverBooleanTermExpected = Resources.DisolverBooleanTermExpected;
			if (!(t is DisolverBooleanTerm result))
			{
				throw new ArgumentException(disolverBooleanTermExpected);
			}
			return result;
		}

		/// <summary>
		///   Down-casting of an array of Boolean Terms.
		/// </summary>
		private static DisolverBooleanTerm[] ToBooleanTermArray(CspTerm[] terms)
		{
			DisolverBooleanTerm[] array = terms as DisolverBooleanTerm[];
			if (array == null)
			{
				int num = terms.Length;
				array = new DisolverBooleanTerm[num];
				for (int i = 0; i < num; i++)
				{
					array[i] = ToBooleanTerm(terms[i]);
				}
			}
			return array;
		}

		/// <summary>
		///   Down-casting of an integer Term.
		/// </summary>
		private DisolverIntegerTerm ToIntegerTerm(CspTerm t)
		{
			string disolverIntegerTermExpected = Resources.DisolverIntegerTermExpected;
			if (t is DisolverIntegerTerm result)
			{
				return result;
			}
			if (t is DisolverBooleanTerm disolverBooleanTerm)
			{
				if (!_booleanToIntegerConversions.TryGetValue(disolverBooleanTerm, out var value))
				{
					value = new DisolverBooleanAsInteger(this, disolverBooleanTerm);
					_booleanToIntegerConversions.Add(disolverBooleanTerm, value);
				}
				return value;
			}
			throw new ArgumentException(disolverIntegerTermExpected);
		}

		/// <summary>
		///   Down-casting of an array of Integer terms
		/// </summary>
		private DisolverIntegerTerm[] ToIntegerTermArray(params CspTerm[] terms)
		{
			int num = terms.Length;
			DisolverIntegerTerm[] array = new DisolverIntegerTerm[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = ToIntegerTerm(terms[i]);
			}
			return array;
		}

		/// <summary>
		///   Down-casting of a Disolver Term
		/// </summary>
		private static DisolverTerm ToDisolverTerm(CspTerm t)
		{
			if (!(t is DisolverTerm result))
			{
				throw new ArgumentException(Resources.DisolverTermExpected);
			}
			return result;
		}

		/// <summary>
		///   Down-casting of an array of Disolver Terms (used in cases
		///   where they can be a mix of Boolean/integer terms)
		/// </summary>
		private static DisolverTerm[] ToDisolverTermArray(params CspTerm[] terms)
		{
			DisolverTerm[] array = terms as DisolverTerm[];
			if (array == null)
			{
				int num = terms.Length;
				array = new DisolverTerm[num];
				for (int i = 0; i < num; i++)
				{
					array[i] = ToDisolverTerm(terms[i]);
				}
			}
			return array;
		}

		private DisolverTerm[] ToDisolverTermArray(long cst, params CspTerm[] terms)
		{
			int num = terms.Length;
			DisolverTerm[] array = new DisolverTerm[num + 1];
			array[0] = Constant(cst) as DisolverTerm;
			for (int i = 0; i < num; i++)
			{
				array[i + 1] = ToDisolverTerm(terms[i]);
			}
			return array;
		}

		/// <summary>
		///   Down-casting, but reversing the order
		/// </summary>
		private static DisolverTerm[] ReversedTermArray(params CspTerm[] terms)
		{
			int num = terms.Length;
			DisolverTerm[] array = new DisolverTerm[num];
			int num2 = 0;
			for (int num3 = num - 1; num3 >= 0; num3--)
			{
				array[num2] = ToDisolverTerm(terms[num3]);
				num2++;
			}
			return array;
		}

		/// <summary>
		///   Creates a fresh internal representation of the problem as
		///   used by backtrack search algorithms
		/// </summary>
		internal Problem Compile()
		{
			return CompilerToProblem.Apply(this, Strategy, _allTerms, _constraints, _objectives, _allIntegerTerms, _allBooleanTerms);
		}
	}
}
