using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A Problem, as manipulated internally by backtrack search algorithms.
	/// </summary>
	/// <remarks>
	///   Every problem is meant to be used by a tree-search algorithm and
	///   cannot be shared. We've been considering to merge the two classes. 
	///   Why not; however: chronologically in the resolution process the
	///   problem is created first, only when completed are we in a position
	///   to really construct the tree search - so a unique class would require
	///   several steps: construction; addition of problem elements like
	///   variables and constraints; then a call to CreateAlgorithm(strategy).
	///   Also: one advantage of decoupling is that potentially we can use
	///   a treesearch algorithm on one problem THEN (sequentially, not in
	///   parallel) discard it and use a new treesearch
	/// </remarks>
	internal class Problem
	{
		/// <summary>
		///   Data that are stored when the bounds of an integer variable
		///   are modified
		/// </summary>
		private struct IntegerVariableArchive
		{
			public IntegerVariable _var;

			public long _previousLowerBound;

			public long _previousUpperBound;

			public int _previousDepth;

			public IntegerVariableArchive(IntegerVariable x, int vardepth)
			{
				_var = x;
				_previousLowerBound = x.LowerBound;
				_previousUpperBound = x.UpperBound;
				_previousDepth = vardepth;
			}
		}

		private bool _consistent;

		private bool _useExplanations;

		private ImplicationGraph _implicationGraph;

		private IntegerSolver _model;

		private Scheduler _scheduler;

		private int _depth;

		private Trail<int> _intTrail;

		private Trail<long> _longTrail;

		private TrailOfIntSets _intSetTrail;

		private TrailOfFiniteDomains _domainTrail;

		private StackOfLists<BooleanVariable> _instantiatedBooleanVars;

		private StackOfLists<IntegerVariableArchive> _modifiedIntegerVars;

		private IndexedCollection<DisolverConstraint> _constraints;

		private IndexedCollection<DiscreteVariable> _allDiscreteVariables;

		private SubSet<DiscreteVariable> _userDefinedDiscreteVariables;

		private List<DiscreteVariable> _orderedUserDefinedDiscreteVariables;

		private Dictionary<CspTerm, BooleanVariable> _term2bool;

		private Dictionary<CspTerm, IntegerVariable> _term2int;

		private Dictionary<long, IntegerVariable> _internalConstants;

		private BooleanVariable _false;

		private BooleanVariable _true;

		private CompilerToProblem _compiler;

		private Dictionary<Type, int> _info;

		private BasicEvent _initialPropagation;

		/// <summary>
		///   Number of saves done on the problem, 
		///   used by backtrackable datastructures
		/// </summary>
		internal int Depth => _depth;

		/// <summary>
		///   True if the problem requires to use explanations
		/// </summary>
		internal bool UseExplanations => _useExplanations;

		/// <summary>
		///   Access to the scheduler in which all events (e.g. variable
		///   state modifications) related to the problem will be stored
		/// </summary>
		internal Scheduler Scheduler => _scheduler;

		/// <summary>
		///   A trail to which any backtrackable int used in this problem
		///   should be connected to
		/// </summary>
		internal Trail<int> IntTrail => _intTrail;

		/// <summary>
		///   A trail to which any backtrackable long used in this problem
		///   should be connected to
		/// </summary>
		internal Trail<long> Longtrail => _longTrail;

		/// <summary>
		///   A trail to which any backtrackable Integer used in this problem
		///   should be connected to
		/// </summary>
		internal Trail<long> IntegerTrail => Longtrail;

		/// <summary>
		///   A trail to which any backtrackable int set used in this problem
		///   should be connected to
		/// </summary>
		internal TrailOfIntSets IntSetTrail => _intSetTrail;

		/// <summary>
		///   A trail to which any backtrackable domain used in this problem
		///   should be connected to
		/// </summary>
		internal TrailOfFiniteDomains DomainTrail => _domainTrail;

		/// <summary>
		///   Gets the set of discrete variables (Boolean and integer)
		///   defined explicitly by the user when she stated the problem.
		///   The variables will be returned in the order they were declared.
		/// </summary>
		internal IEnumerable<DiscreteVariable> UserDefinedVariables => _orderedUserDefinedDiscreteVariables;

		/// <summary>
		///   Gets the set of all discrete variables, user-defined
		///   or generated internally (Boolean and integer)
		/// </summary>
		internal IndexedCollection<DiscreteVariable> DiscreteVariables => _allDiscreteVariables;

		/// <summary>
		///   Get set of all constraints in the problem
		/// </summary>
		public IndexedCollection<DisolverConstraint> Constraints => _constraints;

		/// <summary>
		///   Gets the model (class Solver) from which this Problem
		///   was constructed
		/// </summary>
		internal IntegerSolver Source => _model;

		/// <summary>
		///   Returns a dictionary keeping the number of occurrences of
		///   each type of constraint
		/// </summary>
		internal Dictionary<Type, int> ConstraintCountByType => _info;

		/// <summary>
		///   handle to the compiler of the problem; available only
		///   during the compilation process; null afterwards.
		/// </summary>
		internal CompilerToProblem Compiler => _compiler;

		/// <summary>
		///   True if the problem has been proved inconsistent before
		///   any propagation or between propagations - e.g. preprocessing
		/// </summary>
		internal bool Inconsistent => !_consistent;

		private event VariableModification.Listener _variableModification;

		private event Procedure<Cause> _conflict;

		private event ParameterlessProcedure _problemSaved;

		private event ParameterlessProcedure _problemRestored;

		private event ParameterlessProcedure _solutionFound;

		private event Procedure<DiscreteVariable> _variableRestored;

		private event Procedure<DiscreteVariable> _variablePropagated;

		private event Procedure<DiscreteVariable> _variableUninstantiated;

		/// <summary>
		///   Construction, given a model (Disolver.Problem).
		/// </summary>
		public Problem(IntegerSolver model, CompilerToProblem comp, bool useExplanations)
		{
			_useExplanations = useExplanations;
			_model = model;
			_scheduler = new Scheduler(model.CheckAbortion);
			_intTrail = new Trail<int>();
			_longTrail = new Trail<long>();
			_intSetTrail = new TrailOfIntSets();
			_domainTrail = new TrailOfFiniteDomains();
			_constraints = new IndexedCollection<DisolverConstraint>();
			_allDiscreteVariables = new IndexedCollection<DiscreteVariable>();
			_userDefinedDiscreteVariables = new SubSet<DiscreteVariable>(_allDiscreteVariables);
			_orderedUserDefinedDiscreteVariables = new List<DiscreteVariable>();
			_instantiatedBooleanVars = new StackOfLists<BooleanVariable>(clean: false);
			_modifiedIntegerVars = new StackOfLists<IntegerVariableArchive>(clean: false);
			_instantiatedBooleanVars.PushList();
			_modifiedIntegerVars.PushList();
			_term2bool = new Dictionary<CspTerm, BooleanVariable>();
			_term2int = new Dictionary<CspTerm, IntegerVariable>();
			_internalConstants = new Dictionary<long, IntegerVariable>();
			_false = new BooleanVariable(this, BooleanVariableState.False);
			_true = new BooleanVariable(this, BooleanVariableState.True);
			_info = new Dictionary<Type, int>();
			_compiler = comp;
			_consistent = true;
		}

		/// <summary>
		///   Adds a new Boolean variable to the problem and returns it.
		/// </summary>
		/// <param name="src">model variable from which we derive</param>
		public BooleanVariable CreateBooleanVariable(DisolverBooleanTerm src)
		{
			BooleanVariable booleanVariable;
			switch (src.InitialStatus)
			{
			case BooleanVariableState.False:
				booleanVariable = _false;
				break;
			case BooleanVariableState.True:
				booleanVariable = _true;
				break;
			default:
				booleanVariable = new BooleanVariable(this, src);
				if (src.IsUserDefined())
				{
					_userDefinedDiscreteVariables.Add(booleanVariable);
					_orderedUserDefinedDiscreteVariables.Add(booleanVariable);
				}
				break;
			}
			Associate(src, booleanVariable);
			return booleanVariable;
		}

		/// <summary>
		///   Gets the pre-allocated boolean variable 
		///   for constant true
		/// </summary>
		internal BooleanVariable GetBooleanTrue()
		{
			return _true;
		}

		/// <summary>
		///   Gets the pre-allocated boolean variable 
		///   for constant false
		/// </summary>
		internal BooleanVariable GetBooleanFalse()
		{
			return _false;
		}

		/// <summary>
		///   Construction (for internal use) of a Boolean variable that 
		///   does not correspond to a term in the initial problem.
		///   Its initial state is unassigned.
		/// </summary>
		internal BooleanVariable CreateInternalBooleanVariable()
		{
			return new BooleanVariable(this);
		}

		/// <summary>
		///   Construction (for internal use) of an array of Boolean variables
		///   that do not correspond to a term in the initial problem.
		///   Their initial state is unassigned.
		/// </summary>
		internal BooleanVariable[] CreateInternalBooleanVariableArray(int size)
		{
			BooleanVariable[] array = new BooleanVariable[size];
			for (int num = size - 1; num >= 0; num--)
			{
				array[num] = CreateInternalBooleanVariable();
			}
			return array;
		}

		/// <summary>
		///   Adds a new Integer variable to the problem and returns it.
		/// </summary>
		/// <param name="src">model variable from which we derive</param>
		public IntegerVariable CreateIntegerVariable(DisolverIntegerTerm src)
		{
			IntegerVariable integerVariable;
			if (src.IsInstantiated())
			{
				integerVariable = GetIntegerConstant(src.InitialUpperBound);
			}
			else
			{
				integerVariable = new IntegerVariable(this, src);
				if (src.IsUserDefined())
				{
					_userDefinedDiscreteVariables.Add(integerVariable);
					_orderedUserDefinedDiscreteVariables.Add(integerVariable);
				}
			}
			Associate(src, integerVariable);
			return integerVariable;
		}

		/// <summary>
		///   Construction (for internal use) of an integer variable that 
		///   does not correspond to a term in the initial problem
		/// </summary>
		/// <param name="l">initial lower bound</param>
		/// <param name="r">initial upper bound</param>
		/// <returns></returns>
		internal IntegerVariable CreateInternalIntegerVariable(long l, long r)
		{
			if (l == r)
			{
				return GetIntegerConstant(l);
			}
			return new IntegerVariable(this, l, r);
		}

		/// <summary>
		///   Construction (for internal use) of an integer variable that 
		///   does not correspond to a term in the initial problem.
		///   Its range will be fixed to default.
		/// </summary>
		internal IntegerVariable CreateInternalIntegerVariable()
		{
			return CreateInternalIntegerVariable(-4611686018427387903L, 4611686018427387903L);
		}

		/// <summary>
		///   Gets the Integer variable representing s constant.
		///   (cached: we create one variable per constant)
		/// </summary>
		internal IntegerVariable GetIntegerConstant(long cst)
		{
			if (!_internalConstants.TryGetValue(cst, out var value))
			{
				value = new IntegerVariable(this, cst, cst);
				_internalConstants.Add(cst, value);
			}
			return value;
		}

		/// <summary>
		///   Informs the problem that the image if an integer term
		///   is a certain integer variable
		/// </summary>
		internal void Associate(DisolverIntegerTerm t, IntegerVariable x)
		{
			_term2int.Add(t, x);
		}

		/// <summary>
		///   Informs the problem that the image if a Boolean term
		///   is a certain Boolean variable
		/// </summary>
		internal void Associate(DisolverBooleanTerm t, BooleanVariable x)
		{
			_term2bool.Add(t, x);
		}

		/// <summary>
		///   Add a new constraint to the problem
		/// </summary>
		public void AddConstraint(DisolverConstraint c)
		{
		}

		/// <summary>
		///   Save the problem, so that any modification to it can be
		///   undone (in a LIFO fashion) by a call to Restore.
		/// </summary>
		/// <remarks>
		///   The discipline we impose for event handling and scheduling
		///   here is the following: the scheduler must systematically
		///   be empty when we save, it must be empty when we restore.
		/// </remarks>
		public void Save()
		{
			_scheduler.UnScheduleAll();
			_intTrail.Save();
			_longTrail.Save();
			_intSetTrail.Save();
			_domainTrail.Save();
			_instantiatedBooleanVars.PushList();
			_modifiedIntegerVars.PushList();
			_depth++;
			if (this._problemSaved != null)
			{
				this._problemSaved();
			}
		}

		/// <summary>
		///   Restore the problem, i.e. go back to the state it was in
		///   before the last Save operation.
		/// </summary>
		public void Restore()
		{
			_scheduler.UnScheduleAll();
			_intTrail.Restore();
			_longTrail.Restore();
			_intSetTrail.Restore();
			_domainTrail.Restore();
			RestoreBooleanVariables();
			RestoreIntegerVariables();
			_depth--;
			if (this._problemRestored != null)
			{
				this._problemRestored();
			}
		}

		/// <summary>
		///   Performs simplifcations (propagation) to the problem.
		/// </summary>
		/// <returns>
		///   False if inconsistency detected
		/// </returns>
		public bool Simplify()
		{
			bool result = _scheduler.Activate();
			if (this._variablePropagated != null)
			{
				StackOfLists<BooleanVariable>.List list = _instantiatedBooleanVars.TopList();
				int length = list.Length;
				for (int i = 0; i < length; i++)
				{
					BooleanVariable arg = list[i];
					this._variablePropagated(arg);
				}
				StackOfLists<IntegerVariableArchive>.List list2 = _modifiedIntegerVars.TopList();
				int length2 = list2.Length;
				for (int j = 0; j < length2; j++)
				{
					IntegerVariableArchive integerVariableArchive = list2[j];
					this._variablePropagated(integerVariableArchive._var);
				}
			}
			return result;
		}

		/// <summary>
		///   Performs initial simplifications (propagation) to the problem.
		/// </summary>
		/// <returns>
		///   False if inconsistency detected
		/// </returns>
		public bool InitialSimplifications()
		{
			if (_initialPropagation != null)
			{
				_initialPropagation.RescheduleIfNeeded();
				if (!Simplify())
				{
					return false;
				}
			}
			foreach (DiscreteVariable item in DiscreteVariables.Enumerate())
			{
				item.ScheduleInitialEvents();
			}
			if (_model.UseShaving)
			{
				return Simplify() && Shave();
			}
			return Simplify();
		}

		/// <summary>
		///   The listener will be immediately called when there is a conflict
		/// </summary>
		public void SubscribeToConflicts(Procedure<Cause> listener)
		{
			_conflict += listener;
		}

		/// <summary>
		///   The listener will be immediately called when the problem is saved
		/// </summary>
		public void SubscribeToProblemSaved(ParameterlessProcedure listener)
		{
			_problemSaved += listener;
		}

		public void UnsubscribeToProblemSaved(ParameterlessProcedure listener)
		{
			_problemSaved -= listener;
		}

		/// <summary>
		///   The listener will be immediately called when the problem is restored
		/// </summary>
		public void SubscribeToProblemRestored(ParameterlessProcedure listener)
		{
			_problemRestored += listener;
		}

		public void UnsubscribeToProblemRestored(ParameterlessProcedure listener)
		{
			_problemRestored -= listener;
		}

		/// <summary>
		/// The listener will be immediately called when any variable is restored
		/// </summary>
		public void SubscribeToVariableRestored(Procedure<DiscreteVariable> listener)
		{
			_variableRestored += listener;
		}

		/// <summary>
		/// The listener will be called when an instantiated variable is restored
		/// </summary>
		public void SubscribeToVariableUninstantiated(Procedure<DiscreteVariable> listener)
		{
			_variableUninstantiated += listener;
		}

		/// <summary>
		///   The listener will be called every time propagation is finished, 
		///   for each variable that has been modified
		/// </summary>
		public void SubscribeToVariablePropagated(Procedure<DiscreteVariable> listener)
		{
			_variablePropagated += listener;
		}

		/// <summary>
		///   The listener will be called immediately whenever any variable is
		///   modified by the propagation process.
		/// </summary>
		/// <remarks>
		///   Use sparingly as this would be called very often; use for 
		///   explanations that require very fine-grained trace of solver
		///   deductions.
		/// </remarks>
		public void SubscribeToVariableModification(VariableModification.Listener listener)
		{
			_variableModification += listener;
		}

		public void UnsubscribeToVariableModification(VariableModification.Listener listener)
		{
			_variableModification -= listener;
		}

		/// <summary>
		///   The listener will be called whenever a solution is completed
		///   (useful essentially for assertions)
		/// </summary>
		public void SubscribeToSolutionFound(ParameterlessProcedure listener)
		{
			_solutionFound += listener;
		}

		/// <summary>
		///   The listener will be called during the propagation loop
		///   that is started before the first search node. 
		/// </summary>
		public void SubscribeToInitialPropagation(BasicEvent.Listener l)
		{
			if (_initialPropagation == null)
			{
				_initialPropagation = new BasicEvent(_scheduler);
			}
			_initialPropagation.Subscribe(l);
		}

		/// <summary>
		///   Called whenever a variable becomes inconsistent;
		///   used essentially to keep track of causes.
		/// </summary>
		internal void SignalFailure(DiscreteVariable x, Cause c)
		{
			if (this._variableModification != null)
			{
				this._variableModification(new VariableModification(x, c));
			}
			if (this._conflict != null)
			{
				this._conflict(c);
			}
		}

		/// <summary>
		///   Called by a Boolean Variable to notify the problem
		///   that it has just been instantiated - this causes
		///   a save of the var and a dispatch to all listeners
		/// </summary>
		internal void SignalBooleanVariableInstantiation(BooleanVariable x, Cause c)
		{
			_instantiatedBooleanVars.AddToTopList(x);
			if (this._variableModification != null)
			{
				this._variableModification(new VariableModification(x, c));
			}
		}

		/// <summary>
		///   Called by integer variable to notify the problem
		///   that its domain is about to be narrowed. 
		/// </summary>
		internal void Save(IntegerVariable x)
		{
			int depth = Depth;
			int depthOfLastSave = x.DepthOfLastSave;
			if (depthOfLastSave != depth)
			{
				_modifiedIntegerVars.AddToTopList(new IntegerVariableArchive(x, depthOfLastSave));
				x.DepthOfLastSave = depth;
			}
		}

		/// <summary>
		///   Called by integer variable to inform anyone
		///   interested that the variable has been modified
		/// </summary>
		internal void DispatchVariableModification(IntegerVariable x, Cause c)
		{
			if (this._variableModification != null)
			{
				this._variableModification(new VariableModification(x, c));
			}
		}

		/// <summary>
		///   Unistantiates all Boolean variables instantiated at
		///   current level
		/// </summary>
		private void RestoreBooleanVariables()
		{
			StackOfLists<BooleanVariable>.List list = _instantiatedBooleanVars.TopList();
			int length = list.Length;
			bool flag = this._variableRestored != null;
			bool flag2 = this._variableUninstantiated != null;
			for (int i = 0; i < length; i++)
			{
				BooleanVariable booleanVariable = list[i];
				booleanVariable.Uninstantiate();
				if (flag)
				{
					this._variableRestored(booleanVariable);
				}
				if (flag2)
				{
					this._variableUninstantiated(booleanVariable);
				}
			}
			_instantiatedBooleanVars.PopList();
		}

		/// <summary>
		///   Undoes all modifications made to the Boolean
		///   variables at the current level.
		/// </summary>
		private void RestoreIntegerVariables()
		{
			StackOfLists<IntegerVariableArchive>.List list = _modifiedIntegerVars.TopList();
			int length = list.Length;
			bool flag = this._variableRestored != null;
			bool flag2 = this._variableUninstantiated != null;
			for (int i = 0; i < length; i++)
			{
				IntegerVariableArchive integerVariableArchive = list[i];
				IntegerVariable var = integerVariableArchive._var;
				bool flag3 = flag2 && var.IsInstantiated();
				var.RestoreState(integerVariableArchive._previousLowerBound, integerVariableArchive._previousUpperBound, integerVariableArchive._previousDepth);
				if (flag)
				{
					this._variableRestored(var);
				}
				if (flag3)
				{
					this._variableUninstantiated(var);
				}
			}
			_modifiedIntegerVars.PopList();
		}

		/// <summary>
		///   Get the implication graph associated to the problem
		/// </summary>
		/// <remarks>
		///   The implication graph is only constructed for heuristics
		///   that explictly need it. This is determined at construction
		///   of the problem and there is a switch to update in 
		///   CompilerToProblem for nay new Heuristic that needs it.
		/// </remarks>
		internal ImplicationGraph GetImplicationGraph()
		{
			if (_implicationGraph == null)
			{
				_implicationGraph = new ImplicationGraph(this);
			}
			return _implicationGraph;
		}

		internal void UnplugImplicationGraph()
		{
			_useExplanations = false;
		}

		internal void ReplugImplicationGraph()
		{
			_useExplanations = true;
		}

		/// <summary>
		///   True iff the variable is user-defined
		/// </summary>
		internal bool IsUserDefined(DiscreteVariable x)
		{
			return _userDefinedDiscreteVariables.Contains(x);
		}

		/// <summary>
		///   Get the Boolean Variable representing an original 
		///   Boolean Term in this problem
		/// </summary>
		internal BooleanVariable GetImage(DisolverBooleanTerm src)
		{
			_term2bool.TryGetValue(src, out var value);
			return value;
		}

		/// <summary>
		///   Get the Integer Variable representing an original 
		///   Integer Term in this problem
		/// </summary>
		internal IntegerVariable GetImage(DisolverIntegerTerm src)
		{
			_term2int.TryGetValue(src, out var value);
			return value;
		}

		/// <summary>
		///   Creates a dictionary from terms to ints
		///   representing a solution
		/// </summary>
		internal Dictionary<CspTerm, object> GetSolution()
		{
			if (this._solutionFound != null)
			{
				this._solutionFound();
			}
			Dictionary<CspTerm, object> dictionary = new Dictionary<CspTerm, object>(_term2bool.Count + _term2int.Count);
			foreach (KeyValuePair<CspTerm, BooleanVariable> item in _term2bool)
			{
				BooleanVariable value = item.Value;
				dictionary.Add(item.Key, value.GetValue() ? 1 : 0);
			}
			foreach (KeyValuePair<CspTerm, IntegerVariable> item2 in _term2int)
			{
				IntegerVariable value2 = item2.Value;
				dictionary.Add(item2.Key, (int)value2.GetValue());
			}
			return dictionary;
		}

		/// <summary>
		///   Called by compiler when compilation finished so that
		///   handle is freed and compiler can be GC-ed.
		/// </summary>
		internal void EndCompilation()
		{
			_compiler = null;
		}

		/// <summary>
		///   Adds a constraint that will cause inconsistency
		///   when the first propagation is started. Use to signal
		///   inconsistency during e.g. pre-processing or construction
		/// </summary>
		internal void AddFalsity()
		{
			_consistent = false;
		}

		/// <summary>
		///   Computes statistics on the problem in a given output
		/// </summary>
		internal void ComputeStatistics(TextWriter output, ref TreeSearchStatistics stats)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			foreach (DiscreteVariable item in DiscreteVariables.Enumerate())
			{
				if (item is IntegerVariable)
				{
					num2++;
				}
				else if (item is BooleanVariable)
				{
					num++;
				}
			}
			foreach (DiscreteVariable userDefinedVariable in UserDefinedVariables)
			{
				if (userDefinedVariable is IntegerVariable)
				{
					num4++;
				}
				else if (userDefinedVariable is BooleanVariable)
				{
					num3++;
				}
			}
			long elapsedMilliSec = _model.ElapsedMilliSec;
			int cardinality = _constraints.Cardinality;
			stats.NbBooleanVariables = num;
			stats.NbIntegerVariables = num2;
			stats.NbUserDefinedBooleanVariables = num3;
			stats.NbUserDefinedIntegerVariables = num4;
			stats.NbConstraints = cardinality;
			if (output != null)
			{
				output.Write(Resources.ComputeStatisticsDelaySinceStart0Ms, elapsedMilliSec);
				output.Write(Resources.ComputeStatisticsBoolVars010, num);
				output.Write(Resources.ComputeStatisticsUserdefined08, num3);
				output.Write(Resources.ComputeStatisticsIntVars010, num2);
				output.Write(Resources.ComputeStatisticsUserdefined08, num4);
				output.Write(Resources.ComputeStatisticsConstraints010, cardinality);
			}
		}

		/// <summary>
		///   naive implementation of a leightweight form
		///   of singleton-arc consistency
		/// </summary>
		/// <returns>false iff inconsistency found</returns>
		internal bool Shave()
		{
			foreach (DiscreteVariable item in _userDefinedDiscreteVariables.Enumerate())
			{
				if (!item.CheckIfInstantiated())
				{
					long lowerBound = item.GetLowerBound();
					long upperBound = item.GetUpperBound();
					double num = upperBound - lowerBound + 1;
					long num2 = lowerBound + (long)(num * 0.2);
					if (!SampleVariable(item, lowerBound, num2) && (!item.ImposeIntegerLowerBound(num2 + 1) || !Simplify()))
					{
						return false;
					}
				}
				if (!item.CheckIfInstantiated())
				{
					long lowerBound2 = item.GetLowerBound();
					long upperBound2 = item.GetUpperBound();
					double num3 = upperBound2 - lowerBound2 + 1;
					long num4 = upperBound2 - (long)(num3 * 0.2);
					if (!SampleVariable(item, num4, upperBound2) && (!item.ImposeIntegerUpperBound(num4 - 1) || !Simplify()))
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		///
		///   If we impose that variable x range within [lb, ub] and propagate,
		///   do we obtain a contradiction?
		/// </summary>
		internal bool SampleVariable(DiscreteVariable x, long lb, long ub)
		{
			Save();
			bool result = x.ImposeIntegerLowerBound(lb) && x.ImposeIntegerUpperBound(ub) && Simplify();
			Restore();
			return result;
		}
	}
}
