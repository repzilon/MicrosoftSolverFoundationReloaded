using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class NumberMin : ConstantCombiner<Number>
	{
		private RewriteSystem _rs;

		protected override Number Identity
		{
			get
			{
				Number result = default(Number);
				result._fFloat = false;
				result._rat = Rational.PositiveInfinity;
				return result;
			}
		}

		protected override Expression IdentityExpr => RationalConstant.Create(_rs, Rational.PositiveInfinity);

		public NumberMin(RewriteSystem rs)
		{
			_rs = rs;
		}

		protected override bool IsIdentity(Number val)
		{
			if (!val._fFloat)
			{
				return val._rat == Rational.PositiveInfinity;
			}
			return false;
		}

		protected override bool IsSink(Number val)
		{
			if (!val._fFloat)
			{
				return val._rat == Rational.NegativeInfinity;
			}
			return false;
		}

		protected override bool IsFinalSink(Number val)
		{
			if (!val._fFloat)
			{
				return val._rat == Rational.NegativeInfinity;
			}
			return false;
		}

		protected override bool CombineConsts(ref Number valMin, Expression expr)
		{
			if (expr.GetValue(out Rational val))
			{
				if (!valMin._fFloat && valMin._rat > val)
				{
					valMin = new Number
					{
						_fFloat = false,
						_rat = val
					};
				}
				else if (valMin._fFloat && valMin._dbl > val)
				{
					valMin = new Number
					{
						_fFloat = false,
						_rat = val
					};
				}
			}
			else
			{
				if (!expr.GetValue(out double val2))
				{
					return false;
				}
				if (!valMin._fFloat && valMin._rat > val2)
				{
					valMin = new Number
					{
						_fFloat = true,
						_dbl = val2
					};
				}
				else if (valMin._fFloat && valMin._dbl > val2)
				{
					valMin = new Number
					{
						_fFloat = true,
						_dbl = val2
					};
				}
			}
			return true;
		}

		protected override Expression ExprFromConst(Number val)
		{
			if (val._fFloat)
			{
				return new FloatConstant(_rs, val._dbl);
			}
			return RationalConstant.Create(_rs, val._rat);
		}
	}
}
