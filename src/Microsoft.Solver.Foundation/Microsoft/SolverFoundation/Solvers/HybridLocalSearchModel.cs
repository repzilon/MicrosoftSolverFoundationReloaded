using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>The model for the HybridLocalSearchSolver.
	/// </summary>
	public class HybridLocalSearchModel : ITermModel, IRowVariableModel, IGoalModel
	{
		/// <summary>
		/// information attached to every goal
		/// </summary>
		protected class Goal : IGoal
		{
			/// <summary> the goal variable key  </summary>
			public object Key { get; internal set; }

			/// <summary> The variable index (vid) of this goal's row </summary>
			public int Index { get; internal set; }

			/// <summary> the goal priority. The lower the value, the higher the priority </summary>
			public int Priority { get; set; }

			/// <summary> whether the goal is to minimize the objective row </summary>
			public bool Minimize { get; set; }

			/// <summary> whether the goal is enabled </summary>
			public bool Enabled { get; set; }
		}

		internal const int _smallIntensityStep = 20;

		internal const int _largeIntensityInFocusArea = 1000;

		internal const int _largeIntensityOutOfFocus = 10;

		/// <summary>
		/// Precision at which Large Search stops searching: moves that improve any
		/// quality measure by less than this number are just ignored. This is to 
		/// ignore long chains of insignificant improvements (by steps of say 1e-10),
		/// which can otherwise happen
		/// </summary>
		internal const double LargeSearchAcceptance = 1E-08;

		private static Predicate<EvaluableNumericalTerm> TermIsConstant = (EvaluableNumericalTerm x) => x.IsConstant;

		private static readonly Func<double, double> _sin = Math.Sin;

		private static readonly Func<double, double> _cos = Math.Cos;

		private static readonly Func<double, double> _tan = Math.Tan;

		private static readonly Func<double, double> _sinh = Math.Sinh;

		private static readonly Func<double, double> _cosh = Math.Cosh;

		private static readonly Func<double, double> _tanh = Math.Tanh;

		private static readonly Func<double, double> _arcSin = Math.Asin;

		private static readonly Func<double, double> _arcCos = Math.Acos;

		private static readonly Func<double, double> _arcTan = Math.Atan;

		private static readonly Func<double, double> _ceiling = Math.Ceiling;

		private static readonly Func<double, double> _floor = Math.Floor;

		private static readonly Func<double, double> _exp = Math.Exp;

		private static readonly Func<double, double> _sqrt = Math.Sqrt;

		private static readonly Func<double, double> _log10 = Math.Log10;

		private static readonly Func<double, double> _logE = (double x) => Math.Log(x, Math.E);

		private static readonly Func<double, double, double> _pow = Math.Pow;

		/// <summary>
		/// Caches the terms introduced to convert Boolean to numerical terms.
		/// Always use in pair with _conversionsToBool
		/// </summary>
		private Dictionary<EvaluableBooleanTerm, EvaluableNumericalTerm> _conversionsToNum = new Dictionary<EvaluableBooleanTerm, EvaluableNumericalTerm>();

		/// <summary>
		/// Caches the terms introduced to convert numerical to Boolean terms.
		/// Always use in pair with _conversionsToNum
		/// </summary>
		private Dictionary<EvaluableNumericalTerm, EvaluableBooleanTerm> _conversionsToBool = new Dictionary<EvaluableNumericalTerm, EvaluableBooleanTerm>();

		/// <summary>
		/// Quality of the current search state
		/// </summary>
		private double[] _currentQuality;

		/// <summary>
		/// Quality of the best solution found so far
		/// </summary>
		private double[] _bestQuality;

		/// <summary>
		/// Best solution found so far, 
		/// saves the Values of the _allVariables
		/// </summary>
		private double[] _bestSolution;

		/// <summary>
		/// Timer started at beginning of the search
		/// </summary>
		private Stopwatch _searchTimer;

		/// <summary>
		/// Parameters passed to the search algorithm
		/// </summary>
		private HybridLocalSearchParameters _searchParameters;

		/// <summary>
		/// True if a termination has been requested
		/// </summary>
		private bool _searchTerminationRequested;

		/// <summary>
		/// Flag that is set to true whenever an invalid construct is 
		/// found in the model
		/// </summary>
		internal bool _invalidModelAtConstruction;

		/// <summary>
		/// Number from 0 to MaxFocus that determines how focussed the search 
		/// should be (higher: More focussed). 
		/// </summary>
		internal int _focus = MinFocus;

		/// <summary>
		/// Lists the possible diameters to consider when 
		/// doing 'radius search'. 
		/// </summary>
		internal static double[] Distance = new double[23]
		{
			10000000000.0, 100000000.0, 1000000.0, 100000.0, 10000.0, 1000.0, 316.0, 100.0, 31.6, 10.0,
			3.16, 1.0, 0.316, 0.1, 0.0316, 0.01, 0.00316, 0.001, 0.000316, 0.0001,
			1E-05, 1E-06, 1E-07
		};

		/// <summary>
		/// Minimal value of the focus parameter of method PickNeighbour.
		/// Means that points at arbitrarily large distance from the current point
		/// will be considered
		/// </summary>
		internal static readonly int MinFocus = 0;

		/// <summary>
		/// Minimal value of the focus parameter of method PickNeighbour.
		/// Means that only points at a small distance from the current point 
		/// will be considered
		/// </summary>
		internal static readonly int MaxFocus = Distance.Length - 1;

		internal int _accuracy = MinAccuracy;

		internal static double[] Threshold = new double[12]
		{
			0.01, 0.001, 0.0001, 1E-05, 1E-06, 1E-07, 1E-08, 3.16E-09, 1E-09, 3.16E-10,
			1E-10, 0.0
		};

		internal static readonly int MinAccuracy = 0;

		internal static readonly int MaxAccuracy = Threshold.Length - 1;

		/// <summary>
		/// An enumerator used by PeekConflictingVariable to return different
		/// variables every time it is called, based on a buffering of the
		/// ComputeCause method
		/// </summary>
		private IEnumerator<EvaluableVariable> _enumerator;

		/// <summary>
		/// The evaluator, that keeps all terms, and maintains their values/violations
		/// </summary>
		private TermEvaluator _evaluator;

		/// <summary>
		/// A list of all terms constructed in this problem
		/// </summary>
		internal List<EvaluableTerm> _allTerms;

		/// <summary>
		/// All numerical variables created in this problem
		/// </summary>
		private List<EvaluableVariable> _allVariables;

		/// <summary>
		/// Pseudo-random number generator attached to this search
		/// </summary>
		private Random _prng;

		/// <summary>
		/// Mapping from row/column keys to indexes
		/// </summary>
		private Dictionary<object, int> _indexFromKey;

		/// <summary>
		/// Mapping from row/column indexes to their associated keys
		/// </summary>
		private List<object> _keyFromIndex;

		/// <summary>
		/// Goals, or objectives to minimize or maximize
		/// </summary>
		protected List<Goal> goalList;

		/// <summary>
		/// Depth of iterators currently reading This
		/// </summary>
		private int _modelReadCount;

		/// <summary>
		/// Bookkeeping of any bound constraint associated to Boolean or Numerical terms. 
		/// Allows to RemoveConstraint when a new SetBounds is done on a term that already has bounds
		/// </summary>
		/// <remarks>
		/// For a Boolean term the bound constraint attached to it can be:
		/// - itself! if we have done imposed that term is true 
		///   (lower bound = 1)
		/// - a term expressing its negation: if we have imposed that the term is false 
		///   (upper bound = 0)
		///
		/// For a numerical term the bound constraint attached to it is always
		/// an EvaluableRange constraint.
		///
		/// NOTE: for a Boolean term X we could use the approach where we treat it as 
		/// a numerical term, create an EvaluableRange, and add this evaluableRange
		/// constraint. But this would amount to create a Boolean term (evaluableRange)
		/// that represents that another Boolean term (X) is true, and this would also
		/// introduce a conversion term. The more natural approach is to directly 
		/// add X (or its negation) as a constraint.
		/// </remarks>
		private Dictionary<EvaluableTerm, EvaluableBooleanTerm> _boundConstraints = new Dictionary<EvaluableTerm, EvaluableBooleanTerm>();

		/// <summary>
		/// Set the random seed that (re)initializes the random 
		/// number sequence generation
		/// </summary>
		public int RandomSeed
		{
			set
			{
				_prng = new Random(value);
			}
		}

		/// <summary>
		/// Current Step Number of the search when it is running
		/// </summary>
		public long Step { get; private set; }

		/// <summary>
		/// Violation of the current state
		/// </summary>
		public double Violation => _currentQuality[0];

		/// <summary>
		/// Violation of the best solution found so far
		/// </summary>
		internal double BestViolation => _bestQuality[0];

		/// <summary>
		/// Snapshot the current quality
		/// </summary>
		internal double[] Quality
		{
			get
			{
				double[] result = null;
				_evaluator.Recompute();
				_evaluator.UpdateAggregatedQuality(ref result, Threshold[_accuracy]);
				return result;
			}
		}

		/// <summary>
		/// Magic number that determines the number of steps of the
		/// small neighourhood search algorithm
		/// </summary>
		private int SmallSearchIntensity => 20 * VariableCount;

		/// <summary>
		/// Magic number that determines the number of steps of the
		/// small neighourhood search algorithm
		/// </summary>
		private int LargeSearchIntensity
		{
			get
			{
				double num = Distance[_focus];
				if (0.0001 <= num && num <= 100.0)
				{
					return 1000;
				}
				return 10;
			}
		}

		/// <summary>
		/// Number of constraints
		/// </summary>
		protected int ConstraintsCount => _evaluator.ConstraintsCount;

		/// <summary>
		/// Number of goals
		/// </summary>
		protected int GoalsCount => _evaluator.GoalsCount;

		/// <summary>
		/// Used for row or variable key comparison 
		/// </summary>
		public IEqualityComparer<object> KeyComparer => EqualityComparer<object>.Default;

		/// <summary> return the variable index collection, inclusive of rows
		/// </summary>
		public IEnumerable<int> Indices
		{
			get
			{
				try
				{
					_modelReadCount++;
					int count = _allTerms.Count;
					for (int i = 0; i < count; i++)
					{
						yield return i;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary> Return the variable and row key collection.
		/// Indices are guaranteed to &gt;= 0 and &lt; KeyCount.
		/// </summary>
		public IEnumerable<object> Keys
		{
			get
			{
				try
				{
					_modelReadCount++;
					foreach (KeyValuePair<object, int> pair in _indexFromKey)
					{
						KeyValuePair<object, int> keyValuePair = pair;
						yield return keyValuePair.Key;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary> the number of keys, inclusive of rows and variables.
		/// </summary>
		public int KeyCount => _indexFromKey.Count;

		/// <summary> Return the row index collection. 
		/// </summary>
		public IEnumerable<int> RowIndices
		{
			get
			{
				try
				{
					_modelReadCount++;
					for (int i = 0; i < _allTerms.Count; i++)
					{
						if (_allTerms[i].Operation != (TermModelOperation)(-1))
						{
							yield return i;
						}
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary> Return the row key collection. 
		/// </summary>
		public IEnumerable<object> RowKeys
		{
			get
			{
				try
				{
					_modelReadCount++;
					foreach (KeyValuePair<object, int> kvp in _indexFromKey)
					{
						List<EvaluableTerm> allTerms = _allTerms;
						KeyValuePair<object, int> keyValuePair = kvp;
						if (allTerms[keyValuePair.Value].Operation != (TermModelOperation)(-1))
						{
							KeyValuePair<object, int> keyValuePair2 = kvp;
							yield return keyValuePair2.Key;
						}
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary> The number of rows in the model. 
		/// </summary>
		public int RowCount => ((IRowVariableModel)this).RowIndices.Count();

		/// <summary>
		/// return the variable index collection
		/// </summary>
		public IEnumerable<int> VariableIndices => from x in VariablePairs()
			select x.Value;

		/// <summary>
		/// return the variable key collection 
		/// </summary>
		public IEnumerable<object> VariableKeys => from x in VariablePairs()
			select x.Key;

		/// <summary>
		/// return the variable count 
		/// </summary>
		public int VariableCount => _allVariables.Count;

		/// <summary> return the number of integer variables 
		/// </summary>
		public int IntegerIndexCount
		{
			get
			{
				int num = 0;
				for (int i = 0; i < _allTerms.Count; i++)
				{
					if (((IRowVariableModel)this).GetIntegrality(i))
					{
						num++;
					}
				}
				return num;
			}
		}

		/// <summary>
		/// Return the goal collection of this model. 
		/// </summary>
		public IEnumerable<IGoal> Goals
		{
			get
			{
				try
				{
					_modelReadCount++;
					foreach (Goal goal in goalList)
					{
						yield return goal;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary>
		/// The number of goals in this model.
		/// </summary>
		public int GoalCount => goalList.Count;

		/// <summary>
		/// A solver that uses simple, general-purpose local search strategies.
		/// Can be used for discrete and continuous, linear and non-linear,
		/// satisfaction and/or optimization models.
		/// This solver is incomplete: it does not guarantee optimality. 
		/// </summary>
		public HybridLocalSearchModel()
		{
			_evaluator = new TermEvaluator();
			_allTerms = new List<EvaluableTerm>();
			_allVariables = new List<EvaluableVariable>();
			goalList = new List<Goal>();
			_modelReadCount = 0;
			_indexFromKey = new Dictionary<object, int>();
			_keyFromIndex = new List<object>();
			RandomSeed = 1234567890;
		}

		/// <summary>
		/// Should be called at the beginning of any method that modifies This 
		/// </summary>
		private void PreChange()
		{
			if (_modelReadCount > 0)
			{
				throw new InvalidOperationException();
			}
		}

		private static void ValidateBounds(Rational numLo, Rational numHi)
		{
			if (numLo.IsIndeterminate || numHi.IsIndeterminate || numHi < numLo)
			{
				throw new ArgumentException(Resources.InvalidBounds);
			}
		}

		/// <summary>
		/// Recods any newly created term.
		/// Should be called by ANY variable / operation creation method.
		/// </summary>
		private int Register(EvaluableTerm term)
		{
			PreChange();
			int count = _allTerms.Count;
			_allTerms.Add(term);
			return count;
		}

		/// <summary>
		/// Creates a variable with the specified domain 
		/// The initial value of the variable is chosen within the domain
		/// </summary>
		private int CreateNumericalVariable(LocalSearchDomain dom)
		{
			double init = ((!dom.Contains(0.0)) ? ((dom.Lower > 0.0) ? dom.Lower : dom.Upper) : 0.0);
			EvaluableVariable evaluableVariable = _evaluator.CreateNumericalVariable(init, dom);
			_allVariables.Add(evaluableVariable);
			return Register(evaluableVariable);
		}

		/// <summary>
		/// Creates a new variable that can be assigned any real value
		/// that is at least the lowerBound and at most the upperBound
		/// </summary>
		internal int CreateRealVariable(double lowerBound, double upperBound)
		{
			if (double.IsNaN(lowerBound) || double.IsNaN(upperBound))
			{
				_invalidModelAtConstruction = true;
			}
			if (lowerBound == upperBound)
			{
				return CreateConstant(lowerBound);
			}
			if (lowerBound < upperBound)
			{
				return CreateNumericalVariable(new LocalSearchContinuousInterval(lowerBound, upperBound));
			}
			throw new ArgumentException(Resources.InvalidBounds);
		}

		/// <summary>
		/// Creates a new variable that can be assigned any integer value
		/// that is at least the lowerBound and at most the upperBound
		/// </summary>
		internal int CreateIntegerVariable(long lowerBound, long upperBound)
		{
			if (lowerBound == upperBound)
			{
				return CreateConstant(lowerBound);
			}
			if (lowerBound < upperBound)
			{
				return CreateNumericalVariable(new LocalSearchIntegerInterval(lowerBound, upperBound));
			}
			throw new ArgumentException(Resources.InvalidBounds);
		}

		/// <summary>
		/// Creates a new variable that can be assigned 
		/// a finite number of real values
		/// </summary>
		internal int CreateRealVariable(IEnumerable<double> allowedValues)
		{
			double value = allowedValues.First();
			double[] array = allowedValues.ToArray();
			if (array.Length == 1)
			{
				return CreateConstant(value);
			}
			LocalSearchFiniteRealSet dom = new LocalSearchFiniteRealSet(array);
			return CreateNumericalVariable(dom);
		}

		/// <summary>
		/// Creates a new variable that can be assigned 
		/// a finite number of integer values
		/// </summary>
		internal int CreateIntegerVariable(IEnumerable<long> allowedValues)
		{
			double value = allowedValues.First();
			long[] array = allowedValues.ToArray();
			if (array.Length == 1)
			{
				return CreateConstant(value);
			}
			LocalSearchFiniteIntegerSet dom = new LocalSearchFiniteIntegerSet(array);
			return CreateNumericalVariable(dom);
		}

		/// <summary>
		/// Creates a term whose value is real and immutable
		/// </summary>
		internal int CreateConstant(double value)
		{
			return Register(CreateConstantTerm(value));
		}

		private EvaluableNumericalTerm CreateConstantTerm(double value)
		{
			if (double.IsNaN(value))
			{
				_invalidModelAtConstruction = true;
			}
			return _evaluator.CreateConstant(value);
		}

		/// <summary>
		/// Create a Boolean term whose value is constant
		/// </summary>
		internal int CreateConstant(bool value)
		{
			return Register(value ? _evaluator.ConstantTrue : _evaluator.ConstantFalse);
		}

		/// <summary>
		/// Get all the variables, Boolean and numerical
		/// </summary>
		internal IEnumerable<EvaluableTerm> GetAllVariables()
		{
			try
			{
				_modelReadCount++;
				foreach (EvaluableVariable allVariable in _allVariables)
				{
					yield return allVariable;
				}
			}
			finally
			{
				_modelReadCount--;
			}
		}

		/// <summary>
		/// Creates the sum of two terms
		/// </summary>
		internal int CreatePlus(int vid1, int vid2)
		{
			EvaluableNumericalTerm evaluableNumericalTerm = ExtractNumericalArg(vid1);
			EvaluableNumericalTerm evaluableNumericalTerm2 = ExtractNumericalArg(vid2);
			EvaluableNumericalTerm term = ((evaluableNumericalTerm.IsConstant && evaluableNumericalTerm2.IsConstant) ? CreateConstantTerm(evaluableNumericalTerm.Value + evaluableNumericalTerm2.Value) : _evaluator.CreateSum(evaluableNumericalTerm, evaluableNumericalTerm2));
			return Register(term);
		}

		/// <summary>
		/// Creates the sum of a set of terms
		/// </summary>
		internal int CreatePlus(int[] vids)
		{
			EvaluableNumericalTerm[] array = ExtractNumericalArgs(vids);
			EvaluableNumericalTerm term = (Array.TrueForAll(array, TermIsConstant) ? CreateConstantTerm(array.Select((EvaluableNumericalTerm x) => x.Value).Sum()) : _evaluator.CreateSum(array));
			return Register(term);
		}

		/// <summary>
		/// Creates the sum of a set of terms, weighted by coefficients
		/// </summary>
		internal int CreateWeightedSum(int[] coefs, int[] vids)
		{
			EvaluableNumericalTerm[] args = ExtractNumericalArgs(vids);
			EvaluableNumericalTerm term = (Array.TrueForAll(args, TermIsConstant) ? CreateConstantTerm((from i in Enumerable.Range(0, coefs.Length)
				select (double)coefs[i] * args[i].Value).Sum()) : _evaluator.CreateSum(coefs, args));
			return Register(term);
		}

		/// <summary>
		/// Creates the sum of a set of terms, weighted by coefficients
		/// </summary>
		internal int CreateWeightedSum(double[] coefs, int[] vids)
		{
			EvaluableNumericalTerm[] args = ExtractNumericalArgs(vids);
			EvaluableNumericalTerm term = (Array.TrueForAll(args, TermIsConstant) ? CreateConstantTerm((from i in Enumerable.Range(0, coefs.Length)
				select coefs[i] * args[i].Value).Sum()) : _evaluator.CreateSum(coefs, args));
			return Register(term);
		}

		/// <summary>
		/// Creates the difference of two terms
		/// </summary>
		internal int CreateMinus(int vid1, int vid2)
		{
			EvaluableNumericalTerm evaluableNumericalTerm = ExtractNumericalArg(vid1);
			EvaluableNumericalTerm evaluableNumericalTerm2 = ExtractNumericalArg(vid2);
			EvaluableNumericalTerm term = ((evaluableNumericalTerm.IsConstant && evaluableNumericalTerm2.IsConstant) ? CreateConstantTerm(evaluableNumericalTerm.Value - evaluableNumericalTerm2.Value) : _evaluator.CreateMinus(evaluableNumericalTerm, evaluableNumericalTerm2));
			return Register(term);
		}

		/// <summary>
		/// Creates the opposite (unary minus) of a term
		/// </summary>
		internal int CreateMinus(int vid)
		{
			EvaluableNumericalTerm evaluableNumericalTerm = ExtractNumericalArg(vid);
			EvaluableNumericalTerm term = (evaluableNumericalTerm.IsConstant ? CreateConstantTerm(0.0 - evaluableNumericalTerm.Value) : _evaluator.CreateUnaryMinus(evaluableNumericalTerm));
			return Register(term);
		}

		/// <summary>
		/// Creates the identity of a term
		/// </summary>
		internal int CreateIdentity(int vid)
		{
			CheckIdInRange(vid);
			EvaluableTerm evaluableTerm = _allTerms[vid];
			EvaluableTerm term;
			if (evaluableTerm is EvaluableBooleanTerm arg)
			{
				term = _evaluator.CreateUnaryIdentity(arg);
			}
			else
			{
				EvaluableNumericalTerm arg2 = evaluableTerm as EvaluableNumericalTerm;
				term = _evaluator.CreateUnaryIdentity(arg2);
			}
			return Register(term);
		}

		/// <summary>
		/// Creates the Absolute value of a term
		/// </summary>
		internal int CreateAbs(int vid)
		{
			EvaluableNumericalTerm evaluableNumericalTerm = ExtractNumericalArg(vid);
			EvaluableNumericalTerm term = (evaluableNumericalTerm.IsConstant ? CreateConstantTerm(Math.Abs(evaluableNumericalTerm.Value)) : _evaluator.CreateAbs(evaluableNumericalTerm));
			return Register(term);
		}

		/// <summary>
		/// Creates the quotient (division) of two terms
		/// </summary>
		internal int CreateQuotient(int vid1, int vid2)
		{
			EvaluableNumericalTerm evaluableNumericalTerm = ExtractNumericalArg(vid1);
			EvaluableNumericalTerm evaluableNumericalTerm2 = ExtractNumericalArg(vid2);
			EvaluableNumericalTerm term = ((evaluableNumericalTerm.IsConstant && evaluableNumericalTerm2.IsConstant) ? CreateConstantTerm(evaluableNumericalTerm.Value / evaluableNumericalTerm2.Value) : _evaluator.CreateQuotient(evaluableNumericalTerm, evaluableNumericalTerm2));
			return Register(term);
		}

		/// <summary>
		/// Creates the product of two terms
		/// </summary>
		internal int CreateTimes(int vid1, int vid2)
		{
			EvaluableNumericalTerm evaluableNumericalTerm = ExtractNumericalArg(vid1);
			EvaluableNumericalTerm evaluableNumericalTerm2 = ExtractNumericalArg(vid2);
			EvaluableNumericalTerm term = ((evaluableNumericalTerm.IsConstant && evaluableNumericalTerm2.IsConstant) ? CreateConstantTerm(evaluableNumericalTerm.Value * evaluableNumericalTerm2.Value) : _evaluator.CreateProduct(evaluableNumericalTerm, evaluableNumericalTerm2));
			return Register(term);
		}

		/// <summary>
		/// Creates the product of a set of terms
		/// </summary>
		internal int CreateTimes(int[] vids)
		{
			EvaluableNumericalTerm[] array = ExtractNumericalArgs(vids);
			EvaluableNumericalTerm term = (Array.TrueForAll(array, TermIsConstant) ? CreateConstantTerm(MultiplyAllvalues(array)) : _evaluator.CreateProduct(array));
			return Register(term);
		}

		private double MultiplyAllvalues(EvaluableNumericalTerm[] args)
		{
			double num = 1.0;
			foreach (EvaluableNumericalTerm evaluableNumericalTerm in args)
			{
				num *= evaluableNumericalTerm.Value;
			}
			return num;
		}

		/// <summary>
		/// Creates the minimum of two terms
		/// </summary>
		internal int CreateMin(int vid1, int vid2)
		{
			EvaluableNumericalTerm evaluableNumericalTerm = ExtractNumericalArg(vid1);
			EvaluableNumericalTerm evaluableNumericalTerm2 = ExtractNumericalArg(vid2);
			EvaluableNumericalTerm term = ((evaluableNumericalTerm.IsConstant && evaluableNumericalTerm2.IsConstant) ? CreateConstantTerm(Math.Min(evaluableNumericalTerm.Value, evaluableNumericalTerm2.Value)) : _evaluator.CreateMin(evaluableNumericalTerm, evaluableNumericalTerm2));
			return Register(term);
		}

		/// <summary>
		/// Creates the minimum of a set of terms
		/// </summary>
		internal int CreateMin(int[] vids)
		{
			EvaluableNumericalTerm[] array = ExtractNumericalArgs(vids);
			EvaluableNumericalTerm term = (Array.TrueForAll(array, TermIsConstant) ? CreateConstantTerm(array.Select((EvaluableNumericalTerm x) => x.Value).Min()) : _evaluator.CreateMin(array));
			return Register(term);
		}

		/// <summary>
		/// Creates the maximum of two terms
		/// </summary>
		internal int CreateMax(int vid1, int vid2)
		{
			EvaluableNumericalTerm evaluableNumericalTerm = ExtractNumericalArg(vid1);
			EvaluableNumericalTerm evaluableNumericalTerm2 = ExtractNumericalArg(vid2);
			EvaluableNumericalTerm term = ((evaluableNumericalTerm.IsConstant && evaluableNumericalTerm2.IsConstant) ? CreateConstantTerm(Math.Max(evaluableNumericalTerm.Value, evaluableNumericalTerm2.Value)) : _evaluator.CreateMax(evaluableNumericalTerm, evaluableNumericalTerm2));
			return Register(term);
		}

		/// <summary>
		/// Creates the maximum of a set of terms
		/// </summary>
		internal int CreateMax(int[] vids)
		{
			EvaluableNumericalTerm[] array = ExtractNumericalArgs(vids);
			EvaluableNumericalTerm term = (Array.TrueForAll(array, TermIsConstant) ? CreateConstantTerm(array.Select((EvaluableNumericalTerm x) => x.Value).Max()) : _evaluator.CreateMax(array));
			return Register(term);
		}

		/// <summary>
		/// Creates a term that is true if the arguments are all different
		/// </summary>
		internal int CreateAllDifferent(int[] vids)
		{
			EvaluableNumericalTerm[] args = ExtractNumericalArgs(vids);
			return Register(_evaluator.CreateAllDifferent(args));
		}

		/// <summary>
		/// Creates a term that is true if the two arguments are different
		/// </summary>
		internal int CreateUnequal(int vid1, int vid2)
		{
			EvaluableNumericalTerm x = ExtractNumericalArg(vid1);
			EvaluableNumericalTerm y = ExtractNumericalArg(vid2);
			return Register(_evaluator.CreateDifferent(x, y));
		}

		/// <summary>
		/// Creates a term that is true if the two arguments are equal
		/// </summary>
		internal int CreateEqual(int vid1, int vid2)
		{
			EvaluableNumericalTerm x = ExtractNumericalArg(vid1);
			EvaluableNumericalTerm y = ExtractNumericalArg(vid2);
			return Register(_evaluator.CreateEqual(x, y));
		}

		/// <summary>
		/// Creates a term that is true if the all the arguments are equal
		/// </summary>
		internal int CreateEqual(int[] vids)
		{
			if (vids.Length == 0)
			{
				return CreateConstant(value: true);
			}
			EvaluableNumericalTerm[] array = ExtractNumericalArgs(vids);
			int num = vids.Length - 1;
			EvaluableBooleanTerm[] array2 = new EvaluableBooleanTerm[num];
			for (int i = 0; i < num; i++)
			{
				array2[i] = _evaluator.CreateEqual(array[i], array[i + 1]);
			}
			return Register(_evaluator.CreateAnd(array2));
		}

		/// <summary>
		/// Creates a term that is true if the arguments are in strictly increasing order
		/// </summary>
		internal int CreateLess(int vid1, int vid2)
		{
			EvaluableNumericalTerm x = ExtractNumericalArg(vid1);
			EvaluableNumericalTerm y = ExtractNumericalArg(vid2);
			return Register(_evaluator.CreateLessStrict(x, y));
		}

		/// <summary>
		/// Creates a term that is true if the arguments are in strictly increasing order
		/// </summary>
		internal int CreateLess(int[] vids)
		{
			EvaluableNumericalTerm[] args = ExtractNumericalArgs(vids);
			return Register(_evaluator.CreateLessStrict(args));
		}

		/// <summary>
		/// Creates a term that is true if the arguments are in strictly decreasing order
		/// </summary>
		internal int CreateGreater(int vid1, int vid2)
		{
			return CreateLess(vid2, vid1);
		}

		/// <summary>
		/// Creates a term that is true if the arguments are in strictly decreasing order
		/// </summary>
		internal int CreateGreater(int[] vids)
		{
			EvaluableNumericalTerm[] array = ExtractNumericalArgs(vids);
			Array.Reverse(array);
			return Register(_evaluator.CreateLessStrict(array));
		}

		/// <summary>
		/// Creates a term that is true if the arguments are in non-decreasing order
		/// </summary>
		internal int CreateLessEqual(int vid1, int vid2)
		{
			EvaluableNumericalTerm x = ExtractNumericalArg(vid1);
			EvaluableNumericalTerm y = ExtractNumericalArg(vid2);
			return Register(_evaluator.CreateLessEqual(x, y));
		}

		/// <summary>
		/// Creates a term that is true if the arguments are in non-decreasing order
		/// </summary>
		internal int CreateLessEqual(int[] vids)
		{
			EvaluableNumericalTerm[] args = ExtractNumericalArgs(vids);
			return Register(_evaluator.CreateLessEqual(args));
		}

		/// <summary>
		/// Creates a term that is true if the arguments are in non-increasing order
		/// </summary>
		internal int CreateGreaterEqual(int vid1, int vid2)
		{
			return CreateLessEqual(vid2, vid1);
		}

		/// <summary>
		/// Creates a term that is true if the arguments are in non-increasing order
		/// </summary>
		internal int CreateGreaterEqual(int[] vids)
		{
			EvaluableNumericalTerm[] array = ExtractNumericalArgs(vids);
			Array.Reverse(array);
			return Register(_evaluator.CreateLessEqual(array));
		}

		/// <summary>
		/// Creates a term that is represents the logical And of two Boolean terms
		/// </summary>
		internal int CreateAnd(int vid1, int vid2)
		{
			EvaluableBooleanTerm input = ExtractBooleanArg(vid1);
			EvaluableBooleanTerm input2 = ExtractBooleanArg(vid2);
			return Register(_evaluator.CreateAnd(input, input2));
		}

		/// <summary>
		/// Creates a term that is represents the logical And of a set of Boolean terms
		/// </summary>
		internal int CreateAnd(int[] vids)
		{
			EvaluableBooleanTerm[] args = ExtractBooleanArgs(vids);
			return Register(_evaluator.CreateAnd(args));
		}

		/// <summary>
		/// Creates a term that is represents the logical Or of two Boolean terms
		/// </summary>
		internal int CreateOr(int vid1, int vid2)
		{
			EvaluableBooleanTerm input = ExtractBooleanArg(vid1);
			EvaluableBooleanTerm input2 = ExtractBooleanArg(vid2);
			return Register(_evaluator.CreateOr(input, input2));
		}

		/// <summary>
		/// Creates a term that is represents the logical Or of a set of Boolean terms
		/// </summary>
		internal int CreateOr(int[] vids)
		{
			EvaluableBooleanTerm[] args = ExtractBooleanArgs(vids);
			return Register(_evaluator.CreateOr(args));
		}

		/// <summary>
		/// Creates a term that is represents the logical Or of two Boolean terms
		/// </summary>
		internal int CreateNot(int vid1)
		{
			EvaluableBooleanTerm arg = ExtractBooleanArg(vid1);
			return Register(_evaluator.CreateNot(arg));
		}

		/// <summary>
		/// Creates a term of the form arg1 ? arg2 : arg3
		/// </summary>
		internal int CreateIf(int vid1, int vid2, int vid3)
		{
			EvaluableBooleanTerm condition = ExtractBooleanArg(vid1);
			EvaluableNumericalTerm caseTrue = ExtractNumericalArg(vid2);
			EvaluableNumericalTerm caseFalse = ExtractNumericalArg(vid3);
			return Register(_evaluator.CreateIf(condition, caseTrue, caseFalse));
		}

		/// <summary>
		/// Creates the Sine of a numerical term
		/// </summary>
		internal int CreateSin(int x)
		{
			return CreateUnaryFunction(_sin, x, TermModelOperation.Sin);
		}

		/// <summary>
		/// Creates the Cosine of a numerical term
		/// </summary>
		internal int CreateCos(int x)
		{
			return CreateUnaryFunction(_cos, x, TermModelOperation.Cos);
		}

		/// <summary>
		/// Creates the Tangent of a numerical term
		/// </summary>
		internal int CreateTan(int x)
		{
			return CreateUnaryFunction(_tan, x, TermModelOperation.Tan);
		}

		/// <summary>
		/// Creates the hyperbolic Sine of a numerical term
		/// </summary>
		internal int CreateSinh(int x)
		{
			return CreateUnaryFunction(_sinh, x);
		}

		/// <summary>
		/// Creates the hyperbolic Cosine of a numerical term
		/// </summary>
		internal int CreateCosh(int x)
		{
			return CreateUnaryFunction(_cosh, x);
		}

		/// <summary>
		/// Creates the hyperbolic Tangent of a numerical term
		/// </summary>
		internal int CreateTanh(int x)
		{
			return CreateUnaryFunction(_tanh, x);
		}

		/// <summary>
		/// Creates the ArcSin of a numerical term
		/// </summary>
		internal int CreateArcSin(int x)
		{
			return CreateUnaryFunction(_arcSin, x, TermModelOperation.ArcSin);
		}

		/// <summary>
		/// Creates the ArcCos of a numerical term
		/// </summary>
		internal int CreateArcCos(int x)
		{
			return CreateUnaryFunction(_arcCos, x, TermModelOperation.ArcCos);
		}

		/// <summary>
		/// Creates the ArcTan of a numerical term
		/// </summary>
		internal int CreateArcTan(int x)
		{
			return CreateUnaryFunction(_arcTan, x, TermModelOperation.ArcTan);
		}

		/// <summary>
		/// Creates the Ceiling of a numerical term
		/// </summary>
		internal int CreateCeiling(int x)
		{
			return CreateUnaryFunction(_ceiling, x, TermModelOperation.Ceiling);
		}

		/// <summary>
		/// Creates the Floor of a numerical term
		/// </summary>
		internal int CreateFloor(int x)
		{
			return CreateUnaryFunction(_floor, x, TermModelOperation.Floor);
		}

		/// <summary>
		/// Creates a term representing E raised to the power x, where x is a numerical term
		/// </summary>
		internal int CreateExp(int x)
		{
			return CreateUnaryFunction(_exp, x, TermModelOperation.Exp);
		}

		/// <summary>
		/// Creates the square root of a numerical term
		/// </summary>
		internal int CreateSqrt(int x)
		{
			return CreateUnaryFunction(_sqrt, x);
		}

		/// <summary>
		/// Creates the base 10 log of a numerical term
		/// </summary>
		internal int CreateLog10(int x)
		{
			return CreateUnaryFunction(_log10, x);
		}

		/// <summary>
		/// Creates the base E log of a numerical term
		/// </summary>
		internal int CreateLog(int x)
		{
			return CreateUnaryFunction(_logE, x, TermModelOperation.Log);
		}

		/// <summary>
		/// Creates a term representing x raised to the given power
		/// </summary>
		internal int CreatePower(int x, int power)
		{
			return CreateBinaryFunction(_pow, x, power, TermModelOperation.Power);
		}

		private int CreateUnaryFunction(Func<double, double> f, int x, TermModelOperation op)
		{
			EvaluableNumericalTerm evaluableNumericalTerm = ExtractNumericalArg(x);
			EvaluableNumericalTerm term = (evaluableNumericalTerm.IsConstant ? CreateConstantTerm(f(evaluableNumericalTerm.Value)) : _evaluator.CreateUnaryFunction(f, evaluableNumericalTerm, op));
			return Register(term);
		}

		private int CreateBinaryFunction(Func<double, double, double> f, int x, int y, TermModelOperation op)
		{
			EvaluableNumericalTerm evaluableNumericalTerm = ExtractNumericalArg(x);
			EvaluableNumericalTerm evaluableNumericalTerm2 = ExtractNumericalArg(y);
			EvaluableNumericalTerm term = ((evaluableNumericalTerm.IsConstant && evaluableNumericalTerm2.IsConstant) ? CreateConstantTerm(f(evaluableNumericalTerm.Value, evaluableNumericalTerm2.Value)) : _evaluator.CreateBinaryFunction(f, evaluableNumericalTerm, evaluableNumericalTerm2, op));
			return Register(term);
		}

		/// <summary>
		/// Creates a term that is the application of an arbitrary, user-defined 
		/// function to another term
		/// </summary>
		public int CreateUnaryFunction(Func<double, double> f, int x)
		{
			return CreateUnaryFunction(f, x, (TermModelOperation)(-1));
		}

		/// <summary>
		/// Creates a term that is the application of an arbitrary, user-defined 
		/// function to two other terms
		/// </summary>
		public int CreateBinaryFunction(Func<double, double, double> f, int x, int y)
		{
			return CreateBinaryFunction(f, x, y, (TermModelOperation)(-1));
		}

		/// <summary>
		/// Creates a term that is the application of a user-defined function to two other terms.
		/// </summary>
		public int CreateNaryFunction(Func<double[], double> fun, int[] vids)
		{
			EvaluableNumericalTerm[] array = ExtractNumericalArgs(vids);
			EvaluableNumericalTerm term = (Array.TrueForAll(array, TermIsConstant) ? CreateConstantTerm(fun(array.Select((EvaluableNumericalTerm x) => x.Value).ToArray())) : _evaluator.CreateNaryFunction(fun, array));
			return Register(term);
		}

		/// <summary>
		/// Specifies that a numerical term is a constraint
		/// </summary>
		public void AddConstraint(int vid)
		{
			SetBounds(vid, (double?)1.0, (double?)1.0);
		}

		/// <summary>
		/// Removes a Boolean term from the set of constraints
		/// </summary>
		/// <returns>
		/// True if the constraint is successfully removed
		/// </returns>
		public bool RemoveConstraint(int vid)
		{
			PreChange();
			return _evaluator.RemoveConstraint(ExtractBooleanArg(vid));
		}

		private static long Ceiling(double x)
		{
			double num = Math.Ceiling(x);
			if (num <= -9.223372036854776E+18)
			{
				return long.MinValue;
			}
			if (num >= 9.223372036854776E+18)
			{
				throw new OverflowException(Resources.NumberNotIntegerOrOutOfRange);
			}
			return (long)num;
		}

		private static long Floor(double x)
		{
			double num = Math.Floor(x);
			if (num >= 9.223372036854776E+18)
			{
				return long.MaxValue;
			}
			if (num <= -9.223372036854776E+18)
			{
				throw new OverflowException(Resources.NumberNotIntegerOrOutOfRange);
			}
			return (long)num;
		}

		private static long GetInteger(Rational x)
		{
			if (x < long.MinValue || x > long.MaxValue)
			{
				throw new OverflowException(Resources.NumberNotIntegerOrOutOfRange);
			}
			return (long)x;
		}

		private static double GetReal(Rational x)
		{
			return (double)x;
		}

		private void CheckIdInRange(int id)
		{
			if (0 > id || id >= _allTerms.Count)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { id }));
			}
		}

		/// <summary>
		/// Gets (and validates) the argument with the given ID, seen as a Boolean
		/// </summary>
		private EvaluableBooleanTerm ExtractBooleanArg(int id)
		{
			CheckIdInRange(id);
			EvaluableTerm evaluableTerm = _allTerms[id];
			if (evaluableTerm is EvaluableBooleanTerm result)
			{
				return result;
			}
			EvaluableNumericalTerm x = evaluableTerm as EvaluableNumericalTerm;
			return GetConversionToBool(x);
		}

		/// <summary>
		/// Gets (and validates) the argument with the given ID, seen as Numerical
		/// </summary>
		private EvaluableNumericalTerm ExtractNumericalArg(int id)
		{
			CheckIdInRange(id);
			EvaluableTerm evaluableTerm = _allTerms[id];
			if (evaluableTerm is EvaluableNumericalTerm result)
			{
				return result;
			}
			EvaluableBooleanTerm x = evaluableTerm as EvaluableBooleanTerm;
			return GetConversionToNum(x);
		}

		/// <summary>
		/// Get the term that represents the Boolean value of a numerical term.
		/// Conversions terms are cached in a Dictionary
		/// </summary>
		private EvaluableBooleanTerm GetConversionToBool(EvaluableNumericalTerm x)
		{
			if (!_conversionsToBool.TryGetValue(x, out var value))
			{
				value = _evaluator.CreateConversionToBoolean(x);
				_conversionsToBool.Add(x, value);
				_conversionsToNum.Add(value, x);
			}
			return value;
		}

		/// <summary>
		/// Get the term that represents the numerical value of a Boolean term
		/// Conversions terms are cached in a Dictionary
		/// </summary>
		private EvaluableNumericalTerm GetConversionToNum(EvaluableBooleanTerm x)
		{
			if (!_conversionsToNum.TryGetValue(x, out var value))
			{
				value = _evaluator.CreateConversionToNumerical(x);
				_conversionsToNum.Add(x, value);
				_conversionsToBool.Add(value, x);
			}
			return value;
		}

		private EvaluableBooleanTerm[] ExtractBooleanArgs(int[] ids)
		{
			return Array.ConvertAll(ids, (int id) => ExtractBooleanArg(id));
		}

		private EvaluableNumericalTerm[] ExtractNumericalArgs(int[] ids)
		{
			return Array.ConvertAll(ids, (int id) => ExtractNumericalArg(id));
		}

		internal EvaluableTerm GetTerm(int id)
		{
			CheckIdInRange(id);
			return _allTerms[id];
		}

		private EvaluableVariable GetVar(int id)
		{
			EvaluableTerm term = GetTerm(id);
			if (!(term is EvaluableVariable result))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { id }));
			}
			return result;
		}

		private bool TryGetVar(int id, out EvaluableVariable variable)
		{
			EvaluableTerm term = GetTerm(id);
			variable = term as EvaluableVariable;
			return variable != null;
		}

		/// <summary>
		/// (Re)initializes all fields related to the search algorithm
		/// </summary>
		private void InitializeSearchData(HybridLocalSearchParameters parameters)
		{
			PreChange();
			PrepareGoals();
			Step = 0L;
			_evaluator.PresolveLevel = parameters.PresolveLevel;
			_evaluator.EqualityTolerance = parameters.EqualityTolerance;
			_searchTerminationRequested = false;
			_searchParameters = parameters;
			_searchTimer = new Stopwatch();
			_searchTimer.Start();
			_bestSolution = null;
			_bestQuality = null;
			_currentQuality = null;
			InitialState();
			RecomputeCurrentState();
			SaveBestSolution();
		}

		/// <summary>
		/// Checks if the current solution is better than the best solution.
		/// If so updates the best solution
		/// </summary>
		private void CheckIfBestSolution()
		{
			if (_accuracy >= MaxAccuracy)
			{
				double value = Compare(_currentQuality, _bestQuality).Value;
				if (value < 0.0)
				{
					SaveBestSolution();
				}
			}
		}

		/// <summary>
		/// Save the current state as best solution
		/// </summary>
		private void SaveBestSolution()
		{
			_evaluator.RecomputeEliminatedVariables();
			_evaluator.UpdateAggregatedQuality(ref _bestQuality, 0.0);
			if (_bestSolution == null || _bestSolution.Length != _allVariables.Count)
			{
				_bestSolution = new double[_allVariables.Count];
			}
			SnapshotState(_bestSolution);
			if (Violation == 0.0)
			{
				_searchParameters.CallSolvingEvent();
			}
		}

		/// <summary>
		/// Save the current of all variables into an array
		/// </summary>
		private void SnapshotState(double[] snapshot)
		{
			for (int i = 0; i < snapshot.Length; i++)
			{
				snapshot[i] = _allVariables[i].Value;
			}
		}

		/// <summary>
		/// Reinitialized all variables to the snapshot of a previous state
		/// </summary>
		private void RestoreState(double[] priorSavedState)
		{
			for (int i = 0; i < priorSavedState.Length; i++)
			{
				_evaluator.ChangeValue(_allVariables[i], priorSavedState[i]);
			}
			_evaluator.Recompute();
			_evaluator.UpdateAggregatedQuality(ref _currentQuality, Threshold[_accuracy]);
		}

		/// <summary>
		/// Bring the current state back to the best found solution
		/// </summary>
		private void RestoreBestSolution()
		{
			RestoreState(_bestSolution);
		}

		/// <summary>
		/// Request the end of the search algorithm
		/// </summary>
		public void RequestTermination()
		{
			_searchTerminationRequested = true;
		}

		/// <summary>
		/// Recomputes the evaluation and updates the current quality.
		/// Returns the Comparison with the previous quality
		/// </summary>
		private void RecomputeCurrentState()
		{
			Step++;
			_evaluator.Recompute();
			_evaluator.UpdateAggregatedQuality(ref _currentQuality, Threshold[_accuracy]);
		}

		/// <summary>
		/// Increase the radius of the search algorithm
		/// (i.e. DEcrease the _focus variable)
		/// </summary>
		private void DecreaseFocus()
		{
			_focus = Math.Max(MinFocus, _focus - 1);
		}

		/// <summary>
		/// Decrease the radius of the search algorithm
		/// (i.e. INcrease the _radius variable)
		/// </summary>
		private void IncreaseFocus()
		{
			_focus++;
		}

		private void IncreaseAccuracy()
		{
			_accuracy = Math.Min(MaxAccuracy, _accuracy + 1);
		}

		/// <summary>
		/// Checks whether the search should go on 
		/// </summary>
		private bool Continue()
		{
			if (_searchTerminationRequested)
			{
				return false;
			}
			if (BestViolation == 0.0 && GoalsCount == 0)
			{
				return false;
			}
			long num = _searchParameters.TimeLimitMs;
			if (num < 0)
			{
				num = long.MaxValue;
			}
			if (_searchParameters.CallQueryAbort() || _searchTimer.ElapsedMilliseconds >= num)
			{
				_searchTerminationRequested = true;
			}
			return !_searchTerminationRequested;
		}

		/// <summary>
		/// Compares the two arrays of the same length lexicographically,
		/// returns the first index where a difference occurs, and the difference:
		/// Negative difference means that the first argument is better (lower);
		/// Positive difference means that the second argument is better;
		/// 0 if we cannot tell, in which case the index is negative.
		/// </summary>
		private static KeyValuePair<int, double> Compare(double[] quality1, double[] quality2)
		{
			for (int i = 0; i < quality1.Length; i++)
			{
				double num = SafeMinus(quality1[i], quality2[i]);
				if (num != 0.0)
				{
					return new KeyValuePair<int, double>(i, num);
				}
			}
			return new KeyValuePair<int, double>(-1, 0.0);
		}

		/// <summary>
		/// Compares two violations or objective functions to minimize:
		/// Negative means that the first argument is better (lower)
		/// Positive means that the second argument is better
		/// 0 if we cannot tell.
		/// Deals with infinities and Nans. NaNs are treated worse than anything else.
		/// </summary>
		internal static double SafeMinus(double l, double r)
		{
			switch ((double.IsNaN(l) ? 2 : 0) | (double.IsNaN(r) ? 1 : 0))
			{
			case 0:
			{
				double num = l - r;
				if (!double.IsNaN(num))
				{
					return num;
				}
				return 0.0;
			}
			case 1:
				return double.NegativeInfinity;
			case 2:
				return double.PositiveInfinity;
			default:
				return 0.0;
			}
		}

		/// <summary>
		/// Main search algorithm.
		/// </summary>
		/// <remarks>
		/// This algorithm is not aimed at being tuned by the user;
		/// instead the goal is to have as good a behaviour as possible
		/// by default, and to reach good local minima. 
		/// </remarks>
		/// <returns>The NonlinearResult of the search</returns>
		internal NonlinearResult RadiusSearch(HybridLocalSearchParameters parameters)
		{
			InitializeSearchData(parameters);
			if (_invalidModelAtConstruction)
			{
				return NonlinearResult.Invalid;
			}
			bool localOptimaFound = false;
			while (Continue())
			{
				ReachLocalMinimum();
				if (BestViolation == 0.0 && !_searchTerminationRequested)
				{
					localOptimaFound = true;
				}
				if (!_searchParameters.RunUntilTimeout && !(BestViolation > 0.0))
				{
					break;
				}
				Escape();
			}
			RestoreBestSolution();
			return GetSearchResult(localOptimaFound);
		}

		/// <summary>
		/// We return the best solution we know. Optimal if we can prove optimality, Interrupted just if don't have feasible solution
		/// </summary>
		/// <param name="localOptimaFound"></param>
		/// <returns></returns>
		private NonlinearResult GetSearchResult(bool localOptimaFound)
		{
			if (localOptimaFound)
			{
				if (GoalsCount == 0)
				{
					return NonlinearResult.Feasible;
				}
				return NonlinearResult.LocalOptimal;
			}
			if (Violation == 0.0)
			{
				return NonlinearResult.Feasible;
			}
			return NonlinearResult.Interrupted;
		}

		/// <summary>
		/// Perform a series of large- and small-neighborhood steps
		/// with increasing focus; until reaching a point from which
		/// no improvement can be found within a small radius. 
		/// </summary>
		internal void ReachLocalMinimum()
		{
			int num = 0;
			_accuracy = MinAccuracy;
			_focus = MinFocus;
			while (_focus <= MaxFocus)
			{
				SmallNeighborhoodSearch();
				if (LargeNeighborhoodSearch())
				{
					num++;
					if (num % 5 == 0)
					{
						DecreaseFocus();
					}
				}
				else if (_focus == MaxFocus && _accuracy < MaxAccuracy)
				{
					IncreaseAccuracy();
					_focus = Math.Max(_accuracy + (MaxFocus - MaxAccuracy), MinFocus);
				}
				else
				{
					IncreaseFocus();
					if (_evaluator.CurrentViolation == 0.0)
					{
						IncreaseAccuracy();
					}
				}
			}
		}

		/// <summary>
		/// A local improvement algorithm that makes repeated 
		/// small (one variable) moves. 
		/// </summary>
		private void SmallNeighborhoodSearch()
		{
			RecomputeCurrentState();
			double[] result = null;
			_evaluator.UpdateAggregatedQuality(ref result, Threshold[_accuracy]);
			int num = 0;
			while (Continue() && num < SmallSearchIntensity)
			{
				Array.Copy(_currentQuality, result, _currentQuality.Length);
				EvaluableVariable evaluableVariable = DecideVariable();
				double value = evaluableVariable.Value;
				double newValue = evaluableVariable.Domain.PickNeighbour(_prng, value, Distance[_focus]);
				_evaluator.ChangeValue(evaluableVariable, newValue);
				RecomputeCurrentState();
				double value2 = Compare(_currentQuality, result).Value;
				if (value2 < 0.0)
				{
					num = 0;
					CheckIfBestSolution();
					continue;
				}
				num++;
				if (value2 > 0.0)
				{
					_evaluator.ChangeValue(evaluableVariable, value);
					Array.Copy(result, _currentQuality, _currentQuality.Length);
				}
			}
		}

		/// <summary>
		/// A local improvement algorithm that makes a small number of
		/// large moves (in the sense: all variables are changed, their new value
		/// is taken within the radius)
		/// </summary>
		/// <returns>
		/// True if an improvement is found
		/// </returns>
		private bool LargeNeighborhoodSearch()
		{
			double[] result = null;
			_evaluator.Recompute();
			_evaluator.UpdateAggregatedQuality(ref result, Threshold[_accuracy]);
			Array.Copy(_currentQuality, result, _currentQuality.Length);
			double[] array = new double[_allVariables.Count];
			SnapshotState(array);
			for (int i = 0; i < LargeSearchIntensity; i++)
			{
				if (!Continue())
				{
					break;
				}
				for (int j = 0; j < _allVariables.Count; j++)
				{
					EvaluableVariable evaluableVariable = _allVariables[j];
					double currentValue = array[j];
					double newValue = evaluableVariable.Domain.PickNeighbour(_prng, currentValue, Distance[_focus]);
					_evaluator.ChangeValue(evaluableVariable, newValue);
				}
				RecomputeCurrentState();
				double value = Compare(_currentQuality, result).Value;
				if (value <= -1E-08)
				{
					CheckIfBestSolution();
					return true;
				}
			}
			RestoreState(array);
			return false;
		}

		/// <summary>
		/// Generates the starting point of the search
		/// </summary>
		private void InitialState()
		{
		}

		/// <summary>
		/// Called when the search is stuck in a local minimum:
		/// could either restart fully or just perturbate partially the 
		/// current assignment
		/// </summary>
		private void Escape()
		{
			int count = _allVariables.Count;
			if (count > 0)
			{
				for (int num = (int)Math.Max(1.0, 0.01 * (double)count); num >= 0; num--)
				{
					EvaluableVariable evaluableVariable = _allVariables[_prng.Next(count)];
					_evaluator.ChangeValue(evaluableVariable, evaluableVariable.Domain.Sample(_prng));
				}
			}
			RecomputeCurrentState();
			CheckIfBestSolution();
		}

		/// <summary>
		/// Default method for selecting a variable: 
		/// if one constraint at least is violated we pich a conflict variable;
		/// otherwise we select a variable at random
		/// </summary>
		private EvaluableVariable DecideVariable()
		{
			if (!(Violation > 0.0))
			{
				return PeekVariable();
			}
			return PeekConflictingVariable();
		}

		/// <summary>
		/// Select randomly a variable that contributes to at least 
		/// one violated constraint
		/// </summary>        
		private EvaluableVariable PeekConflictingVariable()
		{
			if (_enumerator == null || !_enumerator.MoveNext())
			{
				_enumerator = _evaluator.ComputeCauses(_prng, diverse: true).GetEnumerator();
				_enumerator.MoveNext();
			}
			return _enumerator.Current;
		}

		/// <summary>
		/// Select a variable uniformly at random
		/// </summary>
		internal EvaluableVariable PeekVariable()
		{
			int index = _prng.Next(_allVariables.Count);
			return _allVariables[index];
		}

		/// <summary>
		/// True if key NOT NULL and already present
		/// </summary>
		private bool IsKeyAlreadyPresent(object key)
		{
			if (key != null)
			{
				return _indexFromKey.ContainsKey(key);
			}
			return false;
		}

		/// <summary>
		/// Associate the key to the index; 
		/// mapping is bi-directional
		/// </summary>
		private void Associate(int index, object key)
		{
			if (key != null)
			{
				_indexFromKey.Add(key, index);
				while (_keyFromIndex.Count <= index)
				{
					_keyFromIndex.Add(null);
				}
				_keyFromIndex[index] = key;
			}
		}

		/// <summary>
		/// Adds an operation row to the model.
		/// </summary>
		public bool AddOperation(TermModelOperation op, out int vidOp, int vid1)
		{
			int num = _allTerms.Count - 1;
			switch (op)
			{
			case TermModelOperation.Identity:
				vidOp = CreateIdentity(vid1);
				break;
			case TermModelOperation.Minus:
				vidOp = CreateMinus(vid1);
				break;
			case TermModelOperation.Abs:
				vidOp = CreateAbs(vid1);
				break;
			case TermModelOperation.Not:
				vidOp = CreateNot(vid1);
				break;
			case TermModelOperation.Sin:
				vidOp = CreateSin(vid1);
				break;
			case TermModelOperation.Cos:
				vidOp = CreateCos(vid1);
				break;
			case TermModelOperation.Tan:
				vidOp = CreateTan(vid1);
				break;
			case TermModelOperation.ArcSin:
				vidOp = CreateArcSin(vid1);
				break;
			case TermModelOperation.ArcCos:
				vidOp = CreateArcCos(vid1);
				break;
			case TermModelOperation.ArcTan:
				vidOp = CreateArcTan(vid1);
				break;
			case TermModelOperation.Sinh:
				vidOp = CreateSinh(vid1);
				break;
			case TermModelOperation.Cosh:
				vidOp = CreateCosh(vid1);
				break;
			case TermModelOperation.Tanh:
				vidOp = CreateTanh(vid1);
				break;
			case TermModelOperation.Exp:
				vidOp = CreateExp(vid1);
				break;
			case TermModelOperation.Log:
				vidOp = CreateLog(vid1);
				break;
			case TermModelOperation.Log10:
				vidOp = CreateLog10(vid1);
				break;
			case TermModelOperation.Sqrt:
				vidOp = CreateSqrt(vid1);
				break;
			case TermModelOperation.Ceiling:
				vidOp = CreateCeiling(vid1);
				break;
			case TermModelOperation.Floor:
				vidOp = CreateFloor(vid1);
				break;
			default:
				return AddOperation(op, out vidOp, new int[1] { vid1 });
			}
			return vidOp > num;
		}

		/// <summary>
		/// Adds an operation row to the model.
		/// </summary>
		public bool AddOperation(TermModelOperation op, out int vidOp, int vid1, int vid2)
		{
			int num = _allTerms.Count - 1;
			switch (op)
			{
			case TermModelOperation.Plus:
				vidOp = CreatePlus(vid1, vid2);
				break;
			case TermModelOperation.Minus:
				vidOp = CreateMinus(vid1, vid2);
				break;
			case TermModelOperation.Times:
				vidOp = CreateTimes(vid1, vid2);
				break;
			case TermModelOperation.Quotient:
				vidOp = CreateQuotient(vid1, vid2);
				break;
			case TermModelOperation.Power:
				vidOp = CreatePower(vid1, vid2);
				break;
			case TermModelOperation.Max:
				vidOp = CreateMax(vid1, vid2);
				break;
			case TermModelOperation.Min:
				vidOp = CreateMin(vid1, vid2);
				break;
			case TermModelOperation.And:
				vidOp = CreateAnd(vid1, vid2);
				break;
			case TermModelOperation.Or:
				vidOp = CreateOr(vid1, vid2);
				break;
			case TermModelOperation.Equal:
				vidOp = CreateEqual(vid1, vid2);
				break;
			case TermModelOperation.Unequal:
				vidOp = CreateUnequal(vid1, vid2);
				break;
			case TermModelOperation.Greater:
				vidOp = CreateLess(vid2, vid1);
				break;
			case TermModelOperation.Less:
				vidOp = CreateLess(vid1, vid2);
				break;
			case TermModelOperation.GreaterEqual:
				vidOp = CreateLessEqual(vid2, vid1);
				break;
			case TermModelOperation.LessEqual:
				vidOp = CreateLessEqual(vid1, vid2);
				break;
			default:
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidArgumentCountForOperator0, new object[1] { op }));
			}
			return vidOp > num;
		}

		/// <summary>
		/// Adds an operation row to the model.
		/// </summary>
		public bool AddOperation(TermModelOperation op, out int vidOp, int vid1, int vid2, int vid3)
		{
			int num = _allTerms.Count - 1;
			if (op == TermModelOperation.If)
			{
				vidOp = CreateIf(vid1, vid2, vid3);
				return vidOp > num;
			}
			return AddOperation(op, out vidOp, new int[3] { vid1, vid2, vid3 });
		}

		/// <summary>
		/// Add a user-defined function.
		/// </summary>
		/// <param name="fun">A user defined function.</param>
		/// <param name="vidOp">The vid for the function.</param>
		/// <param name="vids">The vids that represent the arguments.</param>
		/// <returns>True if a new vid was created for the function.</returns>
		public bool AddFunction(Func<double[], double> fun, out int vidOp, int[] vids)
		{
			int num = _allTerms.Count - 1;
			vidOp = CreateNaryFunction(fun, vids);
			return vidOp > num;
		}

		/// <summary>
		/// Adds an operation row to the model.
		/// </summary>
		/// <remarks>
		/// This overload is supported for the following TermModelOperation values:
		/// And, Equal, Greater, GreaterEqual, Less, LessEqual, Max, Min, Or, Plus, Times, Unequal.
		/// </remarks>
		public bool AddOperation(TermModelOperation op, out int vidOp, params int[] vids)
		{
			int num = _allTerms.Count - 1;
			switch (op)
			{
			case TermModelOperation.Plus:
				vidOp = CreatePlus(vids);
				break;
			case TermModelOperation.Times:
				vidOp = CreateTimes(vids);
				break;
			case TermModelOperation.Max:
				vidOp = CreateMax(vids);
				break;
			case TermModelOperation.Min:
				vidOp = CreateMin(vids);
				break;
			case TermModelOperation.And:
				vidOp = CreateAnd(vids);
				break;
			case TermModelOperation.Or:
				vidOp = CreateOr(vids);
				break;
			case TermModelOperation.Equal:
				vidOp = CreateEqual(vids);
				break;
			case TermModelOperation.Unequal:
				vidOp = CreateAllDifferent(vids);
				break;
			case TermModelOperation.Greater:
				vidOp = CreateGreater(vids);
				break;
			case TermModelOperation.Less:
				vidOp = CreateLess(vids);
				break;
			case TermModelOperation.GreaterEqual:
				vidOp = CreateGreaterEqual(vids);
				break;
			case TermModelOperation.LessEqual:
				vidOp = CreateLessEqual(vids);
				break;
			case TermModelOperation.If:
				if (vids.Length == 3)
				{
					vidOp = CreateIf(vids[0], vids[1], vids[2]);
					break;
				}
				goto default;
			default:
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidArgumentCountForOperator0, new object[1] { op }));
			}
			return vidOp > num;
		}

		/// <summary>
		/// Adds a variable to the model, with bounds and integrality given at creation time.
		/// </summary>
		public bool AddVariable(object key, out int vid, Rational lower, Rational upper, bool isInteger)
		{
			if (IsKeyAlreadyPresent(key))
			{
				vid = -1;
				return false;
			}
			if (isInteger)
			{
				long lowerBound = Ceiling(lower.ToDouble());
				long upperBound = Floor(upper.ToDouble());
				vid = CreateIntegerVariable(lowerBound, upperBound);
			}
			else
			{
				vid = CreateRealVariable(GetReal(lower), GetReal(upper));
			}
			Associate(vid, key);
			return true;
		}

		/// <summary>
		/// Adds a variable to the model, with a fixed set of possible values.
		/// </summary>
		public bool AddVariable(object key, out int vid, IEnumerable<Rational> allowedValues)
		{
			if (IsKeyAlreadyPresent(key))
			{
				vid = -1;
				return false;
			}
			bool flag = true;
			foreach (Rational allowedValue in allowedValues)
			{
				if (!allowedValue.IsInteger())
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				vid = CreateIntegerVariable(allowedValues.Select(GetInteger));
			}
			else
			{
				vid = CreateRealVariable(allowedValues.Select(GetReal));
			}
			Associate(vid, key);
			return true;
		}

		/// <summary>
		/// Adds a variable to the model, with a fixed set of possible values.
		/// </summary>
		public bool AddVariable(out int vid, IEnumerable<Rational> possibleValues)
		{
			return AddVariable(null, out vid, possibleValues);
		}

		/// <summary>
		/// Adds a variable to the model, with bounds and integrality given at creation time.
		/// </summary>
		public bool AddVariable(out int vid, Rational lower, Rational upper, bool isInteger)
		{
			return AddVariable(null, out vid, lower, upper, isInteger);
		}

		/// <summary>
		/// Adds a constant to the model. Constants are considered rows.
		/// </summary>
		public bool AddConstant(Rational value, out int vid)
		{
			int num = _allTerms.Count - 1;
			vid = CreateConstant(GetReal(value));
			return vid > num;
		}

		/// <summary>
		/// Tests if a vid is an operation (not a variable or constant).
		/// </summary>
		public bool IsOperation(int vid)
		{
			return GetTerm(vid).Operation != (TermModelOperation)(-1);
		}

		/// <summary>
		/// Tests if a vid is a constant (not a variable or operation).
		/// </summary>
		public bool IsConstant(int vid)
		{
			return GetTerm(vid).IsConstant;
		}

		/// <summary>
		/// Gets the operation associated with a vid.
		/// </summary>
		public TermModelOperation GetOperation(int vidOp)
		{
			TermModelOperation operation = GetTerm(vidOp).Operation;
			if (operation == (TermModelOperation)(-1))
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownOperation0, new object[1] { vidOp }));
			}
			return operation;
		}

		/// <summary>
		/// Gets the number of operands associated with a vid.
		/// </summary>
		public int GetOperandCount(int vidOp)
		{
			return GetTerm(vidOp).EnumerateInputs().Count();
		}

		/// <summary>
		/// Gets the operands associated with a vid.
		/// </summary>
		public IEnumerable<int> GetOperands(int vidOp)
		{
			return from x in GetTerm(vidOp).EnumerateInputs()
				select GetIndex(x);
		}

		/// <summary>
		/// Gets an operand associated with a vid.
		/// </summary>
		public int GetOperand(int vidOp, int index)
		{
			EvaluableTerm[] array = GetTerm(vidOp).EnumerateInputs().ToArray();
			EvaluableTerm term = array[index];
			return GetIndex(term);
		}

		private int GetIndex(EvaluableTerm term)
		{
			for (int i = 0; i < _allTerms.Count; i++)
			{
				if (object.ReferenceEquals(_allTerms[i], term))
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Maps the variable index from the key. If not found, KeyNotFoundException will be thrown 
		/// </summary>
		/// <param name="key"></param>
		/// <returns>variable index </returns>
		public int GetIndexFromKey(object key)
		{
			return _indexFromKey[key];
		}

		/// <summary>
		/// Try to get the variable index based on the key
		/// </summary>
		/// <param name="key">the key value </param>
		/// <param name="vid">the variable index </param>
		/// <returns>true if the variable exists, otherwise false</returns>
		public bool TryGetIndexFromKey(object key, out int vid)
		{
			return _indexFromKey.TryGetValue(key, out vid);
		}

		/// <summary>
		/// Map from the variable index to the key. If not found, ArgumentException will be thrown
		/// </summary>
		/// <param name="vid">the variable index</param>
		/// <returns>the variable key</returns>
		/// <remarks>key might be null</remarks>
		public object GetKeyFromIndex(int vid)
		{
			CheckIdInRange(vid);
			if (vid < _keyFromIndex.Count)
			{
				return _keyFromIndex[vid];
			}
			return null;
		}

		bool IRowVariableModel.AddRow(object key, out int vid)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Validate if it is a row index and not a variable index.
		/// </summary>
		/// <param name="vid">row index</param>
		/// <returns>True if a row otherwise false.</returns>
		public bool IsRow(int vid)
		{
			return IsOperation(vid);
		}

		void IRowVariableModel.SetIgnoreBounds(int vid, bool fIgnore)
		{
			throw new NotSupportedException();
		}

		bool IRowVariableModel.GetIgnoreBounds(int vid)
		{
			return false;
		}

		/// <summary>Set or adjust the lower bound of the vid. 
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <param name="numLo">The lower bound.</param>
		public void SetLowerBound(int vid, Rational numLo)
		{
			ValidateBounds(numLo, Rational.PositiveInfinity);
			SetBounds(vid, GetReal(numLo), null);
		}

		/// <summary>Set or adjust the upper bound of the vid. 
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <param name="numHi">The upper bound.</param>
		public void SetUpperBound(int vid, Rational numHi)
		{
			ValidateBounds(Rational.NegativeInfinity, numHi);
			SetBounds(vid, null, GetReal(numHi));
		}

		/// <summary>Set the bounds for a vid.</summary>
		/// <remarks>
		/// Logically, a vid may have an upper bound of Infinity and/or a lower bound of -Infinity. 
		/// Specifying any other non-finite values for bounds should be avoided. 
		/// If a vid has a lower bound that is greater than its upper bound, the model is automatically infeasible, 
		/// and ArgumentException is thrown.  
		/// </remarks>
		/// <param name="vid">A vid.</param>
		/// <param name="numLo">The lower bound.</param>
		/// <param name="numHi">The upper bound.</param>
		public void SetBounds(int vid, Rational numLo, Rational numHi)
		{
			ValidateBounds(numLo, numHi);
			SetBounds(vid, (double?)GetReal(numLo), (double?)GetReal(numHi));
		}

		private void SetBounds(int vid, double? numLo, double? numHi)
		{
			PreChange();
			EvaluableTerm term = GetTerm(vid);
			if (term is EvaluableBooleanTerm term2)
			{
				SetBooleanTermBounds(term2, numLo, numHi);
				return;
			}
			if (term is EvaluableVariable x)
			{
				ChangeDomain(x, numLo, numHi);
				return;
			}
			if (term is EvaluableConstant)
			{
				throw new ArgumentException(Resources.ConstantValuesMayNotBeRedefined);
			}
			if (term is EvaluableNumericalTerm term3)
			{
				SetNumericalBounds(term3, numLo, numHi);
			}
		}

		private void SetBooleanTermBounds(EvaluableBooleanTerm term, double? lower, double? upper)
		{
			if (_boundConstraints.TryGetValue(term, out var value))
			{
				if (object.ReferenceEquals(term, value))
				{
					if (!lower.HasValue)
					{
						lower = 1.0;
					}
				}
				else if (!upper.HasValue)
				{
					upper = 0.0;
				}
				_evaluator.RemoveConstraint(value);
			}
			double num = lower ?? double.MinValue;
			double num2 = upper ?? double.MaxValue;
			if (0.0 < num && num <= 1.0 && 1.0 <= num2)
			{
				_evaluator.AddConstraint(term);
				_boundConstraints[term] = term;
				return;
			}
			if (num <= 0.0 && 0.0 <= num2 && num2 < 1.0)
			{
				EvaluableBooleanTerm evaluableBooleanTerm = _evaluator.CreateNot(term);
				_evaluator.AddConstraint(evaluableBooleanTerm);
				_boundConstraints[term] = evaluableBooleanTerm;
				return;
			}
			if (num <= 0.0 && num2 >= 1.0)
			{
				if (value != null)
				{
					_boundConstraints.Remove(term);
				}
				return;
			}
			throw new ArgumentException(Resources.InvalidBounds);
		}

		private void SetNumericalBounds(EvaluableNumericalTerm term, double? numLo, double? numHi)
		{
			EvaluableRangeConstraint evaluableRangeConstraint;
			if (_boundConstraints.TryGetValue(term, out var value))
			{
				evaluableRangeConstraint = value as EvaluableRangeConstraint;
				if (numLo.HasValue)
				{
					evaluableRangeConstraint.Lower = numLo.Value;
				}
				if (numHi.HasValue)
				{
					evaluableRangeConstraint.Upper = numHi.Value;
				}
			}
			else
			{
				evaluableRangeConstraint = _evaluator.CreateLessEqual(numLo ?? double.MinValue, term, numHi ?? double.MaxValue);
				_evaluator.AddConstraint(evaluableRangeConstraint);
				_boundConstraints[term] = evaluableRangeConstraint;
			}
			if (evaluableRangeConstraint.Lower > evaluableRangeConstraint.Upper)
			{
				throw new ArgumentException(Resources.InvalidBounds);
			}
		}

		private void ChangeDomain(EvaluableVariable x, double? lower, double? upper)
		{
			LocalSearchDomain domain = x.Domain;
			double num = lower ?? domain.Lower;
			double num2 = upper ?? domain.Upper;
			if (double.IsNaN(num) || double.IsNaN(num2))
			{
				_invalidModelAtConstruction = true;
				return;
			}
			if (domain.IsDiscrete)
			{
				x.ResetDomain(new LocalSearchIntegerInterval(Ceiling(num), Floor(num2)), _prng);
			}
			else
			{
				x.ResetDomain(new LocalSearchContinuousInterval(num, num2), _prng);
			}
			if (!(num > num2))
			{
				return;
			}
			throw new ArgumentException(Resources.InvalidBounds);
		}

		/// <summary> Return the bounds for the vid.
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <param name="numLo">The lower bound returned.</param>
		/// <param name="numHi">The upper bound returned.</param>
		public void GetBounds(int vid, out Rational numLo, out Rational numHi)
		{
			EvaluableTerm term = GetTerm(vid);
			numLo = Rational.NegativeInfinity;
			numHi = Rational.PositiveInfinity;
			EvaluableBooleanTerm value;
			if (term is EvaluableVariable evaluableVariable)
			{
				numLo = evaluableVariable.Domain.Lower;
				numHi = evaluableVariable.Domain.Upper;
			}
			else if (term is EvaluableConstant evaluableConstant)
			{
				numLo = evaluableConstant.Value;
				numHi = evaluableConstant.Value;
			}
			else if (term is EvaluableBooleanTerm)
			{
				if (_boundConstraints.TryGetValue(term, out value))
				{
					if (object.ReferenceEquals(term, value))
					{
						numLo = (numHi = Rational.One);
					}
					else
					{
						numLo = (numHi = Rational.Zero);
					}
				}
			}
			else if (term is EvaluableNumericalTerm && _boundConstraints.TryGetValue(term, out value))
			{
				EvaluableRangeConstraint evaluableRangeConstraint = value as EvaluableRangeConstraint;
				numLo = evaluableRangeConstraint.Lower;
				numHi = evaluableRangeConstraint.Upper;
			}
		}

		/// <summary>
		/// The AddVariable method ensures that a user variable with the given key is in the model.
		/// If the model already includes a user variable referenced by key, this sets vid to the variable’s index 
		/// and returns false. Otherwise, if the model already includes a row referenced by key, this sets vid to -1 and returns false. 
		/// Otherwise, this adds a new user variable associated with key to the model, assigns the next available index to the new variable, 
		/// sets vid to this index, and returns true.
		/// </summary>
		/// <param name="key"> Variable key </param>
		/// <param name="vid">variable index </param>
		/// <returns>true if added successfully, otherwise false</returns>
		public bool AddVariable(object key, out int vid)
		{
			if (_indexFromKey.ContainsKey(key))
			{
				vid = -1;
				return false;
			}
			vid = CreateNumericalVariable(LocalSearchDomain.DefaultDomain);
			return true;
		}

		private IEnumerable<KeyValuePair<object, int>> VariablePairs()
		{
			try
			{
				_modelReadCount++;
				foreach (KeyValuePair<object, int> kvp in _indexFromKey)
				{
					List<EvaluableTerm> allTerms = _allTerms;
					KeyValuePair<object, int> keyValuePair = kvp;
					EvaluableTerm term = allTerms[keyValuePair.Value];
					if (term is EvaluableVariable)
					{
						yield return kvp;
					}
				}
			}
			finally
			{
				_modelReadCount--;
			}
		}

		/// <summary>
		/// Get the value associated with the variable index. This is typically used to fetch solver result 
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>the variable value</returns>
		Rational IRowVariableModel.GetValue(int vid)
		{
			if (((IGoalModel)this).IsGoal(vid))
			{
				for (int i = 0; i < goalList.Count; i++)
				{
					Goal goal = goalList[i];
					if (goal.Index == vid)
					{
						return goal.Minimize ? _bestQuality[i + 1] : (0.0 - _bestQuality[i + 1]);
					}
				}
			}
			return GetTerm(vid).ValueAsDouble;
		}

		/// <summary>Sets the default value for a vid.
		/// </summary>
		/// <remarks>
		/// The default value for a vid is Indeterminate. An IRowVariableModel can be used to represent not just a model, 
		/// but also a current state for the model’s (user and row) variables. 
		/// The state associates with each vid a current value represented as a Rational. 
		/// This state may be used as a starting point when solving, and may be updated by a solve attempt. 
		/// Some solvers may ignore this initial state for rows and even for variables.
		/// </remarks>
		/// <param name="vid">A vid.</param>
		/// <param name="num">The default value for the variable.</param>    
		public void SetValue(int vid, Rational num)
		{
			if (TryGetVar(vid, out var variable))
			{
				variable.ChangeValue((double)num, out var _);
				return;
			}
			throw new ArgumentException(Resources.OnlyValidForVariables);
		}

		/// <summary>
		/// Mark a variable as an integer variable 
		/// </summary>
		/// <param name="vid">a variable index </param>
		/// <param name="fInteger">whether to be an integer variable</param>
		public void SetIntegrality(int vid, bool fInteger)
		{
			PreChange();
			if (fInteger)
			{
				EvaluableTerm term = GetTerm(vid);
				if (term is EvaluableBooleanTerm)
				{
					return;
				}
				if (!(term is EvaluableVariable evaluableVariable))
				{
					throw new NotSupportedException(Resources.OnlyValidForVariables);
				}
				LocalSearchDomain domain = evaluableVariable.Domain;
				if (domain is LocalSearchContinuousInterval localSearchContinuousInterval)
				{
					long num = Ceiling(localSearchContinuousInterval.Lower);
					long num2 = Floor(localSearchContinuousInterval.Upper);
					if (num > num2)
					{
						throw new ArgumentException(Resources.InvalidBounds);
					}
					evaluableVariable.ResetDomain(new LocalSearchIntegerInterval(num, num2), _prng);
				}
				else if (domain is LocalSearchFiniteRealSet)
				{
					throw new ArgumentException(Resources.InvalidBounds);
				}
			}
			else if (((IRowVariableModel)this).GetIntegrality(vid))
			{
				throw new NotSupportedException(Resources.IntegralityMayNotBeRedefined);
			}
		}

		/// <summary>
		/// Check if a variable is an integer variable
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if this variable is an integer variable. Otherwise false.</returns>
		public bool GetIntegrality(int vid)
		{
			EvaluableTerm term = GetTerm(vid);
			if (term is EvaluableBooleanTerm)
			{
				return true;
			}
			if (term is EvaluableVariable evaluableVariable)
			{
				return evaluableVariable.Domain.IsDiscrete;
			}
			if (term is EvaluableConstant evaluableConstant)
			{
				return EvaluationStatics.IsInteger64(evaluableConstant.Value);
			}
			throw new NotSupportedException(Resources.OnlyValidForVariables);
		}

		/// <summary>Mark a row as a goal.
		/// </summary>
		/// <param name="vid">A row id.</param>
		/// <param name="pri">The priority of the goal (smaller values are prioritized first).</param>
		/// <param name="minimize">Whether to minimize the goal row.</param>
		/// <returns>An IGoal object representing the goal.</returns>
		public IGoal AddGoal(int vid, int pri, bool minimize)
		{
			PreChange();
			object key = null;
			if (vid < _keyFromIndex.Count)
			{
				key = _keyFromIndex[vid];
			}
			Goal goal = new Goal();
			goal.Key = key;
			goal.Enabled = true;
			goal.Priority = pri;
			goal.Index = vid;
			goal.Minimize = minimize;
			Goal goal2 = goal;
			goalList.Add(goal2);
			return goal2;
		}

		/// <summary>
		/// Check if a row id is a goal row.
		/// </summary>
		/// <param name="vid">A row id.</param>
		/// <returns>True if this a goal row, otherwise false.</returns>
		public bool IsGoal(int vid)
		{
			IGoal goal;
			return ((IGoalModel)this).IsGoal(vid, out goal);
		}

		/// <summary>
		/// Check if a row id is a goal and retreive the associated IGoal. 
		/// </summary>
		/// <param name="vid">A row id.</param>
		/// <param name="goal">The IGoal corresponding to the vid.</param>
		/// <returns>True if this a goal row, otherwise false.</returns>
		public bool IsGoal(int vid, out IGoal goal)
		{
			foreach (Goal goal2 in goalList)
			{
				if (goal2.Index == vid)
				{
					goal = goal2;
					return true;
				}
			}
			goal = null;
			return false;
		}

		/// <summary>
		/// Remove a goal row.
		/// </summary>
		/// <param name="vid">A row id.</param>
		/// <returns>True if the goal was removed, otherwise false.</returns>
		public bool RemoveGoal(int vid)
		{
			PreChange();
			return _evaluator.RemoveGoal(ExtractNumericalArg(vid));
		}

		/// <summary>
		/// Clear all the goals .
		/// </summary>
		public void ClearGoals()
		{
			PreChange();
			goalList.Clear();
		}

		/// <summary>
		/// Return a goal entry if the row id is a goal
		/// </summary>
		/// <param name="vid">A variable id.</param>
		/// <returns>A goal entry. Null if the vid does not correspond to a goal.</returns>
		public IGoal GetGoalFromIndex(int vid)
		{
			((IGoalModel)this).IsGoal(vid, out IGoal goal);
			return goal;
		}

		/// <summary>
		/// Specifies that a numerical term is a goal to minimize.
		/// Priorities are determined by the order in which the goals are added
		/// (first goals have higher priorities)
		/// </summary>
		public void AddGoal(int vid)
		{
			PreChange();
			((IGoalModel)this).AddGoal(vid, goalList.Count, minimize: true);
		}

		/// <summary>
		/// (re-)computes the set of goals and adds them to the evaluator
		/// </summary>
		private void PrepareGoals()
		{
			goalList.Sort((Goal x, Goal y) => x.Priority.CompareTo(y.Priority));
			_evaluator.ClearGoals();
			foreach (Goal goal in goalList)
			{
				EvaluableNumericalTerm evaluableNumericalTerm = ExtractNumericalArg(goal.Index);
				if (!goal.Minimize)
				{
					evaluableNumericalTerm = _evaluator.CreateUnaryMinus(evaluableNumericalTerm);
				}
				_evaluator.AddGoal(evaluableNumericalTerm);
			}
		}
	}
}
