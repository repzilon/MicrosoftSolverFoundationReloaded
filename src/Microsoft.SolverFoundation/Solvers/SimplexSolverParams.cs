using System;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Parameters used by a solver 
	/// </summary>
	public class SimplexSolverParams : ISolverParameters, ISolverEvents
	{
		private SimplexCosting _cost;

		private bool _fUseDouble;

		private bool _fUseExact;

		private SolverKind _solverKind;

		private SimplexAlgorithmKind _simplexAlg;

		private bool _fAbort;

		private int _cpivMax;

		private int _cvGeoMax;

		private double _dblGeo;

		private bool _fShiftBounds;

		private double _varEps;

		private double _costEps;

		private bool _userOverideCostEps;

		private bool _userOverideVarEps;

		private SimplexBasisKind _basisKind;

		private Func<bool> _fnQueryAbort;

		private Action _fnSolving;

		private double _mipgapTolerance;

		private SearchStrategy _mipSearchStrategy;

		private BranchingStrategy _mipPreFeasibilityBranchingStrategy;

		private BranchingStrategy _mipPostFeasibilityBranchingStrategy;

		private bool _mipPresolve;

		private bool _mipNodePresolve;

		private bool _fGenerateCuts;

		private int _mipGomoryCutRoundLim;

		private CutKind _cutKinds;

		private bool _fSensitivity;

		private bool _fInfeasibility;

		private int _presolveLevel;

		/// <summary> Callback for ending the solve
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

		/// <summary> Use double arthimetic 
		/// </summary>
		public virtual bool UseDouble
		{
			get
			{
				return _fUseDouble;
			}
			set
			{
				_fUseDouble = value;
			}
		}

		/// <summary> Use exact arthimetic 
		/// </summary>
		public virtual bool UseExact
		{
			get
			{
				return _fUseExact;
			}
			set
			{
				_fUseExact = value;
			}
		}

		/// <summary> Choose a solver 
		/// </summary>
		public virtual SolverKind KindOfSolver => _solverKind;

		/// <summary> Choose a solver algorithm 
		/// </summary>
		public virtual SimplexAlgorithmKind Algorithm
		{
			get
			{
				return _simplexAlg;
			}
			set
			{
				_simplexAlg = value;
			}
		}

		/// <summary> Simplex costing 
		/// </summary>
		public virtual SimplexCosting Costing
		{
			get
			{
				return _cost;
			}
			set
			{
				_cost = value;
			}
		}

		/// <summary> Numerical tolerance for variables 
		/// </summary>
		public virtual double VariableFeasibilityTolerance
		{
			get
			{
				return _varEps;
			}
			set
			{
				_varEps = value;
				_userOverideVarEps = true;
			}
		}

		/// <summary> Numerical tolerance for Simplex pricing 
		/// </summary>
		public virtual double CostTolerance
		{
			get
			{
				return _costEps;
			}
			set
			{
				_costEps = value;
				_userOverideCostEps = true;
			}
		}

		/// <summary> whether being overriden by users
		/// </summary>
		internal bool UserOveride
		{
			get
			{
				if (!_userOverideCostEps)
				{
					return _userOverideVarEps;
				}
				return true;
			}
		}

		/// <summary> whether being overriden by users
		/// </summary>
		internal bool UserOverideCostEps => _userOverideCostEps;

		/// <summary> whether being overriden by users
		/// </summary>
		internal bool UserOverideVarEps => _userOverideVarEps;

		/// <summary> Should the solver stop 
		/// </summary>
		public virtual bool Abort
		{
			get
			{
				return _fAbort;
			}
			set
			{
				_fAbort = value;
			}
		}

		/// <summary> Simplex specific. The upper limit of pivit count a solver can do 
		/// </summary>
		public virtual int MaxPivotCount
		{
			get
			{
				return _cpivMax;
			}
			set
			{
				_cpivMax = value;
			}
		}

		/// <summary> Should a solver shift variable bounds 
		/// </summary>
		public virtual bool ShiftBounds
		{
			get
			{
				return _fShiftBounds;
			}
			set
			{
				_fShiftBounds = value;
			}
		}

		/// <summary> Simplex specific. Choose the initial basis. 
		/// </summary>
		public virtual SimplexBasisKind InitialBasisKind
		{
			get
			{
				return _basisKind;
			}
			set
			{
				_basisKind = value;
			}
		}

		/// <summary> the upper limit of geometric scaling in the reduced model 
		/// </summary>
		public virtual int MaxGeometricScalingIterations
		{
			get
			{
				return _cvGeoMax;
			}
			set
			{
				_cvGeoMax = value;
			}
		}

		/// <summary> the threshhold of geometric scaling in the reduced model 
		/// </summary>
		public virtual double GeometricScalingThreshold
		{
			get
			{
				return _dblGeo;
			}
			set
			{
				_dblGeo = value;
			}
		}

		/// <summary> whether to generate sensitivity report 
		/// </summary>
		public bool GetSensitivityReport
		{
			get
			{
				return _fSensitivity;
			}
			set
			{
				_fSensitivity = value;
			}
		}

		/// <summary> whether to generate infeasibility report 
		/// </summary>
		public bool GetInfeasibilityReport
		{
			get
			{
				return _fInfeasibility;
			}
			set
			{
				_fInfeasibility = value;
			}
		}

		/// <summary> the level of presolve the SimplexSolver will apply 
		/// -1 means default or automatic, 0 means presolve, &gt;0 primal and dual reduce
		/// </summary>
		public int PresolveLevel
		{
			get
			{
				return _presolveLevel;
			}
			set
			{
				_presolveLevel = value;
			}
		}

		/// <summary> Gets or sets the tolerance to declare an integer solution optimal.
		/// </summary>
		public double MixedIntegerGapTolerance
		{
			get
			{
				return _mipgapTolerance;
			}
			set
			{
				_mipgapTolerance = value;
			}
		}

		/// <summary> Gets or sets the search strategy for finding an optimal integer solution.
		/// </summary>
		/// <remarks>
		/// This property controls the post-feasibility search strategy.
		/// The pre-feasibility search strategy cannot be changed.
		/// </remarks>
		public SearchStrategy MixedIntegerSearchStrategy
		{
			get
			{
				return _mipSearchStrategy;
			}
			set
			{
				_mipSearchStrategy = value;
			}
		}

		/// <summary> Gets or sets the way the solver selects variables to branch on, before feasibility is achieved,
		/// </summary>
		public BranchingStrategy MixedIntegerBranchingStrategyPreFeasibility
		{
			get
			{
				return _mipPreFeasibilityBranchingStrategy;
			}
			set
			{
				_mipPreFeasibilityBranchingStrategy = value;
			}
		}

		/// <summary> Gets or sets the way the solver selects variables to branch on, after feasibility is achieved,
		/// </summary>
		public BranchingStrategy MixedIntegerBranchingStrategyPostFeasibility
		{
			get
			{
				return _mipPostFeasibilityBranchingStrategy;
			}
			set
			{
				_mipPostFeasibilityBranchingStrategy = value;
			}
		}

		/// <summary> Gets or sets whether to presolve the model.
		/// </summary>
		public bool MixedIntegerPresolve
		{
			get
			{
				return _mipPresolve;
			}
			set
			{
				_mipPresolve = value;
			}
		}

		/// <summary> Gets or sets whether to perform presolve during the search.
		/// </summary>
		public bool MixedIntegerNodePresolve
		{
			get
			{
				return _mipNodePresolve;
			}
			set
			{
				_mipNodePresolve = value;
			}
		}

		/// <summary> Gets or sets whether to generate cuts.
		/// </summary>
		public bool MixedIntegerGenerateCuts
		{
			get
			{
				return _fGenerateCuts;
			}
			set
			{
				_fGenerateCuts = value;
			}
		}

		/// <summary> Gets or sets the level limit on cutting plane generation
		/// </summary>
		public int MixedIntegerGomoryCutRoundLimit
		{
			get
			{
				return _mipGomoryCutRoundLim;
			}
			set
			{
				_mipGomoryCutRoundLim = value;
			}
		}

		/// <summary> Control what kinds of cut will be generated in cut-and-branch.
		/// </summary>
		public CutKind CutKinds
		{
			get
			{
				return _cutKinds;
			}
			set
			{
				_cutKinds = value;
			}
		}

		/// <summary> construct a solver parameter object
		/// </summary>
		/// <param name="fnQueryAbort">a call back delegate</param>
		public SimplexSolverParams(Func<bool> fnQueryAbort)
		{
			_cost = SimplexCosting.Automatic;
			_cpivMax = int.MaxValue;
			_fUseDouble = true;
			_fUseExact = true;
			_fSensitivity = false;
			_fInfeasibility = false;
			_presolveLevel = -1;
			_solverKind = SolverKind.Simplex;
			_simplexAlg = SimplexAlgorithmKind.Primal;
			_basisKind = SimplexBasisKind.Slack;
			_fnQueryAbort = fnQueryAbort;
			_cvGeoMax = 0;
			_dblGeo = 1000.0;
			_userOverideCostEps = false;
			_userOverideVarEps = false;
			_mipgapTolerance = 1E-05;
			_mipPresolve = true;
			_mipNodePresolve = false;
			_fGenerateCuts = false;
			_mipGomoryCutRoundLim = 200;
			_cutKinds = CutKind.GomoryFractional;
		}

		/// <summary> constructor 
		/// </summary>
		public SimplexSolverParams()
			: this((Func<bool>)null)
		{
		}

		/// <summary> adaptor to convert Directive into SimplexSolverParams
		/// </summary>
		public SimplexSolverParams(Directive directive)
			: this((Func<bool>)null)
		{
			FillInSolverParams(directive);
		}

		/// <summary> copy constructor
		/// </summary>
		/// <param name="prm">a parameter to be cloned from</param>
		public SimplexSolverParams(SimplexSolverParams prm)
		{
			_cost = prm._cost;
			_fUseDouble = prm._fUseDouble;
			_fUseExact = prm._fUseExact;
			_solverKind = prm._solverKind;
			_simplexAlg = prm._simplexAlg;
			_fSensitivity = prm._fSensitivity;
			_fInfeasibility = prm._fInfeasibility;
			_fAbort = prm._fAbort;
			_cpivMax = prm._cpivMax;
			_cvGeoMax = prm._cvGeoMax;
			_dblGeo = prm._dblGeo;
			_fShiftBounds = prm._fShiftBounds;
			_basisKind = prm._basisKind;
			_fnQueryAbort = prm._fnQueryAbort;
			_presolveLevel = prm._presolveLevel;
			_mipgapTolerance = prm.MixedIntegerGapTolerance;
			_mipPresolve = prm.MixedIntegerPresolve;
			_mipNodePresolve = prm.MixedIntegerNodePresolve;
			_mipPreFeasibilityBranchingStrategy = prm.MixedIntegerBranchingStrategyPreFeasibility;
			_mipPostFeasibilityBranchingStrategy = prm.MixedIntegerBranchingStrategyPostFeasibility;
			_mipSearchStrategy = prm.MixedIntegerSearchStrategy;
			_fGenerateCuts = prm.MixedIntegerGenerateCuts;
			_mipGomoryCutRoundLim = prm.MixedIntegerGomoryCutRoundLimit;
			_cutKinds = prm.CutKinds;
		}

		/// <summary> callback on whether the solver should abort execution 
		/// </summary>
		/// <param name="stat">callback context</param>
		/// <returns>true if should stop. otherwise false</returns>
		public virtual bool ShouldAbort(ILinearSimplexStatistics stat)
		{
			if (!_fAbort && (stat == null || stat.PivotCount < _cpivMax))
			{
				if (_fnQueryAbort != null)
				{
					return _fnQueryAbort();
				}
				return false;
			}
			return true;
		}

		/// <summary> callback before the solve starts 
		/// </summary>
		/// <param name="threadIndex">simplex thread id</param>
		/// <returns>return code is not used</returns>
		public virtual bool NotifyStartSolve(int threadIndex)
		{
			return !ShouldAbort(null);
		}

		/// <summary> callback before the matrix factorization 
		/// </summary>
		/// <param name="threadIndex">a simplex thread id</param>
		/// <param name="stat">context</param>
		/// <param name="fDouble">whether double arithmetic is used</param>
		/// <returns>true if the execution should continue. otherwise false</returns>
		public virtual bool NotifyStartFactorization(int threadIndex, ILinearSimplexStatistics stat, bool fDouble)
		{
			RaiseSolvingEvent();
			return !ShouldAbort(stat);
		}

		/// <summary> callback after the matrix factorization 
		/// </summary>
		/// <param name="threadIndex">a simplex thread id</param>
		/// <param name="stat">context</param>
		/// <param name="fDouble">whether double arithmetic is used</param>
		/// <returns>true if the execution should continue. otherwise false</returns>
		public virtual bool NotifyEndFactorization(int threadIndex, ILinearSimplexStatistics stat, bool fDouble)
		{
			RaiseSolvingEvent();
			return !ShouldAbort(stat);
		}

		/// <summary> callback when Simplex finds the next pair of pivoting variables 
		/// </summary>
		/// <param name="threadIndex">a simplex thread id</param>
		/// <param name="stat">context</param>
		/// <param name="fDouble">whether double arithmetic is used</param>
		/// <returns>true if the execution should continue. otherwise false</returns>
		public virtual bool NotifyFindNext(int threadIndex, ILinearSimplexStatistics stat, bool fDouble)
		{
			RaiseSolvingEvent();
			return !ShouldAbort(stat);
		}

		/// <summary> callback before the pivot
		/// </summary>
		/// <param name="threadIndex">a simplex thread id</param>
		/// <param name="stat">context</param>
		/// <param name="pi">pivoting information</param>
		/// <returns>true if the execution should continue. otherwise false</returns>
		public virtual bool NotifyStartPivot(int threadIndex, ILinearSimplexStatistics stat, ISimplexPivotInformation pi)
		{
			RaiseSolvingEvent();
			return !ShouldAbort(stat);
		}

		/// <summary> callback after the pivot
		/// </summary>
		/// <param name="threadIndex">a simplex thread id</param>
		/// <param name="stat">context</param>
		/// <param name="pi">pivoting information</param>
		/// <returns>true if the execution should continue. otherwise false</returns>
		public virtual bool NotifyEndPivot(int threadIndex, ILinearSimplexStatistics stat, ISimplexPivotInformation pi)
		{
			RaiseSolvingEvent();
			return !ShouldAbort(stat);
		}

		/// <summary> Raise the solving event during simplex solve
		/// </summary>
		internal void RaiseSolvingEvent()
		{
			if (Solving != null)
			{
				Solving();
			}
		}

		/// <summary>
		/// Fill in SimplexSolverParams based on the given directive.
		/// </summary>
		/// <param name="dir">The directive instance that contains all the parameter settings</param>
		private void FillInSolverParams(Directive dir)
		{
			if (dir != null && dir is SimplexDirective simplexDirective)
			{
				switch (simplexDirective.Basis)
				{
				case Microsoft.SolverFoundation.Services.SimplexBasis.Crash:
					InitialBasisKind = SimplexBasisKind.Crash;
					break;
				default:
					InitialBasisKind = SimplexBasisKind.Slack;
					break;
				case Microsoft.SolverFoundation.Services.SimplexBasis.Freedom:
					InitialBasisKind = SimplexBasisKind.Freedom;
					break;
				}
				switch (simplexDirective.Algorithm)
				{
				case SimplexAlgorithm.Dual:
					Algorithm = SimplexAlgorithmKind.Dual;
					break;
				default:
					Algorithm = SimplexAlgorithmKind.Primal;
					break;
				}
				switch (simplexDirective.Pricing)
				{
				case SimplexPricing.Partial:
					Costing = SimplexCosting.Partial;
					break;
				case SimplexPricing.ReducedCost:
					Costing = SimplexCosting.BestReducedCost;
					break;
				case SimplexPricing.SteepestEdge:
					Costing = SimplexCosting.SteepestEdge;
					break;
				default:
					Costing = SimplexCosting.Automatic;
					break;
				}
				switch (simplexDirective.Arithmetic)
				{
				case Arithmetic.Double:
					UseDouble = true;
					UseExact = false;
					break;
				case Arithmetic.Exact:
					UseDouble = true;
					UseExact = true;
					break;
				default:
					UseDouble = true;
					UseExact = false;
					break;
				}
				GetSensitivityReport = simplexDirective.GetSensitivity;
				GetInfeasibilityReport = simplexDirective.GetInfeasibility;
				if (simplexDirective.PricingTolerance > 0.0)
				{
					CostTolerance = simplexDirective.PricingTolerance;
				}
				if (simplexDirective.VariableTolerance > 0.0)
				{
					VariableFeasibilityTolerance = simplexDirective.VariableTolerance;
				}
				if (simplexDirective.IterationLimit >= 0)
				{
					MaxPivotCount = simplexDirective.IterationLimit;
				}
				if (dir is MixedIntegerProgrammingDirective mixedIntegerProgrammingDirective && mixedIntegerProgrammingDirective.CuttingPlaneGeneration)
				{
					MixedIntegerGenerateCuts = true;
					CutKinds = CutKind.GomoryFractional;
				}
			}
		}
	}
}
