using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Solvers;

namespace Microsoft.SolverFoundation.Services
{
	internal class DomainProbingWorker : IDisposable
	{
		[Serializable]
		private class DomainProbingAbortException : MsfException
		{
			protected DomainProbingAbortException(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
			}

			public DomainProbingAbortException()
			{
			}
		}

		private class ProbedDecisionData
		{
			internal Domain _baseDomain;

			internal Decision _decision;

			internal bool _finishedNarrowing;

			internal Rational _fixedValue;

			internal ProbedDecisionHandle _handle;

			internal bool _isFixed;

			internal ICollection<Rational> _values;

			internal Domain _workingDomain;
		}

		private readonly SolverContext _context;

		private readonly List<ProbedDecisionData> _decisionData = new List<ProbedDecisionData>();

		private volatile bool _abort;

		private ConstraintSystem _constraintSystem;

		private Action<ProbedDecisionHandle> _decisionCompletedHook;

		private Action<ProbedDecisionHandle, Rational, bool> _feasibilityTestedHook;

		private CspTask _cspTask;

		private Model _model;

		private volatile bool _running;

		private static readonly object[] _emptyArray = new object[0];

		/// <summary>
		/// Can be set to abort DoNarrowing()
		/// </summary>
		internal bool Abort
		{
			get
			{
				return _abort;
			}
			set
			{
				_abort = value;
			}
		}

		/// <summary>
		/// Creates a domain narrowing worker. The domain narrowing worker 'owns' the model while it exists; the model shouldn't
		/// be changed. Might implement a check to prevent changes while domain narrowing is ongoing. The model can be released
		/// by calling Dispose() explicitly.
		/// </summary>
		internal DomainProbingWorker(SolverContext context, Model model, Action<ProbedDecisionHandle> decisionCompletedHook, Action<ProbedDecisionHandle, Rational, bool> feasibilityTestedHook)
		{
			_context = context;
			_model = model;
			_decisionCompletedHook = decisionCompletedHook;
			_feasibilityTestedHook = feasibilityTestedHook;
		}

		public void Dispose()
		{
			_model = null;
			_decisionCompletedHook = null;
			_feasibilityTestedHook = null;
		}

		/// <summary>
		/// Takes a decision and creates a corresponding ProbedDecisionHandle which is used for domain narrowing.
		/// The decision must be unindexed.
		/// </summary>
		/// <param name="decision"></param>
		/// <returns></returns>
		internal ProbedDecisionHandle AddProbedDecision(Decision decision)
		{
			if (decision._indexSets.Length > 0)
			{
				throw new NotSupportedException();
			}
			ProbedDecisionData probedDecisionData = new ProbedDecisionData();
			ProbedDecisionHandle probedDecisionHandle = default(ProbedDecisionHandle);
			probedDecisionHandle._index = _decisionData.Count;
			probedDecisionData._decision = decision;
			probedDecisionData._finishedNarrowing = false;
			probedDecisionData._values = new HashSet<Rational>();
			probedDecisionData._baseDomain = (probedDecisionData._workingDomain = decision._domain);
			probedDecisionData._handle = probedDecisionHandle;
			_decisionData.Add(probedDecisionData);
			return probedDecisionHandle;
		}

		/// <summary>
		/// Marks a UI decision as being fixed to a specific value. If the decision is an enum, the input should
		/// be on the underlying numeric range. If called on an already-fixed decision, overwrites the previous fixed value.
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="value"></param>
		internal void FixProbedDecision(ProbedDecisionHandle handle, Rational value)
		{
			_decisionData[handle._index]._isFixed = true;
			_decisionData[handle._index]._fixedValue = value;
		}

		/// <summary>
		/// Undoes the result of FixUIDecision so that a decision is once again free to vary.
		/// </summary>
		/// <param name="handle"></param>
		internal void UnfixProbedDecision(ProbedDecisionHandle handle)
		{
			_decisionData[handle._index]._isFixed = false;
		}

		/// <summary>
		/// Does domain probing. Each time the values of a probed decision changes, or a probed decision gets final values,
		/// calls _domainConsumerHook with that ProbedDecisionHandle. Returns when probing is complete.
		/// </summary>
		internal void DoProbe(Func<bool> queryAbort, params Directive[] directives)
		{
			_running = true;
			Stopwatch elapsedTime = new Stopwatch();
			int timeLimit = 0;
			Func<bool> queryAbort2 = () => _abort = queryAbort() || (timeLimit > 0 && elapsedTime.ElapsedMilliseconds > timeLimit);
			try
			{
				ResetValues();
				elapsedTime.Start();
				ConstraintProgrammingDirective constraintProgrammingDirective = ((directives != null && directives.Length != 0) ? FindWinningDirective(queryAbort2, directives) : new ConstraintProgrammingDirective());
				timeLimit = constraintProgrammingDirective.TimeLimit;
				CreateTask(constraintProgrammingDirective);
				ResetDomains();
				Presolve(queryAbort2);
				foreach (ProbedDecisionData decisionDatum in _decisionData)
				{
					if (Abort)
					{
						throw new DomainProbingAbortException();
					}
					if (decisionDatum._isFixed)
					{
						decisionDatum._values.Add(decisionDatum._fixedValue);
					}
					else
					{
						TryAllValues(decisionDatum, queryAbort2);
					}
					decisionDatum._finishedNarrowing = true;
					_decisionCompletedHook(decisionDatum._handle);
				}
			}
			catch (DomainProbingAbortException)
			{
			}
			finally
			{
				if (_constraintSystem != null)
				{
					_constraintSystem.ResetSolver();
				}
				_running = false;
				_abort = false;
			}
		}

