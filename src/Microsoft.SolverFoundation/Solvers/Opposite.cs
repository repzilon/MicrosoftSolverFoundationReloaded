namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between 2 integer variables X and Y imposing that 
	///   X and Y be opposite, i.e. Y == -X.
	/// </summary>
	internal class Opposite : BinaryConstraint<IntegerVariable, IntegerVariable>
	{
		public Opposite(Problem p, IntegerVariable x, IntegerVariable y)
			: base(p, x, y)
		{
			x.SubscribeToAnyModification(WhenXmodified);
			y.SubscribeToAnyModification(WhenYmodified);
		}

		private bool WhenXmodified()
		{
			return _y.ImposeRange(-_x.UpperBound, -_x.LowerBound, base.Cause);
		}

		private bool WhenYmodified()
		{
			return _x.ImposeRange(-_y.UpperBound, -_y.LowerBound, base.Cause);
		}
	}
}
