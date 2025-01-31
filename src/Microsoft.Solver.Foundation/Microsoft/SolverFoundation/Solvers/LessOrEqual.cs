namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between two integer variables X and Y imposing that
	///   X be less than or equal to Y
	/// </summary>
	internal class LessOrEqual : BinaryConstraint<IntegerVariable, IntegerVariable>
	{
		public LessOrEqual(Problem p, IntegerVariable x, IntegerVariable y)
			: base(p, x, y)
		{
			x.SubscribeToAnyModification(WhenXinfModified);
			y.SubscribeToAnyModification(WhenYsupModified);
		}

		private bool WhenXinfModified()
		{
			return _y.ImposeLowerBound(_x.LowerBound, base.Cause);
		}

		private bool WhenYsupModified()
		{
			return _x.ImposeUpperBound(_y.UpperBound, base.Cause);
		}
	}
}
