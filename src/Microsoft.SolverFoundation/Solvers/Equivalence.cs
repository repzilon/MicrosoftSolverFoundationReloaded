namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between two Boolean variables X and Y imposing that
	///   X be true iff Y is true.
	/// </summary>
	internal class Equivalence : BinaryConstraint<BooleanVariable, BooleanVariable>
	{
		public Equivalence(Problem p, BooleanVariable x, BooleanVariable y)
			: base(p, x, y)
		{
			x.SubscribeToTrue(WhenXtrue);
			x.SubscribeToFalse(WhenXfalse);
			y.SubscribeToTrue(WhenYtrue);
			y.SubscribeToFalse(WhenYfalse);
		}

		private bool WhenXtrue()
		{
			return _y.ImposeValueTrue(base.Cause);
		}

		private bool WhenXfalse()
		{
			return _y.ImposeValueFalse(base.Cause);
		}

		private bool WhenYtrue()
		{
			return _x.ImposeValueTrue(base.Cause);
		}

		private bool WhenYfalse()
		{
			return _x.ImposeValueFalse(base.Cause);
		}
	}
}
