using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Root class for local search algorithms
	/// </summary>
	internal sealed class LocalSearchSolver : CspSolver, ILocalSearchProcess
	{
		/// <summary>
		/// Gradient Information attached to a Term
		/// </summary>
		private struct GradientInfo
		{
			public int DecGradient;

			public CspVariable DecVariable;

			public int IncGradient;

			public CspVariable IncVariable;

			public ValueWithGradients GetValueWithGradients(int val)
			{
				return new ValueWithGradients(val, DecGradient, DecVariable, IncGradient, IncVariable);
			}

			/// <summary>
			/// Assigns all the fields of the gradient info
			/// </summary>
			/// <returns>
			/// True iff a change occurred
			/// </returns>
			public bool Load(int decgrad, CspVariable decvar, int incgrad, CspVariable incvar)
			{
				bool flag = DecGradient != decgrad || DecVariable != decvar || IncGradient != incgrad || IncVariable != incvar;
				if (flag)
				{
					DecGradient = decgrad;
					DecVariable = decvar;
					IncGradient = incgrad;
					IncVariable = incvar;
				}
				return flag;
			}
		}

		/// <summary> Modified terms that need to be considered in re-evaluation; 
		///           Each is paired with its Value before modification. 
		///           The terms are ranked by depth in order to allow efficient, 
		///           sound re-evaluation.
		/// </summary>
		private List<KeyValuePair<CspSolverTerm, int>>[] _modifiedTerms;

		/// <summary> Values currently associated to all terms </summary> 
		private int[] _values;

		/// <summary> For each term we keep a flag saying whether it's enqueued
		/// </summary>
		private bool[] _isEnqueued;

		private int[] _extraDataForBooleanAnd;

		private int[] _extraDataForBooleanOr;

		private Unequal.LocalSearchData[] _extraDataForUnequal;

		/// <summary> Gradient Info associated to all terms</summary>
		private GradientInfo[] _gradientInfo;

		/// <summary> 
		/// Variables that need to be considered by the 
		/// next gradient recomputation 
		/// </summary>
		private List<CspSolverTerm> _postponedVariables;

		/// <summary>
		/// We keep track of variables that are filtered, i.e. 
		/// their gradients are temporarily fixed to 0, meaning
		/// that we (often temporarily) do not wish to modify its value
		/// </summary>
		private BitArray _filtered;

		/// <summary> quality of the best solution found by the local search so far
		/// </summary>
		private AggregateQuality _bestQuality;

		/// <summary> quality of the current solution
		/// </summary>
		private AggregateQuality _currentQuality;

		/// <summary>A quality that can be used in terminal calls to keep track of
		///          the quality before a move
		/// </summary>
		private AggregateQuality _oldQuality;

		/// <summary> Pseudo-random number generator used in this algorithm
		/// </summary>
		private Random _prng;

		/// <summary> A counter that is increased every time we do a recomputation
		/// </summary>
		/// <remarks> Int64 may look like an overkill but we want to be on the safe
		///           side even for a local search which would be run for weeks,
		///           in which case 2 billion moves might sometimes be conceivable
		/// </remarks>
		private long _step;

		/// <summary> True if the data-structures for gradients is
		///           allocated and gradients should be recomputed
		/// </summary>
		/// <remarks> gradients multiply the memory allocated by each
		///           local search thread by some factor; so in principle it
		///           may make sense to disable them if the heuritic tuning
		///           that is being used does not require them
		/// </remarks>
		private bool _useGradients;

		/// <summary> Number of unit changes done in this algorithm;
		///           this counts the iterations of the inner recomputation loop
		/// </summary>
		private long _numTermRecomputations;

		/// <summary>Delegate specifying the restart strategy:
		///          returns true iff a restart is needed
		/// </summary>
		private LocalSearch.RestartStrategy _restartAdvisor;

		/// <summary>Delegate specifying the initialization strategy:
		///          returns true iff a restart is needed
		/// </summary>
		private LocalSearch.InitializationStrategy _initializationAdvisor;

		/// <summary>Delegate specifying the move strategy:
		///          returns the next move that the local search should perform
		/// </summary>
		private LocalSearch.MoveStrategy _moveAdvisor;

		/// <summary>Delegate specifying the acceptance strategy:
		///          returns true if a move should be accepted
		/// </summary>
		private LocalSearch.AcceptanceStrategy _acceptanceAdvisor;

		/// <summary> When an overflow is raised this keeps the place where it happened
		/// </summary>
		private CspSolverTerm _overflowCause;

		/// <summary>
		/// Set of (the Ordinals of the) violated constraints, maintained incrementally; 
		/// </summary>
		private FiniteIntSet _violatedConstraints;

		/// <summary> Gets / sets the value currently associated to the 
		///           term by the local search algorithm
		/// </summary>
		public int this[CspSolverTerm expr]
		{
			get
			{
				return _values[expr.Ordinal];
			}
			internal set
			{
				int ordinal = expr.Ordinal;
				if (value != _values[ordinal])
				{
					MarkAsModified(expr);
					_values[ordinal] = value;
				}
			}
		}

		/// <summary> Get the pseudo-random number generator of the solver
		/// </summary>
		internal Random RandomSource => _prng;

		/// <summary> Quality of the point currently considered by the algorithm
		/// </summary>
		internal AggregateQuality CurrentQuality => _currentQuality;

		/// <summary> Quality of the best solution explored so far by this algorithm
		/// </summary>
		internal AggregateQuality BestQuality => _bestQuality;

		/// <summary> True iff the current state satisfies all hard constraints
		/// </summary>
		internal bool Satisfying => CurrentViolation == 0;

		/// <summary> True if the local search should terminate
		/// </summary>
		public bool End => _model.CheckAbort();

		/// <summary> True if a numerical error happened during the last
		///           recomputation. 
		/// </summary>
		public bool Overflow => _currentQuality.Overflow;

		/// <summary> Gets the number of recomputations performed by this algorithm;
		///           for statistics
		/// </summary>
		internal long NumberOfRecomputations => _step;

		/// <summary>
		/// Checks / specifies whether the data-structures
		/// for gradient should be allocated and gradients be computed
		/// </summary>
		internal bool UseGradients
		{
			get
			{
				return _useGradients;
			}
			set
			{
				if (value)
				{
					if (_useGradients)
					{
						return;
					}
					_useGradients = true;
					_gradientInfo = new GradientInfo[_values.Length];
					_postponedVariables = new List<CspSolverTerm>();
					_filtered = new BitArray(_values.Length, defaultValue: false);
					foreach (CspSolverTerm allTerm in _model.AllTerms)
					{
						if (allTerm.Depth == 0)
						{
							allTerm.RecomputeGradients(this);
						}
					}
					if (UseGradients)
					{
						_filtered[_model._TFalse.Ordinal] = true;
						_filtered[_model._TTrue.Ordinal] = true;
					}
					InitializeValuesAndGradients();
				}
				else if (_useGradients)
				{
					_useGradients = false;
					_gradientInfo = null;
					_postponedVariables = null;
					_filtered = null;
				}
			}
		}

		/// <summary>Gets the constraint violation, an indicator of how well
		///          the current configuration violates or satisfies the constraints
		/// </summary>
		/// <remarks>In case of the overflow flag is raised we systematically
		///          return the highest possible violation
		/// </remarks>
		public int CurrentViolation
		{
			get
			{
				if (!_currentQuality.Overflow)
				{
					return _currentQuality.Violation;
				}
				return int.MaxValue;
			}
		}

		/// <summary>Gets the constraint system solved by this Local Search solver
		/// </summary>
		public ConstraintSystem Model => _model;

		/// <summary>Event raised when a move has been attempted
		/// </summary>
		private event LocalSearch.MoveListener _moveAttempted;

		/// <summary>Event raised when a restart has occurred
		/// </summary>
		private event Action _restartOccurred;

		/// <summary>A generic local search solver for finite CSPs
		/// </summary>
		/// <param name="model">The model containing all information
		///          on the instance to be solved 
		/// </param>
		internal LocalSearchSolver(ConstraintSystem model)
			: this(model, ConstraintSolverParams.LocalSearchMove.Greedy)
		{
		}

		/// <summary>A generic local search solver for finite CSPs
		/// </summary>
		/// <param name="model">The model containing all information
		///          on the instance to be solved 
		/// </param>
		/// <param name="strategy">A parameter (optional) specifying
		///           one of the pre-defined combinations of strategies
		/// </param>
		internal LocalSearchSolver(ConstraintSystem model, ConstraintSolverParams.LocalSearchMove strategy)
			: base(model)
		{
			InitializeInternals();
			InitializeCallbacks(strategy);
		}

		/// <summary>Construction of a local search algorithm,
		///          customized by a number of call-backs
		/// </summary>
		/// <param name="model">The model containing all information
		///          on the instance to be solved 
		/// </param>
		/// <param name="restart">Delegate specifying the restart strategy:
		///          returns true iff a restart is needed
		/// </param>
		/// <param name="reset">Delegate specifying the initialization strategy:
		///          returns a value for each variable that needs to be reset
		/// </param>
		/// <param name="move">Delegate specifying the move strategy:
		///          returns the next move that the local search should perform
		/// </param>
		/// <param name="accept">Delegate specifying the acceptance strategy:
		///          returns true if a move should be accepted
		/// </param>
		internal LocalSearchSolver(ConstraintSystem model, LocalSearch.RestartStrategy restart, LocalSearch.InitializationStrategy reset, LocalSearch.MoveStrategy move, LocalSearch.AcceptanceStrategy accept)
			: base(model)
		{
			InitializeInternals();
			_restartAdvisor = restart;
			_initializationAdvisor = reset;
			_moveAdvisor = move;
			_acceptanceAdvisor = accept;
		}

		/// <summary>Allocate all fields from the Model
		///          (called during construction)
		/// </summary>
		private void InitializeInternals()
		{
			int num = _model.AllTerms.Count();
			int num2 = (from CspSolverTerm t in _model._allTerms
				select t.Depth).Max();
			_violatedConstraints = new FiniteIntSet(0, num);
			_isEnqueued = new bool[num];
			_values = new int[num];
			_extraDataForBooleanAnd = new int[_model._numBooleanAndTerms];
			_extraDataForBooleanOr = new int[_model._numBooleanOrTerms];
			_bestQuality = new AggregateQuality(_model);
			_currentQuality = new AggregateQuality(_model);
			_oldQuality = new AggregateQuality(_model);
			_extraDataForUnequal = new Unequal.LocalSearchData[_model._numUnequalTerms];
			_modifiedTerms = new List<KeyValuePair<CspSolverTerm, int>>[num2 + 1];
			_prng = new Random(1234);
			for (int i = 0; i < _modifiedTerms.Length; i++)
			{
				_modifiedTerms[i] = new List<KeyValuePair<CspSolverTerm, int>>();
			}
			foreach (CspSolverTerm allTerm in _model.AllTerms)
			{
				if (allTerm.Depth == 0)
				{
					int first = allTerm.FiniteValue.First;
					_values[allTerm.Ordinal] = ToInternalRepresentation(allTerm, first);
				}
			}
			_values[_model._TFalse.Ordinal] = BooleanFunction.Violated;
			_values[_model._TTrue.Ordinal] = BooleanFunction.Satisfied;
			InitializeValuesAndGradients();
		}

		/// <summary>Create the callbacks from a pre-selected choice
		///          (called during construction)
		/// </summary>
		private void InitializeCallbacks(ConstraintSolverParams.LocalSearchMove strategy)
		{
			int randomSeed = _prng.Next();
			switch (strategy)
			{
			default:
				_restartAdvisor = LocalSearch.RestartWhenThresholdHit(0.2);
				_initializationAdvisor = LocalSearch.InitializeRandomly(randomSeed, 0.2);
				_moveAdvisor = LocalSearch.MoveByGreedyImprovements(randomSeed);
				_acceptanceAdvisor = LocalSearch.AcceptIfImproved();
				break;
			case ConstraintSolverParams.LocalSearchMove.GreedyNoise:
			{
				LocalSearch.MoveStrategy move = LocalSearch.MoveByGreedyImprovements(randomSeed);
				LocalSearch.AcceptanceStrategy accept = LocalSearch.AcceptIfImproved();
				LS_NoiseStrategy @object = new LS_NoiseStrategy(move, accept, randomSeed, 0.05);
				_restartAdvisor = LocalSearch.RestartWhenThresholdHit(0.05);
				_initializationAdvisor = LocalSearch.InitializeRandomly(randomSeed, 1.0);
				_moveAdvisor = @object.NextMove;
				_acceptanceAdvisor = @object.Accept;
				break;
			}
			case ConstraintSolverParams.LocalSearchMove.Tabu:
				_restartAdvisor = LocalSearch.RestartWhenThresholdHit(0.05);
				_initializationAdvisor = LocalSearch.InitializeRandomly(randomSeed, 1.0);
				_moveAdvisor = LocalSearch.MoveUsingTabu(randomSeed);
				_acceptanceAdvisor = LocalSearch.AcceptAlways();
				break;
			case ConstraintSolverParams.LocalSearchMove.SimulatedAnnealing:
				_restartAdvisor = LocalSearch.RestartNever();
				_initializationAdvisor = LocalSearch.InitializeRandomly(randomSeed, 1.0);
				_moveAdvisor = LocalSearch.MoveRandomly(randomSeed);
				_acceptanceAdvisor = LocalSearch.AcceptWithSimulatedAnnealing(randomSeed);
				break;
			case ConstraintSolverParams.LocalSearchMove.Gradients:
				_restartAdvisor = LocalSearch.RestartWhenStuckForTooLong();
				_initializationAdvisor = LocalSearch.InitializeRandomly(randomSeed, 0.2);
				_moveAdvisor = LocalSearch.MoveByGradientGuidedFlips(randomSeed);
				_acceptanceAdvisor = LocalSearch.AcceptIfImproved();
				break;
			}
		}

		/// <summary> Recomputes the values of all Terms
		///           in a naive, non-incremental way;
		///           if gradients are used they will be recomputed too
		/// </summary>
		internal void InitializeValuesAndGradients()
		{
			_currentQuality.Overflow = false;
			_overflowCause = null;
			_step++;
			if (_useGradients)
			{
				_postponedVariables.Clear();
			}
			Array.Clear(_isEnqueued, 0, _isEnqueued.Length);
			List<KeyValuePair<CspSolverTerm, int>>[] modifiedTerms = _modifiedTerms;
			foreach (List<KeyValuePair<CspSolverTerm, int>> list in modifiedTerms)
			{
				list.Clear();
			}
			foreach (CspSolverTerm allTerm in _model.AllTerms)
			{
				MarkAsModified(allTerm);
			}
			for (int j = 0; j < _modifiedTerms.Length; j++)
			{
				RecomputeModifiedTerms(_modifiedTerms[j]);
			}
			Array.Clear(_isEnqueued, 0, _isEnqueued.Length);
			RecomputeCurrentQuality(incremental: false);
		}

		/// <summary> Recomputes the value of all differentiable expressions
		///           in an incremental way by propagating changes
		/// </summary>
		private void Recompute(bool recomputeValues, bool recomputeGradients)
		{
			if (Overflow)
			{
				InitializeValuesAndGradients();
				return;
			}
			if (_useGradients)
			{
				if (_postponedVariables.Count > 1000)
				{
					InitializeValuesAndGradients();
					return;
				}
				UpdatePostponedVariables(recomputeGradients);
			}
			_step++;
			for (int i = 0; i < _modifiedTerms.Length; i++)
			{
				DispatchModifiedTerms(_modifiedTerms[i], recomputeValues, recomputeGradients);
			}
			RecomputeCurrentQuality(incremental: true);
		}

		/// <summary>
		/// Recompute (non-incrementally) the values and gradients of
		/// all terms in the given list. 
		/// This method is the inner loop of Initialization method
		/// </summary>
		private void RecomputeModifiedTerms(List<KeyValuePair<CspSolverTerm, int>> list)
		{
			foreach (KeyValuePair<CspSolverTerm, int> item in list)
			{
				CspSolverTerm key = item.Key;
				key.RecomputeValue(this);
				if (_useGradients)
				{
					key.RecomputeGradients(this);
				}
				if (key.IsConstraint)
				{
					UpdateViolatedConstraintSet(key);
				}
			}
			_numTermRecomputations += list.Count;
			list.Clear();
		}

		/// <summary>
		/// Dispatches the change of a number of terms so that the dependents 
		/// of this term update their value and gradients incrementally.
		/// This method is the inner loop of Recompute
		/// </summary>
		private void DispatchModifiedTerms(List<KeyValuePair<CspSolverTerm, int>> list, bool recomputeValues, bool recomputeGradients)
		{
			foreach (KeyValuePair<CspSolverTerm, int> item in list)
			{
				CspSolverTerm key = item.Key;
				int ordinal = key.Ordinal;
				int value = item.Value;
				int num = _values[ordinal];
				_isEnqueued[ordinal] = false;
				if (recomputeValues && value != num)
				{
					key.DispatchValueChange(value, num, this);
				}
				if (recomputeGradients)
				{
					key.DispatchGradientChange(this);
				}
				if (key.IsConstraint)
				{
					UpdateViolatedConstraintSet(key);
				}
			}
			_numTermRecomputations += list.Count;
			list.Clear();
		}

		/// <summary>
		/// Solves the potential issues due to the fact that 
		/// gradients are not systematically re-evaluated: 
		/// when we skip a gradient recomputation but do recompute
		/// the values we have to save the modified variables in order to 
		/// make sure to consider them during the next gradient evaluation
		/// </summary>
		private void UpdatePostponedVariables(bool recomputeGradients)
		{
			if (recomputeGradients)
			{
				foreach (CspSolverTerm postponedVariable in _postponedVariables)
				{
					postponedVariable.RecomputeGradients(this);
					MarkAsModified(postponedVariable);
				}
				_postponedVariables.Clear();
				return;
			}
			foreach (KeyValuePair<CspSolverTerm, int> item in _modifiedTerms[0])
			{
				_postponedVariables.Add(item.Key);
			}
		}

		/// <summary> Recomputes the values only (not the gradients)
		///           of all CspSolverTerms
		///           in an incremental way by propagating changes
		/// </summary>
		internal void RecomputeValues()
		{
			Recompute(recomputeValues: true, recomputeGradients: false);
		}

		/// <summary> Recomputes the gradients only (not the values) 
		///           of all CspSolverTerms 
		///           The update is incremental by propagating changes
		/// </summary>
		internal void RecomputeGradients()
		{
			if (_useGradients)
			{
				Recompute(recomputeValues: false, recomputeGradients: true);
			}
		}

		/// <summary> Method called when the violation of a constraint
		///           changes, for update of the global violation
		/// </summary>
		/// <param name="constraint">a constraint whose violation changed</param>
		/// <param name="oldValue">old violation of the constraint</param>
		/// <param name="newValue">new violation of the constraint</param>
		internal void PropagateChangeInViolation(CspSolverTerm constraint, int oldValue, int newValue)
		{
			oldValue = Math.Max(0, oldValue);
			newValue = Math.Max(0, newValue);
			_currentQuality.Violation += newValue - oldValue;
		}

		/// <summary> Recomputes the quality of the current solution
		/// </summary>
		/// <param name="incremental">True iff the violation should
		///           be recomputed incrementally, rather than 
		///           recomputed from scratch
		/// </param>
		private void RecomputeCurrentQuality(bool incremental)
		{
			for (int i = 0; i < _model._minimizationGoals.Count; i++)
			{
				int value = this[_model._minimizationGoals[i]];
				_currentQuality[i] = value;
			}
			if (incremental)
			{
				return;
			}
			int num = 0;
			foreach (CspSolverTerm constraint in _model.Constraints)
			{
				num += Math.Max(0, this[constraint]);
			}
			_currentQuality.Violation = num;
		}

		/// <summary>
		/// Adds or removes the term from the set of violated constraints
		/// depending on its updated value
		/// </summary>
		private void UpdateViolatedConstraintSet(CspSolverTerm t)
		{
			if (_values[t.Ordinal] > 0)
			{
				_violatedConstraints.Add(t.Ordinal);
			}
			else
			{
				_violatedConstraints.Remove(t.Ordinal);
			}
		}

		[Conditional("DEBUG")]
		private void CheckRecomputePostcondition()
		{
		}

		/// <summary> Gets the current value of the Term, as an integer
		///           (i.e. in external representation)
		/// </summary>
		/// <remarks> A Boolean is represented as 0/1
		/// </remarks>
		internal int GetIntegerValue(CspSolverTerm expr)
		{
			int v = _values[expr.Ordinal];
			return ToExternalRepresentation(expr, v);
		}

		/// <summary> Gets the type-specific data attached to the term
		/// </summary>
		internal int GetExtraData(BooleanAnd term)
		{
			return _extraDataForBooleanAnd[term.OrdinalAmongBooleanAndTerms];
		}

		internal int GetExtraData(BooleanOr term)
		{
			return _extraDataForBooleanOr[term.OrdinalAmongBooleanOrTerms];
		}

		internal Unequal.LocalSearchData GetExtraData(Unequal term)
		{
			return _extraDataForUnequal[term.OrdinalAmongUnequalTerms];
		}

		/// <summary> Sets the type-specific data attached to the term
		/// </summary>
		internal void SetExtraData(BooleanAnd term, int data)
		{
			_extraDataForBooleanAnd[term.OrdinalAmongBooleanAndTerms] = data;
		}

		internal void SetExtraData(BooleanOr term, int data)
		{
			_extraDataForBooleanOr[term.OrdinalAmongBooleanOrTerms] = data;
		}

		internal void SetExtraData(Unequal term, Unequal.LocalSearchData data)
		{
			_extraDataForUnequal[term.OrdinalAmongUnequalTerms] = data;
		}

		/// <summary>
		/// Get the current value of the term together with
		/// its gradient information
		/// </summary>
		/// <remarks>
		/// if the term is Boolean the value and gradients are in 
		/// terms of its violation
		/// </remarks>
		internal ValueWithGradients GetGradients(CspSolverTerm term)
		{
			int ordinal = term.Ordinal;
			return _gradientInfo[ordinal].GetValueWithGradients(_values[ordinal]);
		}

		/// <summary>
		/// Get the integer value of the term together with its
		/// gradient information
		/// </summary>
		/// <remarks>
		/// We have to check if the term is Boolean since a Boolean
		/// term can be involved in, say, a sum.
		/// For instance if a term is Boolean and has violation -10
		/// its current value is 1 (term is true);
		/// Now if the increasing gradient is +12 that means that we
		/// can decrease the integer value by to 0 (decreasing gradient of -1)
		/// </remarks>
		internal ValueWithGradients GetIntegerGradients(CspSolverTerm term)
		{
			int ordinal = term.Ordinal;
			int num = _values[ordinal];
			if (!term.IsBoolean)
			{
				return _gradientInfo[ordinal].GetValueWithGradients(num);
			}
			if (num < 0)
			{
				if (num + _gradientInfo[ordinal].IncGradient > 0)
				{
					return new ValueWithGradients(1, -1, _gradientInfo[ordinal].IncVariable, 0, null);
				}
				return new ValueWithGradients(1, 0, null, 0, null);
			}
			if (num + _gradientInfo[ordinal].DecGradient < 0)
			{
				return new ValueWithGradients(0, 0, null, 1, _gradientInfo[ordinal].DecVariable);
			}
			return new ValueWithGradients(0, 0, null, 0, null);
		}

		/// <summary>
		/// Sets the gradient information attached to a term
		/// </summary>
		internal void SetGradients(CspSolverTerm term, int decgrad, CspVariable decvar, int incgrad, CspVariable incvar)
		{
			if (_gradientInfo[term.Ordinal].Load(decgrad, decvar, incgrad, incvar))
			{
				MarkAsModified(term);
			}
		}

		/// <summary>
		/// Sets the gradient information attached to a term
		/// </summary>
		internal void SetGradients(CspSolverTerm term, ValueWithGradients v)
		{
			SetGradients(term, v.DecGradient, v.DecVariable, v.IncGradient, v.IncVariable);
		}

		/// <summary>
		/// Sets both gradients to 0
		/// </summary>
		internal void CancelGradients(CspSolverTerm term)
		{
			SetGradients(term, 0, null, 0, null);
		}

		/// <summary>
		/// A term is frozen if its gradients are fixed to 0, meaning
		/// that we (often temporarily) do not wish to modify its value
		/// </summary>
		internal bool IsFiltered(CspSolverTerm term)
		{
			return _filtered[term.Ordinal];
		}

		/// <summary>
		/// Has the term got at least one its gradients that is currently zero?
		/// </summary>
		internal bool HasOneNullGradients(CspSolverTerm term)
		{
			if (term == null)
			{
				return false;
			}
			GradientInfo gradientInfo = _gradientInfo[term.Ordinal];
			return gradientInfo.DecGradient == 0 || gradientInfo.IncGradient == 0;
		}

		/// <summary>
		/// Add term to queue of modified terms unless it's already there
		/// </summary>
		private void MarkAsModified(CspSolverTerm term)
		{
			int ordinal = term.Ordinal;
			if (!_isEnqueued[ordinal])
			{
				_modifiedTerms[term.Depth].Add(new KeyValuePair<CspSolverTerm, int>(term, _values[ordinal]));
				_isEnqueued[ordinal] = true;
			}
		}

		/// <summary> Signals that an inconsistency has been reached;
		///           this adds a large penalty to the current evaluation
		/// </summary>
		internal void SignalOverflow(CspSolverTerm t)
		{
			_currentQuality.Overflow = true;
			if (_overflowCause == null)
			{
				_overflowCause = t;
			}
		}

		/// <summary>
		/// Temporarily freeze the variable (gradients become 0)
		/// meaning that we (temporarily) do not wish to modify its value.
		/// variable must NOT already be filtered
		/// </summary>
		/// <remarks>
		/// This prevents the variable from being taken as a hint for
		/// gradients of other terms. Can be seen as a form of "tabu-ing"
		/// We have to recompute the value and gradients: otherwise we
		/// might simply RecomputeValue and loose the fact that 
		/// </remarks>
		internal void Filter(CspVariable x)
		{
			_filtered[x.Ordinal] = true;
			x.RecomputeGradients(this);
		}

		/// <summary>
		/// When a variable has been filtered out this cancels the
		/// effects of filtering and recomputes its gradient.
		/// Variable must currently be filtered
		/// </summary>
		internal void Unfilter(CspVariable x)
		{
			_filtered[x.Ordinal] = false;
			x.RecomputeGradients(this);
		}

		/// <summary> Flips x to the value of y and vice-versa
		/// </summary>
		internal int Swap(CspVariable x, CspVariable y)
		{
			int value = this[x];
			int value2 = this[y];
			long numTermRecomputations = _numTermRecomputations;
			this[x] = value2;
			this[y] = value;
			RecomputeValues();
			return (int)(_numTermRecomputations - numTermRecomputations);
		}

		/// <summary> Change the current state by flipping a variable to a new value
		/// </summary>
		/// <param name="x">The variable to flip</param>
		/// <param name="newValue">A new value in internal format</param>
		private int Flip(CspVariable x, int newValue)
		{
			long numTermRecomputations = _numTermRecomputations;
			this[x] = newValue;
			RecomputeValues();
			return (int)(_numTermRecomputations - numTermRecomputations);
		}

		/// <summary>Makes sure a downcasted Term is correct</summary>
		private void CheckTerm(CspSolverTerm t)
		{
			_ = t.Model;
			_ = _model;
		}

		/// <summary>Takes a non-trusted flip in external format, checks 
		///          it and converts to the internal format
		/// </summary>
		internal KeyValuePair<CspVariable, int> ConvertToInternal(KeyValuePair<CspTerm, int> flip)
		{
			return ConvertToInternal(flip.Key, flip.Value);
		}

		internal KeyValuePair<CspVariable, int> ConvertToInternal(CspTerm t, int val)
		{
			CspVariable cspVariable = t as CspVariable;
			CheckTerm(cspVariable);
			CspSolverDomain finiteValue = cspVariable.FiniteValue;
			if (!finiteValue.Contains(val) && cspVariable.BaseValueSet.Contains(val))
			{
				int first = finiteValue.First;
				if (val < first)
				{
					val = first;
				}
				else
				{
					int last = finiteValue.Last;
					val = ((val <= last) ? finiteValue.Pick(_prng) : last);
				}
			}
			return new KeyValuePair<CspVariable, int>(cspVariable, ToInternalRepresentation(cspVariable, val));
		}

		/// <summary> Takes a non trusted move where the value (if any) is
		///           in external format. Checks it and converts to internal format
		/// </summary>
		internal LocalSearch.Move ConvertToInternal(LocalSearch.Move move)
		{
			switch (move.Type)
			{
			case LocalSearch.MoveType.Flip:
			{
				CheckTerm(move.Var1);
				KeyValuePair<CspVariable, int> keyValuePair = ConvertToInternal(move.Var1, move.Value);
				return LocalSearch.CreateVariableFlip(keyValuePair.Key, keyValuePair.Value);
			}
			case LocalSearch.MoveType.Swap:
				CheckTerm(move.Var1);
				CheckTerm(move.Var2);
				move.Var1.BaseValueSet.Contains(GetIntegerValue(move.Var2));
				move.Var2.BaseValueSet.Contains(GetIntegerValue(move.Var1));
				break;
			}
			return move;
		}

		/// <summary> Changes an external representation to an internal one
		/// </summary>
		/// <remarks> If the variable is Boolean its 0/1 value will be
		///           converted to a non-zero violation
		/// </remarks>
		internal static int ToInternalRepresentation(CspSolverTerm x, int v)
		{
			if (x.IsBoolean)
			{
				if (v != 0)
				{
					return BooleanFunction.Satisfied;
				}
				return BooleanFunction.Violated;
			}
			return v;
		}

		/// <summary> Changes an internal representation into an external one
		/// </summary>
		/// <remarks> If the variable is Boolean its non-zero violation value
		///           is converted into a 0/1 integer value
		/// </remarks>
		internal static int ToExternalRepresentation(CspSolverTerm x, int v)
		{
			if (x.IsBoolean)
			{
				return ViolationToZeroOne(v);
			}
			return v;
		}

		/// <summary> Given a non-zero violation as represented internally, 
		///           returns its 0/1 integer value (0 if violated, 1 if satisfied)
		/// </summary>
		/// <param name="violation">A non-zero violation indicator 
		///           (the higher the more violated)
		/// </param>
		internal static int ViolationToZeroOne(int violation)
		{
			if (violation >= 0)
			{
				return 0;
			}
			return 1;
		}

		/// <summary> Checks that the given value for the given term is in 
		///           internal format, i.e. Booleans represented as violations
		/// </summary>
		[Conditional("DEBUG")]
		internal static void CheckInternal(CspSolverTerm x, int val)
		{
		}

		/// <summary> Checks that the given value for the given term is in 
		///           external format, i.e. Booleans represented as 0/1
		/// </summary>
		[Conditional("DEBUG")]
		internal static void CheckExternal(CspSolverTerm x, int val)
		{
		}

		/// <summary> Gets the current value associated to a Term by this algorithm
		/// </summary>
		internal override object GetValue(CspTerm variable)
		{
			CspSolverTerm expr = variable as CspSolverTerm;
			return GetIntegerValue(expr);
		}

		/// <summary> Creates a representation of the current solution 
		///           where values are integers
		/// </summary>
		internal override Dictionary<CspTerm, int> SnapshotVariablesIntegerValues()
		{
			Dictionary<CspTerm, int> dictionary = new Dictionary<CspTerm, int>();
			foreach (CspSolverTerm variable in _model._variables)
			{
				dictionary.Add(variable, GetIntegerValue(variable));
			}
			return dictionary;
		}

		/// <summary> Creates a representation of the current solution 
		///           where values are objects
		/// </summary>
		internal override Dictionary<CspTerm, object> SnapshotVariablesValues()
		{
			Dictionary<CspTerm, object> dictionary = new Dictionary<CspTerm, object>();
			foreach (CspSolverTerm variable in _model._variables)
			{
				dictionary.Add(variable, GetIntegerValue(variable));
			}
			return dictionary;
		}

		/// <summary> Runs the algorithm; every time a solution is found
		///           the method yield-returns and a solution can be accessed
		///           either value by value or by batch using a snapshot 
		/// </summary>
		internal sealed override IEnumerable<int> Search(bool yieldSuboptimals)
		{
			if (!yieldSuboptimals)
			{
				throw new InvalidOperationException(Resources.EnumerateInterimSolutionsMustBeEnabledForLocalSearch);
			}
			int solId = 0;
			AggregateQuality oldQuality = new AggregateQuality(_model);
			AggregateQuality bestQuality = new AggregateQuality(_model);
			int previousValue = int.MinValue;
			SetNewStartingPoint();
			oldQuality.CopyFrom(CurrentQuality);
			bestQuality.CopyFrom(CurrentQuality);
			if (CurrentViolation == 0)
			{
				yield return solId++;
				TakeControlBackFromCaller();
			}
			while (!End)
			{
				if (GetRestartDecision())
				{
					SetNewStartingPoint();
					continue;
				}
				LocalSearch.Move userGivenMove = GetMove();
				LocalSearch.Move move = ConvertToInternal(userGivenMove);
				if (move.Type == LocalSearch.MoveType.Flip)
				{
					previousValue = this[move.Var1];
				}
				oldQuality.CopyFrom(_currentQuality);
				int flipCost = Perform(move);
				int qualityChange = _currentQuality.Difference(oldQuality);
				bool moveAccepted = GetAcceptance(userGivenMove, flipCost, qualityChange);
				if (moveAccepted)
				{
					RecomputeGradients();
					if (_currentQuality.LessStrict(bestQuality))
					{
						bestQuality.CopyFrom(CurrentQuality);
						if (CurrentViolation == 0)
						{
							yield return solId++;
							TakeControlBackFromCaller();
							if (_model._minimizationGoals.Count == 0)
							{
								SetNewStartingPoint();
							}
						}
					}
				}
				else
				{
					Undo(move, previousValue);
				}
				if (this._moveAttempted != null)
				{
					this._moveAttempted(userGivenMove, qualityChange < 0, moveAccepted);
					TakeControlBackFromCaller();
				}
			}
		}

		/// <summary> Changes the current configuration of this
		///           Local Search as specified by the Move
		/// </summary>
		internal int Perform(LocalSearch.Move move)
		{
			int result = 0;
			switch (move.Type)
			{
			case LocalSearch.MoveType.Flip:
				result = Flip(move.Var1, move.Value);
				break;
			case LocalSearch.MoveType.Swap:
				result = Swap(move.Var1, move.Var2);
				break;
			case LocalSearch.MoveType.Stop:
				SetNewStartingPoint();
				break;
			}
			return result;
		}

		/// <summary> Undoes the effect of a rejected move
		/// </summary>
		/// <remarks> If the move was a restart its effects are not undone
		/// </remarks>
		private void Undo(LocalSearch.Move move, int valueBefore)
		{
			switch (move.Type)
			{
			case LocalSearch.MoveType.Flip:
				Flip(move.Var1, valueBefore);
				break;
			case LocalSearch.MoveType.Swap:
				Swap(move.Var1, move.Var2);
				break;
			case LocalSearch.MoveType.Stop:
				break;
			}
		}

		/// <summary>Reinitialize the coniguration of the local search
		///          Use for initialization and diversification (restart)
		/// </summary>
		internal void SetNewStartingPoint()
		{
			Dictionary<CspTerm, int> dictionary = _initializationAdvisor(this);
			TakeControlBackFromCaller();
			foreach (KeyValuePair<CspTerm, int> item in dictionary)
			{
				KeyValuePair<CspVariable, int> keyValuePair = ConvertToInternal(item);
				_values[keyValuePair.Key.Ordinal] = keyValuePair.Value;
			}
			InitializeValuesAndGradients();
			if (this._restartOccurred != null)
			{
				this._restartOccurred();
				TakeControlBackFromCaller();
			}
		}

		/// <summary>Determines whether the move is accepted, using a call-back
		/// </summary>
		private bool GetAcceptance(LocalSearch.Move userGivenMove, int flipCost, int qualityChange)
		{
			bool result = _acceptanceAdvisor(this, userGivenMove, qualityChange, flipCost);
			TakeControlBackFromCaller();
			return result;
		}

		/// <summary>Determines the next candidate move, using a call-back
		/// </summary>
		private LocalSearch.Move GetMove()
		{
			LocalSearch.Move result = _moveAdvisor(this);
			TakeControlBackFromCaller();
			return result;
		}

		/// <summary>Determines whether we restart, using a call-back
		/// </summary>
		private bool GetRestartDecision()
		{
			bool result = _restartAdvisor(this);
			TakeControlBackFromCaller();
			return result;
		}

		/// <summary>Makes the checks that are required when 
		///          a call-back returns
		/// </summary>
		private void TakeControlBackFromCaller()
		{
			if (_model._fIsInModelingPhase)
			{
				throw new InvalidOperationException(Resources.SolverResetDuringSolve);
			}
		}

		/// <summary>Gets the current value of an integer term</summary>
		public int GetCurrentIntegerValue(CspTerm expr)
		{
			CspSolverTerm cspSolverTerm = expr as CspSolverTerm;
			CheckTerm(cspSolverTerm);
			return GetIntegerValue(cspSolverTerm);
		}

		/// <summary>Estimates the effect of changing a variable to a candidate value.
		///          The flip is not effectively performed
		/// </summary>
		public int EvaluateFlip(CspTerm x, int val)
		{
			KeyValuePair<CspVariable, int> keyValuePair = ConvertToInternal(x, val);
			CspVariable key = keyValuePair.Key;
			int value = keyValuePair.Value;
			_oldQuality.CopyFrom(_currentQuality);
			int newValue = _values[key.Ordinal];
			Flip(key, value);
			int result = _currentQuality.Difference(_oldQuality);
			Flip(key, newValue);
			return result;
		}

		/// <summary>Subscribes a delegate that will be called by the algorithm
		///          every time a move is attempted, even if the move was ultimately rejected
		/// </summary>
		public void SubscribeToMove(LocalSearch.MoveListener listener)
		{
			_moveAttempted += listener;
		}

		/// <summary>Subscribes a delegate that will be called by the local search
		///          every time a restart takes place
		/// </summary>
		public void SubscribeToRestarts(Action listener)
		{
			_restartOccurred += listener;
		}

		/// <summary>Picks at random a term that may be flipped</summary>
		public CspTerm PickVariable(Random prng)
		{
			return PickConcreteVariable(prng);
		}

		private CspVariable PickConcreteVariable(Random prng)
		{
			List<CspVariable> variablesExcludingConstants = _model._variablesExcludingConstants;
			int index = _prng.Next(variablesExcludingConstants.Count);
			return variablesExcludingConstants[index];
		}

		internal KeyValuePair<CspTerm, int> RandomFlip(Random prng)
		{
			CspVariable cspVariable = PickConcreteVariable(prng);
			int integerValue = GetIntegerValue(cspVariable);
			int num = integerValue;
			CspSolverDomain finiteValue = cspVariable.FiniteValue;
			int num2 = 0;
			while (integerValue == num && num2 < 5)
			{
				num = finiteValue.Pick(prng);
				num2++;
			}
			return new KeyValuePair<CspTerm, int>(cspVariable, num);
		}

		/// <summary>Gives a variable hint, i.e. a variable whose re-assignment is
		///          likely to increase the overall quality. The variable is preferably
		///          selected among those that are not filtered-out.
		/// </summary>
		/// <remarks>gradient-guided</remarks>
		public CspTerm SelectBestVariable()
		{
			UseGradients = true;
			if (Overflow)
			{
				return RecoverFromOverflow(_prng).Key;
			}
			if (_currentQuality.Violation == 0)
			{
				return SelectBestObjectiveDecrease();
			}
			return SelectBestViolationDecrease();
		}

		/// <summary>
		/// Selects a variable that is expected to allow a
		/// maximal decrease of the violation
		/// </summary>
		private CspVariable SelectBestViolationDecrease()
		{
			int cardinal = _violatedConstraints.Cardinal;
			int num = _prng.Next(cardinal);
			for (int i = num; i < cardinal; i++)
			{
				int num2 = _violatedConstraints[i];
				CspVariable decVariable = _gradientInfo[num2].DecVariable;
				if (decVariable != null)
				{
					return decVariable;
				}
			}
			for (int j = 0; j < num; j++)
			{
				int num2 = _violatedConstraints[j];
				CspVariable decVariable = _gradientInfo[num2].DecVariable;
				if (decVariable != null)
				{
					return decVariable;
				}
			}
			return PickConcreteVariable(_prng);
		}

		/// <summary>
		/// Selects a variable that is likely to decrease the
		/// value of an objective
		/// </summary>
		private CspTerm SelectBestObjectiveDecrease()
		{
			List<CspSolverTerm> minimizationGoals = _model._minimizationGoals;
			if (minimizationGoals.Count > 0)
			{
				double num = ((minimizationGoals.Count == 1) ? 1.0 : 0.7);
				for (int i = 0; i < minimizationGoals.Count; i++)
				{
					if (_prng.NextDouble() < num)
					{
						int ordinal = minimizationGoals[i].Ordinal;
						CspVariable decVariable = _gradientInfo[ordinal].DecVariable;
						if (decVariable != null)
						{
							return decVariable;
						}
					}
				}
			}
			return PickConcreteVariable(_prng);
		}

		/// <summary>Excludes x from the set of variables that are likely to
		///          be returned by the SelectBestVariable method
		/// </summary>
		public void Filter(CspTerm variable)
		{
			CspVariable cspVariable = variable as CspVariable;
			CheckTerm(cspVariable);
			Filter(cspVariable);
		}

		/// <summary>Cancels the effect of filtering x, i.e. allows x to 
		///          be returned by the SelectBestVariable method
		/// </summary>
		public void Unfilter(CspTerm variable)
		{
			CspVariable cspVariable = variable as CspVariable;
			CheckTerm(cspVariable);
			Unfilter(cspVariable);
		}

		/// <summary>True iff the variable is likely to
		///          be returned by the SelectBestVariable method
		/// </summary>
		public bool IsFiltered(CspTerm variable)
		{
			CspSolverTerm cspSolverTerm = variable as CspSolverTerm;
			CheckTerm(cspSolverTerm);
			return IsFiltered(cspSolverTerm);
		}

		/// <summary>Selects a variable whose value is conflicting with 
		///          one of the constraints, and suggest a value for it that
		///          is heuristically likely to reduce the violation
		/// </summary>
		/// <remarks>
		/// Should be deprecated?
		/// Somehow redundant with SelectMaxViolationDecrease but provides
		/// alternative top-down method instead of the bottom-up gradient-based
		/// approach
		/// </remarks>
		public KeyValuePair<CspTerm, int> PickViolatedVariable(Random prng)
		{
			if (Overflow)
			{
				return RecoverFromOverflow(prng);
			}
			if (_currentQuality.Violation == 0)
			{
				return RandomFlip(prng);
			}
			CspSolverTerm cspSolverTerm = FindViolatedConstraint();
			KeyValuePair<CspVariable, int> keyValuePair = cspSolverTerm.SelectFlip(this, 1);
			return new KeyValuePair<CspTerm, int>(keyValuePair.Key, keyValuePair.Value);
		}

		/// <summary>
		/// Picks a random violated constraint 
		/// </summary>
		private CspSolverTerm FindViolatedConstraint()
		{
			int pos = _prng.Next(_violatedConstraints.Cardinal);
			int index = _violatedConstraints[pos];
			return _model.AllTerms[index] as CspSolverTerm;
		}

		/// <summary> Method used to recover from an overflow
		/// </summary>
		internal KeyValuePair<CspTerm, int> RecoverFromOverflow(Random prng)
		{
			CspVariable key = _overflowCause.SelectFlip(this, 0).Key;
			CspSolverDomain baseValueSet = key.BaseValueSet;
			int num = 0;
			if (!baseValueSet.Contains(num))
			{
				int first = baseValueSet.First;
				int last = baseValueSet.Last;
				num = ((Math.Abs(first) >= Math.Abs(last)) ? last : first);
			}
			return new KeyValuePair<CspTerm, int>(key, num);
		}
	}
}
