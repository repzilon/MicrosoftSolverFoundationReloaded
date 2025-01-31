namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A struct representing any change that can affect the state of a
	///   variable during the problem's lifetime. Serves essentially as
	///   parameter for signaling events related to variables.
	/// </summary>
	internal struct VariableModification
	{
		/// <summary>
		///   Delegates that receive variable modifications
		/// </summary>
		public delegate void Listener(VariableModification change);

		/// <summary>
		///   Decision used to indicate that a variable modification
		///   is asserted at the root level ("implied at level 0")
		/// </summary>
		public static readonly VariableGroup RootLevelDeduction = VariableGroup.EmptyGroup();

		public DiscreteVariable Var;

		public Cause Reason;

		public VariableModification(DiscreteVariable x, Cause c)
		{
			Var = x;
			Reason = c;
		}
	}
}
