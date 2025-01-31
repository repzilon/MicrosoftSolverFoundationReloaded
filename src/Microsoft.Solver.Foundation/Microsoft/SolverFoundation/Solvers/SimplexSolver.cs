using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Implements a branch and bound search for optimizing mixed integer problems.
	/// </summary>
	public class SimplexSolver : LinearModel, ILinearSolution, ISolverSolution, ILinearSimplexStatistics, ILogSource, ILinearSolver, ISolver, ILinearModel, IReportProvider
	{
		internal const int kcpivMaxUnstall = 2;

		internal int _cReInit;

		internal int _cReInitDueToCuts;

		internal int _cReInitDueToNodes;

		private int _cvProtectModel;

		internal LinearResult _lpResult;

		private LinearResult _mipResult;

		internal SimplexTask _thdSolution;

		private LinearSolutionQuality _qualSolution;

		private bool _fMipSolution;

		private bool _fSolutionValuesComputed;

		private OptimalGoalValues _ogvSolution;

		private bool _fSensitivity;

		private SimplexSensitivity _senReport;

		internal ILinearSolverInfeasibilityReport _infeasibleReport;

		private readonly object _syncSolution = new object();

		internal volatile bool _fEndSolve;

		/// <summary> The simplified model that we work on. This reflects
		///           pre-solve and scaling, but otherwise does not change
		///           during a solve.
		/// </summary>
		internal SimplexReducedModel _mod;

		/// <summary> The threads corresponding to the array of SolverParams
		/// </summary>
		private List<SimplexTask> _rgthd;

		/// <summary>
		///  locale state of the main thread
		/// </summary>
		internal CultureInfo _cultureInfo;

		/// <summary>
		///  locale state of the main thread
		/// </summary>
		internal CultureInfo _cultureUIInfo;

		/// <summary> internal flag
		/// </summary>
		internal bool _fRelax;

		/// <summary>
		/// the current solving state of the solver
		/// </summary>
		private SimplexSolveState _solvingState;

		private LogSource _log;

		private static int _sufLast;

		internal int ReInitCount => _cReInit;

		internal int ReInitDueToCutsCount => _cReInitDueToCuts;

		internal int ReInitDueToNodesCount => _cReInitDueToNodes;

		internal int CvProtectModel
		{
			get
			{
				return _cvProtectModel;
			}
			set
			{
				_cvProtectModel = value;
			}
		}

		/// <summary> Return the lower bound of a MIP model run
		/// </summary>
		public Rational MipBestBound => Rational.Indeterminate;

		/// <summary>
		/// indicates the quality level of the solution.
		/// </summary>
		public LinearSolutionQuality SolutionQuality => _qualSolution;

		/// <summary>
		/// Indicates the result of solving the LP relaxation, which is essentially the model with its integrality conditions ignored.
		/// </summary>
		/// <remarks>
		/// In the case of a model with no goals, LinearResult will be reported as Optimal.
		/// </remarks>
		public virtual LinearResult LpResult
		{
			get
			{
				if (CvProtectModel <= 0)
				{
					return _lpResult;
				}
				return LinearResult.Invalid;
			}
		}

		/// <summary>
		/// Indicates the result of considering the integrality conditions
		/// </summary>
		/// <remarks>
		/// In the case of a model with no goals, LinearResult will be reported as Optimal.
		/// </remarks>
		public virtual LinearResult MipResult
		{
			get
			{
				if (CvProtectModel <= 0 && _thdSolution != null)
				{
					return _mipResult;
				}
				return LinearResult.Invalid;
			}
		}

		/// <summary>
		/// indicates the result of the solve attempt
		/// </summary>
		/// <remarks>
		/// In the case of a model with no goals, LinearResult will be reported as Optimal.
		/// </remarks>
		public virtual LinearResult Result
		{
			get
			{
				if (CvProtectModel > 0)
				{
					return LinearResult.Invalid;
				}
				if (_fRelax)
				{
					return _lpResult;
				}
				if (m_cvidInt == 0)
				{
					return _lpResult;
				}
				if (_lpResult != LinearResult.Optimal)
				{
					return _lpResult;
				}
				if (_thdSolution != null && _mipResult != 0)
				{
					return _mipResult;
				}
				return LinearResult.Interrupted;
			}
		}

		/// <summary>
		/// goal count
		/// </summary>
		public virtual int SolvedGoalCount
		{
			get
			{
				if (_mod != null)
				{
					return _mod.GoalCount;
				}
				return 0;
			}
		}

		/// <summary>
		/// The InnerIndexCount property returns the number of user and row variables used 
		/// internally when solving the linear model. This may be less than ILinearModel.KeyCount since variables may be eliminated by presolve
		/// </summary>
		public virtual int InnerIndexCount => _mod.VarLim;

		/// <summary>
		/// The InnerIntegerIndexCount property returns the number of integer user 
		/// and row variables used internally when solving the linear model. 
		/// This may be less than ILinearModel.IntegerIndexCount since variables may be eliminated by presolve
		/// </summary>
		public virtual int InnerIntegerIndexCount => _mod.CvarInt;

		/// <summary>
		/// The InnerSlackCount property returns the number of row variables used internally when 
		/// solving the linear model. This may be less than the ILinearModel.RowCount, since row variables may be eliminated by presolve.
		/// </summary>
		public virtual int InnerSlackCount => _mod.CvarSlack;

		/// <summary>
		/// The InnerRowCount property returns the number of rows used internally when solving the linear model. 
		/// This may be less than ILinearModel.RowCount since rows may be eliminated by presolve.
		/// </summary>
		public virtual int InnerRowCount => _mod.RowLim;

		/// <summary>
		/// The pivot count properties indicate the number of simplex pivots performed. Generally these include both major and minor pivots.
		/// </summary>
		public virtual int PivotCount
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.PivotCount;
				}
				return 0;
			}
		}

		/// <summary>
		/// The pivot count of degenerated pivots
		/// </summary>
		public virtual int PivotCountDegenerate
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.PivotCountDegenerate;
				}
				return 0;
			}
		}

		/// <summary>
		/// the pivot count of exact arithmetic
		/// </summary>
		public virtual int PivotCountExact
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.PivotCountExact;
				}
				return 0;
			}
		}

		/// <summary>
		/// the phase I pivot count of exact arithmetic
		/// </summary>
		public virtual int PivotCountExactPhaseOne
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.PivotCountExactPhaseOne;
				}
				return 0;
			}
		}

		/// <summary>
		/// the phase II pivot count of exact arithmetic
		/// </summary>
		public virtual int PivotCountExactPhaseTwo
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.PivotCountExactPhaseTwo;
				}
				return 0;
			}
		}

		/// <summary>
		/// the pivot count of double arithmetic
		/// </summary>
		public virtual int PivotCountDouble
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.PivotCountDouble;
				}
				return 0;
			}
		}

		/// <summary>
		/// the phase I pivot count of double arithmetic
		/// </summary>
		public virtual int PivotCountDoublePhaseOne
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.PivotCountDoublePhaseOne;
				}
				return 0;
			}
		}

		/// <summary>
		/// the phase II pivot count of double arithmetic
		/// </summary>
		public virtual int PivotCountDoublePhaseTwo
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.PivotCountDoublePhaseTwo;
				}
				return 0;
			}
		}

		/// <summary>
		/// The factor count properties indicate the number of basis matrix LU factorizations performed.
		/// </summary>
		public virtual int FactorCount
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.FactorCount;
				}
				return 0;
			}
		}

		/// <summary>
		/// The factor count of exact arithmetic 
		/// </summary>
		public virtual int FactorCountExact
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.FactorCountExact;
				}
				return 0;
			}
		}

		/// <summary>
		/// The factor count of double arithmetic 
		/// </summary>
		public virtual int FactorCountDouble
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.FactorCountDouble;
				}
				return 0;
			}
		}

		/// <summary>
		/// The BranchCount property indicates the number of branches performed when applying the branch and bound algorithm to a MILP. 
		/// If the model has no integer variables, this will be zero.
		/// </summary>
		public virtual int BranchCount
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.BranchCount;
				}
				return 0;
			}
		}

		/// <summary>
		/// Used by MIP to indicate the difference between an integer solution to a relaxed solution
		/// </summary>
		public virtual Rational Gap
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.Gap;
				}
				return 0;
			}
		}

		/// <summary>
		/// indicate whether the solve attempt was instructed to use exact arithmetic
		/// </summary>
		public virtual bool UseExact
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.UseExact;
				}
				return false;
			}
		}

		/// <summary>
		/// indicate whether the solve attempt was instructed to use double arithmetic
		/// </summary>
		public virtual bool UseDouble
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.UseDouble;
				}
				return false;
			}
		}

		/// <summary>
		/// indicates which algorithm was used to by the solver
		/// </summary>
		public virtual SimplexAlgorithmKind AlgorithmUsed
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.AlgorithmUsed;
				}
				return SimplexAlgorithmKind.Primal;
			}
		}

		/// <summary>
		/// Costing used for exact arithmetic 
		/// </summary>
		public virtual SimplexCosting CostingUsedExact
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.CostingUsedExact;
				}
				return SimplexCosting.Default;
			}
		}

		/// <summary>
		/// costing used for double arithmetic 
		/// </summary>
		public virtual SimplexCosting CostingUsedDouble
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.CostingUsedDouble;
				}
				return SimplexCosting.Default;
			}
		}

		internal virtual int ResultParamsIndex
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.Tid;
				}
				return -1;
			}
		}

		internal virtual SimplexBasisKind InitialBasisUsed
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.InitialBasisUsed;
				}
				return SimplexBasisKind.Slack;
			}
		}

		internal virtual bool ShiftBoundsUsed
		{
			get
			{
				if (_thdSolution != null)
				{
					return _thdSolution.ShiftBounds;
				}
				return false;
			}
		}

		/// <summary> Return the simplex thread that solves the user model. By "solve", we mean
		/// the last thread that finds a definite answer to the model and registered its 
		/// solution to the solver.
		/// </summary>
		internal SimplexTask SolutionThread => _thdSolution;

		internal LogSource Logger => _log;

		internal void IncrementReInitCounter()
		{
			_cReInit++;
		}

		internal void IncrementReInitDueToCutsCounter()
		{
			_cReInitDueToCuts++;
		}

		internal void IncrementReInitDueToNodesCounter()
		{
			_cReInitDueToNodes++;
		}

		/// <summary> Fill our data structures from the given problem
		/// </summary>
		/// <param name="fSolveMip"> If this is a MILP problem </param>
		/// <param name="parameters"> the array of params for thread(s) </param>
		/// <returns> false iff the model is found to be infeasible, typically by PreSolve </returns>
		internal virtual bool Init(bool fSolveMip, SimplexSolverParams[] parameters)
		{
			_lpResult = LinearResult.Invalid;
			_mod = new SimplexReducedModel(this);
			bool flag = false;
			bool flag2 = false;
			foreach (SimplexSolverParams simplexSolverParams in parameters)
			{
				if (simplexSolverParams.GetInfeasibilityReport)
				{
					flag2 = true;
				}
			}
			if (IsSpecialOrderedSet)
			{
				flag2 = true;
			}
			if (!flag2)
			{
				_solvingState = SimplexSolveState.Presolve;
				flag = !_mod.PreSolve(parameters);
				_solvingState = SimplexSolveState.Init;
			}
			_mod.InitRhs();
			_mod.BuildSOSModel();
			if (!flag)
			{
				_rgthd = new List<SimplexTask>();
				bool flag3 = false;
				bool fForceExact = flag;
				foreach (SimplexSolverParams simplexSolverParams2 in parameters)
				{
					if (!flag3 && simplexSolverParams2.UseDouble)
					{
						flag3 = true;
						_mod.InitDbl(simplexSolverParams2);
					}
					SimplexTask item = new SimplexTask(this, _rgthd.Count, simplexSolverParams2, fForceExact);
					_rgthd.Add(item);
					if (flag)
					{
						break;
					}
				}
			}
			ClearSolution();
			return !flag;
		}

		/// <summary>
		/// Reinitialize the internal data structures.
		/// </summary>
		/// <param name="fSolveMip"></param>
		/// <param name="thread"></param>
		/// <returns></returns>
		internal virtual bool ReInit(bool fSolveMip, SimplexTask thread)
		{
			_mod = new SimplexReducedModel(this);
			bool flag = !_mod.PreSolve(new SimplexSolverParams[1] { thread.Params });
			if (0 == 0 && thread.Params.UseDouble)
			{
				bool flag2 = true;
				_mod.InitRhs();
				_mod.InitDbl(thread.Params);
			}
			return !flag;
		}

		/// <summary>Initialize the model.
		/// </summary>
		protected override void InitModel(IEqualityComparer<object> comparer, int cvid, int crid, int cent)
		{
			PreChange();
			base.InitModel(comparer, cvid, crid, cent);
			_lpResult = LinearResult.Invalid;
			ClearSolution();
		}

		internal override void PreChange()
		{
			if (!base.IsMipModel && _cvProtectModel > 0)
			{
				throw new InvalidOperationException(Resources.NoChangesBeforeSolveComplete);
			}
			base.PreChange();
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
			if (propertyName == SimplexProperties.PivotCount)
			{
				return _rgthd[0].PivotCount;
			}
			if (propertyName == SimplexProperties.FactorCount)
			{
				return _rgthd[0].FactorCount;
			}
			if (propertyName == SimplexProperties.MipGap)
			{
				return _rgthd[0].Gap.ToDouble();
			}
			if (propertyName == SimplexProperties.BranchCount)
			{
				return _rgthd[0].BranchCount;
			}
			if (propertyName == SolverProperties.GoalValue)
			{
				return GetCurrentObjectiveValue(_rgthd[0]);
			}
			if (propertyName == SolverProperties.SolveState)
			{
				return GetSolveState(_rgthd[0]).ToString();
			}
			return base.GetProperty(propertyName, vid);
		}

		/// <summary>Set the value of a property.
		/// </summary>
		/// <remarks>Currently there is no support for setting propery during solve.</remarks>
		/// <param name="propertyName">The name of the property to get.</param>
		/// <param name="vid">An index for the item of interest.</param>
		/// <param name="value">The property value.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidSolverPropertyException"></exception>
		public override void SetProperty(string propertyName, int vid, object value)
		{
			if (_solvingState != 0)
			{
				throw new InvalidSolverPropertyException(Resources.ThisSolverDoesNotSupportSettingAPropertyWhileSolving, InvalidSolverPropertyReason.EventDoesNotSupportSetProperty);
			}
			ValidateInSolveState(propertyName);
			base.SetProperty(propertyName, vid, value);
		}

		/// <summary>
		/// If not in solving state and property is one that solver supports, throw
		/// </summary>
		private void ValidateInSolveState(string propertyName)
		{
			if (_solvingState == SimplexSolveState.PreInit && (propertyName == SimplexProperties.BranchCount || propertyName == SimplexProperties.FactorCount || propertyName == SimplexProperties.MipGap || propertyName == SimplexProperties.PivotCount || propertyName == SolverProperties.GoalValue || propertyName == SolverProperties.SolveState))
			{
				throw new InvalidSolverPropertyException(string.Format(CultureInfo.InvariantCulture, Resources.Property0CanOnlyBeAccessedBySolvingEventHandlers, new object[1] { propertyName }), InvalidSolverPropertyReason.EventDoesNotSupportProperty);
			}
		}

		private SimplexSolveState GetSolveState(SimplexTask currentTask)
		{
			if (!currentTask.SolveState.HasValue)
			{
				return _solvingState;
			}
			return currentTask.SolveState.Value;
		}

		private object GetCurrentObjectiveValue(SimplexTask currentTask)
		{
			if (GoalCount == 0)
			{
				return double.NaN;
			}
			OptimalGoalValues optimalGoalValues = currentTask.OptimalGoalValues;
			OptimalGoalValues optimalMipGoalValues = currentTask.OptimalMipGoalValues;
			OptimalGoalValues optimalGoalValues2;
			if (base.IsMipModel)
			{
				if (optimalMipGoalValues == null)
				{
					return double.NaN;
				}
				optimalGoalValues2 = optimalMipGoalValues.ScaleToUserModel(currentTask.MipBestNodeTask);
			}
			else
			{
				if (optimalGoalValues == null)
				{
					return double.NaN;
				}
				optimalGoalValues2 = optimalGoalValues.ScaleToUserModel(currentTask);
			}
			return optimalGoalValues2[0].ToDouble();
		}

		internal virtual void ClearSolution()
		{
			_lpResult = LinearResult.Invalid;
			_mipResult = LinearResult.Invalid;
			_thdSolution = null;
			_qualSolution = LinearSolutionQuality.None;
			_fMipSolution = false;
			_ogvSolution = null;
			_fSolutionValuesComputed = false;
			_fEndSolve = false;
		}

		/// <summary>
		/// Called to register a possible solution. This method determines whether the proposed
		/// solution is an improvement on previously registered solutions. If so, it keeps the
		/// new solution and returns true. If not, it keeps the previous solution and returns true.
		/// </summary>
		internal virtual bool RegisterSolution(SimplexTask thread, LinearResult resLp, bool fMip)
		{
			if (resLp == LinearResult.Invalid)
			{
				return false;
			}
			LinearSolutionQuality solutionQuality = GetSolutionQuality(thread);
			if (solutionQuality == LinearSolutionQuality.None)
			{
				return false;
			}
			lock (_syncSolution)
			{
				if (!IsBetterSolution(thread, solutionQuality, resLp, fMip))
				{
					Logger.LogEvent(10, Resources.SolutionRejectedAlg0Res1Vals2FMip3, thread.AlgorithmUsed, resLp, solutionQuality, fMip);
					return false;
				}
				Logger.LogEvent(10, Resources.SolutionAcceptedAlg0Res1Vals2FMip3, thread.AlgorithmUsed, resLp, solutionQuality, fMip);
				RegisterSolutionCore(thread, solutionQuality, resLp, fMip);
				return true;
			}
		}

		/// <summary>
		/// Called to register a possible solution. This method determines whether the proposed
		/// solution is an improvement on previously registered solutions. If so, it keeps the
		/// new solution and returns true. If not, it keeps the previous solution and returns true.
		/// </summary>
		internal virtual bool RegisterSolution(LinearResult resLp, bool fMip)
		{
			lock (_syncSolution)
			{
				_lpResult = resLp;
				_fMipSolution = fMip;
				_fSolutionValuesComputed = false;
				return true;
			}
		}

		/// <summary>
		/// Called when the search for more solutions terminates. 
		/// </summary>
		/// <remarks>The call to ValidateSolution is needed to make sure that the
		/// registered solution is accepted. A solution is optimal only when we have
		/// exhausted the search tree. 
		/// It is possible that the thread that calls RegisterMipSolutionResult and the thread 
		/// that calls RegisterSolution are different. But it is OK since we do not overwrite
		/// _mipResult to a lower quality.
		/// </remarks>
		internal void RegisterMipSolutionResult(LinearResult resMip)
		{
			lock (_syncSolution)
			{
				if (_mipResult != LinearResult.Optimal && (_mipResult != LinearResult.Feasible || resMip == LinearResult.Optimal))
				{
					_mipResult = resMip;
				}
			}
		}

		internal static LinearSolutionQuality GetSolutionQuality(SimplexTask thread)
		{
			if (thread.AlgorithmExact != null && (thread.Model.RowLim == 0 || thread.AlgorithmExact.TryForValidValues(fPermute: true)))
			{
				return LinearSolutionQuality.Exact;
			}
			if (thread.AlgorithmDouble != null && (thread.Model.RowLim == 0 || thread.AlgorithmDouble.TryForValidValues(fPermute: true)))
			{
				return LinearSolutionQuality.Approximate;
			}
			return LinearSolutionQuality.None;
		}

		internal virtual bool IsBetterSolution(SimplexTask thread, LinearSolutionQuality qual, LinearResult resLp, bool fMip)
		{
			if (_thdSolution == null)
			{
				return true;
			}
			if (_fMipSolution)
			{
				if (fMip)
				{
					return OptimalGoalValues.Compare(thread.OptimalGoalValues.ScaleToUserModel(thread), _ogvSolution) < 0;
				}
				return false;
			}
			if (fMip)
			{
				return true;
			}
			if (!IsComplete(resLp))
			{
				return qual > _qualSolution;
			}
			if (!IsComplete(_lpResult))
			{
				return true;
			}
			if (qual == _qualSolution)
			{
				if (fMip)
				{
					return resLp < _mipResult;
				}
				return resLp < _lpResult;
			}
			return qual > _qualSolution;
		}

		private void RegisterSolutionCore(SimplexTask thread, LinearSolutionQuality qual, LinearResult resLp, bool fMip)
		{
			_lpResult = resLp;
			_thdSolution = thread;
			_fMipSolution = fMip;
			if (resLp == LinearResult.Feasible || resLp == LinearResult.Optimal)
			{
				_ogvSolution = thread.OptimalGoalValues.ScaleToUserModel(thread).Clone();
			}
			_fSolutionValuesComputed = false;
			_qualSolution = qual;
			switch (qual)
			{
			case LinearSolutionQuality.Exact:
				_mod.MapVarValues(thread, thread.AlgorithmExact, _mpvidnum);
				if (_fSensitivity && !thread.Solver.IsMipModel)
				{
					_senReport = new SimplexSensitivity(thread);
					_senReport.Generate();
				}
				break;
			case LinearSolutionQuality.Approximate:
				_mod.MapVarValues(thread, thread.AlgorithmDouble, _mpvidnum);
				break;
			}
			_mod.SetBasicFlagsOnSolver(thread.Basis);
			if (thread.Solver.IsMipModel)
			{
				_mod.RecalculateSlacksAndGoals(_matModel, _mpvidnum, _ogvSolution);
			}
			_ = 2;
		}

		internal virtual void ComputeSolutionValues()
		{
			if (_qualSolution != 0 && !_fSolutionValuesComputed)
			{
				_mod.ComputeEliminatedVidValues(_mpvidnum);
				_fSolutionValuesComputed = true;
			}
		}

		[Conditional("DEBUG")]
		private void CheckSolution()
		{
		}

		internal virtual void EndSolve()
		{
			if (_lpResult == LinearResult.Optimal)
			{
				_fEndSolve = true;
			}
		}

		/// <summary>
		/// Get the information of a solved goal
		/// </summary>
		/// <param name="igoal">a goal index</param>
		/// <param name="key">the goal row key</param>
		/// <param name="vid">the goal row vid</param>
		/// <param name="fMinimize">whether the goal is minimization</param>
		/// <param name="fOptimal">whether the goal is optimal</param>
		public virtual void GetSolvedGoal(int igoal, out object key, out int vid, out bool fMinimize, out bool fOptimal)
		{
			if (igoal < 0 || igoal >= SolvedGoalCount)
			{
				throw new ArgumentOutOfRangeException("igoal");
			}
			vid = _mod.GetGoalVid(igoal);
			fMinimize = _mod.IsGoalMinimize(igoal);
			key = _mpvidvi[vid].Key;
			fOptimal = _ogvSolution != null && _ogvSolution[igoal].IsFinite;
			if (base.IsMipModel)
			{
				fOptimal = fOptimal && _mipResult == LinearResult.Optimal;
			}
		}

		/// <summary>
		/// get the objective value of a goal 
		/// </summary>
		/// <param name="goalIndex">goal id</param>
		/// <returns></returns>
		public virtual Rational GetSolutionValue(int goalIndex)
		{
			if (_ogvSolution == null)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.XIsNull0, new object[1] { "_ogvSolution " }));
			}
			if (goalIndex < 0 || goalIndex >= _ogvSolution.Count)
			{
				throw new ArgumentOutOfRangeException("goalIndex");
			}
			bool flag = _mod.IsGoalMinimize(goalIndex);
			Rational num = ((_ogvSolution == null) ? Rational.Indeterminate : _ogvSolution[goalIndex]);
			if (!flag)
			{
				Rational.Negate(ref num);
			}
			return num;
		}

		private void CheckLicense()
		{
			if (License.VariableLimit != 0 && VariableCount >= License.VariableLimit)
			{
				throw new MsfLicenseException();
			}
			if (License.MipVariableLimit != 0 && base.IsMipModel && VariableCount >= License.MipVariableLimit)
			{
				throw new MsfLicenseException();
			}
			if (License.NonzeroLimit != 0 && base.NonzeroCount >= License.NonzeroLimit)
			{
				throw new MsfLicenseException();
			}
			if (License.MipNonzeroLimit != 0 && base.IsMipModel && base.NonzeroCount >= License.MipNonzeroLimit)
			{
				throw new MsfLicenseException();
			}
			if (License.MipRowLimit != 0 && base.IsMipModel && RowCount >= License.MipRowLimit)
			{
				throw new MsfLicenseException();
			}
		}

		/// <summary> Construct a SimplexSolver with defaults.
		/// </summary>
		public SimplexSolver()
			: this((IEqualityComparer<object>)null)
		{
		}

		/// <summary> Construct a SimplexSolver with given solver environment.
		/// </summary>
		public SimplexSolver(ISolverEnvironment context)
			: this((IEqualityComparer<object>)null)
		{
		}

		/// <summary> Construct a SimplexSolver with specified comparison mechanism for keys
		/// </summary>
		/// <param name="cmp"></param>
		public SimplexSolver(IEqualityComparer<object> cmp)
			: base(cmp)
		{
			InitializeLogging();
		}

		/// <summary> Shutdown the solver
		/// </summary>
		/// <remarks>SimplexSolver is managed. So no memory needs to be explicitly disposed.</remarks>
		public virtual void Shutdown()
		{
			_fEndSolve = true;
		}

		/// <summary>Generate a report
		///
		/// </summary>
		/// <param name="context"></param>
		/// <param name="solution"></param>
		/// <param name="solutionMapping"></param>
		/// <returns></returns>
		public Report GetReport(SolverContext context, Solution solution, SolutionMapping solutionMapping)
		{
			LinearSolutionMapping linearSolutionMapping = solutionMapping as LinearSolutionMapping;
			if (linearSolutionMapping == null && solutionMapping != null)
			{
				throw new ArgumentException(Resources.SolutionMappingIsNotALinearSolutionMapping, "solutionMapping");
			}
			return new SimplexReport(context, this, solution, linearSolutionMapping);
		}

		/// <summary> 
		/// Run the solver with one SolverParam.
		/// </summary>
		public virtual ILinearSolution Solve(ISolverParameters parameter)
		{
			return Solve(parameter as SimplexSolverParams);
		}

		/// <summary> 
		/// Run the solver with one thread per SolverParam.
		/// </summary>
		/// <param name="parameters"> The parameters to apply to the threads </param>
		public virtual ILinearSolution Solve(params SimplexSolverParams[] parameters)
		{
			_cultureInfo = Thread.CurrentThread.CurrentCulture;
			_cultureUIInfo = Thread.CurrentThread.CurrentUICulture;
			if (parameters == null || parameters.Length == 0)
			{
				ClearSolution();
				throw new InvalidOperationException(Resources.InvalidParams);
			}
			foreach (SimplexSolverParams simplexSolverParams in parameters)
			{
				if (simplexSolverParams == null)
				{
					throw new InvalidOperationException(Resources.InvalidParams);
				}
			}
			_solvingState = SimplexSolveState.Init;
			CheckLicense();
			if (base.IsMipModel && GoalCount == 0)
			{
				for (int j = 0; j < parameters.Length; j++)
				{
					if (parameters[j].MixedIntegerBranchingStrategyPreFeasibility == BranchingStrategy.LargestPseudoCost || parameters[j].MixedIntegerBranchingStrategyPreFeasibility == BranchingStrategy.SmallestPseudoCost || parameters[j].MixedIntegerBranchingStrategyPreFeasibility == BranchingStrategy.StrongCost || parameters[j].MixedIntegerBranchingStrategyPreFeasibility == BranchingStrategy.VectorLength)
					{
						ClearSolution();
						throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.PreFeasibilityStrategy0CannotBeUsedWithAModelThatDoesNotHaveGoals, new object[1] { parameters[j].MixedIntegerBranchingStrategyPreFeasibility }));
					}
					if (parameters[j].MixedIntegerBranchingStrategyPostFeasibility == BranchingStrategy.LargestPseudoCost || parameters[j].MixedIntegerBranchingStrategyPostFeasibility == BranchingStrategy.SmallestPseudoCost || parameters[j].MixedIntegerBranchingStrategyPostFeasibility == BranchingStrategy.StrongCost || parameters[j].MixedIntegerBranchingStrategyPostFeasibility == BranchingStrategy.VectorLength)
					{
						ClearSolution();
						throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.PostFeasibilityStrategy0CannotBeUsedWithAModelThatDoesNotHaveGoals, new object[1] { parameters[j].MixedIntegerBranchingStrategyPostFeasibility }));
					}
				}
			}
			if (base.IsQuadraticModel)
			{
				throw new ArgumentException(Resources.PleaseRequestInteriorPointIpmToHandleQuadraticObjectives);
			}
			if (base.IsMipModel)
			{
				foreach (SimplexSolverParams simplexSolverParams2 in parameters)
				{
					if (parameters.Length > 1 && simplexSolverParams2.MixedIntegerGenerateCuts)
					{
						throw new ArgumentException(Resources.CutGenException);
					}
				}
			}
			SolveStub(base.IsMipModel, parameters);
			bool fSensitivity = _fSensitivity;
			foreach (SimplexSolverParams simplexSolverParams3 in parameters)
			{
				if (simplexSolverParams3.GetSensitivityReport)
				{
					_fSensitivity = true;
				}
			}
			PostSolve();
			_fSensitivity = fSensitivity;
			return this;
		}

		private void PostSolve()
		{
			if ((Result == LinearResult.Optimal || Result == LinearResult.Feasible) && _fSensitivity)
			{
				SimplexSolverParams simplexSolverParams = new SimplexSolverParams();
				simplexSolverParams.Algorithm = SimplexAlgorithmKind.Dual;
				simplexSolverParams.UseDouble = true;
				simplexSolverParams.UseExact = true;
				simplexSolverParams.PresolveLevel = 0;
				simplexSolverParams.InitialBasisKind = SimplexBasisKind.Current;
				SolveStub(base.IsMipModel, new SimplexSolverParams[1] { simplexSolverParams });
			}
		}

		/// <summary> Get sensitivity report.
		/// </summary>
		/// <param name="reportType">Simplex report type.</param>
		/// <returns>A linear solver report.</returns>
		public virtual ILinearSolverReport GetReport(LinearSolverReportType reportType)
		{
			switch (reportType)
			{
			case LinearSolverReportType.Sensitivity:
				return _senReport;
			case LinearSolverReportType.Infeasibility:
				return _infeasibleReport;
			default:
				throw new ArgumentException(Resources.NoReportType);
			}
		}

		/// <summary>Returns a string representation of the solver.
		/// </summary>
		/// <returns>Returns a string representation of the solver.</returns>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (ILinearGoal goal in Goals)
			{
				if (goal.Enabled)
				{
					string value = (goal.Minimize ? Resources.MinimizeProblem : Resources.MaximizeProblem);
					stringBuilder.AppendLine(value);
					break;
				}
			}
			stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, Resources.Dimensions0, new object[1] { VariableCount }));
			if (IsComplete(_lpResult))
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, Resources.SolutionQualityIs0, new object[1] { _qualSolution }));
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, Resources.ReportLinePivotCount, new object[1] { PivotCount }));
				foreach (ILinearGoal goal2 in Goals)
				{
					Rational value2 = GetValue(goal2.Index);
					if (!value2.IsIndeterminate)
					{
						string text = ((goal2.Key == null) ? goal2.Index.ToString(CultureInfo.InvariantCulture) : goal2.Key.ToString());
						stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, Resources.FinishingValue0, new object[1] { text }));
						stringBuilder.AppendLine(value2.ToString());
					}
				}
			}
			return stringBuilder.ToString();
		}

		/// <summary> Solve a relaxation problem within MILP or similar context.
		/// </summary>
		/// <param name="parameters"></param>
		public virtual ILinearSolution SolveRelaxation(params SimplexSolverParams[] parameters)
		{
			_cultureInfo = Thread.CurrentThread.CurrentCulture;
			_cultureUIInfo = Thread.CurrentThread.CurrentUICulture;
			_fRelax = true;
			SolveStub(fSolveMip: false, parameters);
			return this;
		}

		internal virtual void SolveStub(bool fSolveMip, SimplexSolverParams[] parameters)
		{
			if (CvProtectModel > 0)
			{
				throw new InvalidOperationException(Resources.AlreadySolving);
			}
			if (parameters == null || parameters.Length <= 0)
			{
				throw new ArgumentException(Resources.SolverParamtersCouldNotBeEmpty, "parameters");
			}
			CvProtectModel++;
			try
			{
				if (!Init(fSolveMip, parameters))
				{
					RegisterSolution(LinearResult.InfeasiblePrimal, fMip: false);
				}
				else
				{
					_solvingState = (fSolveMip ? SimplexSolveState.MipSolving : SimplexSolveState.SimplexSolving);
					int num = _rgthd.Count;
					while (--num > 0)
					{
						_rgthd[num].LaunchThread(fSolveMip);
					}
					_rgthd[0].Run(fSolveMip);
					int num2 = _rgthd.Count;
					while (--num2 > 0)
					{
						_rgthd[num2].SystemThread.Join();
					}
				}
				ComputeSolutionValues();
			}
			finally
			{
				CvProtectModel = 0;
			}
		}

		/// <summary>
		/// Checks whether the solver ran until it got a final result, i.e., it
		/// was not interrupted during the solve process.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns>True if the solver ran to completion; false otherwise.</returns>
		internal static bool IsComplete(LinearResult result)
		{
			return result > LinearResult.Interrupted;
		}

		internal static double ComputeNorm2(VectorDouble vec, int rcExclude)
		{
			double num = 0.0;
			Vector<double>.Iter iter = new Vector<double>.Iter(vec);
			while (iter.IsValid)
			{
				if (iter.Rc != rcExclude)
				{
					double value = iter.Value;
					num += value * value;
				}
				iter.Advance();
			}
			return num;
		}

		internal static double ComputeDblNorm2(VectorRational vec, int rcExclude)
		{
			double num = 0.0;
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(vec);
			while (iter.IsValid)
			{
				if (iter.Rc != rcExclude)
				{
					double num2 = (double)iter.Value;
					num += num2 * num2;
				}
				iter.Advance();
			}
			return num;
		}

		internal static void ComputeReducedCostsAndDual(SimplexFactoredBasis bas, CoefMatrix mat, VectorDouble vecCostSrc, double numEpsRel, VectorDouble vecCostDst, VectorDouble vecDualDst)
		{
			vecDualDst.Clear();
			vecCostDst.Clear();
			Vector<double>.Iter iter = new Vector<double>.Iter(vecCostSrc);
			while (iter.IsValid)
			{
				int basisSlot = bas.GetBasisSlot(iter.Rc);
				if (basisSlot >= 0)
				{
					vecDualDst.SetCoefNonZero(basisSlot, iter.Value);
				}
				else if (bas.GetVvk(iter.Rc) >= SimplexVarValKind.Lower)
				{
					vecCostDst.SetCoefNonZero(iter.Rc, iter.Value);
				}
				iter.Advance();
			}
			if (vecDualDst.EntryCount <= 0)
			{
				return;
			}
			bas.InplaceSolveRow(vecDualDst);
			Vector<double>.Iter iter2 = new Vector<double>.Iter(vecDualDst);
			while (iter2.IsValid)
			{
				double value = iter2.Value;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(mat, iter2.Rc);
				while (rowIter.IsValid)
				{
					int col;
					double num = rowIter.ApproxAndColumn(out col);
					if (bas.GetVvk(col) >= SimplexVarValKind.Lower)
					{
						double coef = vecCostDst.GetCoef(col);
						double num2 = coef - value * num;
						if (Math.Abs(num2) <= numEpsRel * Math.Abs(coef))
						{
							vecCostDst.RemoveCoef(col);
						}
						else
						{
							vecCostDst.SetCoefNonZero(col, num2);
						}
					}
					rowIter.Advance();
				}
				iter2.Advance();
			}
		}

		internal static void ComputeReducedCostsAndDual(SimplexFactoredBasis bas, CoefMatrix mat, VectorRational vecCostSrc, VectorRational vecCostDst, VectorRational vecDualDst)
		{
			vecDualDst.Clear();
			vecCostDst.Clear();
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(vecCostSrc);
			while (iter.IsValid)
			{
				int basisSlot = bas.GetBasisSlot(iter.Rc);
				if (basisSlot >= 0)
				{
					vecDualDst.SetCoefNonZero(basisSlot, iter.Value);
				}
				else if (bas.GetVvk(iter.Rc) >= SimplexVarValKind.Lower)
				{
					vecCostDst.SetCoefNonZero(iter.Rc, iter.Value);
				}
				iter.Advance();
			}
			if (vecDualDst.EntryCount <= 0)
			{
				return;
			}
			bas.InplaceSolveRow(vecDualDst);
			Vector<Rational>.Iter iter2 = new Vector<Rational>.Iter(vecDualDst);
			while (iter2.IsValid)
			{
				Rational num = iter2.Value;
				Rational.Negate(ref num);
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(mat, iter2.Rc);
				while (rowIter.IsValid)
				{
					int column = rowIter.Column;
					if (bas.GetVvk(column) >= SimplexVarValKind.Lower)
					{
						Rational num2 = Rational.AddMul(vecCostDst.GetCoef(column), num, rowIter.Exact);
						if (num2.IsZero)
						{
							vecCostDst.RemoveCoef(column);
						}
						else
						{
							vecCostDst.SetCoefNonZero(column, num2);
						}
					}
					rowIter.Advance();
				}
				iter2.Advance();
			}
		}

		/// <summary>
		/// Solve Bd = a, where "a" is the col'th column of mat.
		/// </summary>
		/// <param name="mat"></param>
		/// <param name="col"></param>
		/// <param name="bas"></param>
		/// <param name="vecDelta">The result. </param>
		internal static void ComputeColumnDelta(CoefMatrix mat, int col, SimplexFactoredBasis bas, VectorDouble vecDelta)
		{
			vecDelta.Clear();
			CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(mat, col);
			while (colIter.IsValid)
			{
				vecDelta.SetCoefNonZero(colIter.Row, colIter.Approx);
				colIter.Advance();
			}
			bas.InplaceSolveCol(vecDelta);
		}

		/// <summary>
		/// Solve Bd = a, where "a" is the col'th column of mat.
		/// </summary>
		/// <param name="mat"></param>
		/// <param name="col"></param>
		/// <param name="bas"></param>
		/// <param name="vecDelta">The result.</param>
		internal static void ComputeColumnDelta(CoefMatrix mat, int col, SimplexFactoredBasis bas, VectorRational vecDelta)
		{
			vecDelta.Clear();
			CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(mat, col);
			while (colIter.IsValid)
			{
				vecDelta.SetCoefNonZero(colIter.Row, colIter.Exact);
				colIter.Advance();
			}
			bas.InplaceSolveCol(vecDelta);
		}

		/// <summary>
		/// This computes the product vec^T * mat restricted to columns of mat that non-basic and not fixed.
		/// The result is placed in rgnumProd. The caller is responsible for zeroing rgnumProd.
		/// Usually this is done incrementally to avoid O(n) operations.
		/// </summary>
		/// <param name="bas"></param>
		/// <param name="mat"></param>
		/// <param name="vec"></param>
		/// <param name="rgnumProd"></param>
		internal static bool[] ComputeProductNonBasic(SimplexBasis bas, CoefMatrix mat, VectorDouble vec, double[] rgnumProd)
		{
			bool[] array = new bool[(mat.ColCount + 31) / 32];
			Vector<double>.Iter iter = new Vector<double>.Iter(vec);
			while (iter.IsValid)
			{
				double value = iter.Value;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(mat, iter.Rc);
				while (rowIter.IsValid)
				{
					int col;
					double num = rowIter.ApproxAndColumn(out col);
					if (bas.GetVvk(col) >= SimplexVarValKind.Lower)
					{
						rgnumProd[col] += value * num;
						array[col >> 5] = true;
					}
					rowIter.Advance();
				}
				iter.Advance();
			}
			return array;
		}

		/// <summary>
		/// This computes the product vec^T * mat restricted to columns of mat that non-basic and not fixed.
		/// The result is placed in rgnumProd. The caller is responsible for zeroing rgnumProd.
		/// Usually this is done incrementally to avoid O(n) operations.
		/// </summary>
		internal static void ComputeProductNonBasicFromExact(SimplexBasis bas, CoefMatrix mat, VectorDouble vec, double[] rgnumProd)
		{
			Vector<double>.Iter iter = new Vector<double>.Iter(vec);
			while (iter.IsValid)
			{
				double value = iter.Value;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(mat, iter.Rc);
				while (rowIter.IsValid)
				{
					int column = rowIter.Column;
					if (bas.GetVvk(column) >= SimplexVarValKind.Lower)
					{
						rgnumProd[column] += value * (double)rowIter.Exact;
					}
					rowIter.Advance();
				}
				iter.Advance();
			}
		}

		/// <summary>
		/// This computes the product vec^T * mat restricted to columns of mat that non-basic and not fixed.
		/// The result is placed in vecDst.  VecDst will be cleared at start.
		/// </summary>
		internal static void ComputeProductNonBasic(SimplexBasis bas, CoefMatrix mat, VectorDouble vecSrc, VectorDouble vecDst)
		{
			vecDst.Clear();
			Vector<double>.Iter iter = new Vector<double>.Iter(vecSrc);
			while (iter.IsValid)
			{
				double value = iter.Value;
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(mat, iter.Rc);
				while (rowIter.IsValid)
				{
					int column = rowIter.Column;
					if (bas.GetVvk(column) >= SimplexVarValKind.Lower)
					{
						double num = vecDst.GetCoef(column) + value * rowIter.Approx;
						if (num != 0.0)
						{
							vecDst.SetCoefNonZero(column, num);
						}
						else
						{
							vecDst.RemoveCoef(column);
						}
					}
					rowIter.Advance();
				}
				iter.Advance();
			}
		}

		internal void VerifySame(Rational[] rgnum1, Rational[] rgnum2, int cnum, int id, string strErr)
		{
			for (int i = 0; i < cnum; i++)
			{
				if (rgnum1[i] != rgnum2[i])
				{
					Logger.LogEvent(id, strErr, i, rgnum1[i], rgnum2[i]);
					break;
				}
			}
		}

		internal void VerifyCloseRel(double[] rgdbl, Rational[] rgrat, int cnum, double dblEps, int id, string strErr)
		{
			double num = 0.0;
			for (int i = 0; i < cnum; i++)
			{
				double num2 = (double)rgrat[i];
				double num3 = Math.Abs(rgdbl[i] - num2);
				double num4 = Math.Max(Math.Abs(rgdbl[i]), Math.Abs(num2));
				if (num4 > 1.0)
				{
					num3 /= num4;
				}
				if (num < num3)
				{
					num = num3;
				}
			}
			if (!(num <= dblEps))
			{
				Logger.LogEvent(id, strErr, num);
			}
		}

		internal void VerifyCloseRel(double[] rgnum1, double[] rgnum2, int cnum, double dblEps, int id, string strErr)
		{
			double num = 0.0;
			for (int i = 0; i < cnum; i++)
			{
				double num2 = Math.Abs(rgnum1[i] - rgnum2[i]);
				double num3 = Math.Max(Math.Abs(rgnum1[i]), Math.Abs(rgnum2[i]));
				if (num3 > 1.0)
				{
					num2 /= num3;
				}
				if (num < num2)
				{
					num = num2;
				}
			}
			if (!(num <= dblEps))
			{
				Logger.LogEvent(id, strErr, num);
			}
		}

		/// <summary>
		/// Initializes the logging framework.
		/// </summary>
		private void InitializeLogging()
		{
			_log = new LogSource("SimplexSolver-" + Interlocked.Increment(ref _sufLast));
		}

		/// <summary>
		/// Add tracing listener 
		/// </summary>
		/// <param name="listener">a listener</param>
		/// <param name="ids">interested events</param>
		/// <returns></returns>
		public virtual bool AddListener(TraceListener listener, LogIdSet ids)
		{
			return Logger.AddListener(listener, ids);
		}

		/// <summary>
		/// Remove tracing listener
		/// </summary>
		/// <param name="listener">a listener</param>
		public virtual void RemoveListener(TraceListener listener)
		{
			Logger.RemoveListener(listener);
		}
	}
}
