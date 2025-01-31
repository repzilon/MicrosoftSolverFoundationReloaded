using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class NumberMax : ConstantCombiner<Number>
	{
		private RewriteSystem _rs;

		protected override Number Identity
		{
			get
			{
				Number result = default(Number);
				result._fFloat = false;
				result._rat = Rational.NegativeInfinity;
				return result;
			}
		}

		protected override Expression IdentityExpr => RationalConstant.Create(_rs, Rational.NegativeInfinity);

		public NumberMax(RewriteSystem rs)
		{
			_rs = rs;
		}

		protected override bool IsIdentity(Number val)
		{
			if (!val._fFloat)
			{
				return val._rat == Rational.NegativeInfinity;
			}
			return false;
		}

		protected override bool IsSink(Number val)
		{
			if (!val._fFloat)
			{
				return val._rat == Rational.PositiveInfinity;
			}
			return false;
		}

		protected override bool IsFinalSink(Number val)
		{
			if (!val._fFloat)
			{
				return val._rat == Rational.PositiveInfinity;
			}
			return false;
		}

		protected override bool CombineConsts(ref Number valMax, Expression expr)
		{
			if (expr.GetValue(out Rational val))
			{
				if (!valMax._fFloat && valMax._rat < val)
				{
					valMax = new Number
					{
						_fFloat = false,
						_rat = val
					};
				}
				else if (valMax._fFloat && valMax._dbl < val)
				{
					valMax = new Number
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
				if (!valMax._fFloat && valMax._rat < val2)
				{
					valMax = new Number
					{
						_fFloat = true,
						_dbl = val2
					};
				}
				else if (valMax._fFloat && valMax._dbl < val2)
				{
					valMax = new Number
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
