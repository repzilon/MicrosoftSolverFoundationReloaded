using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Root class for Disolver Variables on discrete domains
	/// </summary>
	/// <remarks>
	///   We distinguish between several classes of discrete variables including
	///   Boolean and integer ones. This is because specialised representations
	///   and event-handling can be used for different types of variables.
	///   Most access to discrete variables is done in a typed way (e.g. using
	///   generic constraints that know exactly the actual type of their 
	///   arguments) because we are very (overly?) picky about some very
	///   frequently called methods getting their chance to be inlined, so we
	///   avoid many virtual calls. It is nonetheless possible to manipulate
	///   discrete variables directly via the root class through some abstract
	///   methods. In this case Booleans values are seen as integers 0 and 1
	/// </remarks>
	internal abstract class DiscreteVariable : Indexed
	{
		protected const bool Success = true;

		private static long _dummyIntegerVariable;

		protected Problem _problem;

		protected VariableGroup _singleton;

		protected readonly long _initialDomainSize;

		/// <summary>
		///   Number of values currently allowed in the variable's domain
		/// </summary>
		public abstract long DomainSize { get; }

		/// <summary>
		///   Gets a variable group that contains only the variable.
		/// </summary>
		public VariableGroup AsSingleton => _singleton;

		/// <summary>
		///   Constructor
		/// </summary>
		/// <param name="p">Problem in which variable lives</param>
		/// <param name="initialDomainSize">
		///   Domain size of the variable at construction time; 
		///   used to determine whether the variable is in its original state
		/// </param>
		protected DiscreteVariable(Problem p, long initialDomainSize)
			: base(p.DiscreteVariables)
		{
			_problem = p;
			_singleton = (p.UseExplanations ? new VariableGroup(this) : null);
			_initialDomainSize = initialDomainSize;
		}

		/// <summary>
		///   True if the integer value is in the variable's current domain
		/// </summary>
		public abstract bool IsAllowed(long val);

		/// <summary>
		///   Gets the lower bound of the variable, as an integer 
		/// </summary>
		public abstract long GetLowerBound();

		/// <summary>
		///   Gets the lower bound of the variable, as an integer 
		/// </summary>
		public abstract long GetUpperBound();

		/// <summary>
		///   returns true of the var is instantiated and, if so,
		///   assigns its value to the out parameter
		/// </summary>
		public abstract bool TryGetIntegerValue(out long value);

		public bool CheckIfInstantiated()
		{
			return TryGetIntegerValue(out _dummyIntegerVariable);
		}

		/// <summary>
		///   Goes through all the constraints connected to the variable
		/// </summary>
		public abstract IEnumerable<DisolverConstraint> EnumerateConstraints();

		/// <summary>
		///   Schedules all events that need to be scheduled
		///   when starting the solver
		/// </summary>
		public abstract void ScheduleInitialEvents();

		/// <summary>
		///   Constrains the variable to take a value greater or 
		///   equal to a new lower bound.  
		/// </summary>
		/// <param name="lb">the new lower bound</param>
		/// <param name="c">
		///   The explanation (optional); omit when the lower bound
		///   is not propagated but is instead a decision
		/// </param>
		public abstract bool ImposeIntegerLowerBound(long lb, Cause c);

		public bool ImposeIntegerLowerBound(long lb)
		{
			return ImposeIntegerLowerBound(lb, Cause.Decision);
		}

		/// <summary>
		///   Constrains the variable to take a value lower or 
		///   equal to a new upper bound
		/// </summary>
		/// <param name="ub">the new upper bound</param>
		/// <param name="c">
		///   The explanation (optional); omit when the upper bound
		///   is not propagated but is instead a decision
		/// </param>
		public abstract bool ImposeIntegerUpperBound(long ub, Cause c);

		public bool ImposeIntegerUpperBound(long ub)
		{
			return ImposeIntegerUpperBound(ub, Cause.Decision);
		}

		/// <summary>
		///   Constrains the variable to take an integer value
		/// </summary>
		/// <param name="val">the new value</param>
		/// <param name="c">
		///   The explanation (optional); omit when the upper bound
		///   is not propagated but is instead a decision
		/// </param>
		public abstract bool ImposeIntegerValue(long val, Cause c);

		public bool ImposeIntegerValue(long val)
		{
			return ImposeIntegerValue(val, Cause.Decision);
		}

		/// <summary>
		///   True if the state of the variable is as it was 
		///   when it was constructed
		/// </summary>
		public bool IsInInitialState()
		{
			return DomainSize == _initialDomainSize;
		}

		/// <summary>
		///   Called to indicate that the variable is causing
		///   a conflict
		/// </summary>
		public bool ImposeEmptyDomain(Cause c)
		{
			_problem.SignalFailure(this, c);
			return false;
		}
	}
}
