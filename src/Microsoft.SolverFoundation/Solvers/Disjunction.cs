namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between two Boolean variables X and Y imposing that
	///   at least one among X and Y be true.
	/// </summary>
	internal class Disjunction : BinaryConstraint<BooleanVariable, BooleanVariable>
	{
		public Disjunction(Problem p, BooleanVariable x, BooleanVariable y)
			: base(p, x, y)
		{
			x.SubscribeToFalse(WhenXfalse);
			y.SubscribeToFalse(WhenYfalse);
		}

		private bool WhenXfalse()
		{
			return _y.ImposeValueTrue(base.Cause);
		}

		private bool WhenYfalse()
		{
			return _x.ImposeValueTrue(base.Cause);
		}
	}
}
