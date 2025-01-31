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
	/// <summary> The Microsoft Solver Foundation interior point solver.
	/// </summary>
	[DebuggerDisplay("IPM solver: {_reducedModel}")]
	public class InteriorPointSolver : SecondOrderConicModel, ILinearSolver, ISolver, ILinearModel, ILinearSolution, ISolverSolution, ILogSource, IInteriorPointStatistics, IReportProvider, ILinearSolverSensitivityReport, ILinearSolverReport
	{
		private InteriorPointReducedModel _reducedModel;

		internal LinearResult _lpResult;

		internal volatile bool _fEndSolve;

		internal bool _fSolutionValuesComputed;

		private LinearSolutionQuality _qualSolution;

		private int _cvProtectModel;

		private LogSource _log;

		private static int _sufLast;

		/// <summary> Are we using Interior Point instead of Simplex?
		/// </summary>
		public bool IsInteriorPoint => true;

		/// <summary> Parameters select options for solving and metods for
		///           reporting events and checking on Abort.
		/// </summary>
		public InteriorPointSolverParams Parameters { get; private set; }

		/// <summary> IPM algorithm solution metrics.
		/// </summary>
		public IInteriorPointStatistics Statistics => _reducedModel;

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

		/// <summary>
		/// indicates the quality level of the solution.
		/// </summary>
		public LinearSolutionQuality SolutionQuality => _qualSolution;

		/// <summary>
		/// indicates the result of solving the LP relaxation, which is essentially the model with its integrality conditions ignored
		/// </summary>
		public virtual LinearResult LpResult => Result;

		/// <summary>
		/// indicates the result of considering the integrality conditions
		/// </summary>
		public virtual LinearResult MipResult => LinearResult.Invalid;

		/// <summary> No MIP support
		/// </summary>
		public Rational MipBestBound => Rational.Indeterminate;

		/// <summary> solution gap 
		/// </summary>
		public virtual Rational Gap
		{
			get
			{
				if (_reducedModel == null || _reducedModel.Solution == null)
				{
					return Rational.Indeterminate;
				}
				return _reducedModel.Gap;
			}
		}

		/// <summary>
		/// indicates the result of the solve attempt
		/// </summary>
		public virtual LinearResult Result
		{
			get
			{
				if (_reducedModel == null || _reducedModel.Solution == null)
				{
					return LinearResult.Invalid;
				}
				return _lpResult;
			}
		}

		/// <summary>
		/// goal count
		/// </summary>
		public virtual int SolvedGoalCount
		{
			get
			{
				if (_reducedModel == null || _reducedModel.Solution == null)
				{
					return 0;
				}
				return 1;
			}
		}

		internal LogSource Logger => _log;

		/// <summary> Total number of rows in reduced model.
		/// </summary>
		int IInteriorPointStatistics.RowCount => _reducedModel.RowCount;

		/// <summary> Total number of variables, user and slack.
		/// </summary>
		public int VarCount => _reducedModel.VarCount;

		/// <summary>Iteration count.
		/// </summary>
		public int IterationCount => _reducedModel.IterationCount;

		/// <summary> The primal version of the objective.
		/// </summary>
		public double Primal => _reducedModel.Primal;

		/// <summary> The dual version of the objective.
		/// </summary>
		public double Dual => _reducedModel.Dual;

		/// <summary> The kind of IPM algorithm used.
		/// </summary>
		double IInteriorPointStatistics.Gap => _reducedModel.Gap;

		/// <summary> The kind of IPM algorithm used.
		/// </summary>
		public InteriorPointAlgorithmKind Algorithm => _reducedModel.Algorithm;

		/// <summary> The form of KKT matrices used.
		/// </summary>
		public InteriorPointKktForm KktForm => _reducedModel.KktForm;

		/// <summary> Construct an InteriorPointSolver with defaults.
		/// </summary>
		public InteriorPointSolver()
			: this((IEqualityComparer<object>)null)
		{
		}

		/// <summary> Construct an InteriorPointSolver with defaults.
		/// </summary>
		public InteriorPointSolver(ISolverEnvironment context)
			: this((IEqualityComparer<object>)null)
		{
		}

		/// <summary> Construct an InteriorPointSolver with specified comparison mechanism for keys
		/// </summary>
		/// <param name="cmp">Key comparison mechanism.</param>
		public InteriorPointSolver(IEqualityComparer<object> cmp)
			: base(cmp)
		{
			InitializeLogging();
		}

		/// <summary> Add a reference row for a SOS set. Each SOS set has one reference row
		/// </summary>
		/// <param name="key">a SOS key</param>
		/// <param name="sos">type of SOS</param>
		/// <param name="vidRow">the vid of the reference row</param>
		/// <returns></returns>
		public override bool AddRow(object key, SpecialOrderedSetType sos, out int vidRow)
		{
			throw new NotImplementedException();
		}

		/// <summary> Return a list of SOS1 or SOS2 row indexes
		/// </summary>
		/// <param name="sosType"></param>
		/// <returns></returns>
		public override IEnumerable<int> GetSpecialOrderedSetTypeRowIndexes(SpecialOrderedSetType sosType)
		{
			throw new NotImplementedException();
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
			if (propertyName == SolverProperties.IterationCount)
			{
				return _reducedModel.IterationCount;
			}
			if (propertyName == InteriorPointProperties.DualObjectiveValue)
			{
				return _reducedModel.Dual;
			}
			if (propertyName == InteriorPointProperties.PrimalObjectiveValue)
			{
				return _reducedModel.Primal;
			}
			if (propertyName == SolverProperties.GoalValue)
			{
				return (_reducedModel.Primal + _reducedModel.Dual) / 2.0;
			}
			if (propertyName == InteriorPointProperties.AbsoluteGap)
			{
				return _reducedModel.Gap;
			}
			if (propertyName == SolverProperties.SolveState)
			{
				return _reducedModel.SolveState;
			}
			return base.GetProperty(propertyName, vid);
		}

		/// <summary>
		/// If not in solving state and property is one that solver supports, throw
		/// </summary>
		private void ValidateInSolveState(string propertyName)
		{
			if (_reducedModel == null && (propertyName == InteriorPointProperties.AbsoluteGap || propertyName == InteriorPointProperties.DualObjectiveValue || propertyName == InteriorPointProperties.PrimalObjectiveValue || propertyName == SolverProperties.GoalValue || propertyName == SolverProperties.IterationCount || propertyName == SolverProperties.SolveState))
			{
				throw new InvalidSolverPropertyException(string.Format(CultureInfo.InvariantCulture, Resources.Property0CanOnlyBeAccessedBySolvingEventHandlers, new object[1] { propertyName }), InvalidSolverPropertyReason.EventDoesNotSupportProperty);
			}
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
			if (_reducedModel != null && _reducedModel.SolveState != 0)
			{
				throw new InvalidSolverPropertyException(Resources.ThisSolverDoesNotSupportSettingAPropertyWhileSolving, InvalidSolverPropertyReason.EventDoesNotSupportSetProperty);
			}
			ValidateInSolveState(propertyName);
			base.SetProperty(propertyName, vid, value);
		}

		/// <summary> 
		/// Run the solver with one SolverParam.
		/// </summary>
		public virtual ILinearSolution Solve(ISolverParameters parameter)
		{
			return Solve(parameter as InteriorPointSolverParams);
		}

		/// <summary> Run the solver with one thread per SolverParam.
		/// </summary>
		/// <param name="parameters"> The parameters to apply to the threads </param>
		public virtual ILinearSolution Solve(params InteriorPointSolverParams[] parameters)
		{
			if (parameters == null || parameters.Length == 0)
			{
				ClearSolution();
				Parameters = null;
				throw new ArgumentNullException(Resources.InvalidParams);
			}
			foreach (InteriorPointSolverParams interiorPointSolverParams in parameters)
			{
				if (interiorPointSolverParams == null || interiorPointSolverParams.KindOfSolver != SolverKind.InteriorPointCentral)
				{
					throw new InvalidOperationException(Resources.InvalidParams);
				}
			}
			CheckLicense();
			Parameters = parameters[0];
			if (base.IsQuadraticModel)
			{
				if (1 == GoalCount)
				{
					foreach (ILinearGoal goal in Goals)
					{
						GetBounds(goal.Index, out var lower, out var upper);
						if (!lower.IsNegativeInfinity || !upper.IsPositiveInfinity)
						{
							throw new InvalidModelDataException(Resources.CannotSetBoundsOnAQuadraticGoal);
						}
					}
				}
				EnsureArraySize(ref _mpvidQid, _vidLim);
			}
			InitIPM();
			SolveIpmStub();
			return this;
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
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, Resources.NumberOfIterationsPerformed0, new object[1] { IterationCount }));
				if (1 == GoalCount)
				{
					Rational solutionValue = GetSolutionValue(0);
					if (!solutionValue.IsIndeterminate)
					{
						stringBuilder.Append(Resources.FinishingValue);
						stringBuilder.AppendLine(solutionValue.ToString());
					}
				}
			}
			return stringBuilder.ToString();
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

		internal virtual void ClearSolution()
		{
			_lpResult = LinearResult.Invalid;
		}

		private void RegisterIpmSolution(InteriorPointReducedModel reducedModel, LinearResult resLp)
		{
			_lpResult = resLp;
			_qualSolution = ((reducedModel.Solution.status != 0) ? LinearSolutionQuality.Approximate : LinearSolutionQuality.None);
			reducedModel.MapVarValues(_mpvidnum);
			_fSolutionValuesComputed = true;
			Logger.LogEvent(10, Resources.SolutionAcceptedAlg0Res1Vals2FMip3, "CentralPath", _lpResult, _qualSolution, false);
		}

		/// <summary> Fill our data structures from the given problem
		/// </summary>
		/// <returns> false iff the model is found to be infeasible, typically by PreSolve </returns>
		internal virtual bool InitIPM()
		{
			_lpResult = LinearResult.Invalid;
			ClearSolution();
			if (IsSocpModel || Parameters.IpmAlgorithm == InteriorPointAlgorithmKind.SOCP)
			{
				_reducedModel = new SOCPSolverHSD(this, _log, Parameters);
			}
			else if (base.IsQuadraticModel || Parameters.IpmAlgorithm == InteriorPointAlgorithmKind.PredictorCorrector)
			{
				_reducedModel = new CentralPathSolver(this, _log, _qpModel, _mpvidQid, Parameters.PresolveLevel);
			}
			else
			{
				_reducedModel = new HsdGeneralModelSolver(this, _log, Parameters);
			}
			_reducedModel.SolveState = InteriorPointSolveState.Init;
			if (Parameters.ThreadCountLimit > 0)
			{
				AlgebraContext.ThreadCountLimit = Parameters.ThreadCountLimit;
			}
			return true;
		}

		internal virtual void SolveIpmStub()
		{
			LinearResult linearResult = LinearResult.Invalid;
			RegisterIpmSolution(resLp: (!Parameters.NotifyStartSolve(0)) ? LinearResult.Interrupted : _reducedModel.Solve(Parameters), reducedModel: _reducedModel);
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

		/// <summary>InteriorPointSolver does not support integer variables
		/// </summary>
		/// <param name="vid">a variable index </param>
		/// <param name="fInteger">whether to be an integer variable</param>
		public override void SetIntegrality(int vid, bool fInteger)
		{
			ValidateVid(vid);
			if (fInteger)
			{
				throw new NotSupportedException(Resources.InteriorPointSolverDoesNotSupportIntegerVariables);
			}
		}

		/// <summary> Get the information of a solved goal
		/// </summary>
		/// <param name="igoal"> 0 &lt;= goal index &lt; SolvedGoalCount </param>
		/// <param name="key">the goal row key</param>
		/// <param name="vid">the goal row vid</param>
		/// <param name="fMinimize">whether the goal is minimization</param>
		/// <param name="fOptimal">whether the goal is optimal</param>
		public virtual void GetSolvedGoal(int igoal, out object key, out int vid, out bool fMinimize, out bool fOptimal)
		{
			if (igoal != 0)
			{
				throw new ArgumentOutOfRangeException("igoal");
			}
			vid = _reducedModel.GoalVid;
			key = _mpvidvi[vid].Key;
			fMinimize = _reducedModel.IsGoalMinimize(igoal);
			fOptimal = _reducedModel.Solution != null && _reducedModel.Solution.status == LinearResult.Optimal;
		}

		/// <summary>
		/// get the objective value of a goal 
		/// </summary>
		/// <param name="goalIndex">goal id</param>
		/// <returns></returns>
		public virtual Rational GetSolutionValue(int goalIndex)
		{
			if (_reducedModel == null || _reducedModel.Solution == null || goalIndex != 0)
			{
				return Rational.Indeterminate;
			}
			return _reducedModel.Primal;
		}

		/// <summary>
		/// Initializes the logging framework.
		/// </summary>
		private void InitializeLogging()
		{
			_log = new LogSource("InterPointSolver-" + Interlocked.Increment(ref _sufLast));
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

		/// <summary> Get sensitivity report  
		/// </summary>
		/// <param name="reportType">simplex report type</param>
		/// <returns>a linear solver report</returns>
		public ILinearSolverReport GetReport(LinearSolverReportType reportType)
		{
			if (reportType == LinearSolverReportType.Sensitivity)
			{
				return this;
			}
			throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resources.ReportingNotImplementedForSolverKind0, new object[1] { GetType().FullName }));
		}

		/// <summary> Shutdown the solver instance
		/// </summary>
		/// <remarks>Solver needs to dispose any unmanaged memory used upon this call.</remarks>
		public void Shutdown()
		{
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
			return new InteriorPointReport(context, this, solution, linearSolutionMapping);
		}

		/// <summary> Return the dual value for a row constraint.
		/// </summary>
		/// <param name="vidRow">Row vid.</param>
		/// <returns>
		/// Returns the dual value.  If the constraint has both upper and lower bounds
		/// then there are actually two dual values.  In this case the dual for the active bound (if any) will be returned.
		/// If the model has not been solved then the result is indeterminate.
		/// </returns>
		public Rational GetDualValue(int vidRow)
		{
			if (_reducedModel == null)
			{
				return Rational.Indeterminate;
			}
			if (LpResult == LinearResult.Optimal || LpResult == LinearResult.Optimal || LpResult == LinearResult.Interrupted)
			{
				return _reducedModel.GetDualValue(vidRow);
			}
			return Rational.Indeterminate;
		}

		/// <summary> Get the coefficient range on the first goal row.
		/// </summary>
		/// <param name="vid">A variable vid.</param>
		/// <returns>A LinearSolverSensitivityRange object.</returns>
		LinearSolverSensitivityRange ILinearSolverSensitivityReport.GetObjectiveCoefficientRange(int vid)
		{
			return ((ILinearSolverSensitivityReport)this).GetObjectiveCoefficientRange(vid, 0);
		}

		/// <summary> Get the coefficient range for a goal row.
		/// </summary>
		/// <param name="vid">A variable vid.</param>
		/// <param name="pri">The goal index.</param>
		/// <returns>A LinearSolverSensitivityRange object.</returns>
		LinearSolverSensitivityRange ILinearSolverSensitivityReport.GetObjectiveCoefficientRange(int vid, int pri)
		{
			ValidateVid(vid);
			int goalVid = _reducedModel.GoalVid;
			LinearSolverSensitivityRange result = default(LinearSolverSensitivityRange);
			result.Current = GetCoefficient(goalVid, vid);
			result.Lower = Rational.Indeterminate;
			result.Upper = Rational.Indeterminate;
			return result;
		}

		/// <summary> Get the variable range.  
		/// </summary>
		/// <param name="vid">A variable vid.</param>
		/// <returns>A LinearSolverSensitivityRange object.</returns>
		LinearSolverSensitivityRange ILinearSolverSensitivityReport.GetVariableRange(int vid)
		{
			ValidateRowVid(vid);
			LinearSolverSensitivityRange result = default(LinearSolverSensitivityRange);
			result.Current = GetValue(vid);
			result.Lower = Rational.Indeterminate;
			result.Upper = Rational.Indeterminate;
			return result;
		}
	}
}
