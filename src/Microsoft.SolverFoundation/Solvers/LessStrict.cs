namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between two integer variables X and Y imposing that
	///   X be strictly less than Y 
	/// </summary>
	internal class LessStrict : BinaryConstraint<IntegerVariable, IntegerVariable>
	{
		public LessStrict(Problem p, IntegerVariable x, IntegerVariable y)
			: base(p, x, y)
		{
			x.SubscribeToAnyModification(WhenXinfModified);
			y.SubscribeToAnyModification(WhenYsupModified);
		}

		private bool WhenXinfModified()
		{
			return _y.ImposeLowerBound(_x.LowerBound + 1, base.Cause);
		}

		private bool WhenYsupModified()
		{
			return _x.ImposeUpperBound(_y.UpperBound - 1, base.Cause);
		}
	}
}
