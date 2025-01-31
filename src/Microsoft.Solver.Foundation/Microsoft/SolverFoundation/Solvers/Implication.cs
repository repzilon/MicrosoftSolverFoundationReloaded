namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between two Boolean variables X and Y imposing that
	///   if X is true then so is Y.
	/// </summary>
	internal class Implication : BinaryConstraint<BooleanVariable, BooleanVariable>
	{
		public Implication(Problem p, BooleanVariable x, BooleanVariable y)
			: base(p, x, y)
		{
			x.SubscribeToTrue(WhenXtrue);
			y.SubscribeToFalse(WhenYfalse);
		}

		private bool WhenXtrue()
		{
			return _y.ImposeValueTrue(base.Cause);
		}

		private bool WhenYfalse()
		{
			return _x.ImposeValueFalse(base.Cause);
		}
	}
}
