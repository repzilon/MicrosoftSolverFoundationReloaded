using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class NumberPlus : ConstantCombiner<Number>
	{
		private RewriteSystem _rs;

		protected override Number Identity => default(Number);

		protected override Expression IdentityExpr => _rs.Builtin.Integer.Zero;

		public NumberPlus(RewriteSystem rs)
		{
			_rs = rs;
		}

		protected override bool IsIdentity(Number num)
		{
			if (num._rat.IsZero)
			{
				return !num._fFloat;
			}
			return false;
		}

		protected override bool IsSink(Number num)
		{
			return false;
		}

		protected override bool IsFinalSink(Number num)
		{
			if (num._rat.IsFinite)
			{
				if (num._fFloat)
				{
					return !NumberUtils.IsFinite(num._dbl);
				}
				return false;
			}
			return true;
		}

		protected override bool CombineConsts(ref Number numTot, Expression expr)
		{
			if (expr.GetValue(out Rational val))
			{
				numTot._rat += val;
			}
			else
			{
				if (!expr.GetValue(out double val2))
				{
					return false;
				}
				numTot._fFloat = true;
				numTot._dbl += val2;
			}
			return true;
		}

		protected override Expression ExprFromConst(Number val)
		{
			if (!val._fFloat)
			{
				return RationalConstant.Create(_rs, val._rat);
			}
			if (val._rat == 0)
			{
				return new FloatConstant(_rs, val._dbl);
			}
			return new FloatConstant(_rs, val._dbl + (double)val._rat);
		}

		public Expression PostSort(InvocationBuilder ib)
		{
			int num = 0;
			int num2 = 0;
			while (num2 < ib.Count)
			{
				int num3 = num2 + 1;
				if (num3 < ib.Count && SameModCoef(ib[num2], ib[num3]))
				{
					Number numTot = default(Number);
					CombineCoef(ref numTot, ib[num2]);
					CombineCoef(ref numTot, ib[num3++]);
					while (num3 < ib.Count && SameModCoef(ib[num2], ib[num3]))
					{
						CombineCoef(ref numTot, ib[num3++]);
					}
					Expression expression = SetCoef(numTot, ib[num2]);
					if (expression != null)
					{
						ib[num++] = expression;
					}
				}
				else
				{
					if (num2 > num)
					{
						ib[num] = ib[num2];
					}
					num++;
				}
				num2 = num3;
			}
			if (num == ib.Count)
			{
				return null;
			}
			if (num == 0)
			{
				return IdentityExpr;
			}
			ib.RemoveRange(num, ib.Count);
			Expression expression2 = EvaluateInvocationArgs(ib);
			if (expression2 != null)
			{
				return expression2;
			}
			ib.SortArgs();
			return null;
		}

		private static bool HasCoef(Expression expr)
		{
			if (expr.Head == expr.Rewrite.Builtin.Times && expr.Arity >= 1)
			{
				return expr[0].IsNumericValue;
			}
			return false;
		}

		private static bool IsTimes(Expression expr)
		{
			return expr.Head == expr.Rewrite.Builtin.Times;
		}

		private static bool SameModCoef(Expression expr0, Expression expr1)
		{
			if (expr0.Equivalent(expr1))
			{
				return true;
			}
			int num;
			if (HasCoef(expr0))
			{
				if (!IsTimes(expr1))
				{
					if (expr0.Arity == 2)
					{
						return expr0[1].Equivalent(expr1);
					}
					return false;
				}
				num = ((!HasCoef(expr1)) ? 1 : 0);
			}
			else
			{
				if (!HasCoef(expr1))
				{
					return false;
				}
				if (!IsTimes(expr0))
				{
					if (expr1.Arity == 2)
					{
						return expr1[1].Equivalent(expr0);
					}
					return false;
				}
				Statics.Swap(ref expr0, ref expr1);
				num = 1;
			}
			if (expr0.Arity - num != expr1.Arity)
			{
				return false;
			}
			for (int i = 1; i < expr0.Arity; i++)
			{
				if (!expr0[i].Equivalent(expr1[i - num]))
				{
					return false;
				}
			}
			return true;
		}

		private void CombineCoef(ref Number numTot, Expression expr)
		{
			if (expr.Head != _rs.Builtin.Times || expr.Arity == 0 || !CombineConsts(ref numTot, expr[0]))
			{
				numTot._rat += (Rational)1;
			}
		}

		private Expression SetCoef(Number numCoef, Expression exprBase)
		{
			if (IsIdentity(numCoef))
			{
				return null;
			}
			if (numCoef._rat == 1 && !numCoef._fFloat)
			{
				if (exprBase.Head != _rs.Builtin.Times || exprBase.Arity == 0 || !exprBase[0].IsNumericValue)
				{
					return exprBase;
				}
				if (exprBase.Arity == 1)
				{
					return exprBase.Rewrite.Builtin.Integer.One;
				}
				if (exprBase.Arity == 2)
				{
					return exprBase[1];
				}
				using (InvocationBuilder invocationBuilder = InvocationBuilder.GetBuilder((Invocation)exprBase, fKeepAll: true))
				{
					invocationBuilder.RemoveRange(0, 1);
					return invocationBuilder.GetNew();
				}
			}
			Expression expression = ExprFromConst(numCoef);
			if (exprBase.Head != _rs.Builtin.Times || exprBase.Arity == 0)
			{
				return _rs.Builtin.Times.Invoke(expression, exprBase).Evaluate();
			}
			using (InvocationBuilder invocationBuilder2 = InvocationBuilder.GetBuilder((Invocation)exprBase, fKeepAll: true))
			{
				if (!invocationBuilder2[0].IsNumericValue)
				{
					invocationBuilder2.Insert(0, expression);
				}
				else
				{
					invocationBuilder2[0] = expression;
				}
				return invocationBuilder2.GetNew().Evaluate();
			}
		}
	}
}
