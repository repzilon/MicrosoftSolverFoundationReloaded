namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between two integer variables X and Y and a Boolean 
	///   variable B, imposing that B be true iff X is less than or equal to Y.
	/// </summary>
	internal class ReifiedLessEqual : TernaryConstraint<IntegerVariable, IntegerVariable, BooleanVariable>
	{
		public ReifiedLessEqual(Problem p, IntegerVariable x, IntegerVariable y, BooleanVariable b)
			: base(p, x, y, b)
		{
			x.SubscribeToAnyModification(WhenXmodified);
			y.SubscribeToAnyModification(WhenYmodified);
			_z.SubscribeToFalse(WhenBfalse);
			_z.SubscribeToTrue(WhenBtrue);
		}

		private bool WhenXmodified()
		{
			switch (_z.Status)
			{
			case BooleanVariableState.True:
				return _y.ImposeLowerBound(_x.LowerBound, base.Cause);
			case BooleanVariableState.False:
				return _y.ImposeUpperBound(_x.UpperBound - 1, base.Cause);
			default:
				return TryToFixZ();
			}
		}

		private bool WhenYmodified()
		{
			switch (_z.Status)
			{
			case BooleanVariableState.True:
				return _x.ImposeUpperBound(_y.UpperBound, base.Cause);
			case BooleanVariableState.False:
				return _x.ImposeLowerBound(_y.LowerBound + 1, base.Cause);
			default:
				return TryToFixZ();
			}
		}

		private bool WhenBfalse()
		{
			if (_x.ImposeLowerBound(_y.LowerBound + 1, base.Cause))
			{
				return _y.ImposeUpperBound(_x.UpperBound - 1, base.Cause);
			}
			return false;
		}

		private bool WhenBtrue()
		{
			if (_y.ImposeLowerBound(_x.LowerBound, base.Cause))
			{
				return _x.ImposeUpperBound(_y.UpperBound, base.Cause);
			}
			return false;
		}

		private bool TryToFixZ()
		{
			if (_x.LowerBound > _y.UpperBound)
			{
				return _z.ImposeValueFalse(base.Cause);
			}
			if (_x.UpperBound <= _y.LowerBound)
			{
				return _z.ImposeValueTrue(base.Cause);
			}
			return true;
		}
	}
}
