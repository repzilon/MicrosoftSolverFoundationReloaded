namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between an integer variable X and a boolean variable Y 
	///   imposing that X be 1 if Y is true; 0 otherwise.
	/// </summary>
	internal class IntegerBooleanEquivalence : BinaryConstraint<IntegerVariable, BooleanVariable>
	{
		public IntegerBooleanEquivalence(Problem p, IntegerVariable x, BooleanVariable y)
			: base(p, x, y)
		{
			x.SubscribeToAnyModification(WhenXmodified);
			y.SubscribeToFalse(WhenYfalse);
			y.SubscribeToTrue(WhenYtrue);
		}

		private bool WhenXmodified()
		{
			long lowerBound = _x.LowerBound;
			long upperBound = _x.UpperBound;
			if (lowerBound != upperBound)
			{
				return true;
			}
			long num = lowerBound;
			if (num <= 1 && num >= 0)
			{
				switch (num)
				{
				case 0L:
					return _y.ImposeValueFalse(base.Cause);
				case 1L:
					return _y.ImposeValueTrue(base.Cause);
				}
			}
			return _x.ImposeEmptyDomain(base.Cause);
		}

		private bool WhenYfalse()
		{
			return _x.ImposeValue(0L, base.Cause);
		}

		private bool WhenYtrue()
		{
			return _x.ImposeValue(1L, base.Cause);
		}
	}
}
