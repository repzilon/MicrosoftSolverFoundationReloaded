namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between 2 integer variables X and Y imposing that 
	///   X + k be equal to Y, for some constant k.
	/// </summary>
	internal class AdditionToConstant : BinaryConstraint<IntegerVariable, IntegerVariable>
	{
		private long _cst;

		public AdditionToConstant(Problem p, IntegerVariable x, long k, IntegerVariable y)
			: base(p, x, y)
		{
			_cst = k;
			x.SubscribeToAnyModification(WhenXmodified);
			y.SubscribeToAnyModification(WhenYmodified);
		}

		private bool WhenXmodified()
		{
			return _y.ImposeRange(_x.LowerBound + _cst, _x.UpperBound + _cst, base.Cause);
		}

		private bool WhenYmodified()
		{
			return _x.ImposeRange(_y.LowerBound - _cst, _y.UpperBound - _cst, base.Cause);
		}
	}
}
