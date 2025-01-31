namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between 3 Boolean variables X, Y and Z
	///   imposing that Z represent the conjunction X and Y
	/// </summary>
	internal class ReifiedConjunction : TernaryConstraint<BooleanVariable, BooleanVariable, BooleanVariable>
	{
		public ReifiedConjunction(Problem p, BooleanVariable x, BooleanVariable y, BooleanVariable z)
			: base(p, x, y, z)
		{
			BasicEvent.Listener l = WhenXorYfalse;
			x.SubscribeToFalse(l);
			y.SubscribeToFalse(l);
			z.SubscribeToTrue(WhenZtrue);
			x.SubscribeToTrue(WhenXtrue);
			y.SubscribeToTrue(WhenYtrue);
			z.SubscribeToFalse(WhenZfalse);
		}

		private bool WhenXorYfalse()
		{
			return _z.ImposeValueFalse(base.Cause);
		}

		private bool WhenZtrue()
		{
			if (_x.ImposeValueTrue(base.Cause))
			{
				return _y.ImposeValueTrue(base.Cause);
			}
			return false;
		}

		private bool WhenXtrue()
		{
			return BasicConstraintUtilities.ImposeEqual(_y, _z, base.Cause);
		}

		private bool WhenYtrue()
		{
			return BasicConstraintUtilities.ImposeEqual(_x, _z, base.Cause);
		}

		private bool WhenZfalse()
		{
			if (_x.Status == BooleanVariableState.True)
			{
				return _y.ImposeValueFalse(base.Cause);
			}
			if (_y.Status == BooleanVariableState.True)
			{
				return _x.ImposeValueFalse(base.Cause);
			}
			return true;
		}
	}
}
