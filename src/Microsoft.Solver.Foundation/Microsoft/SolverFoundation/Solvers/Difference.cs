namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between two integer variables X and Y imposing that
	///   X be different from Y 
	/// </summary>
	internal class Difference : BinaryConstraint<IntegerVariable, IntegerVariable>
	{
		public Difference(Problem p, IntegerVariable x, IntegerVariable y)
			: base(p, x, y)
		{
			BasicEvent.Listener l = WhenAnyModification;
			_x.SubscribeToAnyModification(l);
			_y.SubscribeToAnyModification(l);
		}

		private bool WhenAnyModification()
		{
			return BasicConstraintUtilities.ImposeBoundsDifferent(_x, _y, base.Cause);
		}
	}
}
