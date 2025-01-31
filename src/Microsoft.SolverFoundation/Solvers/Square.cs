using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between 2 integer variables X and Y and an integer constant
	///   K imposing that Y == X ^ 2
	/// </summary>
	internal class Square : BinaryConstraint<IntegerVariable, IntegerVariable>
	{
		public Square(Problem p, IntegerVariable x, IntegerVariable y)
			: base(p, x, y)
		{
			x.SubscribeToAnyModification(WhenXmodified);
			y.SubscribeToAnyModification(WhenYmodified);
		}

		private bool WhenXmodified()
		{
			long lowerBound = _x.LowerBound;
			long upperBound = _x.UpperBound;
			checked
			{
				long num = lowerBound * lowerBound;
				long num2 = upperBound * upperBound;
				if (lowerBound >= 0)
				{
					return _y.ImposeRange(num, num2, base.Cause);
				}
				if (upperBound < 0)
				{
					return _y.ImposeRange(num2, num, base.Cause);
				}
				long newub = Math.Max(num, num2);
				return _y.ImposeRange(0L, newub, base.Cause);
			}
		}

		private bool WhenYmodified()
		{
			long upperBound = _y.UpperBound;
			long num = (long)Math.Ceiling(Math.Sqrt(upperBound));
			if (!_x.ImposeRange(-num, num, base.Cause))
			{
				return false;
			}
			long lowerBound = _y.LowerBound;
			if (lowerBound > 0)
			{
				long num2 = (long)Math.Floor(Math.Sqrt(lowerBound));
				long num3 = -num2;
				if (_x.LowerBound > num3)
				{
					return _x.ImposeLowerBound(num2, base.Cause);
				}
				if (_x.UpperBound < num2)
				{
					return _x.ImposeUpperBound(num3, base.Cause);
				}
			}
			return true;
		}
	}
}
