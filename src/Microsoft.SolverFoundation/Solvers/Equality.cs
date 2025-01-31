namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between two integer variables X and Y imposing that
	///   X be equal to Y 
	/// </summary>
	internal class Equality : BinaryConstraint<IntegerVariable, IntegerVariable>
	{
		public Equality(Problem p, IntegerVariable x, IntegerVariable y)
			: base(p, x, y)
		{
			x.SubscribeToAnyModification(WhenXmodified);
			y.SubscribeToAnyModification(WhenYmodified);
		}

		private bool WhenXmodified()
		{
			return _y.ImposeRange(_x.LowerBound, _x.UpperBound, base.Cause);
		}

		private bool WhenYmodified()
		{
			return _x.ImposeRange(_y.LowerBound, _y.UpperBound, base.Cause);
		}
	}
}