		/// <summary>
		/// Clear the possible values of all probed decisions.
		/// </summary>
		private void ResetValues()
		{
			foreach (ProbedDecisionData decisionDatum in _decisionData)
			{
				decisionDatum._values.Clear();
				decisionDatum._finishedNarrowing = false;
			}
		}

		/// <summary>
		/// Set the domains of all variables in the solver to the base (either the full domain if unfixed, or the fixed value).
		/// </summary>
		private void ResetDomains()
		{
			foreach (ProbedDecisionData decisionDatum in _decisionData)
			{
				SetToBaseDomain(decisionDatum);
			}
		}

		/// <summary>
		/// Bind the data to the model, creates the task and initiates the solver reference
		/// </summary>
		/// <param name="directive"></param>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		private void CreateTask(ConstraintProgrammingDirective directive)
		{
			if (_cspTask == null)
			{
				DataBinder dataBinder = new DataBinder(_context, _model);
				dataBinder.BindData(boundIfAlreadyBound: false);
				ModelGenerator generator = ModelGenerator.Create(_context, _model);
				Task taskAndSolver = Task.CreateTask(_context, directive, generator);
				SetTaskAndSolver(taskAndSolver);
			}
		}

		private void SetTaskAndSolver(Task task)
		{
			_cspTask = task as CspTask;
			DebugContracts.NonNull(_cspTask);
			_constraintSystem = _cspTask.Solver;
			_constraintSystem.ResetSolver();
		}

		/// <summary>
		/// Find the winning directive from the given directives and ensure no local search algorithm is used
		/// </summary>
		/// <return>A ConstraintProgrammingDirective instance. Either a default one or a winning one. Will never be null.</return>
		private ConstraintProgrammingDirective FindWinningDirective(Func<bool> queryAbort, Directive[] directives)
		{
			List<ConstraintProgrammingDirective> list = new List<ConstraintProgrammingDirective>();
			foreach (Directive directive in directives)
			{
				if (directive is ConstraintProgrammingDirective constraintProgrammingDirective && constraintProgrammingDirective.Algorithm != ConstraintProgrammingAlgorithm.LocalSearch)
				{
					list.Add(constraintProgrammingDirective);
				}
			}
			if (list.Count == 0)
			{
				return new ConstraintProgrammingDirective();
			}
			if (list.Count == 1)
			{
				return list[0];
			}
			Stopwatch dataBindTimer = new Stopwatch();
			Stopwatch hydrateTimer = new Stopwatch();
			Stopwatch solveTimer = new Stopwatch();
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			_context.SolveModel(_model, queryAbort, solveTimer, dataBindTimer, hydrateTimer, list.ToArray());
			stopwatch.Stop();
			if (_context.FinalSolution == null || _context.FinalSolution.Directive == null)
			{
				return new ConstraintProgrammingDirective();
			}
			ConstraintProgrammingDirective constraintProgrammingDirective2 = _context.FinalSolution.Directive as ConstraintProgrammingDirective;
			DebugContracts.NonNull(constraintProgrammingDirective2);
			SetTaskAndSolver(_context.WinningTask);
			return constraintProgrammingDirective2;
		}

		/// <summary>
		/// Take a probed decision which isn't fixed and try every value which isn't already known to be good. After doing that,
		/// we know that we have found every possible value of that decision for which a feasible solution exists.
		/// </summary>
		private void TryAllValues(ProbedDecisionData currentProbedDecision, Func<bool> queryAbort)
		{
			foreach (Rational value in currentProbedDecision._workingDomain.Values)
			{
				if (!currentProbedDecision._values.Contains(value))
				{
					_cspTask.SetDomain(currentProbedDecision._decision, Domain.Set(value));
					Solve(queryAbort);
					if (!currentProbedDecision._values.Contains(value))
					{
						_feasibilityTestedHook(currentProbedDecision._handle, value, arg3: false);
					}
				}
			}
			SetToBaseDomain(currentProbedDecision);
		}

