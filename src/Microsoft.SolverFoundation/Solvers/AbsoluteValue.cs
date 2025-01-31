using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between 2 integer variables X and Y and an integer constant
	///   K imposing that Y == Abs(X)
	/// </summary>
	internal class AbsoluteValue : BinaryConstraint<IntegerVariable, IntegerVariable>
	{
		public AbsoluteValue(Problem p, IntegerVariable x, IntegerVariable y)
			: base(p, x, y)
		{
			x.SubscribeToAnyModification(WhenXmodified);
			y.SubscribeToAnyModification(WhenYmodified);
		}

		private bool WhenXmodified()
		{
			long lowerBound = _x.LowerBound;
			long upperBound = _x.UpperBound;
			if (lowerBound >= 0)
			{
				return _y.ImposeRange(lowerBound, upperBound, base.Cause);
			}
			if (upperBound < 0)
			{
				return _y.ImposeRange(-upperBound, -lowerBound, base.Cause);
			}
			long newub = Math.Max(-lowerBound, upperBound);
			return _y.ImposeRange(0L, newub, base.Cause);
		}

		private bool WhenYmodified()
		{
			long upperBound = _y.UpperBound;
			if (!_x.ImposeRange(-upperBound, upperBound, base.Cause))
			{
				return false;
			}
			if (_y.LowerBound > 0)
			{
				long lowerBound = _y.LowerBound;
				long num = -lowerBound;
				if (_x.LowerBound > num)
				{
					return _x.ImposeLowerBound(lowerBound, base.Cause);
				}
				if (_x.UpperBound < lowerBound)
				{
					return _x.ImposeUpperBound(num, base.Cause);
				}
			}
			return true;
		}
	}
}
