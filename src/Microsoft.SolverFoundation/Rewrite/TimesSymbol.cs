using System.Collections.Generic;
using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class TimesSymbol : Symbol
	{
		private NumberTimes _nt;

		internal TimesSymbol(RewriteSystem rs)
			: base(rs, "Times", new ParseInfo("*", Precedence.Times, Precedence.Power, ParseInfoOptions.VaryadicInfix))
		{
			AddAttributes(rs.Attributes.Flat, rs.Attributes.Listable, rs.Attributes.UnaryIdentity, rs.Attributes.Orderless);
			_nt = new NumberTimes(rs);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			return _nt.EvaluateInvocationArgs(ib);
		}

		public override Expression PostSort(InvocationBuilder ib)
		{
			List<Expression> rgexpr = null;
			Expression expression2;
			while (true)
			{
				int num = 0;
				int num2 = 0;
				while (num2 < ib.Count)
				{
					int i = num2 + 1;
					if (i < ib.Count && InitSameBase(ib[num2], ib[i], ref rgexpr, out var exprBase))
					{
						for (i++; i < ib.Count && AddSameBase(exprBase, ib[i], rgexpr); i++)
						{
						}
						Expression expression = base.Rewrite.Builtin.Plus.Invoke(rgexpr.ToArray());
						Expression value = base.Rewrite.Builtin.Power.Invoke(exprBase, expression).Evaluate();
						ib[num++] = value;
					}
					else
					{
						if (num2 > num)
						{
							ib[num] = ib[num2];
						}
						num++;
					}
					num2 = i;
				}
				if (num == ib.Count)
				{
					return null;
				}
				if (num == 0)
				{
					return base.Rewrite.Builtin.Integer.One;
				}
				ib.RemoveRange(num, ib.Count);
				expression2 = _nt.EvaluateInvocationArgs(ib);
				if (expression2 != null)
				{
					break;
				}
				ib.SortArgs();
			}
			return expression2;
		}

		private static bool IsPower(Expression expr)
		{
			if (expr.Head == expr.Rewrite.Builtin.Power)
			{
				return expr.Arity == 2;
			}
			return false;
		}

		private bool InitSameBase(Expression expr0, Expression expr1, ref List<Expression> rgexpr, out Expression exprBase)
		{
			if (IsPower(expr0) && IsPower(expr1) && expr0[0].Equivalent(expr1[0]))
			{
				exprBase = expr0[0];
				expr0 = expr0[1];
				expr1 = expr1[1];
			}
			else if (IsPower(expr0) && expr0[0].Equivalent(expr1))
			{
				exprBase = expr1;
				expr0 = expr0[1];
				expr1 = base.Rewrite.Builtin.Integer.One;
			}
			else if (IsPower(expr1) && expr0.Equivalent(expr1[0]))
			{
				exprBase = expr0;
				expr0 = base.Rewrite.Builtin.Integer.One;
				expr1 = expr1[1];
			}
			else
			{
				if (!expr0.Equivalent(expr1))
				{
					exprBase = null;
					return false;
				}
				exprBase = expr0;
				expr0 = base.Rewrite.Builtin.Integer.Two;
				expr1 = null;
			}
			if (rgexpr == null)
			{
				rgexpr = new List<Expression>();
			}
			else
			{
				rgexpr.Clear();
			}
			rgexpr.Add(expr0);
			if (expr1 != null)
			{
				rgexpr.Add(expr1);
			}
			return true;
		}

		private bool AddSameBase(Expression exprBase, Expression expr, List<Expression> rgexpr)
		{
			if (exprBase.Equivalent(expr))
			{
				rgexpr.Add(base.Rewrite.Builtin.Integer.One);
				return true;
			}
			if (IsPower(expr) && exprBase.Equivalent(expr[0]))
			{
				rgexpr.Add(expr[1]);
				return true;
			}
			return false;
		}

		protected override void FormatInvocationBinary(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			int length = sb.Length;
			bool flag = true;
			bool flag2 = false;
			int num = -1;
			precLeft = Precedence.None;
			precRight = Precedence.None;
			for (int i = 0; i < inv.Arity; i++)
			{
				if (inv[i] is IntegerConstant integerConstant)
				{
					if (integerConstant.Value == -1)
					{
						flag2 = !flag2;
						continue;
					}
					if (integerConstant.Value == 1)
					{
						continue;
					}
				}
				if (!flag)
				{
					if ((int)precRight >= (int)ParseInfo.LeftPrecedence && num >= 0)
					{
						sb.Insert(num, '(');
						sb.Append(')');
					}
					formatter.BeforeBinaryOperator(sb, inv);
					sb.Append(ParseInfo.OperatorText);
					formatter.AfterBinaryOperator(sb, inv);
				}
				else
				{
					flag = false;
				}
				num = sb.Length;
				inv[i].Format(sb, out precLeft, out precRight, formatter);
				if ((int)precLeft > (int)ParseInfo.RightPrecedence && !flag)
				{
					sb.Insert(num, '(');
					sb.Append(')');
					num = -1;
				}
			}
			if (flag)
			{
				sb.Append(1);
			}
			if (flag2)
			{
				if (sb[length] == '-')
				{
					sb.Remove(length, 1);
				}
				else
				{
					sb.Insert(length, '-');
				}
			}
			precLeft = ParseInfo.LeftPrecedence;
			precRight = ParseInfo.RightPrecedence;
		}
	}
}
