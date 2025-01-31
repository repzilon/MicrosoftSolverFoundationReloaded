namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between two integer variables X and Y and a Boolean 
	///   variable B, imposing that B be true iff X is equal to Y.
	/// </summary>
	internal class ReifiedEquality : TernaryConstraint<IntegerVariable, IntegerVariable, BooleanVariable>
	{
		public ReifiedEquality(Problem p, IntegerVariable x, IntegerVariable y, BooleanVariable b)
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
			case BooleanVariableState.False:
				return WhenBfalse();
			case BooleanVariableState.True:
				return _y.ImposeRange(_x.LowerBound, _x.UpperBound, base.Cause);
			default:
				return TryToFixZ();
			}
		}

		private bool WhenYmodified()
		{
			switch (_z.Status)
			{
			case BooleanVariableState.False:
				return WhenBfalse();
			case BooleanVariableState.True:
				return _x.ImposeRange(_y.LowerBound, _y.UpperBound, base.Cause);
			default:
				return TryToFixZ();
			}
		}

		private bool WhenBtrue()
		{
			if (_x.ImposeRange(_y.LowerBound, _y.UpperBound, base.Cause))
			{
				return _y.ImposeRange(_x.LowerBound, _x.UpperBound, base.Cause);
			}
			return false;
		}

		private bool WhenBfalse()
		{
			return BasicConstraintUtilities.ImposeBoundsDifferent(_x, _y, base.Cause);
		}

		private bool TryToFixZ()
		{
			long lowerBound = _x.LowerBound;
			long upperBound = _x.UpperBound;
			long lowerBound2 = _y.LowerBound;
			long upperBound2 = _y.UpperBound;
			if (lowerBound > upperBound2 || lowerBound2 > upperBound)
			{
				return _z.ImposeValueFalse(base.Cause);
			}
			if (lowerBound == upperBound && upperBound == lowerBound2 && lowerBound2 == upperBound2)
			{
				return _z.ImposeValueTrue(base.Cause);
			}
			return true;
		}
	}
}
