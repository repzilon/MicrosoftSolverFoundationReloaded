#define TRACE
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class MipSolver
	{
		private SimplexTask _solverContext;

		private OptimalGoalValues _mipOgvBest;

		private MipNode _mipBestNode;

		private MipNode _rootNode;

		private OptimalGoalValues _upperBound;

		private CutStore _cutStore;

		private Stack<MipNode> _mipActiveList;

		private int _counter;

		private int _branchCount;

		private SimplexSolveState _solveState;

		private MIPVariableBranchingAlgorithm _branchAlg;

		public SimplexTask Root => _solverContext;

		public SimplexReducedModel Model => _solverContext.Model;

		public SimplexBasis Basis => _solverContext.Basis;

		public CutStore CutStore => _cutStore;

		public MipNode BestNode => _mipBestNode;

		public int BranchCount => _branchCount;

		public OptimalGoalValues BestGoalValues => _mipOgvBest;

		public SimplexSolveState SolveState => _solveState;

		public MipSolver(SimplexTask solverContext)
		{
			_solverContext = solverContext;
			_mipActiveList = new Stack<MipNode>();
			_counter = -1;
			if (solverContext.Model.GoalCount == 0)
			{
				_branchAlg = new MIPMostFractionalVariableBranching();
			}
			else if (solverContext.Model.GoalCount == 1)
			{
				_branchAlg = new MIPSingleGoalPseudocostVariableBranching(solverContext.Model);
			}
			else
			{
				_branchAlg = new MIPMultiGoalPseudocostVariableBranching(solverContext.Model);
			}
			_cutStore = new CutStore(this);
			_solveState = SimplexSolveState.MipSolving;
		}

		public int GetCounter()
		{
			return ++_counter;
		}

		public void Solve()
		{
			_solverContext.CheckDone();
			_mipOgvBest = null;
			_mipBestNode = null;
			_upperBound = null;
			_counter = -1;
			_rootNode = new MipNode(this);
			_mipActiveList.Push(_rootNode);
			SolveCore(out var _, out var _);
			_solverContext.Solver.EndSolve();
		}

		public void CheckDone(LinearResult res)
		{
			Root.CheckDone();
			if (!SimplexSolver.IsComplete(res))
			{
				throw new SimplexThreadAbortException();
			}
		}

		private void RaiseSolvingEvent(SimplexSolveState state)
		{
			_solveState = state;
			RaiseSolvingEvent();
		}

		private void RaiseSolvingEvent()
		{
			_solverContext.Params.RaiseSolvingEvent();
		}

		private void SolveCore(out LinearResult resLp, out LinearResult resMip)
		{
			int num = 0;
			bool flag = Root.Params.MixedIntegerPresolve;
			resLp = LinearResult.Invalid;
			resMip = LinearResult.Invalid;
			LinearResult linearResult = resLp;
			LinearResult rootNodeResLp = resLp;
			try
			{
				while (_mipActiveList.Count > 0)
				{
					MipNode mipNode = _mipActiveList.Pop();
					num++;
					if (flag)
					{
						if (!mipNode.PreSolve())
						{
							rootNodeResLp = (linearResult = LinearResult.InfeasibleOrUnbounded);
							continue;
						}
						flag = false;
					}
					linearResult = mipNode.Solve();
					if (num == 1)
					{
						rootNodeResLp = linearResult;
					}
					CheckDone(linearResult);
					LogSource.SimplexTracer.TraceEvent(TraceEventType.Verbose, 0, "MIP: node {0}: {1}", mipNode.ID, linearResult.ToString());
					if (num % 1000 == 0)
					{
						LogSource.SimplexTracer.TraceEvent(TraceEventType.Information, 0, "MIP: #remaining nodes : {0}, GAP = {1}", _mipActiveList.Count, ComputeUserModelGap());
					}
					if (linearResult != LinearResult.Optimal)
					{
						continue;
					}
					_branchAlg.UpdateAfterNodeSolve(mipNode, linearResult);
					OptimalGoalValues optimalGoalValues = mipNode.Task.OptimalGoalValues;
					if (optimalGoalValues.Count > 0)
					{
						LogSource.SimplexTracer.TraceEvent(TraceEventType.Verbose, 0, "MIP: node {0} optimal relaxation value: {1}", mipNode.ID, optimalGoalValues.ScaleToUserModel(mipNode.Task));
					}
					if (_upperBound != null && _mipOgvBest.CompareTo(optimalGoalValues) < 0)
					{
						continue;
					}
					if (!_branchAlg.FindBranchVariable(mipNode, out var _, out var branchVar, out var branchingLowerBound, out var branchingUpperBound))
					{
						if (_mipOgvBest == null)
						{
							_mipOgvBest = optimalGoalValues;
							_mipBestNode = mipNode;
							_upperBound = _mipOgvBest;
							if (optimalGoalValues.Count > 0)
							{
								LogSource.SimplexTracer.TraceEvent(TraceEventType.Information, 0, "MIP: node {0} feasible integer solution: {1}, Gap = {2}", mipNode.ID, optimalGoalValues.ScaleToUserModel(mipNode.Task), ComputeUserModelGap());
							}
							else
							{
								LogSource.SimplexTracer.TraceEvent(TraceEventType.Information, 0, "MIP: node {0} feasible integer solution, Gap = {1}", mipNode.ID, ComputeUserModelGap());
							}
							RaiseSolvingEvent(SimplexSolveState.MipNewSolution);
						}
						else if (_mipOgvBest.CompareTo(optimalGoalValues) > 0)
						{
							_mipBestNode.ClearTask();
							_mipOgvBest = optimalGoalValues;
							_mipBestNode = mipNode;
							_upperBound = _mipOgvBest;
							if (optimalGoalValues.Count > 0)
							{
								LogSource.SimplexTracer.TraceEvent(TraceEventType.Information, 0, "MIP: node {0} feasible integer solution: {1}, Gap = {2}", mipNode.ID, optimalGoalValues.ScaleToUserModel(mipNode.Task), ComputeUserModelGap());
							}
							else
							{
								LogSource.SimplexTracer.TraceEvent(TraceEventType.Information, 0, "MIP: node {0} feasible integer solution, Gap = {1}", mipNode.ID, ComputeUserModelGap());
							}
							_solverContext.Params.RaiseSolvingEvent();
							RaiseSolvingEvent(SimplexSolveState.MipNewSolution);
						}
						continue;
					}
					if (Root.Params.MixedIntegerGenerateCuts && mipNode.Level < 10)
					{
						_cutStore.Age(mipNode);
						int num2 = _cutStore.Count(mipNode);
						if (!mipNode.GenerateCuts(_cutStore))
						{
							LogSource.SimplexTracer.TraceEvent(TraceEventType.Verbose, 0, "MIP: node {0} generates infeasible cuts", mipNode.ID);
							continue;
						}
						if (_cutStore.Count(mipNode) > num2)
						{
							LogSource.SimplexTracer.TraceEvent(TraceEventType.Verbose, 0, "MIP: node {0} generates {1} cuts", mipNode.ID, _cutStore.Count(mipNode) - num2);
						}
					}
					Branch(mipNode, branchVar, branchingLowerBound, branchingUpperBound, ref _branchCount);
					RaiseSolvingEvent(SimplexSolveState.MipBranchCreated);
				}
			}
			finally
			{
				PostSolve(rootNodeResLp, _branchCount, out resLp, out resMip);
			}
		}

		private void Branch(MipNode node, int branchVar, double newLowerBound, double newUpperBound, ref int branchCount)
		{
			MipNode item = node.Clone(branchVar, newUpperBound, isLowerBound: false);
			MipNode item2 = node.Clone(branchVar, newLowerBound, isLowerBound: true);
			_mipActiveList.Push(item2);
			_mipActiveList.Push(item);
			if (node != _rootNode)
			{
				node.ClearTask();
			}
			branchCount++;
		}

		internal double ComputeUserModelGap()
		{
			if (_upperBound == null || _upperBound.Count == 0)
			{
				return double.NaN;
			}
			if (_rootNode.RelaxationOptimalGoalValues == null)
			{
				return double.NaN;
			}
			if (_mipBestNode == null || _mipBestNode.Task == null || _rootNode.Task == null)
			{
				return double.NaN;
			}
			OptimalGoalValues optimalGoalValues = _rootNode.RelaxationOptimalGoalValues.ScaleToUserModel(_rootNode.Task);
			OptimalGoalValues optimalGoalValues2 = _upperBound.ScaleToUserModel(_mipBestNode.Task);
			return (optimalGoalValues2[0] - optimalGoalValues[0]).ToDouble();
		}

		private void PostSolve(LinearResult rootNodeResLp, int branchCount, out LinearResult resLp, out LinearResult resMip)
		{
			LogSource.SimplexTracer.TraceEvent(TraceEventType.Information, 0, "MIP: total number of cuts generated: {0}", _cutStore.TotalCutCount);
			resLp = rootNodeResLp;
			if (_mipBestNode != null)
			{
				if (_mipActiveList.Count == 0)
				{
					resMip = LinearResult.Optimal;
				}
				else
				{
					resMip = LinearResult.Feasible;
				}
				_solverContext.Solver.RegisterSolution(_mipBestNode.Task, resLp, fMip: true);
				_mipBestNode.Task.BranchCount = branchCount;
				if (_mipBestNode.RelaxationOptimalGoalValues.Count > 0 && resMip != LinearResult.Optimal)
				{
					_mipBestNode.Task.Gap = ComputeUserModelGap();
				}
				LogSource.SimplexTracer.TraceEvent(TraceEventType.Information, 0, "MIP: final gap = {0}", ComputeUserModelGap());
			}
			else if (rootNodeResLp == LinearResult.Optimal && _mipActiveList.Count == 0)
			{
				resMip = LinearResult.InfeasiblePrimal;
				_solverContext.Solver.RegisterSolution(_solverContext, resLp, fMip: false);
			}
			else
			{
				resMip = LinearResult.Invalid;
				_solverContext.Solver.RegisterSolution(_solverContext, resLp, fMip: false);
			}
			_solverContext.Solver.RegisterMipSolutionResult(resMip);
		}
	}
}
