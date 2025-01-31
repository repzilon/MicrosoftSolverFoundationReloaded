#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
    /// <summary>This class provides methods for creating, loading, saving, and solving models.
    /// A new instance is obtained using the static GetContext method. This class is multi-thread safe.
    /// </summary>
    public sealed class SolverContext
    {
        private class SchedulerQueue : Queue<Task>
        {
        }

        internal class TaskSummary
        {
            public Solution Solution;

            public Directive Directive;

            public Exception Exception;

            public SolutionMapping SolutionMapping;

            public ISolver Solver;

            protected void ChangeSolutionQuality(SolverQuality quality)
            {
                Solution.Quality = quality;
                if (quality != 0 || quality != SolverQuality.Feasible)
                {
                    SolutionMapping = null;
                }
            }
        }

        /// <summary>
        /// SolveState encapsulating the state when scheduling and solving
        /// tasks. It is also used by the Context for sync
        /// REVIEW shahark: SolveState needs to carefully initiated when getting to the next iteration
        /// </summary>
        internal class SolveState
        {
            internal bool _queryAbortReturnedTrue;

            internal List<Task> CompletedTasks;

            public SolveState()
            {
                CompletedTasks = new List<Task>();
            }
        }

        private static readonly object _contextLock = new object();

        private static readonly object _registrationLock = new object();

        private static SolverRegistrationCollection _registeredSolvers;

        private readonly object _modelLock = new object();

        private readonly TraceSource _trace = new TraceSource("SolverContext");

        internal CultureInfo _cultureInfo;

        internal CultureInfo _cultureUIInfo;

        internal SolverFoundationEnvironment _solverEnv;

        internal bool _abortFlag;

        private long _currentTimeLimit;

        private Model _currentModel;

        private Task _winningTask;

        private readonly SamplingParameters _samplingParameters;

        internal static readonly object[] _emptyArray = new object[0];

        internal TaskSummary FinalSolution { get; private set; }

        internal ScenarioGenerator ScenarioGenerator { get; set; }

        /// <summary>
        /// The database context for bound data.
        /// </summary>
        public CallContextReplacement DataSource { get; set; }

        /// <summary>
        /// Gets or sets the current model loaded into the context.
        /// </summary>
        public Model CurrentModel
        {
            get
            {
                return _currentModel;
            }
            set
            {
                lock (_modelLock)
                {
                    if (_currentModel != null)
                    {
                        throw new InvalidOperationException(Resources.ThereIsAlreadyAnActiveModelInTheContext);
                    }
                    if (value._context != this)
                    {
                        throw new InvalidOperationException(Resources.ModelsCannotBeSharedAcrossSolverContextInstances);
                    }
                    _currentModel = value;
                }
            }
        }

        /// <summary>Random sampling parameters.
        /// </summary>
        public SamplingParameters SamplingParameters => _samplingParameters;

        /// <summary>Get all registered plug-in solvers.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public SolverRegistrationCollection RegisteredSolvers => _registeredSolvers;

        /// <summary>
        /// A trace listener which receives informational messages about the SFS.
        /// </summary>
        internal TraceSource TraceSource => _trace;

        /// <summary>
        /// Current time limit for Solve or GetNext call
        /// </summary>
        internal long CurrentTimeLimit
        {
            get
            {
                return _currentTimeLimit;
            }
            set
            {
                _currentTimeLimit = value;
            }
        }

        internal Task WinningTask => _winningTask;

        internal bool HasDataBindingEvent => this.DataBinding != null;

        internal bool HasSolvingEvent => this.Solving != null;

        /// <summary> An event that fires periodically during Solve.  
        /// Changing the model during this event (for example by adding or removing decisions or constraints) 
        /// can cause unpredictable results, and should be avoided.
        /// </summary> 
        /// <remarks> Plug-in solver which its pramaters class doesn't implement ISolverEvents can not call for this callback. 
        /// Expect no calls for this callback during solve</remarks>
        public event EventHandler<SolvingEventArgs> Solving;

        /// <summary> An event that fires as variables are added to the 
        /// solver object. 
        /// Changing the model during this event (for example by adding or removing decisions or constraints) 
        /// can cause unpredictable results, and should be avoided.
        /// </summary> 
        public event EventHandler<DataBindingEventArgs> DataBinding;

        /// <summary>
        /// Construct a solver service context.
        /// </summary>
        public SolverContext()
        {
            _samplingParameters = new SamplingParameters();
            _cultureInfo = Thread.CurrentThread.CurrentCulture;
            _cultureUIInfo = Thread.CurrentThread.CurrentUICulture;
            _solverEnv = new SolverFoundationEnvironment();
            lock (_registrationLock)
            {
                if (_registeredSolvers == null)
                {
                    _registeredSolvers = SolverRegistrationCollection.Create();
                }
            }
        }

        /// <summary>Returns the singleton context.
        /// </summary>
        /// <returns>Singleton context.</returns>
        public static SolverContext GetContext()
        {
            object solverContext;
            if (CallContextReplacement.GetData() == null)
            {
                lock (_contextLock)
                {
                    solverContext = GetSolverContext();
                }
            }
            else
            {
                solverContext = GetSolverContext();
            }
            return (SolverContext)solverContext;
        }

        private static object GetSolverContext()
        {
            object obj = CallContextReplacement.GetData();
            if (obj == null)
            {
                obj = new SolverContext();
                CallContextReplacement.SetData(obj.ToString());
            }
            return obj;
        }

        /// <summary>Constructs an empty model.
        /// </summary>
        /// <remarks>This method can only be called when there is no active model in the context.
        /// </remarks>
        /// <returns>Newly created model.</returns>
        /// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">Thrown if the context already contains a model.</exception>
        public Model CreateModel()
        {
            lock (_modelLock)
            {
                if (_currentModel != null)
                {
                    throw new InvalidOperationException(Resources.ThereIsAlreadyAnActiveModelInTheContext);
                }
                _currentModel = new Model(this);
                return _currentModel;
            }
        }

        /// <summary>
        /// Clears the current model so a new one can be created.
        /// </summary>
        public void ClearModel()
        {
            lock (_modelLock)
            {
                CleanupCore();
            }
        }

        private void CleanupCore()
        {
            _currentModel = null;
            if (DataSource != null)
            {
                DataSource = null;
            }
            if (_winningTask != null)
            {
                _winningTask.Dispose();
                if (FinalSolution != null)
                {
                    MarkSolutionInvalid(FinalSolution.Solution);
                }
                FinalSolution = null;
            }
        }

        private static void MarkSolutionInvalid(Solution solution)
        {
            if (solution != null)
            {
                solution._validSolution = false;
            }
        }

        /// <summary>
        /// Write decision values from the current solution back to the database.
        /// </summary>
        /// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A decision data binding contained an out-of-range index.</exception>
        public void PropagateDecisions()
        {
            lock (_modelLock)
            {
                if (!_currentModel._hasValidSolution)
                {
                    throw new InvalidOperationException(Resources.ThereIsNoSolutionToPropagate);
                }
                foreach (Decision decision in _currentModel._decisions)
                {
                    ((IDataBindable)decision).PropagateValues(this);
                }
                if (DataSource != null)
                {
                    DataSource.SubmitChanges();
                }
            }
        }

        /// <summary>Solve the constructed and loaded model. This is a blocking call.
        /// </summary>
        /// <returns></returns>
        public Solution Solve()
        {
            return Solve(new Directive());
        }

        /// <summary>Check the model for obvious errors. If any are found, throws an exception.
        /// </summary>
        public ModelReport CheckModel()
        {
            ModelReport modelReport = new ModelReport(isValid: true);
            if (_currentModel.IsEmpty)
            {
                modelReport.AddError(Resources.TheModelIsEmpty);
            }
            foreach (Parameter parameter in _currentModel._parameters)
            {
                if (parameter.Binding == null)
                {
                    modelReport.AddError(string.Format(CultureInfo.InvariantCulture, Resources.Parameter0IsNotBoundToADataSource, new object[1] { parameter._name }));
                }
            }
            foreach (RandomParameter allRandomParameter in _currentModel.AllRandomParameters)
            {
                if (allRandomParameter.Binding == null && allRandomParameter.NeedsBind)
                {
                    modelReport.AddError(string.Format(CultureInfo.InvariantCulture, Resources.Parameter0IsNotBoundToADataSource, new object[1] { allRandomParameter._name }));
                }
            }
            foreach (Tuples allTuple in _currentModel.AllTuples)
            {
                if (!allTuple.IsBound && allTuple.NeedsBind)
                {
                    modelReport.AddError(string.Format(CultureInfo.InvariantCulture, Resources.Tuples0IsNotBoundToADataSource, new object[1] { allTuple.Name }));
                }
            }
            return modelReport;
        }

        /// <summary>Solve the constructed and loaded model.  This is a blocking call.
        /// </summary>
        /// <param name="directives">An array of Directive objects that specify how the model is to be solved.</param>
        /// <returns>A Solution object.</returns>
        /// <exception cref="T:Microsoft.SolverFoundation.Common.UnsolvableModelException">Thrown when no solver can accept the model.</exception>
        /// <exception cref="T:System.ArgumentException">Thrown when an empty directives array is supplied.</exception>
        public Solution Solve(params Directive[] directives)
        {
            return Solve(null, directives);
        }

        /// <summary>Solve the constructed and loaded model.  This is a blocking call.
        /// </summary>
        /// <param name="directives">An array of Directive objects that specify how the model is to be solved.</param>
        /// <returns>A Solution object.</returns>
        /// <exception cref="T:Microsoft.SolverFoundation.Common.UnsolvableModelException">Thrown when no solver can accept the model.</exception>
        /// <param name="queryAbort"></param>
        public Solution Solve(Func<bool> queryAbort, params Directive[] directives)
        {
            if (directives == null)
            {
                throw new ArgumentNullException("directives");
            }
            foreach (Directive directive in directives)
            {
                if (directive == null)
                {
                    throw new ArgumentNullException("directives");
                }
            }
            _abortFlag = false;
            Model currentModel;
            lock (_modelLock)
            {
                if (_currentModel == null)
                {
                    throw new InvalidOperationException(Resources.CannotSolveBecauseThereIsNoActiveModel);
                }
                currentModel = _currentModel;
            }
            try
            {
                return Solve(currentModel, queryAbort, directives);
            }
            finally
            {
                _abortFlag = false;
            }
        }

        /// <summary>
        /// Abort a current Solve() operation. May be called asynchronously from any thread.
        /// If no Solve() is running, does nothing.
        /// </summary>
        internal void AbortAsync()
        {
            _abortFlag = true;
        }

        /// <exception cref="T:Microsoft.SolverFoundation.Common.UnsolvableModelException">Thrown when no solver can accept the model.</exception>
        private Solution Solve(Model model, Func<bool> queryAbort, params Directive[] directives)
        {
            ModelReport modelReport = CheckModel();
            if (!modelReport.IsValid)
            {
                foreach (string error in modelReport.Errors)
                {
                    TraceSource.TraceEvent(TraceEventType.Error, 0, error);
                }
                throw new InvalidOperationException(modelReport._errors[0]);
            }
            if (License.Expiration.HasValue && License.Expiration < DateTime.Today)
            {
                throw new MsfLicenseException(Resources.ExpiredEvaluationCopy);
            }
            Stopwatch stopwatch = new Stopwatch();
            Stopwatch stopwatch2 = new Stopwatch();
            Stopwatch stopwatch3 = new Stopwatch();
            Stopwatch stopwatch4 = new Stopwatch();
            model._hasValidSolution = false;
            TraceSource.TraceInformation("Solving");
            TraceSource.TraceInformation("The model has {0} decision(s), {1} constraint(s) and {2} goal(s)", model._decisions.Count, model._constraints.Count, model._goals.Count);
            stopwatch4.Start();
            SolveModel(model, queryAbort, stopwatch3, stopwatch, stopwatch2, directives);
            DebugContracts.NonNull(FinalSolution);
            DebugContracts.NonNull(FinalSolution.Solution);
            TraceSource.TraceInformation("Getting solution values");
            FinalSolution.Solution._directives = directives;
            if (FinalSolution.Directive != null)
            {
                FinalSolution.Solution._winningDirective = FinalSolution.Directive;
            }
            FinalSolution.Solution._dataBindTimeMilliseconds = stopwatch.ElapsedMilliseconds;
            FinalSolution.Solution._hydrateTimeMilliseconds = stopwatch2.ElapsedMilliseconds;
            FinalSolution.Solution._solveTimeMilliseconds = stopwatch3.ElapsedMilliseconds;
            FinalSolution.Solution._totalTimeMilliseconds = stopwatch4.ElapsedMilliseconds;
            stopwatch4.Stop();
            return FinalSolution.Solution;
        }

        /// <summary>
        /// Saves a model in MPS or OML format.
        /// </summary>
        /// <param name="format">The output format.</param>
        /// <param name="writer">A TextWriter where the output will be written to.</param>
        public void SaveModel(FileFormat format, TextWriter writer)
        {
            if (_currentModel.IsStochastic && format != 0)
            {
                throw new NotSupportedException(Resources.StochasticModelCanOnlyBeSavedToAnOMLFile);
            }
            _abortFlag = false;
            try
            {
                if (format == FileFormat.MPS || format == FileFormat.FreeMPS)
                {
                    DataBinder dataBinder = new DataBinder(this, _currentModel);
                    dataBinder.BindData(boundIfAlreadyBound: false);
                }
                switch (format)
                {
                    case FileFormat.MPS:
                        SaveModelToMps(_currentModel, writer, fFixed: true);
                        break;
                    case FileFormat.FreeMPS:
                        SaveModelToMps(_currentModel, writer, fFixed: false);
                        break;
                    case FileFormat.OML:
                        {
                            SolveRewriteSystem rs = new SolveRewriteSystem();
                            OmlWriter omlWriter = new OmlWriter(rs, this);
                            Expression expression = omlWriter.Translate(_currentModel);
                            string value = expression.ToString(new OmlFormatter(rs));
                            writer.Write(value);
                            break;
                        }
                    default:
                        throw new NotSupportedException();
                }
            }
            finally
            {
                _abortFlag = false;
            }
        }

        /// <summary>
        /// Load a model from a file.
        /// </summary>
        /// <param name="format">OML, MPS or SMPS.</param>
        /// <param name="path">For MPS and OML exact path to the file is needed.
        /// For SMPS files, the path represents the core file, or a directory. 
        /// In the first case the other two files (stoch and time) should be in the same directory as the core file and 
        /// their prefix should be the same to the core file. The path can be also be to the "time" file or the "stoch" file
        /// In the latter case the directory should contain the "core", "stoch" and "time" files
        /// with a prefix which is identical to the directory name.
        /// Suffixes supported - Core files: .cor, .core. Time files: .tim, .time.
        /// Stoch files: .sto, .stoch.</param>
        public void LoadModel(FileFormat format, string path)
        {
            _abortFlag = false;
            try
            {
                switch (format)
                {
                    case FileFormat.OML:
                    case FileFormat.MPS:
                    case FileFormat.FreeMPS:
                        {
                            if (!File.Exists(path))
                            {
                                throw new FileNotFoundException(path);
                            }
                            using (StreamReader reader = new StreamReader(path))
                            {
                                LoadModel(format, reader);
                                break;
                            }
                        }
                    case FileFormat.SMPS:
                        LoadSmpsModel(path, isFixed: true);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            finally
            {
                _abortFlag = false;
            }
        }

        /// <summary>
        /// Import an ILinearModel.
        /// </summary>
        /// <param name="linearModel">An ILinearModel containing a model to be imported.</param>
        public void LoadModel(ILinearModel linearModel)
        {
            TraceSource.TraceInformation("Loading model from ILinearModel");
            Model model = CreateModel();
            TransferLinearModelToModel(model, linearModel, null);
        }

        /// <summary>
        /// Import an ITermModel.
        /// </summary>
        /// <param name="termModel">An ITermModel containing a model to be imported.</param>
        public void LoadModel(ITermModel termModel)
        {
            TraceSource.TraceInformation("Loading model from ITermModel");
            Model model = CreateModel();
            TransferTermModelToModel(model, termModel);
        }

        /// <summary>
        /// Reads a model, for example in MPS or OML format.
        /// </summary>
        /// <param name="format">The input format.</param>
        /// <param name="reader">A TextReader with the model data.</param>
        public void LoadModel(FileFormat format, TextReader reader)
        {
            _abortFlag = false;
            try
            {
                switch (format)
                {
                    case FileFormat.OML:
                        LoadOmlModel(reader);
                        break;
                    case FileFormat.MPS:
                        LoadMpsModel(reader, fFixed: true);
                        break;
                    case FileFormat.FreeMPS:
                        LoadMpsModel(reader, fFixed: false);
                        break;
                    case FileFormat.SMPS:
                        throw new NotSupportedException(Resources.SMPSCanOnlyBeParsedUsingFileOrDirectoryPath);
                    default:
                        throw new NotSupportedException();
                }
            }
            finally
            {
                _abortFlag = false;
            }
        }

        private void LoadOmlModel(TextReader reader)
        {
            TraceSource.TraceInformation("Loading model from OML");
            SolveRewriteSystem solveRewriteSystem = new SolveRewriteSystem();
            solveRewriteSystem.MessageLog += delegate (string s)
            {
                TraceSource.TraceEvent(TraceEventType.Error, 0, "OML load error: {0}", s);
            };
            OmlParser omlParser = new OmlParser(solveRewriteSystem, this, new OmlLexer());
            omlParser.ProcessFile("<input>", reader, fStrict: false);
        }

        private void LoadMpsModel(TextReader reader, bool fFixed)
        {
            TraceSource.TraceInformation("Loading model from MPS");
            Model model = CreateModel();
            SimplexSolver simplexSolver = new SimplexSolver(EqualityComparer<object>.Default);
            MpsParser mpsParser = new MpsParser(new MpsLexer(new NormStr.Pool()));
            if (!mpsParser.ProcessSource(new FileText("<input>", reader), fFixed, simplexSolver))
            {
                string message = Resources.CouldNotParseMPSModel;
                if (mpsParser._errors != null)
                {
                    message = mpsParser._errors[0];
                }
                throw new MsfException(message);
            }
            if (_abortFlag)
            {
                throw new MsfException(Resources.Aborted);
            }
            TraceSource.TraceInformation("Loaded model has {0} columns, {1} rows and {2} nonzeroes", simplexSolver.VariableCount, simplexSolver.RowCount, simplexSolver.NonzeroCount);
            TransferLinearModelToModel(model, simplexSolver, null);
        }

        /// <summary>
        /// Loads the SMPS files and transfer it to Term tree
        /// </summary>
        /// <param name="path">path to core file or directory</param>
        /// <param name="isFixed">currently always true</param>
        private void LoadSmpsModel(string path, bool isFixed)
        {
            TraceSource.TraceInformation("Loading model from SMPS");
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, Resources.CouldNotFindStochasticCoreFileOrDirectory0, new object[1] { path }), path);
            }
            Model model = CreateModel();
            SimplexSolver simplexSolver = new SimplexSolver(EqualityComparer<object>.Default);
            SmpsParser smpsParser = new SmpsParser(new SmpsLexer(new NormStr.Pool()));
            if (!smpsParser.ProcessSmpsSource(path, isFixed, simplexSolver))
            {
                string message = Resources.CouldNotParseSMPSModel;
                if (smpsParser._errors != null)
                {
                    message = smpsParser._errors[0];
                }
                throw new MsfException(message);
            }
            if (_abortFlag)
            {
                throw new MsfException(Resources.Aborted);
            }
            TraceSource.TraceInformation("Loaded model has {0} columns, {1} rows and {2} nonzeroes", simplexSolver.VariableCount, simplexSolver.RowCount, simplexSolver.NonzeroCount);
            if (smpsParser.PeriodsInfo._implicit)
            {
                TraceSource.TraceInformation("Time file contains 2 periods and using IMPLICIT period definitions");
            }
            else
            {
                TraceSource.TraceInformation("Time file contains 2 periods and using EXPLICIT period definitions");
            }
            TraceSource.TraceInformation("Stoch file has {0} random paramters", smpsParser.RandomParametersSubstitution.Count);
            TransferLinearModelToModel(model, simplexSolver, smpsParser);
        }

        private void TransferLinearModelToModel(Model model, ILinearModel linearModel, SmpsParser smpsParser)
        {
            LinearModelTransferrer linearModelTransferrer = new LinearModelTransferrer(model, linearModel, this, smpsParser);
            linearModelTransferrer.TransferModel();
        }

        private void TransferTermModelToModel(Model model, ITermModel termModel)
        {
            TermModelTransferrer termModelTransferrer = new TermModelTransferrer(model, termModel, this);
            termModelTransferrer.TransferModel();
        }

        /// <summary>Exports the current model to C# code.
        /// </summary>
        /// <param name="writer">A TextWriter where the output is written.</param>
        internal void ExportModelToCSharp(TextWriter writer)
        {
            using (CSharpWriter cSharpWriter = new CSharpWriter())
            {
                cSharpWriter.Write(_currentModel, writer);
            }
        }

        /// <summary>
        /// Finds all allowed values for a set of decision. An allowed value is one which is part of some feasible solution of the problem.
        /// This is much more efficient than enumerating all feasible solutions to find the allowed values.
        /// </summary>
        /// <param name="decisions">The decisions to find allowed values for.</param>
        public void FindAllowedValues(IEnumerable<DecisionBinding> decisions)
        {
            FindAllowedValues(decisions, () => false, new Directive());
        }

        /// <summary>
        /// Finds all allowed values for a set of decision. An allowed value is one which is part of some feasible solution of the problem.
        /// This is much more efficient than enumerating all feasible solutions to find the allowed values.
        /// </summary>
        /// <param name="decisions">The decisions to find allowed values for.</param>
        /// <param name="queryAbort">A function which will be called periodically during the computation. If it returns true, computation is aborted.</param>
        /// <param name="directives">A series of directives to use when solving.</param>
        public void FindAllowedValues(IEnumerable<DecisionBinding> decisions, Func<bool> queryAbort, params Directive[] directives)
        {
            if (License.Expiration.HasValue && License.Expiration < DateTime.Today)
            {
                throw new MsfLicenseException(Resources.ExpiredEvaluationCopy);
            }
            if (queryAbort == null)
            {
                throw new ArgumentNullException("queryAbort");
            }
            if (decisions == null)
            {
                throw new ArgumentNullException("decisions");
            }
            Dictionary<ProbedDecisionHandle, DecisionBinding> decisionsByHandle = new Dictionary<ProbedDecisionHandle, DecisionBinding>();
            DomainProbingWorker worker = null;
            Action<ProbedDecisionHandle> decisionCompletedHook = delegate (ProbedDecisionHandle handle)
            {
                DecisionBinding decisionBinding = decisionsByHandle[handle];
                decisionBinding.SetFeasibleValues(worker.GetValues(handle));
            };
            Action<ProbedDecisionHandle, Rational, bool> feasibilityTestedHook = delegate (ProbedDecisionHandle handle, Rational value, bool feasible)
            {
                DecisionBinding decisionBinding2 = decisionsByHandle[handle];
                if (feasible)
                {
                    decisionBinding2.SetFeasibleValues(worker.GetValues(handle));
                }
                decisionBinding2.SetFeasibility(value, feasible);
            };
            using (worker = new DomainProbingWorker(this, CurrentModel, decisionCompletedHook, feasibilityTestedHook))
            {
                foreach (DecisionBinding decision in decisions)
                {
                    if (decision == null)
                    {
                        throw new ArgumentNullException("decisions");
                    }
                    ProbedDecisionHandle probedDecisionHandle = worker.AddProbedDecision(decision.Decision);
                    if (decision._isFixed)
                    {
                        worker.FixProbedDecision(probedDecisionHandle, decision._fixedValue);
                    }
                    decisionsByHandle[probedDecisionHandle] = decision;
                }
                worker.DoProbe(queryAbort, directives);
            }
        }

        /// <exception cref="T:Microsoft.SolverFoundation.Common.UnsolvableModelException">Thrown when no solver can accept the model.</exception>
        /// <exception cref="T:System.ArgumentException">Thrown when an empty directives array is supplied.</exception>
        internal void SolveModel(Model model, Func<bool> queryAbort, Stopwatch solveTimer, Stopwatch dataBindTimer, Stopwatch hydrateTimer, Directive[] directives)
        {
            if (directives.Length == 0)
            {
                throw new ArgumentException(Resources.DirectiveRequired, "directives");
            }
            SchedulerQueue schedulerQueue = new SchedulerQueue();
            ModelGenerator modelGenerator = ModelGenerator.Create(this, model);
            if (FinalSolution != null)
            {
                MarkSolutionInvalid(FinalSolution.Solution);
            }
            dataBindTimer.Start();
            DataBinder dataBinder = new DataBinder(this, model);
            ModelGenerator.BindData(dataBinder);
            dataBindTimer.Stop();
            modelGenerator.RewriteModel();
            Task currentWinner = null;
            Task winningCandidate = null;
            SolveState solveState = new SolveState();
            GetWaitingTimes(directives, out _currentTimeLimit, out var waitLimit);
            long updatedTimeLimit = GetUpdatedTimeLimit(CurrentTimeLimit, dataBindTimer);
            try
            {
                List<ModelException> list = new List<ModelException>();
                BuildTasks(hydrateTimer, directives, schedulerQueue, modelGenerator, list);
                if (!schedulerQueue.Any())
                {
                    throw new UnsolvableModelException(list.ToArray());
                }
                solveTimer.Start();
                CreateTasks(schedulerQueue, queryAbort, solveState);
                ScheduleTasksToRun(schedulerQueue);
                updatedTimeLimit = GetUpdatedTimeLimit(CurrentTimeLimit, solveTimer, hydrateTimer);
                WaitForTasksToFinish(schedulerQueue, updatedTimeLimit, waitLimit);
                solveTimer.Stop();
                currentWinner = PickWinningTask(solveState);
                DisposeTasks(schedulerQueue, currentWinner);
                RegisterCandidate(currentWinner, ref winningCandidate);
                schedulerQueue.Clear();
            }
            finally
            {
                DisposeTasks(schedulerQueue, currentWinner);
            }
            try
            {
                if (winningCandidate != null)
                {
                    RegisterFinalSolution(winningCandidate, winningCandidate._task.Result);
                }
                else
                {
                    RegisterEmptySolution(solveState);
                }
            }
            finally
            {
                ClearSolverState(solveState);
            }
        }

        private static void RegisterCandidate(Task currentWinner, ref Task winningCandidate)
        {
            DebugContracts.NonNull(currentWinner);
            if (winningCandidate != currentWinner)
            {
                if (winningCandidate != null)
                {
                    winningCandidate.Dispose();
                }
                winningCandidate = currentWinner;
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void BuildTasks(Stopwatch hydrateTimer, Directive[] directives, SchedulerQueue schedulerQueue, ModelGenerator generator, List<ModelException> creationExceptions)
        {
            hydrateTimer.Start();
            foreach (Directive directive in directives)
            {
                Task task = null;
                bool flag = false;
                try
                {
                    task = Task.CreateTask(this, directive, generator);
                    schedulerQueue.Enqueue(task);
                    flag = true;
                }
                catch (ModelException item)
                {
                    creationExceptions.Add(item);
                    flag = true;
                }
                finally
                {
                    if (!flag)
                    {
                        task?.Dispose();
                    }
                }
            }
            hydrateTimer.Stop();
        }

        /// <summary>
        /// We have no reason to hold those till the next solve call
        /// even that the solvers has already been disposed
        /// </summary>
        private static void ClearSolverState(SolveState solveState)
        {
            solveState.CompletedTasks.Clear();
        }

        /// <summary>
        /// Create innerQueryAbort for each task
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tasks"></param>
        /// <param name="queryAbort"></param>
        /// <returns></returns>
        private static Dictionary<Task, Func<Task, bool>> GetQueryAborts(SolverContext context, IEnumerable<Task> tasks, Func<bool> queryAbort)
        {
            bool queryAbortReturnedTrue = false;
            int num = tasks.Count();
            Dictionary<Task, Func<Task, bool>> dictionary = new Dictionary<Task, Func<Task, bool>>(num);
            foreach (Task task in tasks)
            {
                Func<Task, bool> value = ((queryAbort != null) ? ((num > 1) ? ((Func<Task, bool>)delegate (Task innerTask)
                {
                    if (innerTask._abort || context._abortFlag)
                    {
                        return true;
                    }
                    lock (queryAbort)
                    {
                        if (queryAbortReturnedTrue)
                        {
                            return true;
                        }
                        if (queryAbort())
                        {
                            queryAbortReturnedTrue = true;
                            return true;
                        }
                    }
                    return false;
                }) : ((Func<Task, bool>)delegate (Task innerTask)
                {
                    if (innerTask._abort || context._abortFlag)
                    {
                        return true;
                    }
                    if (queryAbortReturnedTrue)
                    {
                        return true;
                    }
                    if (queryAbort())
                    {
                        queryAbortReturnedTrue = true;
                        return true;
                    }
                    return false;
                })) : ((Func<Task, bool>)((Task innerTask) => innerTask._abort || context._abortFlag)));
                dictionary[task] = value;
            }
            return dictionary;
        }

        /// <summary>
        /// Take all tasks from schedulerQueue, build the abort delegate
        /// and schedule to ThreadPool queue
        /// </summary>
        /// <param name="schedulerQueue"></param>
        private static void ScheduleTasksToRun(SchedulerQueue schedulerQueue)
        {
            foreach (Task item in schedulerQueue)
            {
                item._task.Start();
            }
        }

        private void CreateTasks(SchedulerQueue schedulerQueue, Func<bool> queryAbort, SolveState solveState)
        {
            foreach (KeyValuePair<Task, Func<Task, bool>> queryAbort2 in GetQueryAborts(this, schedulerQueue, queryAbort))
            {
                Task taskToRun = queryAbort2.Key;
                Func<Task, bool> modifiedQueryAbort = queryAbort2.Value;
                taskToRun._task = new Task<TaskSummary>(delegate
                {
                    try
                    {
                        return taskToRun.Execute(modifiedQueryAbort);
                    }
                    finally
                    {
                        lock (solveState)
                        {
                            solveState.CompletedTasks.Add(taskToRun);
                        }
                    }
                });
            }
        }

        private void WaitForTasksToFinish(SchedulerQueue schedulerQueue, long timeLimit, long waitLimit)
        {
            List<Task> list = schedulerQueue.ToList();
            DateTime now = DateTime.Now;
            DateTime dateTime = now + TimeSpan.FromMilliseconds(timeLimit);
            Task task;
            do
            {
                int num = ((timeLimit >= 0) ? System.Threading.Tasks.Task.WaitAny(list.Select((Task t) => t._task).ToArray(), dateTime - DateTime.Now) : System.Threading.Tasks.Task.WaitAny(list.Select((Task t) => t._task).ToArray()));
                if (num < 0)
                {
                    break;
                }
                task = list[num];
                list.RemoveAt(num);
            }
            while ((task._task.IsFaulted || task._task.Result.Solution.Quality == SolverQuality.Unknown) && list.Count != 0);
            foreach (Task item in list)
            {
                if (!item._task.IsCompleted)
                {
                    item.AbortTask();
                }
            }
            bool flag;
            if (waitLimit < 0)
            {
                System.Threading.Tasks.Task.WaitAll(list.Select((Task t) => t._task).ToArray());
                flag = true;
            }
            else
            {
                flag = System.Threading.Tasks.Task.WaitAll(list.Select((Task t) => t._task).ToArray(), TimeSpan.FromMilliseconds(waitLimit));
            }
            if (!flag)
            {
                TraceSource.TraceInformation(Resources.OneOrMoreSolversWereNotAbortedWithinWaitLimitSpecified);
                throw new MsfFatalException(Resources.OneOrMoreSolversWereNotAbortedWithinWaitLimitSpecified);
            }
        }

        /// <summary>Pick the best solution from all that were solved up to 
        /// this point in time 
        /// </summary>
        /// <returns></returns>
        private static Task PickWinningTask(SolveState solveState)
        {
            TaskSummary taskSummary = null;
            Task result = null;
            lock (solveState)
            {
                foreach (Task completedTask in solveState.CompletedTasks)
                {
                    DebugContracts.NonNull(completedTask._task);
                    if (completedTask._task.IsFaulted)
                    {
                        if (taskSummary == null)
                        {
                            result = completedTask;
                            TaskSummary taskSummary2 = new TaskSummary();
                            taskSummary2.Exception = completedTask._task.Exception;
                            taskSummary = taskSummary2;
                        }
                    }
                    else
                    {
                        TaskSummary result2 = completedTask._task.Result;
                        if (taskSummary == null || IsBetterSolution(result2, taskSummary))
                        {
                            result = completedTask;
                            taskSummary = result2;
                        }
                    }
                }
                return result;
            }
        }

        private static void DisposeTasks(SchedulerQueue schedulerQueue, Task currentWinner)
        {
            foreach (Task item in schedulerQueue)
            {
                if (item != currentWinner || currentWinner == null)
                {
                    item.Dispose();
                }
            }
        }

        /// <summary>
        /// To do this right, we'd need to combine the gate with time limits per thread
        /// I think we should create an ordered list of time limits and do a wait with timing and check each of the threads.  
        /// practically speaking we are limited in the number of threads, so I think linear passing through the thread list 
        /// determining which one(s) to stop are possible.
        /// Instead, we just use the longest time limit. However, timelimits of infinity (-1) are ignored for deciding the longest.
        /// </summary>
        /// <param name="directives">directives</param>
        /// <param name="timeLimit">-1 for Infinity</param>
        /// <param name="waitLimit">-1 for Infinity</param>
        private static void GetWaitingTimes(Directive[] directives, out long timeLimit, out long waitLimit)
        {
            timeLimit = -1L;
            waitLimit = -1L;
            foreach (Directive directive in directives)
            {
                if (directive.TimeLimit >= 0 && (timeLimit < 0 || directive.TimeLimit > timeLimit))
                {
                    timeLimit = directive.TimeLimit;
                }
                if (directive.WaitLimit >= 0 && (waitLimit < 0 || directive.WaitLimit > waitLimit))
                {
                    waitLimit = directive.WaitLimit;
                }
            }
        }

        /// <summary>
        /// Time limit should be recalculated each iteration
        /// </summary>
        /// <param name="originalTimeLimit"></param>
        /// <param name="timers">All timers (hydration, data binding and solving</param>
        /// <returns></returns>
        internal static long GetUpdatedTimeLimit(long originalTimeLimit, params Stopwatch[] timers)
        {
            if (originalTimeLimit == -1)
            {
                return -1L;
            }
            long num = timers.Sum((Stopwatch timer) => timer.ElapsedMilliseconds);
            long num2 = originalTimeLimit - num;
            if (num2 < 0)
            {
                num2 = 0L;
            }
            return num2;
        }

        /// <summary>Take the candidate and register it as the final solution
        /// Dispose of the former one if needed
        /// </summary>
        /// <param name="task"></param>
        /// <param name="summary"></param>
        private void RegisterFinalSolution(Task task, TaskSummary summary)
        {
            TraceSource.TraceInformation("Register final solution");
            if (_winningTask != null && _winningTask != task)
            {
                _winningTask.Dispose();
            }
            _winningTask = task;
            FinalSolution = summary;
            if (summary.Exception != null)
            {
                task.Dispose();
                throw new UnsolvableModelException(Resources.TheSolverSThrewAnExceptionWhileSolvingTheModel, summary.Exception);
            }
            if (summary.SolutionMapping != null && summary.SolutionMapping.ShouldExtractDecisionValues(summary.Solution.Quality))
            {
                _currentModel.ExtractDecisionValues(summary.SolutionMapping);
            }
        }

        private void RegisterEmptySolution(SolveState solveState)
        {
            TaskSummary taskSummary = new TaskSummary();
            taskSummary.SolutionMapping = null;
            taskSummary.Solution = new Solution(this, SolverQuality.Unknown);
            taskSummary.Directive = null;
            taskSummary.Solver = null;
            TaskSummary finalSolution = taskSummary;
            FinalSolution = finalSolution;
            IEnumerable<Exception> source = from task in solveState.CompletedTasks
                                            let exception = task._task.Exception ?? task._task.Result.Exception
                                            where exception != null
                                            select exception;
            if (source.Any())
            {
                TraceSource.TraceInformation("No solution: Exception in solve thread(s)");
                throw new UnsolvableModelException(Resources.TheSolverSThrewAnExceptionWhileSolvingTheModel, source.ToArray());
            }
            TraceSource.TraceInformation("No solution: Solve thread(s) timed out");
        }

        private static bool IsBetterSolution(TaskSummary newSol, TaskSummary oldSol)
        {
            if (oldSol.Solution.Quality == SolverQuality.Optimal || oldSol.Solution.Quality == SolverQuality.Infeasible || oldSol.Solution.Quality == SolverQuality.Unbounded)
            {
                return false;
            }
            if (newSol.Solution.Quality == SolverQuality.Optimal || newSol.Solution.Quality == SolverQuality.Infeasible || newSol.Solution.Quality == SolverQuality.Unbounded)
            {
                return true;
            }
            if ((oldSol.Solution.Quality == SolverQuality.Feasible || oldSol.Solution.Quality == SolverQuality.LocalOptimal) && (newSol.Solution.Quality == SolverQuality.Feasible || newSol.Solution.Quality == SolverQuality.LocalOptimal))
            {
                foreach (Goal goal in oldSol.Solution.Goals)
                {
                    Rational value = oldSol.SolutionMapping.GetValue(goal, _emptyArray);
                    Rational value2 = newSol.SolutionMapping.GetValue(goal, _emptyArray);
                    switch (goal.Direction)
                    {
                        case GoalKind.Minimize:
                            if (value < value2)
                            {
                                return false;
                            }
                            if (value2 < value)
                            {
                                return true;
                            }
                            break;
                        case GoalKind.Maximize:
                            if (value > value2)
                            {
                                return false;
                            }
                            if (value2 > value)
                            {
                                return true;
                            }
                            break;
                    }
                }
            }
            if (oldSol.Solution.Quality == SolverQuality.InfeasibleOrUnbounded)
            {
                return false;
            }
            if (newSol.Solution.Quality == SolverQuality.InfeasibleOrUnbounded)
            {
                return true;
            }
            if (newSol.Solution.Quality == SolverQuality.Unknown)
            {
                return false;
            }
            if (oldSol.Solution.Quality == SolverQuality.Unknown)
            {
                return true;
            }
            return false;
        }

        private void SaveModelToMps(Model mod, TextWriter writer, bool fFixed)
        {
            TraceSource.TraceInformation("Saving model as MPS");
            try
            {
                ModelGenerator modelGenerator = ModelGenerator.Create(this, mod);
                modelGenerator.RewriteModel();
                using (Task task = Task.CreateTask(this, new Directive(), modelGenerator, mpsWrite: true))
                {
                    DebugContracts.NonNull(task);
                    if (!(task is MpsWriterTask mpsWriterTask))
                    {
                        throw new NotSupportedException(Resources.OutputOfNonLinearModelsIsNotSupported);
                    }
                    ILinearModel solver = mpsWriterTask.Solver;
                    DebugContracts.NonNull(solver);
                    MpsWriter mpsWriter = new MpsWriter(solver);
                    mpsWriter.WriteMps(writer, fFixed);
                }
            }
            catch (ModelException innerException)
            {
                throw new NotSupportedException(Resources.OutputOfNonLinearModelsIsNotSupported, innerException);
            }
        }

        internal void FireDataBindingEvent(Decision d, Task task)
        {
            if (this.DataBinding != null)
            {
                DataBindingEventArgs dataBindingEventArgs = new DataBindingEventArgs();
                dataBindingEventArgs._name = d.Name;
                dataBindingEventArgs._target = d;
                dataBindingEventArgs._task = task;
                dataBindingEventArgs._directive = task.Directive;
                this.DataBinding(this, dataBindingEventArgs);
            }
        }

        internal void FireSolvingEvent(Task task)
        {
            if (!task._abort && this.Solving != null)
            {
                SolvingEventArgs solvingEventArgs = new SolvingEventArgs();
                solvingEventArgs._task = task;
                solvingEventArgs._directive = task.Directive;
                this.Solving(this, solvingEventArgs);
            }
        }
    }

    public class CallContextReplacement
    {
        // Using AsyncLocal to store context data
        private static AsyncLocal<string> _asyncLocalData = new AsyncLocal<string>();

        public static void SetData(string value)
        {
            _asyncLocalData.Value = value;
        }

        public static string GetData()
        {
            return _asyncLocalData.Value;
        }

        internal void SubmitChanges()
        {
            throw new NotImplementedException();
        }
    }
}
