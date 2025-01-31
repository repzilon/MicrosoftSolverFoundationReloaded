using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class PowerSymbolBase : Symbol
	{
		internal PowerSymbolBase(RewriteSystem rs, string strName)
			: base(rs, strName)
		{
		}

		internal PowerSymbolBase(RewriteSystem rs, string strName, ParseInfo parseInfo)
			: base(rs, strName, parseInfo)
		{
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			Number number = default(Number);
			number._fFloat = false;
			number._rat = 1;
			number._dbl = 1.0;
			int num = ib.Count;
			int num2 = num;
			while (--num2 >= 0)
			{
				if (ib[num2].GetValue(out Rational val))
				{
					if (num2 == num - 1)
					{
						Rational ratRes;
						if (number._fFloat)
						{
							number._dbl = Math.Pow((double)val, number._dbl);
						}
						else if (Rational.Power(val, number._rat, out ratRes))
						{
							number._rat = ratRes;
						}
						else
						{
							number._fFloat = true;
							number._dbl = Math.Pow((double)val, (double)number._rat);
						}
						num = num2;
					}
					else if (val == 1)
					{
						number._rat = 1;
						number._fFloat = false;
						num = num2;
					}
				}
				else
				{
					if (!ib[num2].GetValue(out double val2))
					{
						continue;
					}
					if (num2 == num - 1)
					{
						if (!number._fFloat)
						{
							number._fFloat = true;
							number._dbl = (double)number._rat;
						}
						number._dbl = Math.Pow(val2, number._dbl);
						num = num2;
					}
					else if (number._dbl == 1.0)
					{
						number._dbl = 1.0;
						number._fFloat = true;
						num = num2;
					}
				}
			}
			if (num == 0)
			{
				if (!number._fFloat)
				{
					return RationalConstant.Create(base.Rewrite, number._rat);
				}
				return new FloatConstant(base.Rewrite, number._dbl);
			}
			Expression expression = ib[num - 1];
			BigInteger bn;
			if (number._fFloat)
			{
				if (++num < ib.Count)
				{
					ib[num - 1] = new FloatConstant(base.Rewrite, number._dbl);
				}
			}
			else if (!number._rat.IsInteger(out bn))
			{
				if (++num < ib.Count)
				{
					ib[num - 1] = RationalConstant.Create(base.Rewrite, number._rat);
				}
			}
			else if (bn.IsZero)
			{
				num--;
			}
			else if (!(bn == 1))
			{
				if ((expression = ib[num - 1]).Head == base.Rewrite.Builtin.Power && expression.Arity == 2)
				{
					ib[num - 1] = base.Rewrite.Builtin.Power.Invoke(expression[0], expression[1] * bn).Evaluate();
				}
				else if (expression.Head == base.Rewrite.Builtin.Times)
				{
					ib[num - 1] = DistributePower((Invocation)expression, bn);
				}
				else if (++num < ib.Count)
				{
					ib[num - 1] = new IntegerConstant(base.Rewrite, bn);
				}
			}
			if (num == 0)
			{
				return base.Rewrite.Builtin.Integer.One;
			}
			if (num < ib.Count)
			{
				ib.RemoveRange(num, ib.Count);
			}
			if (num <= 2)
			{
				return null;
			}
			Expression expression2 = base.Rewrite.Builtin.Power.Invoke(ib[num - 2], ib[num - 1]);
			int num3 = num - 2;
			while (--num3 >= 0)
			{
				expression2 = base.Rewrite.Builtin.Power.Invoke(ib[num3], expression2);
			}
			return expression2;
		}

		private Expression DistributePower(Invocation inv, BigInteger bn)
		{
			using (InvocationBuilder invocationBuilder = InvocationBuilder.GetBuilder(inv, fKeepAll: true))
			{
				Expression expression = new IntegerConstant(base.Rewrite, bn);
				for (int i = 0; i < invocationBuilder.Count; i++)
				{
					invocationBuilder[i] = base.Rewrite.Builtin.Power.Invoke(invocationBuilder[i], expression);
				}
				return invocationBuilder.GetNew().Evaluate();
			}
		}
	}
}
