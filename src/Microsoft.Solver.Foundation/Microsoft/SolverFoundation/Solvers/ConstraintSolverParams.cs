using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Optional solver parameters specifying the search strategy of the 
	/// CSP algorithm. Use for performance tuning 
	/// </summary>
	public sealed class ConstraintSolverParams : ISolverParameters, ISolverEvents
	{
		/// <summary>
		/// Choice of algorithm 
		/// </summary>
		public enum CspSearchAlgorithm
		{
			/// <summary>
			/// Let the solver choose the search algorithm
			/// </summary>
			Any,
			/// <summary>
			/// Backtracking based complete search algorithm
			/// </summary>
			TreeSearch,
			/// <summary>
			/// Local search algorithm
			/// </summary>
			LocalSearch
		}

		/// <summary>
		/// Choice of tree search algorithm  
		/// </summary>
		public enum TreeSearchVariableOrdering
		{
			/// <summary>
			/// Let the solver choose the variable ordering heuristics
			/// </summary>
			Any,
			/// <summary>
			/// Enumeration that chooses a variable with smallest domain
			/// </summary> 
			MinimalDomainFirst,
			/// <summary>
			/// Enumeration following the declaration order of the variables
			/// </summary>
			DeclarationOrder,
			/// <summary>
			/// Weigh variables dynamically according to their dependents and current domain sizes
			/// </summary>
			DynamicWeighting,
			/// <summary>
			/// Enumeration based on conflict analysis following a variant 
			/// of the VSIDS heuristic used in SAT solvers
			/// </summary>
			ConflictDriven,
			/// <summary>
			/// Enumeration based on a forecast of the impact 
			/// of the decision
			/// </summary>
			ImpactPrediction,
			/// <summary>
			/// Enumeration based on the "domain over weighted degree"
			/// </summary> 
			DomainOverWeightedDegree
		}

		/// <summary>
		/// Choice of value
		/// </summary>
		public enum TreeSearchValueOrdering
		{
			/// <summary>
			/// Let the solver choose the variable ordering heuristics
			/// </summary>
			Any,
			/// <summary>
			/// Value enumeration based on a prediction of the success
			/// </summary>
			SuccessPrediction,
			/// <summary>
			/// Value enumeration that follows the order of the values
			/// </summary>
			ForwardOrder,
			/// <summary>
			/// Value enumeration that picks uniformly at random
			/// </summary>
			RandomOrder
		}

		/// <summary>
		/// Choice of moves for local search
		/// </summary>
		public enum LocalSearchMove
		{
			/// <summary>
			/// Let the solver choose the move heuristics
			/// </summary>
			Any,
			/// <summary>
			/// Violation-guided greedy move
			/// </summary>
			Greedy,
			/// <summary>
			/// Simulated annealing
			/// </summary>
			SimulatedAnnealing,
			/// <summary>
			/// Violation-guided greedy with noise
			/// </summary>
			GreedyNoise,
			/// <summary>
			/// Violation-guided greedy with noise and tabu
			/// </summary>
			Tabu,
			/// <summary>
			/// Gradient-guided with tabu and escape strategy
			/// </summary>
			Gradients
		}

		private CspSearchAlgorithm _algorithm;

		private TreeSearchVariableOrdering _treeSearchVarOrdering;

		private TreeSearchValueOrdering _treeSearchValOrdering;

		private LocalSearchMove _localSearchMoveSelection;

		private ConstraintSolverHeuristics _customizedHeuristic;

		private bool _fAbort;

		private int _timeLimitMilliSec;

		private int _elapsedMilliSec;

		private List<CspTerm> _searchFirst;

		private bool _enableRestarts;

		private bool _enumerateInterimSolutions;

		private Func<bool> _fnQueryAbort;

		private Action _fnSolving;

		internal bool _forceIntegerSolver;

		/// <summary>
		/// Get/set the callback function that decides when to abort the search
		/// </summary>
		public Func<bool> QueryAbort
		{
			get
			{
				return _fnQueryAbort;
			}
			set
			{
				_fnQueryAbort = value;
			}
		}

		/// <summary>
		/// Callback called during solve
		/// </summary>
		public Action Solving
		{
			get
			{
				return _fnSolving;
			}
			set
			{
				_fnSolving = value;
			}
		}

		/// <summary>
		/// Specifies which class of algorithm should be used
		/// </summary>
		public CspSearchAlgorithm Algorithm
		{
			get
			{
				return _algorithm;
			}
			set
			{
				_algorithm = value;
			}
		}

		/// <summary>
		/// Get/set the variable ordering heuristic
		/// </summary>
		public TreeSearchVariableOrdering VariableSelection
		{
			get
			{
				return _treeSearchVarOrdering;
			}
			set
			{
				_treeSearchVarOrdering = value;
			}
		}

		/// <summary>
		/// Get/set the value ordering heuristic
		/// </summary>
		public TreeSearchValueOrdering ValueSelection
		{
			get
			{
				return _treeSearchValOrdering;
			}
			set
			{
				_treeSearchValOrdering = value;
			}
		}

		/// <summary>
		/// Get/set the move selection heuristic
		/// </summary>
		public LocalSearchMove MoveSelection
		{
			get
			{
				return _localSearchMoveSelection;
			}
			set
			{
				_localSearchMoveSelection = value;
			}
		}

		/// <summary> Get/set the customized (user-defined) heuristic 
		/// </summary>
		/// <remarks> This strategy will effectively be used only if the properties
		///           describing the choice of heuristic specify that the 
		/// </remarks>
		internal ConstraintSolverHeuristics CustomizedHeuristic
		{
			get
			{
				return _customizedHeuristic;
			}
			set
			{
				_customizedHeuristic = value;
			}
		}

		/// <summary> Provide a list of variables that should be searched, breadth first,
		///           in priority over the internal search strategy.
		///           If called multiple times, the subsequent calls append terms to the
		///           list (appended terms are searched later than those already in).
		/// </summary>
		public ReadOnlyCollection<CspTerm> UserOrderVariables
		{
			get
			{
				return _searchFirst.AsReadOnly();
			}
			internal set
			{
				_searchFirst = new List<CspTerm>(value);
			}
		}

		/// <summary> Restarts are used to prevent investing too much time searching.
		///           By default they are enabled.  When restarting, FDModel records learned
		///           constraints that summarize the search space already covered
		///           and then resumes in some other part of the state space.
		///           It can be useful to disable restarts if you are supplying a
		///           SearchFirst list.
		/// </summary>
		public bool RestartEnabled
		{
			get
			{
				return _enableRestarts;
			}
			set
			{
				_enableRestarts = value;
			}
		}

		/// <summary> Whether to enumerate interim solutions or only optimal solutions. Default is true. Local search algorithm ignores this flag and will always return interim solutions.
		/// </summary>
		public bool EnumerateInterimSolutions
		{
			get
			{
				return _enumerateInterimSolutions;
			}
			set
			{
				_enumerateInterimSolutions = value;
			}
		}

		/// <summary> The Solver will check the abort flag frequently and will
		///           come to a graceful stop then return to the caller if the
		///           abort flag is set.  This is useful when running the solver
		///           on a background thread.
		/// </summary>
		public bool Abort
		{
			get
			{
				return _fAbort = _fAbort || (_fnQueryAbort != null && _fnQueryAbort());
			}
			set
			{
				_fAbort = value;
			}
		}

		/// <summary> The limit on the CPU time before giving up.  This is a
		///           simple alternative to spinning off a separate thread and
		///           using Abort, if the interval is short.  Clear is forever.
		/// </summary>
		public int TimeLimitMilliSec
		{
			get
			{
				if (_timeLimitMilliSec > 0)
				{
					return _timeLimitMilliSec;
				}
				return int.MaxValue;
			}
			set
			{
				_timeLimitMilliSec = value;
			}
		}

		/// <summary> The time it took to solve the most recent solution.
		/// </summary>
		public int ElapsedMilliSec => _elapsedMilliSec;

		/// <summary>
		/// Default constructor
		/// </summary>
		public ConstraintSolverParams()
			: this((Collection<CspTerm>)null)
		{
		}

		/// <summary> adaptor to convert Directive into SimplexSolverParams
		/// </summary>
		public ConstraintSolverParams(Directive directive)
			: this((Collection<CspTerm>)null)
		{
			FillInSolverParams(directive);
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ConstraintSolverParams(Collection<CspTerm> searchFirstVars)
		{
			_algorithm = CspSearchAlgorithm.Any;
			_treeSearchVarOrdering = TreeSearchVariableOrdering.Any;
			_treeSearchValOrdering = TreeSearchValueOrdering.Any;
			_fAbort = false;
			_timeLimitMilliSec = 0;
			_elapsedMilliSec = 0;
			_enableRestarts = true;
			if (searchFirstVars != null)
			{
				_searchFirst = new List<CspTerm>(searchFirstVars);
			}
			else
			{
				_searchFirst = new List<CspTerm>();
			}
			_forceIntegerSolver = false;
			_enumerateInterimSolutions = false;
		}

		/// <summary>
		/// Copy constructor
		/// </summary>
		/// <param name="dir">The solver parameter to clone</param>
		/// <param name="termMap">The mapping between the modeling terms and actual solver terms. Needed to clone the SearchFirst list.</param>
		internal ConstraintSolverParams(ConstraintSolverParams dir, Dictionary<CspTerm, CspTerm> termMap)
		{
			_algorithm = dir._algorithm;
			_treeSearchVarOrdering = dir._treeSearchVarOrdering;
			_treeSearchValOrdering = dir._treeSearchValOrdering;
			_enumerateInterimSolutions = dir._enumerateInterimSolutions;
			_fAbort = dir._fAbort;
			_timeLimitMilliSec = dir._timeLimitMilliSec;
			_elapsedMilliSec = dir._elapsedMilliSec;
			_enableRestarts = dir._enableRestarts;
			_fnQueryAbort = dir._fnQueryAbort;
			_forceIntegerSolver = dir._forceIntegerSolver;
			_localSearchMoveSelection = dir._localSearchMoveSelection;
			_customizedHeuristic = dir._customizedHeuristic;
			if (dir._searchFirst.Count == 0)
			{
				_searchFirst = new List<CspTerm>();
				return;
			}
			if (termMap == null || termMap.Count == 0)
			{
				_searchFirst = new List<CspTerm>(dir._searchFirst);
				return;
			}
			_searchFirst = new List<CspTerm>();
			for (int i = 0; i < dir._searchFirst.Count; i++)
			{
				if (!termMap.ContainsKey(dir._searchFirst[i]))
				{
					throw new ArgumentException(Resources.SearchFirstTermError);
				}
				_searchFirst.Add(termMap[dir._searchFirst[i]]);
			}
		}

		/// <summary>
		/// Fill in this ISolverParameters instance based on the given directive instance
		/// </summary>
		private void FillInSolverParams(Directive dir)
		{
			if (dir != null && dir is ConstraintProgrammingDirective constraintProgrammingDirective)
			{
				switch (constraintProgrammingDirective.Algorithm)
				{
				case ConstraintProgrammingAlgorithm.LocalSearch:
					Algorithm = CspSearchAlgorithm.LocalSearch;
					break;
				case ConstraintProgrammingAlgorithm.TreeSearch:
					Algorithm = CspSearchAlgorithm.TreeSearch;
					break;
				default:
					Algorithm = CspSearchAlgorithm.Any;
					break;
				}
				RestartEnabled = constraintProgrammingDirective.RestartEnabled;
				switch (constraintProgrammingDirective.ValueSelection)
				{
				case TreeSearchValueSelection.ForwardOrder:
					ValueSelection = TreeSearchValueOrdering.ForwardOrder;
					break;
				case TreeSearchValueSelection.RandomOrder:
					ValueSelection = TreeSearchValueOrdering.RandomOrder;
					break;
				case TreeSearchValueSelection.SuccessPrediction:
					ValueSelection = TreeSearchValueOrdering.SuccessPrediction;
					break;
				case TreeSearchValueSelection.Default:
					ValueSelection = TreeSearchValueOrdering.Any;
					break;
				}
				switch (constraintProgrammingDirective.VariableSelection)
				{
				case TreeSearchVariableSelection.DynamicWeighting:
					VariableSelection = TreeSearchVariableOrdering.DynamicWeighting;
					break;
				case TreeSearchVariableSelection.ConflictDriven:
					VariableSelection = TreeSearchVariableOrdering.ConflictDriven;
					break;
				case TreeSearchVariableSelection.DeclarationOrder:
					VariableSelection = TreeSearchVariableOrdering.DeclarationOrder;
					break;
				case TreeSearchVariableSelection.DomainOverWeightedDegree:
					VariableSelection = TreeSearchVariableOrdering.DomainOverWeightedDegree;
					break;
				case TreeSearchVariableSelection.ImpactPrediction:
					VariableSelection = TreeSearchVariableOrdering.ImpactPrediction;
					break;
				case TreeSearchVariableSelection.MinimalDomainFirst:
					VariableSelection = TreeSearchVariableOrdering.MinimalDomainFirst;
					break;
				case TreeSearchVariableSelection.Default:
					VariableSelection = TreeSearchVariableOrdering.Any;
					break;
				}
				switch (constraintProgrammingDirective.MoveSelection)
				{
				case LocalSearchMoveSelection.Gradients:
					MoveSelection = LocalSearchMove.Gradients;
					break;
				case LocalSearchMoveSelection.Greedy:
					MoveSelection = LocalSearchMove.Greedy;
					break;
				case LocalSearchMoveSelection.GreedyNoise:
					MoveSelection = LocalSearchMove.GreedyNoise;
					break;
				case LocalSearchMoveSelection.SimulatedAnnealing:
					MoveSelection = LocalSearchMove.SimulatedAnnealing;
					break;
				case LocalSearchMoveSelection.Tabu:
					MoveSelection = LocalSearchMove.Tabu;
					break;
				case LocalSearchMoveSelection.Default:
					MoveSelection = LocalSearchMove.Any;
					break;
				}
			}
		}

		/// <summary>
		/// Internal method to set the time used in search
		/// </summary>
		internal void SetElapsed(int millisecs)
		{
			_elapsedMilliSec = millisecs;
		}
	}
}
