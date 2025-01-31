namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Utilities that are proper to the basic constraints in this file
	/// </summary>
	internal static class BasicConstraintUtilities
	{
		/// <summary>
		///   Makes sure that if one Boolean Variable is instantiated
		///   the other will be fixed to the same value
		/// </summary>
		public static bool ImposeEqual(BooleanVariable x, BooleanVariable y, Cause c)
		{
			switch (x.Status)
			{
			case BooleanVariableState.True:
				return y.ImposeValueTrue(c);
			case BooleanVariableState.False:
				return y.ImposeValueFalse(c);
			default:
				switch (y.Status)
				{
				case BooleanVariableState.True:
					return x.ImposeValueTrue(c);
				case BooleanVariableState.False:
					return x.ImposeValueFalse(c);
				default:
					return true;
				}
			}
		}

		/// <summary>
		///   Makes sure that if one Integer Variable is instantiated
		///   to some value no bound of the other variable is equal to it
		/// </summary>
		public static bool ImposeBoundsDifferent(IntegerVariable x, IntegerVariable y, Cause c)
		{
			if (x.IsInstantiated())
			{
				return y.ImposeBoundsDifferentFrom(x.GetValue(), c);
			}
			if (y.IsInstantiated())
			{
				return x.ImposeBoundsDifferentFrom(y.GetValue(), c);
			}
			return true;
		}
	}
}
