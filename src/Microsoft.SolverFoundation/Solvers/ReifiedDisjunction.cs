namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between 3 Boolean variables X, Y and Z
	///   imposing that Z represent the disjunction X || Y
	/// </summary>
	internal class ReifiedDisjunction : TernaryConstraint<BooleanVariable, BooleanVariable, BooleanVariable>
	{
		public ReifiedDisjunction(Problem p, BooleanVariable x, BooleanVariable y, BooleanVariable z)
			: base(p, x, y, z)
		{
			BasicEvent.Listener l = WhenXorYtrue;
			x.SubscribeToTrue(l);
			y.SubscribeToTrue(l);
			z.SubscribeToFalse(WhenZfalse);
			x.SubscribeToFalse(WhenXfalse);
			y.SubscribeToFalse(WhenYfalse);
			z.SubscribeToTrue(WhenZtrue);
		}

		private bool WhenXorYtrue()
		{
			return _z.ImposeValueTrue(base.Cause);
		}

		private bool WhenZfalse()
		{
			if (_x.ImposeValueFalse(base.Cause))
			{
				return _y.ImposeValueFalse(base.Cause);
			}
			return false;
		}

		private bool WhenXfalse()
		{
			return BasicConstraintUtilities.ImposeEqual(_y, _z, base.Cause);
		}

		private bool WhenYfalse()
		{
			return BasicConstraintUtilities.ImposeEqual(_x, _z, base.Cause);
		}

		private bool WhenZtrue()
		{
			if (_x.Status == BooleanVariableState.False)
			{
				return _y.ImposeValueTrue(base.Cause);
			}
			if (_y.Status == BooleanVariableState.False)
			{
				return _x.ImposeValueTrue(base.Cause);
			}
			return true;
		}
	}
}