		/// <summary> Set base domains of probed decisions after presolve. Certain values may have been removed from presolve. Need to fire events for these values.
		/// </summary>
		/// <remarks>This method is called only when Presolve does not find that the model is infeasible</remarks>
		private void SetBaseDomainAfterPresolve()
		{
			foreach (ProbedDecisionData decisionDatum in _decisionData)
			{
				Domain domain = (decisionDatum._workingDomain = _cspTask.GetDomainAfterPresolve(decisionDatum._decision));
				if (_feasibilityTestedHook == null || (domain.ValidValues != null && decisionDatum._baseDomain.ValidValues != null && domain.ValidValues.Count() == decisionDatum._baseDomain.ValidValues.Count()) || (domain.ValidValues == null && decisionDatum._baseDomain.ValidValues == null && domain.MinValue == decisionDatum._baseDomain.MinValue && domain.MaxValue == decisionDatum._baseDomain.MaxValue))
				{
					continue;
				}
				foreach (Rational value in decisionDatum._baseDomain.Values)
				{
					if (!domain.IsValidValue(value))
					{
						_feasibilityTestedHook(decisionDatum._handle, value, arg3: false);
					}
				}
			}
		}

		/// <summary> Perform a special round of propagation to eliminate conflicting values from domains of bound decisions
		/// </summary>
		private void Presolve(Func<bool> queryAbort)
		{
			CspSolverTerm conflict;
			bool flag = _constraintSystem.Presolve(queryAbort, out conflict);
			if (Abort)
			{
				throw new DomainProbingAbortException();
			}
			if (!flag)
			{
				_constraintSystem.ResetSolver();
				return;
			}
			SetBaseDomainAfterPresolve();
			_constraintSystem.ResetSolver();
		}

		/// <summary>
		/// Solve with the solver domains as currently set, and add the results to the appropriate value sets
		/// </summary>
		private void Solve(Func<bool> queryAbort)
		{
			SolverContext.TaskSummary taskSummary = _cspTask.Execute((Task task) => queryAbort());
			if (Abort)
			{
				throw new DomainProbingAbortException();
			}
			RegisterSolutionValues(taskSummary.Solution.Quality, taskSummary);
			_constraintSystem.ResetSolver();
		}

		/// <summary>
		/// Add all values from a solution to the appropriate value sets
		/// </summary>
		/// <param name="quality"></param>
		/// <param name="summary"></param>
		private void RegisterSolutionValues(SolverQuality quality, SolverContext.TaskSummary summary)
		{
			if (quality != SolverQuality.Feasible && quality != 0 && quality != SolverQuality.LocalOptimal)
			{
				return;
			}
			foreach (ProbedDecisionData decisionDatum in _decisionData)
			{
				RegisterValue(decisionDatum, summary);
			}
		}

		/// <summary>
		/// Get the current value of a UI decision from the solver and add it to the value set
		/// </summary>
		/// <param name="resultProbedDecision"></param>
		/// <param name="summary"></param>
		private void RegisterValue(ProbedDecisionData resultProbedDecision, SolverContext.TaskSummary summary)
		{
			DebugContracts.NonNull(summary);
			SolutionMapping solutionMapping = summary.SolutionMapping;
			DebugContracts.NonNull(solutionMapping);
			Rational value = solutionMapping.GetValue(resultProbedDecision._decision, _emptyArray);
			if (!resultProbedDecision._values.Contains(value))
			{
				resultProbedDecision._values.Add(value);
				_feasibilityTestedHook(resultProbedDecision._handle, value, arg3: true);
				if (Abort)
				{
					throw new DomainProbingAbortException();
				}
			}
		}

		/// <summary>
		/// Set the domain of a probed decision in the solver to its full domain if it's unfixed, or its fixed value if it's fixed.
		/// </summary>
		/// <param name="data"></param>
		private void SetToBaseDomain(ProbedDecisionData data)
		{
			if (data._isFixed)
			{
				_cspTask.SetDomain(data._decision, Domain.Set(data._fixedValue));
			}
			else
			{
				_cspTask.SetDomain(data._decision, data._baseDomain);
			}
		}

		/// <summary>
		/// Returns the Decision associated with a ProbedDecisionHandle
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>
		internal Decision GetDecision(ProbedDecisionHandle handle)
		{
			return _decisionData[handle._index]._decision;
		}

		/// <summary>
		/// Gets the current possible set of values of a ProbedDecisionHandle
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>
		internal Rational[] GetValues(ProbedDecisionHandle handle)
		{
			return _decisionData[handle._index]._values.ToArray();
		}

		/// <summary>
		/// Returns true if the current set of values for a ProbedDecisionHandle is final
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>
		internal bool FinishedNarrowing(ProbedDecisionHandle handle)
		{
			return _decisionData[handle._index]._finishedNarrowing;
		}
	}
}
