namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Cause of a domain reduction, passed as an argument to any variable
	///   modification. Captures both a constraint and a variableGroup. The
	///   variable group may not in general be the whole set of variables
	///   constrained by the constraint as some constraints might specify
	///   a subset directly responsible for the deduction.
	/// </summary>
	internal struct Cause
	{
		public readonly DisolverConstraint Constraint;

		public readonly VariableGroup Signature;

		public static Cause Decision = new Cause(null, null);

		public static Cause RootLevelDecision = new Cause(null, VariableGroup.EmptyGroup());

		/// <summary>
		///   No cause because represents a decision
		/// </summary>
		public bool IsDecision => Signature == null;

		/// <summary>
		///   No cause because deduced but holds at level 0;
		///   for instance a refined optimization bound
		/// </summary>
		public bool IsRootLevelDecision => Signature == VariableGroup.EmptyGroup();

		public Cause(DisolverConstraint c, VariableGroup g)
		{
			Constraint = c;
			Signature = g;
		}
	}
}
