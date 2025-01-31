namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between 2 integer variables X and Y and an integer constant
	///   K imposing that Y == K*X
	/// </summary>
	internal class ProductByConstant : BinaryConstraint<IntegerVariable, IntegerVariable>
	{
		private readonly long _coef;

		public ProductByConstant(Problem p, IntegerVariable x, long k, IntegerVariable y)
			: base(p, x, y)
		{
			_coef = k;
			if (k > 0)
			{
				_x.SubscribeToAnyModification(WhenXmodified_WithCoefPositive);
				_y.SubscribeToAnyModification(WhenYmodified_WithCoefPositive);
			}
			else
			{
				_x.SubscribeToAnyModification(WhenXmodified_WithCoefNegative);
				_y.SubscribeToAnyModification(WhenYmodified_WithCoefNegative);
			}
		}

		private bool WhenXmodified_WithCoefPositive()
		{
			return _y.ImposeRange(_coef * _x.LowerBound, _coef * _x.UpperBound, base.Cause);
		}

		private bool WhenYmodified_WithCoefPositive()
		{
			return _x.ImposeRange(Utils.FloorOfDiv(_y.LowerBound, _coef), Utils.CeilOfDiv(_y.UpperBound, _coef), base.Cause);
		}

		private bool WhenXmodified_WithCoefNegative()
		{
			return _y.ImposeRange(_coef * _x.UpperBound, _coef * _x.LowerBound, base.Cause);
		}

		private bool WhenYmodified_WithCoefNegative()
		{
			return _x.ImposeRange(Utils.FloorOfDiv(_y.UpperBound, _coef), Utils.CeilOfDiv(_y.LowerBound, _coef), base.Cause);
		}
	}
}
