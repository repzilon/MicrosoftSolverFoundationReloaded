namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Struct representing the decisions that can be taken 
	///   during the search.
	/// </summary>
	internal struct DisolverDecision
	{
		/// <summary>
		///   Flag giving the type of decision
		/// </summary>
		public enum Type
		{
			Assignment,
			ImposeLowerBound,
			ImposeUpperBound,
			SolutionFound,
			Restart,
			ContextSwitch
		}

		private const int novalue = -1234567890;

		internal readonly Type Tag;

		internal readonly DiscreteVariable Target;

		internal readonly long Value;

		/// <summary>
		///   Creates a decision that specifies to assign a discrete variable
		///   to a certain value. 
		/// </summary>
		public static DisolverDecision AssignDiscreteVariable(DiscreteVariable x, long val)
		{
			return new DisolverDecision(Type.Assignment, x, val);
		}

		/// <summary>
		///   Creates a decision that specifies to tighten the lower bound of
		///   an integer variable.
		/// </summary>
		public static DisolverDecision ImposeLowerBound(DiscreteVariable x, long lb)
		{
			return new DisolverDecision(Type.ImposeLowerBound, x, lb);
		}

		/// <summary>
		///   Creates a decison that specifies to tighten the upper bound
		///   of an integer variable.
		/// </summary>
		public static DisolverDecision ImposeUpperBound(DiscreteVariable x, long ub)
		{
			return new DisolverDecision(Type.ImposeUpperBound, x, ub);
		}

		/// <summary>
		///   Creates a decision that specifies that search be stopped
		///   because a solution is found.
		/// </summary>
		public static DisolverDecision SolutionFound()
		{
			return new DisolverDecision(Type.SolutionFound, null, -1234567890L);
		}

		/// <summary>
		///   Creates a decision that specifies to restart the search
		///   for diversification purposes.
		/// </summary>
		public static DisolverDecision Restart()
		{
			return new DisolverDecision(Type.Restart, null, -1234567890L);
		}

		/// <summary>
		///   creates a decision that specifies to undo all decisions
		///   because we'll switch to another heuristic
		/// </summary>
		public static DisolverDecision ContextSwitch()
		{
			return new DisolverDecision(Type.ContextSwitch, null, -1234567890L);
		}

		private DisolverDecision(Type t, DiscreteVariable x, long v)
		{
			Tag = t;
			Target = x;
			Value = v;
		}
	}
}
