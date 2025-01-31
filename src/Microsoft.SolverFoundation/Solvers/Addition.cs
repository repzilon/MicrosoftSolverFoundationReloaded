namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between 3 integer variables X, Y and Z imposing that 
	///   X + Y be equal to Z
	/// </summary>
	internal class Addition : TernaryConstraint<IntegerVariable, IntegerVariable, IntegerVariable>
	{
		public Addition(Problem p, IntegerVariable x, IntegerVariable y, IntegerVariable z)
			: base(p, x, y, z)
		{
			x.SubscribeToAnyModification(WhenXmodified);
			y.SubscribeToAnyModification(WhenYmodified);
			z.SubscribeToAnyModification(WhenZmodified);
		}

		private bool WhenXmodified()
		{
			long lowerBound = _x.LowerBound;
			long upperBound = _x.UpperBound;
			if (_z.ImposeRange(_y.LowerBound + lowerBound, _y.UpperBound + upperBound, base.Cause))
			{
				return _y.ImposeRange(_z.LowerBound - upperBound, _z.UpperBound - lowerBound, base.Cause);
			}
			return false;
		}

		private bool WhenYmodified()
		{
			long lowerBound = _y.LowerBound;
			long upperBound = _y.UpperBound;
			if (_z.ImposeRange(_x.LowerBound + lowerBound, _x.UpperBound + upperBound, base.Cause))
			{
				return _x.ImposeRange(_z.LowerBound - upperBound, _z.UpperBound - lowerBound, base.Cause);
			}
			return false;
		}

		private bool WhenZmodified()
		{
			long lowerBound = _z.LowerBound;
			long upperBound = _z.UpperBound;
			if (_x.ImposeRange(lowerBound - _y.UpperBound, upperBound - _y.LowerBound, base.Cause))
			{
				return _y.ImposeRange(lowerBound - _x.UpperBound, upperBound - _x.LowerBound, base.Cause);
			}
			return false;
		}
	}
}
