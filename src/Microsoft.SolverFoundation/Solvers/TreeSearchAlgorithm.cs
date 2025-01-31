using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Solver used for enumerating the solutions of a problem. 
	/// </summary>
	internal class TreeSearchAlgorithm
	{
		public const bool Success = true;

		public const bool Failure = false;

		/// <summary>
		///   Stack of decisions; one per level
		/// </summary>
		private Stack<DisolverDecision> _stack;

		/// <summary>
		///   Keeps track of the range of each decision variables 
		///   before it's branch on
		/// </summary>
		private Stack<long> _ranges;

		/// <summary>
		///   Stack of additional "decisions" coming from the refutation
		///   of previous branches
		/// </summary>
		private StackOfLists<Pair<DisolverDecision, VariableGroup>> _refutations;

		private readonly Problem _problem;

		public readonly CspSearchStrategy _searchStrategy;

		private Heuristic _heuristic;

		private Tracker _failDepth;

		private Tracker _estimatedTreeSize;

		private IntegerVariable[] _goalsToMinimize;

		private long[] _bestValuesFound;

		private TreeSearchStatistics _statistics;

		private ParameterlessProcedure _checkAbortion;

		private TextWriter _output;

		private DateTime _startTime;

		private DateTime _lastTrackTime;

		/// <summary>
		///   True if a trace should be output
		/// </summary>
		private bool Verbose => _output != null;

		/// <summary>
		///   Are we doing minimization (or just finding arbitrary solutions)?
		/// </summary>
		internal bool Minimizing => _goalsToMinimize != null;

		/// <summary>
		///   Gets internal representation of the problem as handled
		///   by the solver.
		/// </summary>
		internal Problem Problem => _problem;

		/// <summary>
		///   Gets statistics on the work done by the algorithm
		/// </summary>
		internal TreeSearchStatistics Statistics => _statistics;

		/// <summary>
		///   Construction of a solver for optimization
		///   with list of minimization goals.
		/// </summary>
		/// <param name="s">FiniteSolver from which created</param>
		/// <param name="strat">the search strategy</param>
		/// <param name="goalsToMinimize">list of goals - can be null</param>
		/// <param name="stop">Predicate to call to interrupt execution</param>
		/// <param name="p">the problem</param>
		public TreeSearchAlgorithm(IntegerSolver s, Problem p, CspSearchStrategy strat, DisolverIntegerTerm[] goalsToMinimize, ParameterlessProcedure stop)
			: this(s, p, strat, goalsToMinimize, stop, Console.Out)
		{
		}

		/// <summary>
		///   Construction of a Solver for optimization
		///   with list of minimization goals.
		/// </summary>
		/// <remarks>
		///   Should also have a means to parametrize the heuristics
		/// </remarks>
		/// <param name="s">FiniteSolver from which created</param>
		/// <param name="strat">the search strategy</param>
		/// <param name="goalsToMinimize">list of goals - can be null</param>
		/// <param name="stop">Predicate to call to interrupt execution</param>
		/// <param name="output">Stream in which to display info</param>
		/// <param name="p">the problem</param>
		public TreeSearchAlgorithm(IntegerSolver s, Problem p, CspSearchStrategy strat, DisolverIntegerTerm[] goalsToMinimize, ParameterlessProcedure stop, TextWriter output)
		{
			_problem = p;
			_checkAbortion = stop;
			_output = (s.Verbose ? output : null);
			_startTime = DateTime.Now;
			_lastTrackTime = _startTime;
			_statistics = s.Statistics;
			_failDepth = new Tracker();
			_estimatedTreeSize = new Tracker();
			_stack = new Stack<DisolverDecision>();
			_ranges = new Stack<long>();
			_refutations = new StackOfLists<Pair<DisolverDecision, VariableGroup>>();
			_refutations.PushList();
			_searchStrategy = strat;
			_heuristic = null;
			if (goalsToMinimize != null)
			{
				int num = goalsToMinimize.Length;
				_goalsToMinimize = new IntegerVariable[num];
				_bestValuesFound = new long[num];
				for (int i = 0; i < num; i++)
				{
					long num2 = Math.Min(4611686018427387903L, goalsToMinimize[i].InitialUpperBound + 1);
					_goalsToMinimize[i] = _problem.GetImage(goalsToMinimize[i]);
					_bestValuesFound[i] = num2;
				}
			}
		}

		/// <summary>
		///   Construction of the heuristic
		/// </summary>
		private Heuristic GetHeuristic(CspSearchStrategy s)
		{
			int num = 1234567890;
			if (Verbose)
			{
				_output.WriteLine(Resources.Strategy01, s.Variables.ToString() + "-" + s.Values, _problem.Source.UseRestarts ? "restarts" : "basic");
			}
			VariableSelector variableSelector = GetVariableSelector(s.Variables, num);
			Heuristic heuristic;
			if (s.Values == ValueEnumerationStrategy.Dicho)
			{
				heuristic = new DichotomicBranchingHeuristic(this, num, variableSelector);
			}
			else
			{
				ValueSelector valueSelector = GetValueSelector(s.Values, num);
				heuristic = new VariableValueHeuristic(this, num, variableSelector, valueSelector);
			}
			if (_problem.Source.UseRestarts)
			{
				return new RestartHeuristic(this, heuristic);
			}
			return heuristic;
		}

		/// <summary>
		///   Construction of ValueSelector from 
		///   ValueEnumerationStrategy
		/// </summary>
		private ValueSelector GetValueSelector(ValueEnumerationStrategy ves, int seed)
		{
			switch (ves)
			{
			case ValueEnumerationStrategy.Lex:
				return new LexValueSelector(this);
			case ValueEnumerationStrategy.InvLex:
				return new InvLexValueSelector(this);
			case ValueEnumerationStrategy.Random:
				return new RandomValueSelector(this, seed);
			default:
				return null;
			}
		}

		/// <summary>
		///   Construction of Variable selector from 
		///   VariableEnumerationStrategy
		/// </summary>
		private VariableSelector GetVariableSelector(VariableEnumerationStrategy ves, int seed)
		{
			switch (ves)
			{
			case VariableEnumerationStrategy.Lex:
				return new LexVariableSelector(this);
			case VariableEnumerationStrategy.MinDom:
				return new MinDomainVariableSelector(this, seed);
			case VariableEnumerationStrategy.RoundRobin:
				return new RoundRobinVariableSelector(this);
			case VariableEnumerationStrategy.Impact:
				return new SimpleImpactVariableSelector(this);
			case VariableEnumerationStrategy.Vsids:
				return new VsidsVariableSelector(this, seed);
			case VariableEnumerationStrategy.Random:
				return new RandomVariableSelector(this, seed);
			case VariableEnumerationStrategy.ConfLex:
				return new LastConflictLexVariableSelector(this);
			case VariableEnumerationStrategy.DomWdeg:
				return new DomWdegVariableSelector(this, seed);
			default:
				return null;
			}
		}

		/// <summary>
		///   Method used to start the search. Starting from a fresh
		///   solver, goes to the first solution, if one exists.
		/// </summary>
		/// <returns>true iff a first solution is found</returns>
		public bool FindFirstSolution()
		{
			try
			{
				_problem.ComputeStatistics(_output, ref _statistics);
				DateTime now = DateTime.Now;
				bool flag;
				if (!InitialPropagations())
				{
					flag = false;
				}
				else
				{
					_heuristic = GetHeuristic(_searchStrategy);
					flag = !_problem.Inconsistent && (ExtendToSolution() || FindNextSolution());
				}
				if (flag)
				{
					TimeSpan timeSpan = DateTime.Now - now;
					_statistics.TimeToFirstSolution = timeSpan.TotalMilliseconds;
					_statistics.NbNodesFirstSolution = _statistics.TotalNbNodes;
				}
				if (flag && Verbose)
				{
					_output.Write(Resources.TimeToFirstSolution);
					_output.Write(Resources.Ms, _statistics.TimeToFirstSolution);
				}
				return flag;
			}
			catch (TimeLimitReachedException)
			{
				return false;
			}
		}

		/// <summary>
		///   Method used to continue the search. Starting from a solver
		///   that is positioned on a solution, goes to another solution,
		///   if one exists.
		/// </summary>
		/// <returns>true if a next solution exists</returns>
		public bool FindNextSolution()
		{
			try
			{
				do
				{
					if (!Backtrack())
					{
						if (Verbose)
						{
							PrintStats();
						}
						return false;
					}
				}
				while (!ExtendToSolution());
				return true;
			}
			catch (TimeLimitReachedException)
			{
				return false;
			}
		}

		/// <summary>
		///   Starting from consistent (possibly empty) partial solution,
		///   extend it until full solution (return true)
		///   or inconsistency reached (return false)
		/// </summary>
		/// <returns>
		///   true iff solution found; 
		///   false iff dead-end (inconsistent leaf) reached
		/// </returns>
		private bool ExtendToSolution()
		{
			DisolverDecision d = _heuristic.Decide();
			while (d.Tag != DisolverDecision.Type.SolutionFound)
			{
				_checkAbortion();
				if (d.Tag == DisolverDecision.Type.Restart || d.Tag == DisolverDecision.Type.ContextSwitch)
				{
					_statistics.NbRestarts++;
					UndoAll();
				}
				else if (!TryChoice(d))
				{
					_statistics.NbFails++;
					return false;
				}
				d = _heuristic.Decide();
			}
			if (Minimizing)
			{
				if (!ImprovedSolution())
				{
					_statistics.NbFails++;
					return false;
				}
				for (int i = 0; i < _goalsToMinimize.Length; i++)
				{
					_bestValuesFound[i] = _goalsToMinimize[i].GetValue();
				}
			}
			return true;
		}

		/// <summary>
		/// Checks whether a newly found solution is strictly
		/// better than the best one found so far. Strictly 
		/// better means following a lexicographical order:
		/// objective 0 should be better or equal; if equal
		/// then objective 1 should be better or equal...
		/// </summary>
		private bool ImprovedSolution()
		{
			for (int i = 0; i < _goalsToMinimize.Length; i++)
			{
				long num = _bestValuesFound[i];
				long value = _goalsToMinimize[i].GetValue();
				if (value < num)
				{
					return true;
				}
				if (value > num)
				{
					return false;
				}
			}
			return false;
		}

		/// <summary>
		///   starting from an inconsistent branch,
		///   undoes all assignments until a "lexicographically"
		///   higher one that does not look inconsistent is found
		///   (return true) or levels empty (return false)
		/// </summary>
		/// <returns>
		///   true if we managed to restore a consistent point
		///   false if not and we have exhausted the stack
		/// </returns>
		private bool Backtrack()
		{
			while (_stack.Count != 0)
			{
				_statistics.TimeToLastSolution = (DateTime.Now - _startTime).TotalMilliseconds;
				_statistics.TotalNbEvents = _problem.Scheduler.NbEventsActivated;
				_checkAbortion();
				if (RefuteLastLevel())
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		///   Tries to make and propagate a choice;
		/// </summary>
		private bool TryChoice(DisolverDecision d)
		{
			_statistics.TotalNbNodes++;
			if (Verbose)
			{
				Track();
			}
			_problem.Save();
			_stack.Push(d);
			_ranges.Push(d.Target.DomainSize);
			_refutations.PushList();
			return Perform(d, Cause.Decision);
		}

		/// <summary>
		///   Performs a bound decision and propagates it
		///   (also taking into account the best-known bounds if we
		///   are optimising)
		/// </summary>
		private bool Perform(DisolverDecision d, Cause cause)
		{
			DisolverDecision.Type tag = d.Tag;
			bool flag = ((tag != DisolverDecision.Type.ImposeLowerBound) ? d.Target.ImposeIntegerUpperBound(d.Value, cause) : d.Target.ImposeIntegerLowerBound(d.Value, cause));
			bool flag2 = flag && RefineBounds() && _problem.Simplify();
			if (!flag2)
			{
				FailureStatUpdates();
			}
			return flag2;
		}

		private void FailureStatUpdates()
		{
			_failDepth.RecordValue(_stack.Count);
			double num = 0.0;
			foreach (long range in _ranges)
			{
				num += Math.Log(range);
			}
			_estimatedTreeSize.RecordValue(num);
		}

		/// <summary>
		///   Imposes the negation of a decision and propagate it.
		///   The decision must be a bound constraint.
		/// </summary>
		/// <returns>true iff the refutation succeeded</returns>
		private bool RefuteLastLevel()
		{
			DisolverDecision d = _stack.Peek();
			ConflictDiagnostic conflictDiagnostic = default(ConflictDiagnostic);
			if (_problem.UseExplanations)
			{
				conflictDiagnostic = _problem.GetImplicationGraph().AnalyseConflictUIP(this);
				if (!conflictDiagnostic.Status)
				{
					_problem.UnplugImplicationGraph();
				}
			}
			Undo();
			DisolverDecision disolverDecision = Inverse(d);
			_refutations.AddToTopList(new Pair<DisolverDecision, VariableGroup>(disolverDecision, conflictDiagnostic.Cause.Signature));
			return Perform(disolverDecision, conflictDiagnostic.Cause);
		}

		/// <summary>
		/// Get the inverse of a decision, i.e. branches the other way around
		/// (restricted to lower/upper bound constraints)
		/// </summary>
		private static DisolverDecision Inverse(DisolverDecision d)
		{
			DisolverDecision.Type tag = d.Tag;
			if (tag == DisolverDecision.Type.ImposeLowerBound)
			{
				return DisolverDecision.ImposeUpperBound(d.Target, d.Value - 1);
			}
			return DisolverDecision.ImposeLowerBound(d.Target, d.Value + 1);
		}

		/// <summary>
		///   Undoes the last decision
		/// </summary>
		private void Undo()
		{
			_problem.Restore();
			_stack.Pop();
			_ranges.Pop();
			_refutations.PopList();
		}

		/// <summary>
		///   Undo all decisions because of a restart
		/// </summary>
		private void UndoAll()
		{
			while (_stack.Count != 0)
			{
				Undo();
			}
			_refutations.Clear();
			_refutations.PushList();
		}

		/// <summary>
		///   When doing optimization, updates the optimality constraints
		///   imposing to find solutions of quality better for one
		///   of the objectives.
		/// </summary>
		private bool RefineBounds()
		{
			if (Minimizing)
			{
				int kmin = 0;
				int kmax = _goalsToMinimize.Length - 1;
				for (int i = 0; i < _goalsToMinimize.Length; i++)
				{
					if (!ReviseGoal(i, ref kmin, ref kmax))
					{
						return false;
					}
				}
				for (int num = _goalsToMinimize.Length - 1; num >= 0; num--)
				{
					if (!ReviseGoal(num, ref kmin, ref kmax))
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Performs the propagation of the lexicographical constraint on
		/// (multiple) objectives specifically for objective number i.
		/// </summary>
		private bool ReviseGoal(int i, ref int kmin, ref int kmax)
		{
			IntegerVariable integerVariable = _goalsToMinimize[i];
			long num = _bestValuesFound[i];
			bool flag = true;
			if (i < kmin)
			{
				flag = integerVariable.ImposeIntegerValue(num, Cause.RootLevelDecision);
			}
			else if (i <= kmin)
			{
				bool flag2 = kmax == i && kmin == i;
				flag = integerVariable.ImposeUpperBound(flag2 ? (num - 1) : num, Cause.RootLevelDecision);
			}
			if (!flag)
			{
				return false;
			}
			if (integerVariable.LowerBound > num)
			{
				kmax = Math.Min(kmax, i - 1);
			}
			else if (integerVariable.UpperBound < num)
			{
				kmax = Math.Min(kmax, i);
			}
			else if (integerVariable.LowerBound == num)
			{
				if (kmax == i)
				{
					kmax--;
				}
				if (kmin == i)
				{
					kmin++;
				}
			}
			if (kmin > kmax)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		///   Performs the propagations required when starting
		///   to solve the problem.
		/// </summary>
		private bool InitialPropagations()
		{
			DateTime now = DateTime.Now;
			if (Verbose)
			{
				_output.Write(Resources.Preprocessing);
			}
			bool result = _problem.InitialSimplifications();
			if (Verbose)
			{
				double totalMilliseconds = (DateTime.Now - now).TotalMilliseconds;
				_output.WriteLine(Resources.PreprocessingDone);
				_output.WriteLine(Resources.PreprocessingTime0Ms, totalMilliseconds);
			}
			return result;
		}

		/// <summary>
		///   True if we are at level 0 i.e. no decision has been made
		/// </summary>
		public bool IsRootLevel()
		{
			return _stack.Count == 0;
		}

		/// <summary>
		///   Gets the last decision 
		/// </summary>
		public DisolverDecision LastDecision()
		{
			return _stack.Peek();
		}

		/// <summary>
		///   Undoes the propagations done at the last level
		///   and replays them entirely.
		/// </summary>
		public void Recompute()
		{
			_problem.Restore();
			_problem.Save();
			DisolverDecision d = _stack.Peek();
			bool flag = Perform(d, Cause.Decision);
			StackOfLists<Pair<DisolverDecision, VariableGroup>>.List list = _refutations.TopList();
			int length = list.Length;
			int num = 0;
			while (flag && num < length)
			{
				d = list[num].First;
				VariableGroup second = list[num].Second;
				flag = Perform(d, new Cause(null, second));
				num++;
			}
		}

		/// <summary>
		///   Prints some extra info on the output stream of the solver;
		///   typically called in end of search.
		/// </summary>
		public void PrintStats()
		{
			if (Verbose)
			{
				double totalMilliseconds = (DateTime.Now - _startTime).TotalMilliseconds;
				int totalNbNodes = _statistics.TotalNbNodes;
				long nbEventsActivated = _problem.Scheduler.NbEventsActivated;
				double num = 1000.0 * ((double)totalNbNodes / totalMilliseconds);
				double num2 = 1000.0 * ((double)nbEventsActivated / totalMilliseconds);
				_output.Write(Resources.TreeSearchStatsTotalTime0Ms, totalMilliseconds);
				_output.Write(Resources.TreeSearchStatsTotalNbNodes010, totalNbNodes);
				_output.Write(Resources.TreeSearchStatsPerSec, num);
				_output.Write(Resources.TreeSearchStatsTotalNbEvents010, nbEventsActivated);
				_output.Write(Resources.TreeSearchStatsPerSec, num2);
			}
		}

		/// <summary>
		///   Displays every 2 seconds a line of information 
		///   on the activity of the solver
		/// </summary>
		private void Track()
		{
			int totalNbNodes = _statistics.TotalNbNodes;
			if (totalNbNodes % 20 == 0)
			{
				DateTime now = DateTime.Now;
				double totalMilliseconds = (now - _lastTrackTime).TotalMilliseconds;
				double totalMilliseconds2 = (now - _startTime).TotalMilliseconds;
				if (totalMilliseconds >= 2000.0)
				{
					_lastTrackTime = now;
					_output.WriteLine(Resources.NodesSearchedIn1Ms, totalNbNodes, totalMilliseconds2);
					_output.WriteLine(Resources.AverageDepthEstimatedTreeSize01, _failDepth.Average, _estimatedTreeSize.Average);
				}
			}
		}
	}
}
