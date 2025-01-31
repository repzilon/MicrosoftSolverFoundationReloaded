using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A factory that generates terms (variables, functions of other terms),
	/// which can be Boolean or numerical (real-valued). 
	/// Allows to change the value of the variables and to maintain incrementally 
	/// the value of some numerical terms ("goals") and the violation of some
	/// Boolean terms ("constraints").
	/// </summary>
	/// <remarks>
	/// Note on NaN: when we evaluate the terms we accept NaNs: for instance
	/// if we have terms expressing X / Y and Y turns out to be zero. 
	/// This means that goals can potentially have a NaN value.
	/// Violations can also have a NaN value, for instance X / Y &gt; 0 with Y = 0.
	/// </remarks>
	internal class TermEvaluator
	{
		/// <summary>
		/// When we have array terms such as Sums, this represents the limit
		/// for a simple array representation of the term. Beyond this we'll
		/// start constructing a balanced tree of expressions to guarantee
		/// a log-time bound of recomputation of commutative/associate operations.
		/// </summary>
		internal const int MaxSize = 64;

		private EvaluableBooleanTerm _cstTrue;

		private EvaluableBooleanTerm _cstFalse;

		private EvaluableNumericalTerm _cstZero;

		private EvaluableNumericalTerm _cstOne;

		private Func<EvaluableNumericalTerm, EvaluableNumericalTerm, EvaluableNumericalTerm> _createSum2;

		private Func<EvaluableNumericalTerm[], EvaluableNumericalTerm> _createSumN;

		private Func<EvaluableNumericalTerm, EvaluableNumericalTerm, EvaluableNumericalTerm> _createProd2;

		private Func<EvaluableNumericalTerm[], EvaluableNumericalTerm> _createProdN;

		private Func<EvaluableNumericalTerm, EvaluableNumericalTerm, EvaluableNumericalTerm> _createMin2;

		private Func<EvaluableNumericalTerm[], EvaluableNumericalTerm> _createMinN;

		private Func<EvaluableNumericalTerm, EvaluableNumericalTerm, EvaluableNumericalTerm> _createMax2;

		private Func<EvaluableNumericalTerm[], EvaluableNumericalTerm> _createMaxN;

		private Func<EvaluableBooleanTerm, EvaluableBooleanTerm, EvaluableBooleanTerm> _createAnd2;

		private Func<EvaluableBooleanTerm[], EvaluableBooleanTerm> _createAndN;

		private Func<EvaluableBooleanTerm, EvaluableBooleanTerm, EvaluableBooleanTerm> _createOr2;

		private Func<EvaluableBooleanTerm[], EvaluableBooleanTerm> _createOrN;

		/// <summary>
		/// True if all data-structures have been correctly initialized 
		/// for the problem. These data-structures must be undone / recomputed
		/// every time a term is added / removed
		/// </summary>
		private bool _initialized;

		/// <summary> 
		/// Modified terms that need to be considered in re-evaluation;
		/// Each is paired with its Value before modification. 
		/// The terms are ranked by depth in order to allow sound re-evaluation.
		/// </summary>
		private Stack<EvaluableTerm>[] _reEvaluationQueue;

		/// <summary>
		/// All the variables created in this evaluator
		/// </summary>
		/// <remarks>
		/// CODE REVIEW (lucasb): superfluous. Can be removed or used in DEBUG only
		/// Is only used for assertions
		/// </remarks>
		private List<EvaluableTerm> _allVariables;

		/// <summary>
		/// Number of elements in the queue
		/// </summary>
		private int _nbEnqueued;

		/// <summary>
		/// List of constraints
		/// </summary>
		private List<EvaluableBooleanTerm> _constraints;

		/// <summary>
		/// List of objective functions 
		/// </summary>
		private List<EvaluableNumericalTerm> _goals;

		/// <summary>
		/// Term that captures the sum of violations of all added constraints
		/// </summary>
		private EvaluableNumericalTerm _violation;

		/// <summary>
		/// Variables eliminated in presolve
		/// </summary>
		private List<KeyValuePair<EvaluableVariable, EvaluableNumericalTerm>> _eliminatedVariables;

		/// <summary>Presolve level. -1 means automatic, 0 means no presolve.
		/// </summary>
		internal int PresolveLevel { get; set; }

		/// <summary>
		/// Tolerance on equality constraints.
		/// </summary>
		internal double EqualityTolerance { get; set; }

		/// <summary>
		/// Term representing the Boolean True
		/// </summary>
		public EvaluableBooleanTerm ConstantTrue
		{
			get
			{
				if (_cstTrue == null)
				{
					EvaluableNumericalTerm constantOne = ConstantOne;
					_cstTrue = CreateConversionToBoolean(constantOne);
					SignalNewTerm(_cstTrue);
				}
				return _cstTrue;
			}
		}

		/// <summary>
		/// Term representing the Boolean False
		/// </summary>
		public EvaluableBooleanTerm ConstantFalse
		{
			get
			{
				if (_cstFalse == null)
				{
					EvaluableNumericalTerm constantZero = ConstantZero;
					_cstFalse = CreateConversionToBoolean(constantZero);
					SignalNewTerm(_cstFalse);
				}
				return _cstFalse;
			}
		}

		/// <summary>
		/// Term representing the constant zero
		/// </summary>
		private EvaluableNumericalTerm ConstantZero
		{
			get
			{
				if (_cstZero == null)
				{
					_cstZero = new EvaluableConstant(0.0);
					SignalNewTerm(_cstZero);
				}
				return _cstZero;
			}
		}

		/// <summary>
		/// Term representing the constant one
		/// </summary>
		private EvaluableNumericalTerm ConstantOne
		{
			get
			{
				if (_cstOne == null)
				{
					_cstOne = new EvaluableConstant(1.0);
					SignalNewTerm(_cstOne);
				}
				return _cstOne;
			}
		}

		/// <summary>
		/// Get the violation component of the current quality
		/// </summary>
		public double CurrentViolation => _violation.Value;

		/// <summary>
		/// Number of constraints
		/// </summary>
		public int ConstraintsCount => _constraints.Count;

		/// <summary>
		/// Number of goals
		/// </summary>
		public int GoalsCount => _goals.Count;

		/// <summary>
		/// A solver that can (try to) solve arbitrarily complex
		/// problems modelled in SFS
		/// </summary>
		public TermEvaluator()
		{
			_allVariables = new List<EvaluableTerm>();
			_reEvaluationQueue = new Stack<EvaluableTerm>[1];
			_reEvaluationQueue[0] = new Stack<EvaluableTerm>();
			_nbEnqueued = 0;
			_initialized = false;
			_constraints = new List<EvaluableBooleanTerm>();
			_goals = new List<EvaluableNumericalTerm>();
			_eliminatedVariables = new List<KeyValuePair<EvaluableVariable, EvaluableNumericalTerm>>();
			InitDelegates();
			EqualityTolerance = 1E-08;
		}

		/// <summary>
		/// (re)initializes the data-structures for evaluation. 
		/// Does nothing if the data-structures have already been 
		/// allocated and if nothing has changed since they have.
		/// </summary>
		/// <remarks>
		/// This is a heavy operation:
		/// (1) allocates temporary hashsets and sorted lists; 
		/// (2) initializes the dependency lists and evaluation queue.
		///
		/// However the benefit from separating reinitialization from term 
		/// construction is that reinitialization can be re-done on demand 
		/// if terms are added, or removed. So the evaluator is flexible:
		/// both incremental and decremental.
		///
		/// One thing that complicates a bit the initialization is the 
		/// (anticipated) support for compilation. If some terms are compiled 
		/// and some are interpreted this requires a flexible initialization
		/// that decides *a posteriori* of the term construction which ones
		/// finally have the right size for compilation, and possibly allocates
		/// other terms for the compiled code. Keeping the incremental/decremental
		/// semantics with this requires to produce a fresh compiled code whenever
		/// a term is added / removed.
		/// </remarks>
		private void Initialize()
		{
			if (!_initialized)
			{
				List<EvaluableBooleanTerm> source = ((PresolveLevel == 0) ? _constraints : Presolve());
				_violation = CreateSum(source.Select((EvaluableBooleanTerm x) => new EvaluableNormalizationTerm(x)).ToArray());
				EvaluableTerm[] array = CollectTerms();
				InitializeDependencies(array);
				ReinitializeAll(array);
				int depth = array[array.Length - 1].Depth;
				int num = Math.Max(_reEvaluationQueue.Length, depth + 1);
				_reEvaluationQueue = new Stack<EvaluableTerm>[num];
				for (int i = 0; i < num; i++)
				{
					_reEvaluationQueue[i] = new Stack<EvaluableTerm>();
				}
				_initialized = true;
			}
		}

		private List<EvaluableBooleanTerm> Presolve()
		{
			List<EvaluableBooleanTerm> list = new List<EvaluableBooleanTerm>();
			HashSet<EvaluableBooleanTerm> hashSet = new HashSet<EvaluableBooleanTerm>();
			Dictionary<EvaluableTerm, EvaluableTerm> dictionary = new Dictionary<EvaluableTerm, EvaluableTerm>();
			HashSet<EvaluableVariable> hashSet2 = new HashSet<EvaluableVariable>();
			List<EvaluableBooleanTerm> list2 = new List<EvaluableBooleanTerm>();
			foreach (EvaluableBooleanTerm constraint in _constraints)
			{
				if (!(constraint is EvaluableRangeConstraint evaluableRangeConstraint) || evaluableRangeConstraint.Lower != 0.0 || evaluableRangeConstraint.Upper != 0.0)
				{
					continue;
				}
				EvaluableNumericalTerm input = evaluableRangeConstraint.Input;
				if (!TryGetVariableAssignment(input, out var inputVar, out var inputExpr) || dictionary.ContainsKey(inputVar) || hashSet2.Contains(inputVar) || inputExpr.CollectSubTerms().Contains(inputVar))
				{
					continue;
				}
				LocalSearchDomain domain = inputVar.Domain;
				if (domain.IsDiscrete)
				{
					continue;
				}
				inputExpr = ((EvaluableNumericalTerm)inputExpr.Substitute(dictionary)) ?? inputExpr;
				if (!double.IsNegativeInfinity(inputVar.Domain.Lower) || !double.IsPositiveInfinity(inputVar.Domain.Upper))
				{
					list2.Add(new EvaluableRangeConstraint(inputVar.Domain.Lower, inputExpr, inputVar.Domain.Upper, this));
				}
				hashSet.Add(constraint);
				dictionary.Add(inputVar, inputExpr);
				_eliminatedVariables.Add(new KeyValuePair<EvaluableVariable, EvaluableNumericalTerm>(inputVar, inputExpr));
				foreach (EvaluableVariable item2 in inputExpr.CollectSubTerms().OfType<EvaluableVariable>())
				{
					hashSet2.Add(item2);
				}
			}
			foreach (EvaluableBooleanTerm item3 in _constraints.Concat(list2))
			{
				if (!hashSet.Contains(item3))
				{
					EvaluableBooleanTerm item = ((EvaluableBooleanTerm)item3.Substitute(dictionary)) ?? item3;
					list.Add(item);
				}
			}
			for (int i = 0; i < _goals.Count; i++)
			{
				_goals[i] = ((EvaluableNumericalTerm)_goals[i].Substitute(dictionary)) ?? _goals[i];
			}
			return list;
		}

		private static bool TryGetVariableAssignment(EvaluableNumericalTerm input, out EvaluableVariable inputVar, out EvaluableNumericalTerm inputExpr)
		{
			EvaluableBinaryPlus evaluableBinaryPlus = input as EvaluableBinaryPlus;
			inputVar = null;
			inputExpr = null;
			if (evaluableBinaryPlus != null)
			{
				EvaluableUnaryMinus evaluableUnaryMinus = evaluableBinaryPlus.Input1 as EvaluableUnaryMinus;
				EvaluableUnaryMinus evaluableUnaryMinus2 = evaluableBinaryPlus.Input2 as EvaluableUnaryMinus;
				if (evaluableBinaryPlus.Input1 is EvaluableVariable)
				{
					inputVar = (EvaluableVariable)evaluableBinaryPlus.Input1;
					if (evaluableUnaryMinus2 != null)
					{
						inputExpr = evaluableUnaryMinus2.Input;
					}
					else
					{
						inputExpr = new EvaluableUnaryMinus(evaluableBinaryPlus.Input2);
					}
					return true;
				}
				if (evaluableBinaryPlus.Input2 is EvaluableVariable)
				{
					inputVar = (EvaluableVariable)evaluableBinaryPlus.Input2;
					if (evaluableUnaryMinus != null)
					{
						inputExpr = evaluableUnaryMinus.Input;
					}
					else
					{
						inputExpr = new EvaluableUnaryMinus(evaluableBinaryPlus.Input1);
					}
					return true;
				}
				if (evaluableUnaryMinus != null && evaluableUnaryMinus.Input is EvaluableVariable)
				{
					inputVar = (EvaluableVariable)evaluableUnaryMinus.Input;
					inputExpr = evaluableBinaryPlus.Input2;
					return true;
				}
				if (evaluableUnaryMinus2 != null && evaluableUnaryMinus2.Input is EvaluableVariable)
				{
					inputVar = (EvaluableVariable)evaluableUnaryMinus2.Input;
					inputExpr = evaluableBinaryPlus.Input1;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// (Re)-initializes the lists of dependents of each term
		/// </summary>
		private static void InitializeDependencies(EvaluableTerm[] allTerms)
		{
			Dictionary<EvaluableTerm, int> dictionary = new Dictionary<EvaluableTerm, int>();
			for (int i = 0; i < allTerms.Length; i++)
			{
				dictionary.Add(allTerms[i], i);
			}
			int[] array = new int[allTerms.Length];
			foreach (EvaluableTerm evaluableTerm in allTerms)
			{
				foreach (EvaluableTerm item2 in evaluableTerm.EnumerateInputs())
				{
					int num = dictionary[item2];
					array[num]++;
				}
			}
			EvaluableTerm[][] array2 = new EvaluableTerm[allTerms.Length][];
			EvaluableTerm[] array3 = new EvaluableTerm[0];
			for (int k = 0; k < allTerms.Length; k++)
			{
				array2[k] = ((array[k] == 0) ? array3 : new EvaluableTerm[array[k]]);
			}
			foreach (EvaluableTerm evaluableTerm2 in allTerms)
			{
				foreach (EvaluableTerm item3 in evaluableTerm2.EnumerateInputs())
				{
					int num2 = dictionary[item3];
					int num3 = array[num2] - 1;
					array2[num2][num3] = evaluableTerm2;
					array[num2] = num3;
				}
			}
			HashSet<EvaluableTerm> hashSet = new HashSet<EvaluableTerm>();
			for (int m = 0; m < allTerms.Length; m++)
			{
				EvaluableTerm evaluableTerm3 = allTerms[m];
				if (evaluableTerm3.IsConstant)
				{
					continue;
				}
				EvaluableTerm[] array4 = array2[m];
				if (array4.Length > 1)
				{
					bool flag = true;
					hashSet.Clear();
					EvaluableTerm[] array5 = array4;
					foreach (EvaluableTerm item in array5)
					{
						flag &= hashSet.Add(item);
					}
					if (!flag)
					{
						array4 = hashSet.ToArray();
					}
				}
				evaluableTerm3.InitializeDependentList(array4);
			}
		}

		/// <summary>
		/// Computes the set of all terms that are inputs of
		/// inputs... of constraints and goals
		/// </summary>
		private EvaluableTerm[] CollectTerms()
		{
			IEnumerable<EvaluableTerm> initialTerms = EvaluationStatics.Union<EvaluableNumericalTerm, EvaluableTerm, EvaluableTerm>(_goals, new EvaluableTerm[1] { _violation });
			return EvaluableTerm.CollectSubTerms(initialTerms).ToArray();
		}

		[Conditional("DEBUG")]
		private static void Check(bool condition)
		{
		}

		/// <summary>
		/// Specifies that a Boolean term is a constraint;
		/// The evaluator will maintain its violation incrementally
		/// </summary>
		public void AddConstraint(EvaluableBooleanTerm cstr)
		{
			_initialized = false;
			_constraints.Add(cstr);
		}

		/// <summary>
		/// Specifies that a numerical term is a goal;
		/// The evaluator will maintain its value incrementally
		/// </summary>
		public void AddGoal(EvaluableNumericalTerm goal)
		{
			_initialized = false;
			_goals.Add(goal);
		}

		/// <summary>
		/// Removes a Boolean term from the set of constraints
		/// </summary>
		/// <returns>
		/// True if the constraint is successfully removed
		/// </returns>
		public bool RemoveConstraint(EvaluableBooleanTerm cstr)
		{
			_initialized = false;
			return _constraints.Remove(cstr);
		}

		/// <summary>
		/// Removes a numerical term from the set of goals
		/// </summary>
		/// <returns>
		/// True if the goal is successfully removed
		/// </returns>
		public bool RemoveGoal(EvaluableNumericalTerm goal)
		{
			_initialized = false;
			return _goals.Remove(goal);
		}

		/// <summary>
		/// Removes all goals
		/// </summary>
		public void ClearGoals()
		{
			_initialized = false;
			_goals.Clear();
		}

		/// <summary>
		/// Creates a new variable ranging over doubles
		/// </summary>
		/// <param name="init">the initial value of the variable</param>
		public EvaluableVariable CreateNumericalVariable(double init)
		{
			EvaluableVariable evaluableVariable = new EvaluableVariable(init);
			_allVariables.Add(evaluableVariable);
			SignalNewTerm(evaluableVariable);
			return evaluableVariable;
		}

		/// <summary>
		/// Creates a new variable with a specified domain
		/// </summary>
		/// <param name="init">the initial value of the variable</param>
		/// <param name="dom">the initial domain of the variable</param>
		public EvaluableVariable CreateNumericalVariable(double init, LocalSearchDomain dom)
		{
			EvaluableVariable evaluableVariable = new EvaluableVariable(init, dom);
			_allVariables.Add(evaluableVariable);
			SignalNewTerm(evaluableVariable);
			return evaluableVariable;
		}

		/// <summary>
		/// Creates a new numerical constant, immutable
		/// </summary>
		public EvaluableNumericalTerm CreateConstant(double value)
		{
			EvaluableNumericalTerm evaluableNumericalTerm = ((value == 0.0) ? ConstantZero : ((value != 1.0) ? new EvaluableConstant(value) : ConstantOne));
			SignalNewTerm(evaluableNumericalTerm);
			return evaluableNumericalTerm;
		}

		/// <summary>
		/// Creates the product of a numerical term by a constant
		/// </summary>
		public EvaluableNumericalTerm CreateProduct(double input1, EvaluableNumericalTerm input2)
		{
			if (input1 == 0.0)
			{
				return ConstantZero;
			}
			if (input1 == 1.0)
			{
				return input2;
			}
			EvaluableProductByConstant evaluableProductByConstant = new EvaluableProductByConstant(input1, input2);
			SignalNewTerm(evaluableProductByConstant);
			return evaluableProductByConstant;
		}

		/// <summary>
		/// Creates the product of two numerical terms
		/// </summary>
		public EvaluableNumericalTerm CreateProduct(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
		{
			if (input1.IsConstant)
			{
				return CreateProduct(input1.Value, input2);
			}
			if (input2.IsConstant)
			{
				return CreateProduct(input2.Value, input1);
			}
			EvaluableBinaryProduct evaluableBinaryProduct = new EvaluableBinaryProduct(input1, input2);
			SignalNewTerm(evaluableBinaryProduct);
			return evaluableBinaryProduct;
		}

		/// <summary>
		/// Creates the division of the first term by the second
		/// </summary>
		public EvaluableNumericalTerm CreateQuotient(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
		{
			EvaluableQuotient evaluableQuotient = new EvaluableQuotient(input1, input2);
			SignalNewTerm(evaluableQuotient);
			return evaluableQuotient;
		}

		/// <summary>
		/// Creates the product of a list of terms
		/// </summary>
		public EvaluableNumericalTerm CreateProduct(EvaluableNumericalTerm[] args)
		{
			switch (args.Length)
			{
			case 0:
				return ConstantOne;
			case 1:
				return args[0];
			case 2:
				return CreateProduct(args[0], args[1]);
			default:
				return CreateArrayTerm(_createProdN, _createProd2, args, 0, args.Length - 1);
			}
		}

		/// <summary>
		/// Creates the opposite (unary minus) of a numerical term
		/// </summary>
		public EvaluableNumericalTerm CreateUnaryMinus(EvaluableNumericalTerm arg)
		{
			EvaluableUnaryMinus evaluableUnaryMinus = new EvaluableUnaryMinus(arg);
			SignalNewTerm(evaluableUnaryMinus);
			return evaluableUnaryMinus;
		}

		/// <summary>
		/// Creates the identity of a numerical term
		/// </summary>
		public EvaluableNumericalTerm CreateUnaryIdentity(EvaluableNumericalTerm arg)
		{
			EvaluableUnaryIdentity evaluableUnaryIdentity = new EvaluableUnaryIdentity(arg);
			SignalNewTerm(evaluableUnaryIdentity);
			return evaluableUnaryIdentity;
		}

		/// <summary>
		/// Creates the identity of a Boolean term
		/// </summary>
		public EvaluableBooleanTerm CreateUnaryIdentity(EvaluableBooleanTerm arg)
		{
			EvaluableBooleanIdentity evaluableBooleanIdentity = new EvaluableBooleanIdentity(arg);
			SignalNewTerm(evaluableBooleanIdentity);
			return evaluableBooleanIdentity;
		}

		/// <summary>
		/// Creates the Absolute value of a numerical term
		/// </summary>
		public EvaluableNumericalTerm CreateAbs(EvaluableNumericalTerm arg)
		{
			EvaluableAbs evaluableAbs = new EvaluableAbs(arg);
			SignalNewTerm(evaluableAbs);
			return evaluableAbs;
		}

		/// <summary>
		/// Creates the difference of two numerical terms
		/// </summary>
		public EvaluableNumericalTerm CreateMinus(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
		{
			if (input2.IsConstant)
			{
				return CreateMinus(input1, input2.Value);
			}
			EvaluableMinus evaluableMinus = new EvaluableMinus(input1, input2);
			SignalNewTerm(evaluableMinus);
			return evaluableMinus;
		}

		/// <summary>
		/// Creates the difference of two numerical terms
		/// </summary>
		public EvaluableNumericalTerm CreateMinus(EvaluableNumericalTerm input1, double input2)
		{
			EvaluablePlusWithConstant evaluablePlusWithConstant = new EvaluablePlusWithConstant(input1, 0.0 - input2);
			SignalNewTerm(evaluablePlusWithConstant);
			return evaluablePlusWithConstant;
		}

		/// <summary>
		/// Creates the sum of two numerical terms
		/// </summary>
		public EvaluableNumericalTerm CreateSum(EvaluableNumericalTerm input1, double input2)
		{
			EvaluablePlusWithConstant evaluablePlusWithConstant = new EvaluablePlusWithConstant(input1, input2);
			SignalNewTerm(evaluablePlusWithConstant);
			return evaluablePlusWithConstant;
		}

		/// <summary>
		/// Creates the sum of two numerical terms
		/// </summary>
		public EvaluableNumericalTerm CreateSum(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
		{
			if (input1.IsConstant)
			{
				return CreateSum(input2, input1.Value);
			}
			if (input2.IsConstant)
			{
				return CreateSum(input1, input2.Value);
			}
			EvaluableBinaryPlus evaluableBinaryPlus = new EvaluableBinaryPlus(input1, input2);
			SignalNewTerm(evaluableBinaryPlus);
			return evaluableBinaryPlus;
		}

		/// <summary>
		/// Creates the sum of a list of terms
		/// </summary>
		public EvaluableNumericalTerm CreateSum(EvaluableNumericalTerm[] args)
		{
			switch (args.Length)
			{
			case 0:
				return ConstantZero;
			case 1:
				return args[0];
			case 2:
				return CreateSum(args[0], args[1]);
			default:
				return CreateArrayTerm(_createSumN, _createSum2, args, 0, args.Length - 1);
			}
		}

		/// <summary>
		/// Creates the sum of a weighted list of terms, i.e.
		/// the sum of the terms coefs[i] * args[i], forall i
		/// </summary>
		public EvaluableNumericalTerm CreateSum(double[] coefs, EvaluableNumericalTerm[] args)
		{
			switch (args.Length)
			{
			case 0:
				return ConstantZero;
			case 1:
				return CreateProduct(coefs[0], args[0]);
			default:
				if (Array.TrueForAll(coefs, EvaluationStatics.IsInteger32))
				{
					return CreateSum(Array.ConvertAll(coefs, (double x) => (int)x), args);
				}
				return CreateWeightedSum(coefs, args, 0, args.Length - 1);
			}
		}

		/// <summary>
		/// Creates the sum of a weighted list of terms, i.e.
		/// the sum of the terms coefs[i] * args[i], forall i
		/// </summary>
		public EvaluableNumericalTerm CreateSum(int[] coefs, EvaluableNumericalTerm[] args)
		{
			switch (args.Length)
			{
			case 0:
				return ConstantZero;
			case 1:
				return CreateProduct(coefs[0], args[0]);
			default:
				if (Array.TrueForAll(coefs, (int elt) => elt == 1))
				{
					return CreateSum(args);
				}
				return CreateWeightedSum(coefs, args, 0, args.Length - 1);
			}
		}

		/// <summary>
		/// Creates the Minimum of two numerical terms
		/// </summary>
		public EvaluableNumericalTerm CreateMin(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
		{
			EvaluableBinaryMin evaluableBinaryMin = new EvaluableBinaryMin(input1, input2);
			SignalNewTerm(evaluableBinaryMin);
			return evaluableBinaryMin;
		}

		/// <summary>
		/// Creates the Minimum of a list of terms
		/// </summary>
		public EvaluableNumericalTerm CreateMin(EvaluableNumericalTerm[] args)
		{
			switch (args.Length)
			{
			case 0:
				return CreateConstant(double.MaxValue);
			case 1:
				return args[0];
			case 2:
				return CreateMin(args[0], args[1]);
			default:
				return CreateArrayTerm(_createMinN, _createMin2, args, 0, args.Length - 1);
			}
		}

		/// <summary>
		/// Creates the Maximum of two numerical terms
		/// </summary>
		public EvaluableNumericalTerm CreateMax(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
		{
			EvaluableBinaryMax evaluableBinaryMax = new EvaluableBinaryMax(input1, input2);
			SignalNewTerm(evaluableBinaryMax);
			return evaluableBinaryMax;
		}

		/// <summary>
		/// Creates the Maximum of a list of terms
		/// </summary>
		public EvaluableNumericalTerm CreateMax(EvaluableNumericalTerm[] args)
		{
			switch (args.Length)
			{
			case 0:
				return CreateConstant(double.MinValue);
			case 1:
				return args[0];
			case 2:
				return CreateMax(args[0], args[1]);
			default:
				return CreateArrayTerm(_createMaxN, _createMax2, args, 0, args.Length - 1);
			}
		}

		/// <summary>
		/// Creates a term that is true iff all the arguments take different values
		/// </summary>
		public EvaluableBooleanTerm CreateAllDifferent(EvaluableNumericalTerm[] args)
		{
			switch (args.Length)
			{
			case 0:
			case 1:
				return ConstantTrue;
			case 2:
				return CreateDifferent(args[0], args[1]);
			default:
			{
				EvaluableAllDifferent evaluableAllDifferent = new EvaluableAllDifferent(args);
				SignalNewTerm(evaluableAllDifferent);
				return evaluableAllDifferent;
			}
			}
		}

		/// <summary>
		/// Creates a term that is true iff the arguments are equal
		/// </summary>
		public EvaluableBooleanTerm CreateEqual(EvaluableNumericalTerm x, EvaluableNumericalTerm y)
		{
			EvaluableEqual evaluableEqual = new EvaluableEqual(x, y, this);
			SignalNewTerm(evaluableEqual);
			return evaluableEqual;
		}

		/// <summary>
		/// Creates a term that is true iff the arguments are different
		/// </summary>
		public EvaluableBooleanTerm CreateDifferent(EvaluableNumericalTerm x, EvaluableNumericalTerm y)
		{
			EvaluableDifferent evaluableDifferent = new EvaluableDifferent(x, y);
			SignalNewTerm(evaluableDifferent);
			return evaluableDifferent;
		}

		/// <summary>
		/// Creates a term that is true iff the arguments
		/// are in strictly increasing order
		/// </summary>
		public EvaluableBooleanTerm CreateLessStrict(EvaluableNumericalTerm x, EvaluableNumericalTerm y)
		{
			EvaluableBinaryLessStrict evaluableBinaryLessStrict = new EvaluableBinaryLessStrict(x, y);
			SignalNewTerm(evaluableBinaryLessStrict);
			return evaluableBinaryLessStrict;
		}

		/// <summary>
		/// Creates a term that is true iff the arguments
		/// are in strictly increasing order
		/// </summary>
		public EvaluableBooleanTerm CreateLessStrict(EvaluableNumericalTerm[] args)
		{
			switch (args.Length)
			{
			case 0:
			case 1:
				return ConstantTrue;
			case 2:
				return CreateLessStrict(args[0], args[1]);
			default:
			{
				EvaluableLessStrict evaluableLessStrict = new EvaluableLessStrict(args);
				SignalNewTerm(evaluableLessStrict);
				return evaluableLessStrict;
			}
			}
		}

		/// <summary>
		/// Creates a term that is true iff the arguments
		/// are in non-decreasing order
		/// </summary>
		public EvaluableBooleanTerm CreateLessEqual(EvaluableNumericalTerm x, EvaluableNumericalTerm y)
		{
			EvaluableBinaryLessEqual evaluableBinaryLessEqual = new EvaluableBinaryLessEqual(x, y);
			SignalNewTerm(evaluableBinaryLessEqual);
			return evaluableBinaryLessEqual;
		}

		/// <summary>
		/// Creates a term that is true iff the arguments
		/// are in non-decreasing order
		/// </summary>
		public EvaluableBooleanTerm CreateLessEqual(EvaluableNumericalTerm[] args)
		{
			switch (args.Length)
			{
			case 0:
			case 1:
				return ConstantTrue;
			case 2:
				return CreateLessEqual(args[0], args[1]);
			default:
			{
				EvaluableLessEqual evaluableLessEqual = new EvaluableLessEqual(args);
				SignalNewTerm(evaluableLessEqual);
				return evaluableLessEqual;
			}
			}
		}

		/// <summary>
		/// Term that is true if X is in the range [lower, upper]
		/// </summary>
		internal EvaluableRangeConstraint CreateLessEqual(double lower, EvaluableNumericalTerm x, double upper)
		{
			EvaluableRangeConstraint evaluableRangeConstraint = new EvaluableRangeConstraint(lower, x, upper, this);
			SignalNewTerm(evaluableRangeConstraint);
			return evaluableRangeConstraint;
		}

		/// <summary>
		/// Creates the negation (opposite truth value) of a Boolean term
		/// </summary>
		public EvaluableBooleanTerm CreateNot(EvaluableBooleanTerm arg)
		{
			EvaluableNot evaluableNot = new EvaluableNot(arg);
			SignalNewTerm(evaluableNot);
			return evaluableNot;
		}

		/// <summary>
		/// Creates the Boolean And of two Boolean terms
		/// </summary>
		public EvaluableBooleanTerm CreateAnd(EvaluableBooleanTerm input1, EvaluableBooleanTerm input2)
		{
			EvaluableBinaryAnd evaluableBinaryAnd = new EvaluableBinaryAnd(input1, input2);
			SignalNewTerm(evaluableBinaryAnd);
			return evaluableBinaryAnd;
		}

		/// <summary>
		/// Creates the Boolean And of a list of Boolean terms
		/// </summary>
		public EvaluableBooleanTerm CreateAnd(EvaluableBooleanTerm[] args)
		{
			switch (args.Length)
			{
			case 0:
				return ConstantTrue;
			case 1:
				return args[0];
			case 2:
				return CreateAnd(args[0], args[1]);
			default:
				return CreateArrayTerm(_createAndN, _createAnd2, args, 0, args.Length - 1);
			}
		}

		/// <summary>
		/// Creates the Boolean Or of two Boolean terms
		/// </summary>
		public EvaluableBooleanTerm CreateOr(EvaluableBooleanTerm input1, EvaluableBooleanTerm input2)
		{
			EvaluableBinaryOr evaluableBinaryOr = new EvaluableBinaryOr(input1, input2);
			SignalNewTerm(evaluableBinaryOr);
			return evaluableBinaryOr;
		}

		/// <summary>
		/// Creates the Boolean Or of a list of Boolean terms
		/// </summary>
		public EvaluableBooleanTerm CreateOr(EvaluableBooleanTerm[] args)
		{
			switch (args.Length)
			{
			case 0:
				return ConstantFalse;
			case 1:
				return args[0];
			case 2:
				return CreateOr(args[0], args[1]);
			default:
				return CreateArrayTerm(_createOrN, _createOr2, args, 0, args.Length - 1);
			}
		}

		/// <summary>
		/// Creates the a conditional term, equal to one case
		/// when the condition is true, else to the other
		/// </summary>
		public EvaluableNumericalTerm CreateIf(EvaluableBooleanTerm condition, EvaluableNumericalTerm caseTrue, EvaluableNumericalTerm caseFalse)
		{
			EvaluableIf evaluableIf = new EvaluableIf(condition, caseTrue, caseFalse);
			SignalNewTerm(evaluableIf);
			return evaluableIf;
		}

		/// <summary>
		/// Creates a term of the form F(x), where x is a numerical term
		/// and F is an arbitrary function from double to double 
		/// </summary>
		public EvaluableNumericalTerm CreateUnaryFunction(Func<double, double> fun, EvaluableNumericalTerm x, TermModelOperation op)
		{
			EvaluableUnaryNumericalFunction evaluableUnaryNumericalFunction = new EvaluableUnaryNumericalFunction(fun, x, op);
			SignalNewTerm(evaluableUnaryNumericalFunction);
			return evaluableUnaryNumericalFunction;
		}

		/// <summary>
		/// Creates a term of the form F(x), where x is a numerical term
		/// and F is an arbitrary function from double to double 
		/// </summary>
		public EvaluableNumericalTerm CreateBinaryFunction(Func<double, double, double> fun, EvaluableNumericalTerm x, EvaluableNumericalTerm y, TermModelOperation op)
		{
			EvaluableBinaryNumericalFunction evaluableBinaryNumericalFunction = new EvaluableBinaryNumericalFunction(fun, x, y, op);
			SignalNewTerm(evaluableBinaryNumericalFunction);
			return evaluableBinaryNumericalFunction;
		}

		/// <summary>
		/// Creates a term of the form F(x), where x is a numerical term
		/// and F is an arbitrary function from double to double 
		/// </summary>
		public EvaluableNumericalTerm CreateNaryFunction(Func<double[], double> fun, EvaluableNumericalTerm[] x)
		{
			EvaluableNaryNumericalFunction evaluableNaryNumericalFunction = new EvaluableNaryNumericalFunction(fun, x);
			SignalNewTerm(evaluableNaryNumericalFunction);
			return evaluableNaryNumericalFunction;
		}

		/// <summary>
		/// Creates a term that casts a Boolean term to a numerical with value 0 or 1
		/// </summary>
		public EvaluableNumericalTerm CreateConversionToNumerical(EvaluableBooleanTerm x)
		{
			EvaluableConversionBooleanToNumerical evaluableConversionBooleanToNumerical = new EvaluableConversionBooleanToNumerical(x);
			SignalNewTerm(evaluableConversionBooleanToNumerical);
			return evaluableConversionBooleanToNumerical;
		}

		/// <summary>
		/// Creates a term that casts a numerical term with value 0 or 1 to its Boolean value
		/// </summary>
		public EvaluableBooleanTerm CreateConversionToBoolean(EvaluableNumericalTerm x)
		{
			EvaluableConversionNumericalToBoolean evaluableConversionNumericalToBoolean = new EvaluableConversionNumericalToBoolean(x);
			SignalNewTerm(evaluableConversionNumericalToBoolean);
			return evaluableConversionNumericalToBoolean;
		}

		private EvaluableNumericalTerm CreateSum_Nary(EvaluableNumericalTerm[] args)
		{
			EvaluableSimpleSum evaluableSimpleSum = new EvaluableSimpleSum(args);
			SignalNewTerm(evaluableSimpleSum);
			return evaluableSimpleSum;
		}

		private EvaluableNumericalTerm CreateProd_Nary(EvaluableNumericalTerm[] args)
		{
			EvaluableProduct evaluableProduct = new EvaluableProduct(args);
			SignalNewTerm(evaluableProduct);
			return evaluableProduct;
		}

		private EvaluableNumericalTerm CreateMin_Nary(EvaluableNumericalTerm[] args)
		{
			EvaluableMin evaluableMin = new EvaluableMin(args);
			SignalNewTerm(evaluableMin);
			return evaluableMin;
		}

		private EvaluableNumericalTerm CreateMax_Nary(EvaluableNumericalTerm[] args)
		{
			EvaluableMax evaluableMax = new EvaluableMax(args);
			SignalNewTerm(evaluableMax);
			return evaluableMax;
		}

		private EvaluableBooleanTerm CreateAnd_Nary(EvaluableBooleanTerm[] args)
		{
			EvaluableAnd evaluableAnd = new EvaluableAnd(args);
			SignalNewTerm(evaluableAnd);
			return evaluableAnd;
		}

		private EvaluableBooleanTerm CreateOr_Nary(EvaluableBooleanTerm[] args)
		{
			EvaluableOr evaluableOr = new EvaluableOr(args);
			SignalNewTerm(evaluableOr);
			return evaluableOr;
		}

		/// <summary>
		/// This should be called when constructing the Evaluator
		/// </summary>
		private void InitDelegates()
		{
			_createSum2 = CreateSum;
			_createSumN = CreateSum_Nary;
			_createAnd2 = CreateAnd;
			_createAndN = CreateAnd_Nary;
			_createOr2 = CreateOr;
			_createOrN = CreateOr_Nary;
			_createProd2 = CreateProduct;
			_createProdN = CreateProd_Nary;
			_createMin2 = CreateMin;
			_createMinN = CreateMin_Nary;
			_createMax2 = CreateMax;
			_createMaxN = CreateMax_Nary;
		}

		/// <summary>
		/// Create terms representing the application of a commutative, 
		/// associative operations, for instance Sums, to a (possible long)
		/// array of arguments. 
		/// </summary>
		/// <remarks>
		/// Guarantees a logarithmic bound on the recomputation of these operations.
		/// Below a certain limit, say 20 arguments, it is more memory- and time- 
		/// efficient to simply allocate a sum with 20 arguments, recomputed non-
		/// incrementally. 
		///
		/// Beyond the limit we use a balanced tree of operations (say sums) to 
		/// guarantee the logarithmic bound, and allow to scale to very large 
		/// number of terms. 
		///
		/// For instance if we have a sum of 2000 terms we divide it in about 20 
		/// chunks. Each chunk contains 100 arguments and is itself, recursively,
		/// decomposed into chunks of about 20 chunks. (The MaxSize is not
		/// necessarily 20, and should be tuned empirically)
		/// </remarks>
		private T CreateArrayTerm<T>(Func<T[], T> naryConstructor, Func<T, T, T> binaryConstructor, T[] args, int l, int r)
		{
			int num = r - l + 1;
			if (num == 1)
			{
				return args[l];
			}
			if (num == 2)
			{
				return binaryConstructor(args[l], args[r]);
			}
			if (num <= 64)
			{
				return naryConstructor(EvaluationStatics.SnapshotRange(args, l, r));
			}
			int num2 = num / 64;
			List<T> list = new List<T>();
			for (int i = l; i <= r; i += num2)
			{
				int r2 = Math.Min(r, i + num2 - 1);
				T item = CreateArrayTerm(naryConstructor, binaryConstructor, args, i, r2);
				list.Add(item);
			}
			return naryConstructor(list.ToArray());
		}

		/// <summary>
		/// Specialized code to CreateArrayTerm for weighted sums
		/// </summary>
		private EvaluableNumericalTerm CreateWeightedSum(int[] coefs, EvaluableNumericalTerm[] args, int l, int r)
		{
			int num = r - l + 1;
			EvaluableNumericalTerm evaluableNumericalTerm;
			if (num == 1)
			{
				evaluableNumericalTerm = new EvaluableProductByConstant(coefs[l], args[l]);
			}
			else if (num <= 64)
			{
				evaluableNumericalTerm = new EvaluableWeightedIntSum(EvaluationStatics.SnapshotRange(coefs, l, r), EvaluationStatics.SnapshotRange(args, l, r));
			}
			else
			{
				int num2 = num / 64;
				List<EvaluableNumericalTerm> list = new List<EvaluableNumericalTerm>();
				for (int i = l; i <= r; i += num2)
				{
					int r2 = Math.Min(r, i + num2 - 1);
					EvaluableNumericalTerm item = CreateWeightedSum(coefs, args, i, r2);
					list.Add(item);
				}
				evaluableNumericalTerm = new EvaluableSimpleSum(list.ToArray());
			}
			SignalNewTerm(evaluableNumericalTerm);
			return evaluableNumericalTerm;
		}

		/// <summary>
		/// Specialized code to CreateArrayTerm for weighted sums
		/// </summary>
		private EvaluableNumericalTerm CreateWeightedSum(double[] coefs, EvaluableNumericalTerm[] args, int l, int r)
		{
			int num = r - l + 1;
			EvaluableNumericalTerm evaluableNumericalTerm;
			if (num == 1)
			{
				evaluableNumericalTerm = new EvaluableProductByConstant(coefs[l], args[l]);
			}
			else if (num <= 64)
			{
				evaluableNumericalTerm = new EvaluableWeightedRealSum(EvaluationStatics.SnapshotRange(coefs, l, r), EvaluationStatics.SnapshotRange(args, l, r));
			}
			else
			{
				int num2 = num / 64;
				List<EvaluableNumericalTerm> list = new List<EvaluableNumericalTerm>();
				for (int i = l; i <= r; i += num2)
				{
					int r2 = Math.Min(r, i + num2 - 1);
					EvaluableNumericalTerm item = CreateWeightedSum(coefs, args, i, r2);
					list.Add(item);
				}
				evaluableNumericalTerm = new EvaluableSimpleSum(list.ToArray());
			}
			SignalNewTerm(evaluableNumericalTerm);
			return evaluableNumericalTerm;
		}

		/// <summary>
		/// This ritual should be called by every method that creates a term
		/// </summary>
		private void SignalNewTerm(EvaluableTerm t)
		{
			_initialized = false;
		}

		/// <summary>
		/// Indicate that the expression should be re-evaluated
		/// </summary>
		internal void Reschedule(EvaluableTerm e)
		{
			if (!e.IsEnqueued)
			{
				_reEvaluationQueue[e.Depth].Push(e);
				e.MarkEnqueued();
				_nbEnqueued++;
			}
		}

		/// <summary>
		/// Re-evaluates all terms
		/// </summary>
		public void Recompute()
		{
			Initialize();
			int num = 1;
			while (_nbEnqueued > 0)
			{
				Stack<EvaluableTerm> stack = _reEvaluationQueue[num];
				_nbEnqueued -= stack.Count;
				while (stack.Count > 0)
				{
					EvaluableTerm evaluableTerm = stack.Pop();
					evaluableTerm.MarkDequeued();
					evaluableTerm.Recompute(out var change);
					if (change)
					{
						evaluableTerm.RescheduleDependents(this);
					}
				}
				num++;
			}
		}

		public void RecomputeEliminatedVariables()
		{
			Initialize();
			Recompute();
			foreach (KeyValuePair<EvaluableVariable, EvaluableNumericalTerm> eliminatedVariable in _eliminatedVariables)
			{
				eliminatedVariable.Key.ChangeValue(eliminatedVariable.Value.Value, out var _);
			}
		}

		/// <summary>
		/// (re)initializes all terms systematically, in a non-incremental way.
		/// Note that the array is first sorted by increasing depths
		/// </summary>
		/// <returns>
		/// True if the value of at least one term has changed
		/// </returns>
		private bool ReinitializeAll(EvaluableTerm[] termList)
		{
			bool flag = false;
			Array.Sort(termList, (EvaluableTerm x, EvaluableTerm y) => x.Depth.CompareTo(y.Depth));
			foreach (EvaluableTerm evaluableTerm in termList)
			{
				evaluableTerm.Reinitialize(out var change);
				flag |= change && !double.IsNaN(evaluableTerm.StoredValue);
			}
			return flag;
		}

		/// <summary>
		/// Reassign the variable 
		/// </summary>
		public void ChangeValue(EvaluableVariable x, double newValue)
		{
			Initialize();
			x.ChangeValue(newValue, out var change);
			if (change)
			{
				x.RescheduleDependents(this);
			}
		}

		/// <summary>
		/// Computes (a conservative estimate, i.e. superset) of the variables 
		/// that are involved in the violation of at least one constraint
		/// </summary>
		/// <param name="prng">A pseudo-random number generator</param>
		/// <param name="diverse">
		/// When set to true (which is best, except for checking the sanity
		/// of the returned set) random elements are added if the set does
		/// not reach a certain size, amortizing this operation and avoiding
		/// improving diversification
		/// </param>
		public IEnumerable<EvaluableVariable> ComputeCauses(Random prng, bool diverse)
		{
			Recompute();
			Stack<EvaluableTerm> stack = new Stack<EvaluableTerm>();
			List<EvaluableTerm> candidates = new List<EvaluableTerm>();
			EvaluableTerm.Explore(candidates, stack, _violation);
			while (stack.Count > 0)
			{
				EvaluableTerm evaluableTerm = stack.Pop();
				foreach (EvaluableTerm input in evaluableTerm.EnumerateMoveCandidates())
				{
					EvaluableTerm.Explore(candidates, stack, input);
				}
			}
			int count = _allVariables.Count;
			if (diverse)
			{
				for (int num = count / 100; num >= 0; num--)
				{
					EvaluableTerm x = _allVariables[prng.Next(count)];
					if (!x.IsEnqueued)
					{
						candidates.Add(x);
						x.MarkEnqueued();
					}
				}
			}
			EvaluableTerm.DequeueAll(candidates);
			EvaluableVariable[] array = (from c in candidates
				let x = c as EvaluableVariable
				where x != null
				select x).ToArray();
			EvaluationStatics.Permutate(array, prng);
			return array;
		}

		/// <summary>
		/// Get an array representation of the quality of the current evaluation:
		/// current violation at position 0, 
		/// then current value of the first minimization goal (if any) at position 1, 
		/// of the second minimization goal (if any) at position 2,
		/// etc. This representation means that lexicographically lower is better
		/// </summary>
		public void UpdateAggregatedQuality(ref double[] result, double tolerance)
		{
			if (result == null || result.Length != _goals.Count + 1)
			{
				result = new double[_goals.Count + 1];
			}
			result[0] = _violation.Value;
			if (result[0] < tolerance)
			{
				result[0] = 0.0;
			}
			for (int i = 0; i < _goals.Count; i++)
			{
				result[i + 1] = _goals[i].Value;
			}
		}

		/// <summary>
		/// All the constraints declared in this evaluator
		/// </summary>
		public IEnumerable<EvaluableBooleanTerm> EnumerateConstraints()
		{
			return _constraints;
		}

		/// <summary>
		/// All the goals declared in this evaluator
		/// </summary>
		public IEnumerable<EvaluableNumericalTerm> EnumerateGoals()
		{
			return _goals;
		}
	}
}
