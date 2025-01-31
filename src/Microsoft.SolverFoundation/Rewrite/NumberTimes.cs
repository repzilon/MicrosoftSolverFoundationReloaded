using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class NumberTimes : ConstantCombiner<Number>
	{
		private RewriteSystem _rs;

		protected override Number Identity
		{
			get
			{
				Number result = default(Number);
				result._fFloat = false;
				result._rat = 1;
				result._dbl = 1.0;
				return result;
			}
		}

		protected override Expression IdentityExpr => _rs.Builtin.Integer.One;

		public NumberTimes(RewriteSystem rs)
		{
			_rs = rs;
		}

		protected override bool IsIdentity(Number val)
		{
			if (!val._fFloat)
			{
				return val._rat == 1;
			}
			return false;
		}

		protected override bool IsSink(Number val)
		{
			return false;
		}

		protected override bool IsFinalSink(Number num)
		{
			if (num._rat.IsZero)
			{
				return true;
			}
			if (!num._fFloat)
			{
				return num._rat.IsIndeterminate;
			}
			return double.IsNaN(num._dbl * (double)num._rat);
		}

		protected override bool CombineConsts(ref Number valTot, Expression expr)
		{
			if (expr.GetValue(out Rational val))
			{
				valTot._rat *= val;
			}
			else
			{
				if (!expr.GetValue(out double val2))
				{
					return false;
				}
				valTot._fFloat = true;
				valTot._dbl *= val2;
			}
			return true;
		}

		protected override Expression ExprFromConst(Number val)
		{
			if (!val._fFloat || (val._rat == 0 && NumberUtils.IsFinite(val._dbl)))
			{
				return RationalConstant.Create(_rs, val._rat);
			}
			return new FloatConstant(_rs, val._dbl * (double)val._rat);
		}
	}
}
