#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class SimplexTask : ILinearSimplexStatistics
	{
		[DebuggerDisplay("{_varDel}({_vvkDel}) -> {_varAdd}({_vvkAdd}) {_scale}")]
		private struct PivotInfo
		{
			private readonly bool _fDouble;

			public readonly int _varAdd;

			public readonly int _varDel;

			public readonly SimplexVarValKind _vvkAdd;

			public readonly SimplexVarValKind _vvkDel;

			public readonly Rational _scale;

			public PivotInfo(bool fDouble, int varAdd, int varDel, SimplexVarValKind vvkAdd, SimplexVarValKind vvkDel, Rational scale)
			{
				_fDouble = fDouble;
				_varAdd = varAdd;
				_varDel = varDel;
				_vvkAdd = vvkAdd;
				_vvkDel = vvkDel;
				_scale = scale;
			}
		}

		private const double _diveDegradationAllowed = 0.9;

		private const int _presolveNodeCountLimit = 100;

		private const int _cutGenerationNodeCountLimit = 100;

		private const int kcpiCheckInit = 20;

		private LinearResult _mipResult;

		private NodeManager _manager;

		/// <summary>
		/// The dive manager is used to perform limited depth-first search even when the 
		/// general search strategy is different (e.g., best-bound or best-estimate). 
		/// </summary>
		private NodeManager _diveManager;

		private bool _diving;

		private PseudoCosts _pseudoCosts;

		private Presolving _presolve;

		private SimplexReducedModel _originalReducedModel;

		private SimplexBasis _slackBasis;

		private bool _keepBasis;

		private OptimalGoalValues _relaxationOptimalGoalValues;

		private Rational[] _integerOptimalVariableValues;

		private static object _syncRoot = new object();

		private LinearResult _sosResult;

		private SOSNodeManager _branchingManager;

		private OptimalGoalValues _sosOgvBest;

		private SOSRowNode _sosNode;

		private readonly SimplexSolver _solver;

		private BoundManager _boundManager;

		private readonly int _tid;

		private readonly SimplexSolverParams _prm;

		private readonly LogSource _logger;

		private SimplexReducedModel _mod;

		private readonly bool _fDual;

		private readonly bool _fUseExact;

		private readonly bool _fUseDouble;

		private SimplexCosting _costingExact;

		private SimplexCosting _costingDouble;

		private AlgorithmRational _pes;

		private AlgorithmDouble _pds;

		private SimplexFactoredBasis _bas;

		private int _cvBranch;

		private Rational _gap;

		private readonly bool _fShiftBounds;

		private readonly SimplexBasisKind _basisKind;

		private MipSolver _workingMipSolver;

		private OptimalGoalValues _currentOptimalGoalValues;

		private Thread _thr;

		private int[] _fRowUsed;

		private int _infeasibleCount;

		internal bool _fSOSFastPath;

		internal Dictionary<int, ISOSStatus> _sosStatus;

		private List<PivotInfo> _rgpi;

		private int _cpiNonDegen;

		private int _cpiCheckForCycle;

		private SimplexVarValKind[] _mpvarvvkCheck;

		private Rational _numStallThreshold;

		private List<Rational[]> _listTempExact;

		private List<double[]> _listTempDbl;

		private VectorRational _vecRowUser;

		private VectorRational _vecRowReduced;

		/// <summary>
		/// Gets the result of solving the mixed-integer problem.
		/// </summary>
		public LinearResult MipResult => _mipResult;

		public SimplexSolver Solver => _solver;

		internal BoundManager BoundManager => _boundManager;

		protected LogSource Logger => _logger;

		public int Tid => _tid;

		public SimplexSolverParams Params => _prm;

		public SimplexReducedModel Model => _mod;

		public SimplexCosting CostingRequested => _prm.Costing;

		public AlgorithmRational AlgorithmExact => _pes;

		public AlgorithmDouble AlgorithmDouble => _pds;

		public OptimalGoalValues OptimalGoalValues => _currentOptimalGoalValues;

		public OptimalGoalValues OptimalMipGoalValues
		{
			get
			{
				if (_workingMipSolver != null)
				{
					return _workingMipSolver.BestGoalValues;
				}
				return null;
			}
		}

		public SimplexTask MipBestNodeTask
		{
			get
			{
				if (_workingMipSolver != null)
				{
					return _workingMipSolver.BestNode.Task;
				}
				return null;
			}
		}

		public Thread SystemThread => _thr;

		public virtual bool ShiftBounds => _fShiftBounds;

		public SimplexFactoredBasis Basis => _bas;

		public int InfeasibleCount
		{
			get
			{
				return _infeasibleCount;
			}
			set
			{
				_infeasibleCount = value;
			}
		}

		public SimplexSolveState? SolveState
		{
			get
			{
				if (_workingMipSolver == null)
				{
					return null;
				}
				return _workingMipSolver.SolveState;
			}
		}

		public virtual int InnerIndexCount => _solver.InnerIndexCount;

		public virtual int InnerIntegerIndexCount => _solver.InnerIntegerIndexCount;

		public virtual int InnerSlackCount => _solver.InnerSlackCount;

		public virtual int InnerRowCount => _solver.InnerRowCount;

		public virtual int PivotCount => PivotCountExact + PivotCountDouble;

		public virtual int PivotCountDegenerate => ((_pes != null) ? _pes.PivotCountDegenerate : 0) + ((_pds != null) ? _pds.PivotCountDegenerate : 0);

		public virtual int PivotCountExact
		{
			get
			{
				if (_pes != null)
				{
					return _pes.PivotCount;
				}
				return 0;
			}
		}

		public virtual int PivotCountExactPhaseOne
		{
			get
			{
				if (_pes != null)
				{
					return _pes.PivotCountPhaseOne;
				}
				return 0;
			}
		}

		public virtual int PivotCountExactPhaseTwo
		{
			get
			{
				if (_pes != null)
				{
					return _pes.PivotCountPhaseTwo;
				}
				return 0;
			}
		}

		public virtual int PivotCountDouble
		{
			get
			{
				if (_pds != null)
				{
					return _pds.PivotCount;
				}
				return 0;
			}
		}

		public virtual int PivotCountDoublePhaseOne
		{
			get
			{
				if (_pds != null)
				{
					return _pds.PivotCountPhaseOne;
				}
				return 0;
			}
		}

		public virtual int PivotCountDoublePhaseTwo
		{
			get
			{
				if (_pds != null)
				{
					return _pds.PivotCountPhaseTwo;
				}
				return 0;
			}
		}

		public virtual int FactorCount => FactorCountExact + FactorCountDouble;

		public virtual int FactorCountExact
		{
			get
			{
				if (_pes != null)
				{
					return _pes.FactorCount;
				}
				return 0;
			}
		}

		public virtual int FactorCountDouble
		{
			get
			{
				if (_pds != null)
				{
					return _pds.FactorCount;
				}
				return 0;
			}
		}

		public virtual int BranchCount
		{
			get
			{
				if (_workingMipSolver == null)
				{
					return _cvBranch;
				}
				return _workingMipSolver.BranchCount;
			}
			set
			{
				_cvBranch = value;
			}
		}

		public virtual Rational Gap
		{
			get
			{
				if (_workingMipSolver == null)
				{
					return _gap;
				}
				return _workingMipSolver.ComputeUserModelGap();
			}
			set
			{
				_gap = value;
			}
		}

		public virtual bool UseExact => _fUseExact;

		public virtual bool UseDouble => _fUseDouble;

		public virtual SimplexAlgorithmKind AlgorithmUsed
		{
			get
			{
				if (!_fDual)
				{
					return SimplexAlgorithmKind.Primal;
				}
				return SimplexAlgorithmKind.Dual;
			}
		}

		public virtual SimplexBasisKind InitialBasisUsed => _basisKind;

		public virtual SimplexCosting CostingUsedExact
		{
			get
			{
				return _costingExact;
			}
			set
			{
				_costingExact = value;
			}
		}

		public virtual SimplexCosting CostingUsedDouble
		{
			get
			{
				return _costingDouble;
			}
			set
			{
				_costingDouble = value;
			}
		}

		/// <summary>
		/// Solves the mixed-integer problem using branch and bound.
		/// </summary>
		/// <param name="resLp">The result of the relaxation.</param>
		private void DoBranchAndBound(LinearResult resLp)
		{
			if (resLp != LinearResult.Optimal)
			{
				_mipResult = LinearResult.Invalid;
				return;
			}
			if (_mod.CvarInt == 0 || !Branching.HasFractionalVariable(this))
			{
				lock (_syncRoot)
				{
					_solver.RegisterSolution(this, LinearResult.Optimal, fMip: true);
					SetIntegerOptimalVariableValues();
					_solver.RegisterMipSolutionResult(LinearResult.Optimal);
					return;
				}
			}
			InitOnceBranchAndBound();
			OptimalGoalValues integerOptimalGoalValues = null;
			_mipResult = LinearResult.Invalid;
			bool flag = BranchAndBound(ref integerOptimalGoalValues);
			Logger.LogEvent(14, Resources.OfReInitialization0, _solver.ReInitCount);
			Logger.LogEvent(14, Resources.OfReInitializationDueToCuts0, _solver.ReInitDueToCutsCount);
			Logger.LogEvent(14, Resources.OfReInitializationDueToNodes0, _solver.ReInitDueToNodesCount);
			if (integerOptimalGoalValues != null && _mipResult == LinearResult.Optimal)
			{
				_currentOptimalGoalValues = integerOptimalGoalValues;
			}
			else
			{
				_currentOptimalGoalValues.Clear();
			}
			if (flag)
			{
				_solver.RegisterMipSolutionResult(LinearResult.Optimal);
			}
		}

		/// <summary>
		/// Initializes the data structures used in branch and bound.
		/// </summary>
		private void InitOnceBranchAndBound()
		{
			InitBranchAndBound();
			CuttingPlanePool.ResetCoverGenFlag();
			_solver._cReInit = 0;
			_solver._cReInitDueToCuts = 0;
			_solver._cReInitDueToNodes = 0;
		}

		/// <summary>
		/// Initializes the data structures used in branch and bound.
		/// </summary>
		/// <remarks>
		/// This method is called by PseudoCosts when doing strong branching.
		/// </remarks>
		internal void InitBranchAndBound()
		{
			_originalReducedModel = Model;
			_integerOptimalVariableValues = null;
			_manager = new NodeManager(SearchStrategy.BestEstimate);
			_diveManager = new NodeManager(SearchStrategy.DepthFirst);
			_pseudoCosts = new PseudoCosts(this, Params.MixedIntegerBranchingStrategyPreFeasibility == BranchingStrategy.StrongCost);
			_boundManager.InitBounds(this);
			_presolve = new Presolving();
		}

		/// <summary>
		/// Checks whether the model has an objective.
		/// </summary>
		/// <returns></returns>
		internal bool HasGoal()
		{
			return _currentOptimalGoalValues.Count > 0;
		}

		/// <summary>
		/// Gets how many objectives are in the model.
		/// </summary>
		/// <returns></returns>
		internal int GetGoalCount()
		{
			return _currentOptimalGoalValues.Count;
		}

		/// <summary>
		/// Performs one step of the branch and bound algorithm and calls itself recursively.
		/// </summary>
		/// <param name="integerOptimalGoalValues">The currently best integer solution.</param>
		/// <returns>
		/// Returns true if we solve to completion.
		/// </returns>
		private bool BranchAndBound(ref OptimalGoalValues integerOptimalGoalValues)
		{
			int presolveNodeCount = 0;
			int cutGenerationNodeCount = 0;
			OptimalGoalValues b = _currentOptimalGoalValues.ScaleToUserModel(this);
			Statics.Swap(ref _relaxationOptimalGoalValues, ref b);
			Rational expectedGoalValue = (HasGoal() ? _relaxationOptimalGoalValues[0] : ((Rational)0));
			Node node = new Node(0, _relaxationOptimalGoalValues, expectedGoalValue);
			_manager.Add(node);
			Logger.LogEvent(12, "Initial relaxation: {0}", HasGoal() ? ((double)_relaxationOptimalGoalValues[0]) : double.NaN);
			while (_diveManager.Count > 0 || _manager.Count > 0)
			{
				_cvBranch++;
				Node node2;
				if (_diveManager.Count > 0)
				{
					node2 = _diveManager.Pop();
					_diving = true;
				}
				else
				{
					node2 = _manager.Pop();
					_diving = false;
					_keepBasis = false;
					if (integerOptimalGoalValues != null && HasGoal() && GetGoalCount() == 1 && _manager.SearchStrategy == SearchStrategy.BestBound && (node2.LowerBoundGoalValue[0] - integerOptimalGoalValues[0]).AbsoluteValue < Params.MixedIntegerGapTolerance * (1 + node2.LowerBoundGoalValue[0].AbsoluteValue))
					{
						Logger.LogEvent(11, "Branch: ({0}) {1} vs. {2} mipgap cutoff ({3})", _cvBranch, (double)node2.LowerBoundGoalValue[0], (double)_integerOptimalVariableValues[0], Params.MixedIntegerGapTolerance);
						_manager.Clear();
						continue;
					}
				}
				BoundConstraint boundConstraint = node2.LatestConstraint as BoundConstraint;
				int branchingVariable = boundConstraint?.Variable ?? (-1);
				Rational branchingValue = boundConstraint?.Bound ?? Rational.Indeterminate;
				LogBranch(integerOptimalGoalValues, ref node2, boundConstraint, branchingVariable, branchingValue);
				if (integerOptimalGoalValues != null && (!HasGoal() || node2.LowerBoundGoalValue >= integerOptimalGoalValues))
				{
					Logger.LogEvent(11, "Branch: ({0}) {1} cutoff1", _cvBranch, HasGoal() ? ((double)node2.LowerBoundGoalValue[0]) : double.NaN);
					EmptyDiveManager();
					continue;
				}
				if (!ResetReducedModel(ref node2, forceReInit: false))
				{
					UpdateInfeasiblePseudoCosts(integerOptimalGoalValues, ref node2, branchingVariable, branchingValue);
					node2.ResetConstraints(this);
					continue;
				}
				ApplyConstraints(ref node2);
				if (_keepBasis)
				{
					_keepBasis = false;
				}
				else
				{
					_slackBasis = _bas.Clone();
					_slackBasis.SetToSlacks();
					_bas.SetTo(_slackBasis);
				}
				presolveNodeCount++;
				if (Params.MixedIntegerNodePresolve && presolveNodeCount < 100 && !NodePresolve(ref node2, ref presolveNodeCount))
				{
					UpdateInfeasiblePseudoCosts(integerOptimalGoalValues, ref node2, branchingVariable, branchingValue);
					node2.ResetConstraints(this);
					continue;
				}
				LinearResult relaxationResult = RunSimplex(integerOptimalGoalValues, restart: true);
				if (!SimplexSolver.IsComplete(relaxationResult))
				{
					return false;
				}
				cutGenerationNodeCount++;
				bool appliedCuttingPlane = false;
				if (Params.MixedIntegerGenerateCuts && cutGenerationNodeCount < 100 && relaxationResult == LinearResult.Optimal)
				{
					appliedCuttingPlane = GenerateCuttingPlanes(integerOptimalGoalValues, ref node2, ref relaxationResult);
					if (!SimplexSolver.IsComplete(relaxationResult))
					{
						return false;
					}
				}
				if (relaxationResult == LinearResult.Optimal && integerOptimalGoalValues != null && (!HasGoal() || _currentOptimalGoalValues >= integerOptimalGoalValues))
				{
					UpdatePseudoCosts(ref node2, boundConstraint, branchingVariable, branchingValue);
					Logger.LogEvent(11, "Branch: ({0}) {1} cutoff2", _cvBranch, (double)_currentOptimalGoalValues[0]);
					EmptyDiveManager();
					node2.ResetConstraints(this);
					continue;
				}
				if (relaxationResult == LinearResult.Optimal)
				{
					UpdatePseudoCosts(ref node2, boundConstraint, branchingVariable, branchingValue);
					bool flag = false;
					lock (_syncRoot)
					{
						flag = _solver.IsBetterSolution(this, SimplexSolver.GetSolutionQuality(this), LinearResult.Optimal, fMip: true);
					}
					if (!flag)
					{
						node2.ResetConstraints(this);
						continue;
					}
					if (!FindBranchingVariable(node2, integerOptimalGoalValues, out var strategy, out var branchingVariable2, out var branchingValue2))
					{
						UpdateIntegerOptimalGoalValues(ref integerOptimalGoalValues, ref presolveNodeCount, ref cutGenerationNodeCount);
						EmptyDiveManager();
					}
					else
					{
						CreateNodes(integerOptimalGoalValues, strategy, ref node2, appliedCuttingPlane, branchingVariable2, branchingValue2);
					}
				}
				else
				{
					UpdateInfeasiblePseudoCosts(integerOptimalGoalValues, ref node2, branchingVariable, branchingValue);
					EmptyDiveManager();
				}
				node2.ResetConstraints(this);
			}
			return true;
		}

		/// <summary>
		/// Empties the dive manager into the general node manager.
		/// </summary>
		private void EmptyDiveManager()
		{
			while (_diveManager.Count > 0)
			{
				_manager.Add(_diveManager.Pop());
			}
		}

		/// <summary>
		/// Logs information about the branch.
		/// </summary>
		/// <param name="integerOptimalGoalValues">The current best integer solution.</param>
		/// <param name="node">The current node.</param>
		/// <param name="latestBoundConstraint">The latest bound constraint.</param>
		/// <param name="branchingVariable">The latest branching variable.</param>
		/// <param name="branchingValue">The latest branching value.</param>
		private void LogBranch(OptimalGoalValues integerOptimalGoalValues, ref Node node, BoundConstraint latestBoundConstraint, int branchingVariable, Rational branchingValue)
		{
			if (latestBoundConstraint != null)
			{
				Logger.LogEvent(11, "Branch: ({0}) var={1} type={2} num={3} bnd={4} best={5} depth={6} nodes left={7} ({8}) parent={9}", _cvBranch, _solver._mpvidvi[branchingVariable].Key, latestBoundConstraint.GetType().Name, branchingValue, HasGoal() ? ((double)node.LowerBoundGoalValue[0]) : double.NaN, (!HasGoal()) ? double.NaN : ((integerOptimalGoalValues == null) ? double.PositiveInfinity : ((double)integerOptimalGoalValues[0])), node.ConstraintCount, _manager.Count, _diveManager.Count, node.Parent);
			}
		}

		/// <summary>
		/// Re-initializes the reduced model if needed.
		/// </summary>
		/// <param name="node">The current node.</param>
		/// <param name="forceReInit">Indicates whether reinitialization is forced.</param>
		/// <returns>False if the model is detected to be infeasible; true otherwise.</returns>
		private bool ResetReducedModel(ref Node node, bool forceReInit)
		{
			CuttingPlanePool ancestorCutPool = node.GetAncestorCutPool();
			if (ancestorCutPool != null)
			{
				if (forceReInit || ancestorCutPool.Model == null || ancestorCutPool.Model != _solver._mod)
				{
					node.ApplyConstraints(this);
					if (!_solver.ReInit(fSolveMip: true, this))
					{
						node.ResetConstraints(this);
						return false;
					}
					ReInit();
					_solver.IncrementReInitCounter();
					if (forceReInit)
					{
						_solver.IncrementReInitDueToCutsCounter();
					}
					else
					{
						_solver.IncrementReInitDueToNodesCounter();
					}
					ancestorCutPool.Model = _solver._mod;
					_originalReducedModel = _solver._mod;
				}
			}
			else if (_solver._mod != _originalReducedModel)
			{
				_solver.ReInit(fSolveMip: true, this);
				ReInit();
				_solver.IncrementReInitCounter();
				_solver.IncrementReInitDueToNodesCounter();
				_originalReducedModel = _solver._mod;
			}
			return true;
		}

		/// <summary>
		/// Applies the constraints to the node. 
		/// </summary>
		/// <param name="node"></param>
		private void ApplyConstraints(ref Node node)
		{
			CuttingPlanePool ancestorCutPool = node.GetAncestorCutPool();
			if (ancestorCutPool != null)
			{
				ApplyCuttingPlanes(ref node, forceReInit: false);
			}
			else
			{
				node.ApplyConstraints(this);
			}
		}

		/// <summary>
		/// Presolve a node.
		/// </summary>
		/// <param name="node">The node to presolve.</param>
		/// <param name="presolveNodeCount"></param>
		/// <returns>False if infeasibility was detected; true otherwise.</returns>
		private bool NodePresolve(ref Node node, ref int presolveNodeCount)
		{
			if (!_presolve.NodeMipPreSolve(this, ref node, out var tightenRowBoundCount, out var tightenVariableBoundCount))
			{
				return false;
			}
			if (tightenRowBoundCount > 0 || tightenVariableBoundCount > 0)
			{
				presolveNodeCount--;
			}
			return true;
		}

		/// <summary>
		/// Repeatedly try to generate cutting planes, apply them and solve.
		/// </summary>
		/// <param name="integerOptimalGoalValues">The best integer solution found so far.</param>
		/// <param name="node">The current node.</param>
		/// <param name="relaxationResult">The curren relaxation.</param>
		/// <returns>Whether cutting planes were generated.</returns>
		private bool GenerateCuttingPlanes(OptimalGoalValues integerOptimalGoalValues, ref Node node, ref LinearResult relaxationResult)
		{
			int num = 30 - 2 * node.ConstraintCount;
			int num2 = 0;
			while (Params.MixedIntegerGenerateCuts && num2 < num && relaxationResult == LinearResult.Optimal && Branching.HasFractionalVariable(this) && (integerOptimalGoalValues == null || (HasGoal() && !(_currentOptimalGoalValues >= integerOptimalGoalValues))) && CuttingPlanePool.GenerateCuts(this, 1000, ref node, _prm.CutKinds, relaxationResult))
			{
				num2++;
				CuttingPlanePool cuttingPlanePool = node.LatestConstraint as CuttingPlanePool;
				if (Logger.ShouldLog(14))
				{
					Logger.LogEvent(14, "Branch: ({0}) # of Gomory fractional cuts = {1}, # of cover cuts = {2}, # of mixed cover cuts = {3}, # of flow cuts = {4}", _cvBranch, cuttingPlanePool.GomoryFractionalCutCount, cuttingPlanePool.CoverCutCount, cuttingPlanePool.MixedCoverCutCount, cuttingPlanePool.FlowCoverCutCount);
				}
				OptimalGoalValues optimalGoalValues = new OptimalGoalValues(GetGoalCount());
				if (HasGoal())
				{
					optimalGoalValues = _currentOptimalGoalValues.ScaleToUserModel(this);
					if (Logger.ShouldLog(14))
					{
						Logger.LogEvent(14, "Branch: ({0}) bound {1}", _cvBranch, (double)optimalGoalValues[0]);
					}
				}
				if (!ApplyCuttingPlanes(ref node, forceReInit: true))
				{
					relaxationResult = LinearResult.InfeasiblePrimal;
					return false;
				}
				relaxationResult = RunSimplex(integerOptimalGoalValues, restart: true);
				if (HasGoal())
				{
					OptimalGoalValues optimalGoalValues2 = _currentOptimalGoalValues.ScaleToUserModel(this);
					if (Logger.ShouldLog(14))
					{
						Logger.LogEvent(14, "Branch: ({0}) new bound {1}", _cvBranch, (double)optimalGoalValues2[0]);
					}
				}
			}
			return num2 > 0;
		}

		/// <summary>
		/// Applies the curring planes associated with the node. Re-initializes the reduced model if needed.
		/// </summary>
		/// <param name="node">The current node.</param>
		/// <param name="forceReInit">Whether to force a re-initialization of the reduced model.</param>
		/// <returns>False if the model is detected to be infeasible; true otherwise.</returns>
		private bool ApplyCuttingPlanes(ref Node node, bool forceReInit)
		{
			if (forceReInit && !ResetReducedModel(ref node, forceReInit: true))
			{
				return false;
			}
			node.ApplyConstraints(this);
			return true;
		}

		/// <summary>
		/// Registers a new best integer solution.
		/// </summary>
		/// <param name="integerOptimalGoalValues">The best integer solution.</param>
		/// <param name="presolveNodeCount"></param>
		/// <param name="cutGenerationNodeCount"></param>
		private void UpdateIntegerOptimalGoalValues(ref OptimalGoalValues integerOptimalGoalValues, ref int presolveNodeCount, ref int cutGenerationNodeCount)
		{
			lock (_syncRoot)
			{
				_solver.RegisterSolution(this, LinearResult.Optimal, fMip: true);
				SetIntegerOptimalVariableValues();
				_mipResult = LinearResult.Optimal;
			}
			integerOptimalGoalValues = _currentOptimalGoalValues.ScaleToUserModel(this).Clone();
			if (HasGoal())
			{
				_gap = 100 * ((integerOptimalGoalValues[0] - _relaxationOptimalGoalValues[0]) / (_relaxationOptimalGoalValues[0].AbsoluteValue + 1E-06));
			}
			if (_manager.SearchStrategy != Params.MixedIntegerSearchStrategy && Params.MixedIntegerSearchStrategy != SearchStrategy.DepthFirst)
			{
				_manager.SwitchTo(Params.MixedIntegerSearchStrategy);
				presolveNodeCount = 0;
				cutGenerationNodeCount = 0;
			}
			Logger.LogEvent(12, "Branch: ({0}) best={1} nodes left={2} ({3})", _cvBranch, HasGoal() ? ((double)integerOptimalGoalValues[0]) : double.NaN, _manager.Count, _diveManager.Count);
		}

		/// <summary>
		/// Creates new chid nodes.
		/// </summary>
		/// <param name="integerOptimalGoalValues"></param>
		/// <param name="strategy"></param>
		/// <param name="node">The parent node.</param>
		/// <param name="appliedCuttingPlane">Whether cutting planes have been applied.</param>
		/// <param name="newBranchingVariable">The branching variable.</param>
		/// <param name="newBranchingValue">The branching value.</param>
		private void CreateNodes(OptimalGoalValues integerOptimalGoalValues, BranchingStrategy strategy, ref Node node, bool appliedCuttingPlane, int newBranchingVariable, Rational newBranchingValue)
		{
			if (HasGoal())
			{
				OptimalGoalValues optimalGoalValues = _currentOptimalGoalValues.ScaleToUserModel(this);
				Rational rational = Branching.BestUpEstimate(node, _pseudoCosts, optimalGoalValues[0], newBranchingVariable);
				Node upChild = new Node(_cvBranch, optimalGoalValues, rational, new LowerBoundConstraint(newBranchingVariable, newBranchingValue, node.LatestConstraint));
				Rational rational2 = Branching.BestDownEstimate(node, _pseudoCosts, optimalGoalValues[0], newBranchingVariable);
				Node downChild = new Node(_cvBranch, optimalGoalValues, rational2, new UpperBoundConstraint(newBranchingVariable, newBranchingValue, node.LatestConstraint));
				AddNodes(integerOptimalGoalValues, strategy, newBranchingVariable, newBranchingValue, ref node, rational, ref upChild, rational2, ref downChild);
				if (appliedCuttingPlane)
				{
					_keepBasis = false;
				}
			}
			else
			{
				Node node2 = new Node(_cvBranch, new OptimalGoalValues(0), 0, new LowerBoundConstraint(newBranchingVariable, newBranchingValue, node.LatestConstraint));
				Node node3 = new Node(_cvBranch, new OptimalGoalValues(0), 0, new UpperBoundConstraint(newBranchingVariable, newBranchingValue, node.LatestConstraint));
				_manager.Add(node2);
				_manager.Add(node3);
			}
		}

		/// <summary>
		/// Adds new nodes to the node managers.
		/// </summary>
		/// <param name="integerOptimalGoalValues"></param>
		/// <param name="strategy"></param>
		/// <param name="newBranchingVariable"></param>
		/// <param name="newBranchingValue"></param>
		/// <param name="node">The parent node.</param>
		/// <param name="upChildExpectedGoalValue">The expected goal value of the first child node.</param>
		/// <param name="upChild">The first child node.</param>
		/// <param name="downChildExpectedGoalValue">The expected goal value of the second child node.</param>
		/// <param name="downChild">The second child node.</param>
		private void AddNodes(OptimalGoalValues integerOptimalGoalValues, BranchingStrategy strategy, int newBranchingVariable, Rational newBranchingValue, ref Node node, Rational upChildExpectedGoalValue, ref Node upChild, Rational downChildExpectedGoalValue, ref Node downChild)
		{
			bool flag = strategy == BranchingStrategy.LargestPseudoCost || strategy == BranchingStrategy.MostFractional || strategy == BranchingStrategy.StrongCost || strategy == BranchingStrategy.VectorLength;
			Node childNode;
			Node childNode2;
			if (PreferUpChild(strategy, newBranchingVariable, newBranchingValue, upChildExpectedGoalValue, ref upChild, downChildExpectedGoalValue, ref downChild))
			{
				childNode = upChild;
				childNode2 = downChild;
			}
			else
			{
				childNode = downChild;
				childNode2 = upChild;
			}
			if (flag)
			{
				if (ShouldDive(integerOptimalGoalValues, ref childNode2))
				{
					_diveManager.Add(childNode2);
					_keepBasis = true;
				}
				else
				{
					_manager.Add(childNode2);
				}
				if (ShouldDive(integerOptimalGoalValues, ref childNode))
				{
					_diveManager.Add(childNode);
					_keepBasis = true;
				}
				else
				{
					_manager.Add(childNode);
				}
			}
			else
			{
				if (ShouldDive(integerOptimalGoalValues, ref childNode))
				{
					_diveManager.Add(childNode);
					_keepBasis = true;
				}
				_manager.Add(childNode2);
				_manager.Add(childNode);
			}
		}

		/// <summary>
		/// Checks whether the node is worth investigating by diving.
		/// </summary>
		/// <param name="integerOptimalGoalValues"></param>
		/// <param name="childNode"></param>
		/// <returns></returns>
		private bool ShouldDive(OptimalGoalValues integerOptimalGoalValues, ref Node childNode)
		{
			if (_diving)
			{
				return true;
			}
			if (_manager.SearchStrategy != SearchStrategy.DepthFirst)
			{
				if (integerOptimalGoalValues != null)
				{
					return childNode.ExpectedGoalValue < integerOptimalGoalValues[0] * 0.9;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Updates the pseudo costs.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="latestBoundConstraint"></param>
		/// <param name="branchingVariable"></param>
		/// <param name="branchingValue"></param>
		private void UpdatePseudoCosts(ref Node node, BoundConstraint latestBoundConstraint, int branchingVariable, Rational branchingValue)
		{
			if (HasGoal())
			{
				if (latestBoundConstraint is LowerBoundConstraint)
				{
					_pseudoCosts.UpdateUpPseudoCost(branchingVariable, branchingValue.GetCeiling() - branchingValue, _currentOptimalGoalValues.ScaleToUserModel(this)[0] - node.LowerBoundGoalValue[0]);
				}
				if (latestBoundConstraint is UpperBoundConstraint)
				{
					_pseudoCosts.UpdateDownPseudoCost(branchingVariable, branchingValue - branchingValue.GetFloor(), _currentOptimalGoalValues.ScaleToUserModel(this)[0] - node.LowerBoundGoalValue[0]);
				}
			}
		}

		/// <summary>
		/// Update the pseudo costs when the branaching makes the problem infeasible.
		/// </summary>
		/// <param name="integerOptimalGoalValues"></param>
		/// <param name="node"></param>
		/// <param name="branchingVariable"></param>
		/// <param name="branchingValue"></param>
		private void UpdateInfeasiblePseudoCosts(OptimalGoalValues integerOptimalGoalValues, ref Node node, int branchingVariable, Rational branchingValue)
		{
			if (HasGoal())
			{
				Rational rational = ((integerOptimalGoalValues != null) ? (integerOptimalGoalValues[0] - node.LowerBoundGoalValue[0]) : ((Rational)int.MaxValue));
				BoundConstraint boundConstraint = node.LatestConstraint as BoundConstraint;
				if (boundConstraint is LowerBoundConstraint)
				{
					_pseudoCosts.UpdateUpPseudoCost(branchingVariable, branchingValue.GetCeiling() - branchingValue, rational);
				}
				if (boundConstraint is UpperBoundConstraint)
				{
					_pseudoCosts.UpdateDownPseudoCost(branchingVariable, branchingValue - branchingValue.GetFloor(), rational);
				}
				Logger.LogEvent(11, "Branch: ({0}) infeasible. Increase {1}.", _cvBranch, (double)rational);
			}
		}

		/// <summary>
		/// Indicates whether the up child should be investigated first.
		/// </summary>
		/// <param name="strategy"></param>
		/// <param name="newBranchingVariable"></param>
		/// <param name="newBranchingValue"></param>
		/// <param name="upChildExpectedGoalValue"></param>
		/// <param name="upChild"></param>
		/// <param name="downChildExpectedGoalValue"></param>
		/// <param name="downChild"></param>
		/// <returns></returns>
		private bool PreferUpChild(BranchingStrategy strategy, int newBranchingVariable, Rational newBranchingValue, Rational upChildExpectedGoalValue, ref Node upChild, Rational downChildExpectedGoalValue, ref Node downChild)
		{
			if (_diving && _integerOptimalVariableValues != null && Model.GetVar(newBranchingVariable) != -1 && Model.IsBinary(Model.GetVar(newBranchingVariable)))
			{
				if (_integerOptimalVariableValues[Model.GetVar(newBranchingVariable)] >= newBranchingValue)
				{
					return true;
				}
				return false;
			}
			if (strategy == BranchingStrategy.LeastFractional)
			{
				return newBranchingValue >= 0.5;
			}
			return upChildExpectedGoalValue < downChildExpectedGoalValue;
		}

		/// <summary>
		/// Gets the values of the variables.
		/// </summary>
		/// <returns></returns>
		private void SetIntegerOptimalVariableValues()
		{
			if (_integerOptimalVariableValues == null || _integerOptimalVariableValues.Length != Model.VarLim)
			{
				_integerOptimalVariableValues = new Rational[Model.VarLim];
			}
			for (int i = 0; i < Model.VarLim; i++)
			{
				Rational rational = Model.MapValueFromVarToVid(i, AlgorithmExact.GetVarValue(i));
				_integerOptimalVariableValues[i] = rational;
			}
		}

		/// <summary>
		/// Checks whether there is a variable to branch on and returns it if one is found.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="integerOptimalGoalValues">The current best integer solutions.</param>
		/// <param name="strategy"></param>
		/// <param name="branchingVariable">A variable that can be branched on.</param>
		/// <param name="branchingValue">The value of the variable to branch on.</param>
		/// <returns>True if a variable to branch on is found; false otherwise.</returns>
		/// <remarks>
		/// Variables that can be branched on are variables restricted to be integers and
		/// whose current values are not integers. 
		/// </remarks>
		private bool FindBranchingVariable(Node node, OptimalGoalValues integerOptimalGoalValues, out BranchingStrategy strategy, out int branchingVariable, out Rational branchingValue)
		{
			if (integerOptimalGoalValues == null || integerOptimalGoalValues.Count == 0 || _diving)
			{
				strategy = Params.MixedIntegerBranchingStrategyPreFeasibility;
			}
			else
			{
				strategy = Params.MixedIntegerBranchingStrategyPostFeasibility;
			}
			if (strategy == BranchingStrategy.Automatic)
			{
				if (integerOptimalGoalValues == null || integerOptimalGoalValues.Count == 0)
				{
					strategy = BranchingStrategy.MostFractional;
				}
				else if (_diving)
				{
					strategy = BranchingStrategy.LargestPseudoCost;
				}
				else
				{
					strategy = BranchingStrategy.LargestPseudoCost;
				}
			}
			bool preferBinaryVariables = _diving || integerOptimalGoalValues == null || integerOptimalGoalValues.Count == 0;
			if (strategy == BranchingStrategy.MostFractional)
			{
				return Branching.FindMostFractionalVariable(this, preferBinaryVariables, out branchingVariable, out branchingValue);
			}
			if (strategy == BranchingStrategy.LeastFractional)
			{
				return Branching.FindLeastFractionalVariable(this, preferBinaryVariables, out branchingVariable, out branchingValue);
			}
			if (strategy == BranchingStrategy.SmallestPseudoCost)
			{
				return Branching.FindSmallestPeudoCost(node, _pseudoCosts, preferBinaryVariables, out branchingVariable, out branchingValue);
			}
			if (strategy == BranchingStrategy.LargestPseudoCost || strategy == BranchingStrategy.StrongCost)
			{
				return Branching.FindLargestPeudoCost(node, _pseudoCosts, preferBinaryVariables, out branchingVariable, out branchingValue);
			}
			return Branching.FindVectorLengthVariable(node, _pseudoCosts, preferBinaryVariables, out branchingVariable, out branchingValue);
		}

		/// <summary> The gate method for sos solving
		/// </summary>
		/// <param name="resLp">lp relaxation solution</param>
		private void DoSOSBranchAndBound(LinearResult resLp)
		{
			if (resLp != LinearResult.Optimal)
			{
				_sosResult = LinearResult.Invalid;
				return;
			}
			_boundManager.InitBounds(this);
			_branchingManager = new SOSNodeManager();
			foreach (SOSRowNode item in EnumerateSOSBranchRow())
			{
				_branchingManager.Push(item);
				if (!BranchAndBoundSOS())
				{
					_sosResult = LinearResult.InfeasiblePrimal;
					break;
				}
			}
		}

		private bool BranchAndBoundSOS()
		{
			LogSource.SimplexTracer.TraceEvent(TraceEventType.Start, 0, "enter BranchAndBoundSOS");
			_sosOgvBest = null;
			_sosNode = null;
			while (!_branchingManager.IsEmpty)
			{
				SOSRowNode sOSRowNode = _branchingManager.Pop();
				if (!SetVariablesToZero(sOSRowNode, out var stoppedAtVid))
				{
					if (stoppedAtVid >= 0)
					{
						ResetVariableBounds(sOSRowNode, stoppedAtVid);
						if (sOSRowNode._lower)
						{
							sOSRowNode._lower = false;
							_branchingManager.Push(sOSRowNode);
						}
					}
					continue;
				}
				_bas.SetTo(sOSRowNode._bas);
				LinearResult linearResult = RunSimplex(null, restart: true);
				if (!SimplexSolver.IsComplete(linearResult))
				{
					return false;
				}
				if (linearResult == LinearResult.Optimal && (_sosOgvBest == null || _currentOptimalGoalValues.CompareTo(_sosOgvBest) <= 0))
				{
					SOSRowNode sOSRowNode2 = sOSRowNode.Clone();
					if (sOSRowNode._lower)
					{
						sOSRowNode2._first = ((sOSRowNode._sosType == SpecialOrderedSetType.SOS2) ? sOSRowNode._split : (sOSRowNode._split + 1));
					}
					else
					{
						sOSRowNode2._last = sOSRowNode._split;
					}
					if (ComputeBreakingPoints(sOSRowNode2))
					{
						_branchingManager.Push(sOSRowNode2);
					}
					else
					{
						if (Logger.ShouldLog(11))
						{
							Logger.LogEvent(11, "SOS Branch: row: {0} with current Optimal value: {1}", _solver.GetKeyFromIndex(sOSRowNode._vidRow), (double)_currentOptimalGoalValues[0]);
						}
						if (_currentOptimalGoalValues.CompareTo(_sosOgvBest) < 0)
						{
							_sosNode = sOSRowNode.Clone();
							Statics.Swap(ref _sosOgvBest, ref _currentOptimalGoalValues);
						}
					}
				}
				ResetVariableBounds(sOSRowNode);
				if (sOSRowNode._lower)
				{
					sOSRowNode._lower = false;
					_branchingManager.Push(sOSRowNode);
				}
			}
			if (_sosOgvBest != null)
			{
				FixVariables();
				Statics.Swap(ref _sosOgvBest, ref _currentOptimalGoalValues);
				LogSource.SimplexTracer.TraceEvent(TraceEventType.Stop, 0, "exit BranchAndBoundSOS");
				return true;
			}
			return false;
		}

		private void FixVariables()
		{
			_bas.SetTo(_sosNode._bas);
			SetVariablesToZero(_sosNode, out var _);
			RunSimplex(null, restart: true);
		}

		[Conditional("DEBUG")]
		private void DumpRefRow(int vidRow)
		{
		}

		[Conditional("DEBUG")]
		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		private void DumpNode(SOSRowNode node)
		{
		}

		private void SetNonzerosToZeroUpperBound(SOSRowNode node)
		{
			for (int i = node._first; i <= node._last; i++)
			{
				int vid = node._vids[i];
				double num = (double)GetValue(vid);
				if (num == 0.0)
				{
					PushUpperBound(vid);
				}
			}
		}

		/// <summary>Set the relevant variables' upper bound to zero. 
		/// </summary>
		/// <param name="node"></param>
		/// <param name="stoppedAtVid">vid of the var that could not been set to zero, if any, otherwise -1</param>
		/// <returns>true if all that needed to be set to zero are set to zero, false otherwise</returns>
		private bool SetVariablesToZero(SOSRowNode node, out int stoppedAtVid)
		{
			stoppedAtVid = -1;
			FindStartEndIndexes(node, out var start, out var end);
			if (start >= end)
			{
				return false;
			}
			for (int i = 0; i < node._first; i++)
			{
				if (!SetVariableToZero(node, ref stoppedAtVid, i))
				{
					return false;
				}
			}
			for (int j = node._vids.Length - 1; j < node._last; j++)
			{
				if (!SetVariableToZero(node, ref stoppedAtVid, j))
				{
					return false;
				}
			}
			for (int k = start; k < end; k++)
			{
				if (!SetVariableToZero(node, ref stoppedAtVid, k))
				{
					return false;
				}
			}
			return true;
		}

		private bool SetVariableToZero(SOSRowNode node, ref int stoppedAtVid, int index)
		{
			int num = node._vids[index];
			if (!PushUpperBound(num))
			{
				stoppedAtVid = num;
				return false;
			}
			return true;
		}

		private void PopUpperBound(int vid)
		{
			int var = _mod.GetVar(vid);
			BoundManager.ResetUpperBound(var);
		}

		private bool PushUpperBound(int vid)
		{
			int var = _mod.GetVar(vid);
			return BoundManager.SetUpperBound(var, 0);
		}

		private void ResetVariableBounds(SOSRowNode node)
		{
			ResetVariableBounds(node, -1);
		}

		private void ResetVariableBounds(SOSRowNode node, int stopAtVid)
		{
			FindStartEndIndexes(node, out var start, out var end);
			for (int i = 0; i < node._first; i++)
			{
				int num = node._vids[i];
				if (num == stopAtVid)
				{
					return;
				}
				PopUpperBound(num);
			}
			for (int j = node._vids.Length - 1; j < node._last; j++)
			{
				int num2 = node._vids[j];
				if (num2 == stopAtVid)
				{
					return;
				}
				PopUpperBound(num2);
			}
			for (int k = start; k < end; k++)
			{
				int num3 = node._vids[k];
				if (num3 == stopAtVid)
				{
					break;
				}
				PopUpperBound(num3);
			}
		}

		private static void FindStartEndIndexes(SOSRowNode node, out int start, out int end)
		{
			if (node._lower)
			{
				start = node._first;
				end = ((node._sosType == SpecialOrderedSetType.SOS2) ? node._split : (node._split + 1));
			}
			else
			{
				start = node._split + 1;
				end = node._last + 1;
			}
		}

		private Rational GetValue(int vid)
		{
			int var = _mod.GetVar(vid);
			if (var < 0)
			{
				return _mod._mpvidnumDelta[vid];
			}
			int num = 0;
			Rational result = Rational.Indeterminate;
			if ((num = _bas.GetBasisSlot(var)) > 0)
			{
				if (_fUseExact)
				{
					result = _mod.MapValueFromVarToVid(var, AlgorithmExact.GetBasicValue(num));
				}
				else
				{
					Rational num2 = _mod.MapValueFromDoubleToExact(var, AlgorithmDouble.GetBasicValue(num));
					result = _mod.MapValueFromVarToVid(var, num2);
				}
			}
			else
			{
				SimplexVarValKind vvk = _bas.GetVvk(var);
				if (vvk != 0)
				{
					if (_fUseExact)
					{
						result = _mod.MapValueFromVarToVid(var, AlgorithmExact.GetVarBound(var, vvk));
					}
					else
					{
						Rational num3 = _mod.MapValueFromDoubleToExact(var, AlgorithmDouble.GetVarBound(var, vvk));
						result = _mod.MapValueFromVarToVid(var, num3);
					}
				}
			}
			return result;
		}

		private IEnumerable<SOSRowNode> EnumerateSOSBranchRow()
		{
			IEnumerable<int> sos2RowVids = ((_solver._sos2Rows != null) ? _solver._sos2Rows.AsEnumerable() : new int[0]);
			IEnumerable<int> sos1RowVids = ((_solver._sos1Rows != null) ? _solver._sos1Rows.AsEnumerable() : new int[0]);
			var sos2Rows = sos2RowVids.Select((int vidRow) => new
			{
				Vid = vidRow,
				SosType = SpecialOrderedSetType.SOS2
			});
			var sos1Rows = sos1RowVids.Select((int vidRow) => new
			{
				Vid = vidRow,
				SosType = SpecialOrderedSetType.SOS1
			});
			foreach (var sosRow in sos1Rows.Concat(sos2Rows))
			{
				SOSRowNode node = new SOSRowNode(sosRow.Vid, sosRow.SosType, this);
				if (ComputeBreakingPoints(node))
				{
					if (sosRow.SosType == SpecialOrderedSetType.SOS2)
					{
						SOSRowNode sOSRowNode = node.RightClone();
						if (sOSRowNode != null)
						{
							_branchingManager.Push(sOSRowNode);
						}
					}
					if (Logger.ShouldLog(11))
					{
						Logger.LogEvent(11, "SOS Branch: row: ({0}) of type {1}", _solver.GetKeyFromIndex(node._vidRow), sosRow.SosType);
					}
					yield return node;
				}
				else
				{
					_bas.SetTo(node._bas);
					SetNonzerosToZeroUpperBound(node);
					RunSimplex(null, restart: true);
				}
			}
		}

		/// <summary>Compute the breaking points at a SOS ref row
		/// </summary>
		private bool ComputeBreakingPoints(SOSRowNode node)
		{
			int num = -1;
			int num2 = -1;
			int num3 = 0;
			double num4 = 0.0;
			double num5 = 0.0;
			int vidRow = node._vidRow;
			if (!node._sorted)
			{
				for (int i = node._first; i <= node._last; i++)
				{
					int vidVar = node._vids[i];
					double num6 = (double)_solver.GetCoefficient(vidRow, vidVar);
					node._weights[i] = num6;
				}
				Array.Sort(node._weights, node._vids);
				node._sorted = true;
			}
			for (int j = node._first; j <= node._last; j++)
			{
				int vid = node._vids[j];
				double num7 = (double)GetValue(vid);
				double num8 = node._weights[j];
				if (num7 != 0.0)
				{
					if (num < 0)
					{
						num = j;
					}
					num2 = j;
					num3++;
					num4 += num7;
					num5 += num8 * num7;
				}
			}
			if (node._sosType == SpecialOrderedSetType.SOS1)
			{
				if (num2 == num)
				{
					return false;
				}
			}
			else
			{
				if (node._sosType != SpecialOrderedSetType.SOS2)
				{
					throw new NotSupportedException();
				}
				if (num2 == num || num + 1 == num2)
				{
					return false;
				}
			}
			double value = num5 / num4;
			value = Math.Abs(value);
			MarkBreakingPoints(node, value);
			return true;
		}

		private void MarkBreakingPoints(SOSRowNode node, double avgweight)
		{
			int vidRow = node._vidRow;
			for (int i = node._first; i <= node._last; i++)
			{
				int vidVar = node._vids[i];
				double num = Math.Abs((double)_solver.GetCoefficient(vidRow, vidVar));
				if (!(num < avgweight))
				{
					break;
				}
				node._split = i;
			}
			if (node._first == node._split)
			{
				node._split = (node._first + node._last) / 2;
			}
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="solver">The solver associated with the thread.</param>
		/// <param name="tid">The thread ID.</param>
		/// <param name="prm">The thread parameters.</param>
		/// <param name="fForceExact"></param>
		public SimplexTask(SimplexSolver solver, int tid, SimplexSolverParams prm, bool fForceExact)
		{
			_solver = solver;
			_tid = tid;
			_prm = prm;
			_logger = _solver.Logger;
			_mod = _solver._mod;
			_fDual = prm.Algorithm == SimplexAlgorithmKind.Dual;
			_fUseDouble = prm.UseDouble;
			_fUseExact = prm.UseExact || !_fUseDouble || fForceExact;
			_fShiftBounds = _prm.ShiftBounds;
			_bas = new SimplexFactoredBasis(this, _mod);
			_basisKind = _prm.InitialBasisKind;
			_boundManager = new BoundManager(this);
			_infeasibleCount = -1;
			_ = Model.IsSOS;
			if (_fUseExact)
			{
				_pes = (_fDual ? ((AlgorithmRational)new DualExact(this)) : ((AlgorithmRational)new PrimalExact(this)));
				_pes.Init(_mod, _bas);
			}
			else
			{
				_pes = null;
			}
			if (_fUseDouble)
			{
				_pds = (_fDual ? ((AlgorithmDouble)new DualDouble(this)) : ((AlgorithmDouble)new PrimalDouble(this)));
				_pds.Init(_mod, _bas);
			}
			else
			{
				if (SimplexBasisKind.Freedom == _basisKind)
				{
					_basisKind = SimplexBasisKind.Slack;
				}
				_pds = null;
			}
			switch (_basisKind)
			{
			case SimplexBasisKind.Freedom:
				_bas.SetToFreedom();
				break;
			case SimplexBasisKind.Current:
				_bas.SetToVidValues(_solver._mpvidnum);
				break;
			case SimplexBasisKind.Crash:
				_bas.SetToCrash();
				break;
			default:
				_bas.SetToSlacks();
				break;
			}
			if (_rgpi != null)
			{
				_rgpi.Clear();
			}
			_cvBranch = 0;
			_mipResult = LinearResult.Invalid;
			_fRowUsed = null;
		}

		/// <summary> Create a Dual Simplex Solver Task to be used by MIP
		/// </summary>
		/// <param name="node"></param>
		public SimplexTask(MipNode node)
			: this(node, node.Task.Model)
		{
		}

		/// <summary> Create a Dual Simplex Solver Task to be used by MIP
		/// </summary>
		/// <param name="parentNode">parent node</param>
		/// <param name="mod">reduced model for this task</param>
		public SimplexTask(MipNode parentNode, SimplexReducedModel mod)
		{
			SimplexBasis basis = parentNode.Task.Basis;
			_mod = mod;
			_solver = mod.UserModel;
			_tid = -1;
			_prm = new SimplexSolverParams(parentNode.Task.Params);
			_prm.UseDouble = true;
			_prm.Algorithm = SimplexAlgorithmKind.Dual;
			_logger = _solver.Logger;
			_fDual = true;
			_fUseDouble = true;
			if (basis._rgvarBasic.Length < Model.RowLim)
			{
				_bas = new SimplexFactoredBasis(this, _mod);
			}
			else
			{
				_bas = new SimplexFactoredBasis(this, basis);
			}
			_basisKind = SimplexBasisKind.Current;
			_boundManager = new BoundManager(this, parentNode.Task);
			_infeasibleCount = -1;
			_pds = new DualDouble(this);
			_pds.Init(_mod, _bas);
			_bas.SetToSlacks();
			if (_rgpi != null)
			{
				_rgpi.Clear();
			}
			_mipResult = LinearResult.Invalid;
		}

		/// <summary>
		/// Reinitialize the internal data structures.
		/// </summary>
		internal void ReInit()
		{
			ReInit(SimplexBasisKind.Slack);
		}

		/// <summary>
		/// Reinitialize the internal data structures.
		/// </summary>
		internal void ReInit(SimplexBasisKind basKind)
		{
			_mod = _solver._mod;
			_boundManager.InitBounds(this);
			_bas = new SimplexFactoredBasis(this, _mod);
			if (_fUseExact)
			{
				_pes.Init(_mod, _bas);
			}
			else
			{
				_pes = null;
			}
			if (_fUseDouble)
			{
				_pds.Init(_mod, _bas);
			}
			else
			{
				_pds = null;
			}
			if (basKind == SimplexBasisKind.Current)
			{
				_bas.SetToVidValues(_solver._mpvidnum);
			}
			else
			{
				_bas.SetToSlacks();
			}
		}

		public void CheckDone()
		{
			if (_solver._fEndSolve || (Params.QueryAbort != null && Params.QueryAbort()) || Params.ShouldAbort(null))
			{
				_solver._fEndSolve = true;
				throw new SimplexThreadAbortException();
			}
		}

		public virtual void LaunchThread(bool fSolveMip)
		{
			_thr = new Thread((ThreadStart)delegate
			{
				Run(fSolveMip);
			});
			_thr.Start();
		}

		public virtual void Run(bool fSolveMip)
		{
			Thread.CurrentThread.CurrentCulture = _solver._cultureInfo;
			Thread.CurrentThread.CurrentUICulture = _solver._cultureUIInfo;
			try
			{
				if (!fSolveMip)
				{
					TraceStats();
					LinearResult linearResult = RunSimplex(null, restart: false);
					CheckDone();
					if (_solver.IsSpecialOrderedSet && !_fSOSFastPath)
					{
						DoSOSBranchAndBound(linearResult);
						if (_sosResult == LinearResult.InfeasiblePrimal)
						{
							linearResult = _sosResult;
						}
					}
					_solver.RegisterSolution(this, linearResult, fMip: false);
					if (linearResult != LinearResult.Optimal && linearResult != LinearResult.Interrupted && Params.GetInfeasibilityReport)
					{
						SimplexInfeasibilityDeletionFilter simplexInfeasibilityDeletionFilter = new SimplexInfeasibilityDeletionFilter(this);
						InfeasibilityReport value = (InfeasibilityReport)simplexInfeasibilityDeletionFilter.Generate();
						Interlocked.Exchange(ref _solver._infeasibleReport, value);
					}
					_solver.EndSolve();
				}
				else
				{
					_workingMipSolver = new MipSolver(this);
					_workingMipSolver.Solve();
				}
			}
			catch (SimplexThreadAbortException)
			{
			}
			finally
			{
				if (_workingMipSolver != null)
				{
					_cvBranch = _workingMipSolver.BranchCount;
					_gap = _workingMipSolver.ComputeUserModelGap();
				}
				_workingMipSolver = null;
			}
		}

		/// <summary>
		/// This is called to just compute the initial exact variable values and goal values.
		/// It is typically only called when PreSolve finds an infeasibility.
		/// </summary>
		public void ComputeExactVars()
		{
			if (_currentOptimalGoalValues == null || _currentOptimalGoalValues.Count != _mod.GoalCount)
			{
				_currentOptimalGoalValues = new OptimalGoalValues(_mod.GoalCount);
			}
			else
			{
				_currentOptimalGoalValues.Clear();
			}
			_pes.ComputeExactVars();
		}

		internal LinearResult RunSimplex(OptimalGoalValues ogvMin, bool restart)
		{
			if (_currentOptimalGoalValues == null || _currentOptimalGoalValues.Count != _mod.GoalCount)
			{
				_currentOptimalGoalValues = new OptimalGoalValues(_mod.GoalCount);
			}
			else
			{
				_currentOptimalGoalValues.Clear();
			}
			if (_rgpi != null)
			{
				_rgpi.Clear();
				_cpiNonDegen = 0;
				_cpiCheckForCycle = 20;
				_numStallThreshold = Rational.Get(1, 268435456);
			}
			for (int i = 0; i < _mod.GoalCount; i++)
			{
				int goalVar = _mod.GetGoalVar(i);
				if (_bas.GetVvk(goalVar) == SimplexVarValKind.Fixed)
				{
					Rational lowerBound = BoundManager.GetLowerBound(goalVar);
					Rational upperBound = BoundManager.GetUpperBound(goalVar);
					if (!(lowerBound == upperBound))
					{
						SimplexVarValKind vvkNew = ((upperBound.AbsoluteValue < lowerBound.AbsoluteValue) ? SimplexVarValKind.Upper : (lowerBound.IsFinite ? SimplexVarValKind.Lower : SimplexVarValKind.Zero));
						_bas.MinorPivot(goalVar, vvkNew);
					}
				}
			}
			if (!_fUseDouble)
			{
				_pes.RunSimplex(int.MaxValue, fStopAtNextGoal: false, ogvMin, out var res);
				return res;
			}
			if (!_fUseExact)
			{
				_pds.RunSimplex(fStopAtNextGoal: false, restart, out var res2);
				return res2;
			}
			int num = 2;
			LinearResult res4;
			while (true)
			{
				_pds.RunSimplex(fStopAtNextGoal: true, restart, out var res3);
				CheckDone();
				if (!SimplexSolver.IsComplete(res3))
				{
					return LinearResult.Interrupted;
				}
				if (_pes.RunSimplex(num, fStopAtNextGoal: true, ogvMin, out res4))
				{
					break;
				}
				CheckDone();
				num += 2;
			}
			return res4;
		}

		private void TraceStats()
		{
			LogSource.SimplexTracer.TraceEvent(TraceEventType.Information, 0, "Variable Count:{0}, InnerSlackCount:{1}, RowCount:{2}, NonZeros:{3}, Eliminated Slack Variables:{4}", _solver.VariableCount, _solver.InnerSlackCount, _solver.RowCount, _solver.CoefficientCount - _solver.RowCount, _solver.InnerRowCount - _solver.InnerSlackCount);
		}

		public void RecordPivot(ISimplexPivotInformation ps, SimplexVarValKind vvkLeave)
		{
			if (!Logger.ShouldLog(8) && !Logger.ShouldLog(9))
			{
				return;
			}
			if (_rgpi == null)
			{
				_rgpi = new List<PivotInfo>();
			}
			PivotInfo item = new PivotInfo(ps.IsDouble, ps.VarEnter, ps.VarLeave, ps.VvkEnter, vvkLeave, ps.Scale);
			if (Logger.ShouldLog(8))
			{
				if (item._varAdd != item._varDel)
				{
					Logger.LogEvent(8, "Pivot: {0}) {1} => {2} scale={3} delta={4}", _rgpi.Count, item._varDel, item._varAdd, (double)item._scale, (double)ps.Determinant);
				}
				else
				{
					Logger.LogEvent(8, "Minor Pivot: {0}) {1} scale={2}", _rgpi.Count, item._varDel, (double)item._scale);
				}
			}
			_rgpi.Add(item);
			if (!Logger.ShouldLog(9))
			{
				return;
			}
			if (ps.Scale > 0 && (!ps.IsDouble || ps.Scale.AbsoluteValue > _numStallThreshold))
			{
				_cpiNonDegen = _rgpi.Count;
				_cpiCheckForCycle = 20;
			}
			else
			{
				if (_rgpi.Count - _cpiNonDegen < _cpiCheckForCycle)
				{
					return;
				}
				if (_mpvarvvkCheck == null || _mpvarvvkCheck.Length < _mod.VarLim)
				{
					_mpvarvvkCheck = new SimplexVarValKind[_mod.VarLim];
				}
				int num = _mod.VarLim;
				while (--num >= 0)
				{
					_mpvarvvkCheck[num] = _bas.GetVvk(num);
				}
				int num2 = 0;
				int num3 = _rgpi.Count;
				while (--num3 >= _cpiNonDegen)
				{
					item = _rgpi[num3];
					if (_mpvarvvkCheck[item._varDel] == _bas.GetVvk(item._varDel))
					{
						num2++;
					}
					_mpvarvvkCheck[item._varDel] = SimplexVarValKind.Basic;
					if (_mpvarvvkCheck[item._varDel] == _bas.GetVvk(item._varDel))
					{
						num2--;
					}
					if (_mpvarvvkCheck[item._varAdd] == _bas.GetVvk(item._varAdd))
					{
						num2++;
					}
					_mpvarvvkCheck[item._varAdd] = item._vvkAdd;
					if (_mpvarvvkCheck[item._varAdd] == _bas.GetVvk(item._varAdd))
					{
						num2--;
					}
					if (num2 <= 0)
					{
						Logger.LogEvent(9, Resources.Cycle01, _rgpi.Count - 1, num3);
						break;
					}
				}
				_cpiNonDegen = _rgpi.Count;
				_cpiCheckForCycle += 20;
			}
		}

		public void ResetIpiMin()
		{
			_cpiNonDegen = ((_rgpi == null) ? 1 : (_rgpi.Count + 1));
			_cpiCheckForCycle = 20;
		}

		public Rational[] GetTempArrayExact(int cnum, bool fClear)
		{
			return GetTempArrayCore(ref _listTempExact, cnum, fClear);
		}

		public double[] GetTempArrayDbl(int cnum, bool fClear)
		{
			return GetTempArrayCore(ref _listTempDbl, cnum, fClear);
		}

		protected static Number[] GetTempArrayCore<Number>(ref List<Number[]> list, int cnum, bool fClear)
		{
			if (list != null && list.Count > 0)
			{
				int num = -1;
				int num2 = int.MaxValue;
				int index = -1;
				int num3 = int.MaxValue;
				int num4 = list.Count;
				while (--num4 >= 0)
				{
					Number[] array = list[num4];
					int num5 = array.Length;
					if (num5 == cnum)
					{
						list.RemoveAt(num4);
						if (fClear)
						{
							Array.Clear(array, 0, cnum);
						}
						return array;
					}
					if (num3 > num5)
					{
						num3 = num5;
						index = num4;
					}
					if (num2 > num5 && num5 > cnum)
					{
						num2 = num5;
						num = num4;
					}
				}
				if (num >= 0)
				{
					Number[] array2 = list[num];
					list.RemoveAt(num);
					if (fClear)
					{
						Array.Clear(array2, 0, cnum);
					}
					return array2;
				}
				list.RemoveAt(index);
			}
			return new Number[cnum];
		}

		public void ReleaseTempArray(ref Rational[] rgnum)
		{
			if (_listTempExact == null)
			{
				_listTempExact = new List<Rational[]>();
			}
			_listTempExact.Add(rgnum);
			rgnum = null;
		}

		public void ReleaseTempArray(ref double[] rgnum)
		{
			if (_listTempDbl == null)
			{
				_listTempDbl = new List<double[]>();
			}
			_listTempDbl.Add(rgnum);
			rgnum = null;
		}

		/// <summary>
		/// This computes the row of the tableau where vid is currently the basic variable in that row after solve.
		/// Of course we never realize the full tableau. Note that the coef of the basic variable is 1, not -1.
		/// </summary>
		internal VectorRational ComputeTableauRow(int vid)
		{
			if (_vecRowUser == null || _vecRowUser.RcCount < Solver.ColCount)
			{
				_vecRowUser = new VectorRational(Solver.ColCount);
			}
			else
			{
				_vecRowUser.Clear();
			}
			_vecRowReduced = Model.ComputeTableauRow(vid, _bas);
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecRowReduced);
			while (iter.IsValid)
			{
				Rational value = iter.Value;
				int vid2 = Model.GetVid(iter.Rc);
				_vecRowUser.SetCoefNonZero(vid2, value);
				iter.Advance();
			}
			return _vecRowUser;
		}

		/// <summary>
		/// Initialized the flags array in this thread: create/resize the array, set all elements to 0.
		/// </summary>
		internal void InitRowUsedFlags()
		{
			if (_fRowUsed == null)
			{
				_fRowUsed = new int[_solver.RowCount];
				return;
			}
			if (_fRowUsed.Length < _solver.RowCount)
			{
				Array.Resize(ref _fRowUsed, _solver.RowCount);
			}
			Array.Clear(_fRowUsed, 0, _fRowUsed.Length);
		}

		/// <summary>
		/// Set the bit "kind" of byte "vidRow" in the array of flags that indicates which user rows have been used to generate cuts.
		/// </summary>
		/// <param name="vidRow"></param>
		/// <param name="kind"></param>
		internal void SetRowUsedFlag(int vidRow, CutKind kind)
		{
			int rid = _solver._mpvidvi[vidRow].Rid;
			_fRowUsed[rid] |= (int)kind;
		}

		/// <summary>
		/// Test if "vidRow" has been used to generate "kind" cut.
		/// </summary>
		/// <param name="vidRow">The row variable id</param>
		/// <param name="kind">The type of cut</param>
		/// <returns></returns>
		internal bool HasRowUsedFlag(int vidRow, CutKind kind)
		{
			int rid = _solver._mpvidvi[vidRow].Rid;
			return ((uint)_fRowUsed[rid] & (uint)kind) != 0;
		}

		/// <summary>
		/// Checks whether a given user model vid corresponds to a basic var in the reduced model.
		/// </summary>
		/// <param name="vid">User model vid</param>
		/// <returns></returns>
		internal bool IsBasicVarInReducedModel(int vid)
		{
			int var = Model.GetVar(vid);
			if (var == -1)
			{
				return false;
			}
			return Basis.IsBasic(var);
		}
	}
}
