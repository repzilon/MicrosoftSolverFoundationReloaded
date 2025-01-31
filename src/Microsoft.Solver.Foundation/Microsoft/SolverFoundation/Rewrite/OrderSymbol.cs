using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class OrderSymbol : Symbol
	{
		internal OrderSymbol(RewriteSystem rs)
			: base(rs, "Order")
		{
		}

		public static int Compare(Expression expr0, Expression expr1)
		{
			if (expr0 == expr1)
			{
				return 0;
			}
			Invocation invocation = expr0 as Invocation;
			Invocation invocation2 = expr1 as Invocation;
			if (invocation != null)
			{
				if (invocation2 != null)
				{
					return CompareInvInv(invocation, invocation2);
				}
				return CompareInvNon(invocation, expr1);
			}
			if (invocation2 != null)
			{
				return -CompareInvNon(invocation2, expr0);
			}
			return CompareNonNon(expr0, expr1);
		}

		private static int CompareNonNon(Expression expr0, Expression expr1)
		{
			Symbol symbol = expr0 as Symbol;
			Symbol symbol2 = expr1 as Symbol;
			if (symbol == null != (symbol2 == null))
			{
				if (symbol != null)
				{
					return 1;
				}
				return -1;
			}
			if (symbol != null)
			{
				int value;
				if ((value = symbol.ParseInfo.LeftPrecedence - symbol2.ParseInfo.LeftPrecedence) == 0 && (value = symbol.ParseInfo.RightPrecedence - symbol2.ParseInfo.RightPrecedence) == 0 && (value = string.Compare(symbol.Name, symbol2.Name, StringComparison.Ordinal)) == 0)
				{
					value = symbol.Id - symbol2.Id;
				}
				return Math.Sign(value);
			}
			ConstantHeadSymbol constantHeadSymbol = (ConstantHeadSymbol)expr0.Head;
			ConstantHeadSymbol constantHeadSymbol2 = (ConstantHeadSymbol)expr1.Head;
			if (constantHeadSymbol == constantHeadSymbol2)
			{
				return constantHeadSymbol.CompareConstants(expr0, expr1);
			}
			Rational val2;
			double val3;
			if (expr0.GetValue(out Rational val))
			{
				if (expr1.GetValue(out val2))
				{
					return val.CompareTo(val2);
				}
				if (expr1.GetValue(out val3))
				{
					return val.CompareTo(val3);
				}
				return -1;
			}
			if (expr0.GetValue(out double val4))
			{
				if (expr1.GetValue(out val2))
				{
					return -val2.CompareTo(val4);
				}
				if (expr1.GetValue(out val3))
				{
					return val4.CompareTo(val3);
				}
				return -1;
			}
			if (expr1.GetValue(out val2) || expr1.GetValue(out val3))
			{
				return 1;
			}
			return Compare(constantHeadSymbol, constantHeadSymbol2);
		}

		private static bool IsTimes(Invocation inv)
		{
			if (inv.Arity >= 1)
			{
				return inv.Head == inv.Rewrite.Builtin.Times;
			}
			return false;
		}

		private static bool HasCoef(Invocation inv)
		{
			if (inv.Arity >= 1 && inv.Head == inv.Rewrite.Builtin.Times)
			{
				return inv[0].IsNumericValue;
			}
			return false;
		}

		private static bool HasExp(Invocation inv)
		{
			if (inv.Head == inv.Rewrite.Builtin.Power)
			{
				return inv.Arity >= 2;
			}
			return false;
		}

		private static int CompareInvInv(Invocation inv0, Invocation inv1)
		{
			bool flag = IsTimes(inv0);
			bool flag2 = IsTimes(inv1);
			bool flag3 = flag && inv0[0].IsNumericValue;
			bool flag4 = flag2 && inv1[0].IsNumericValue;
			int num;
			int num2;
			int result;
			if (flag3)
			{
				if (!flag2)
				{
					if (inv0.Arity == 2 && (result = Compare(inv0[1], inv1)) != 0)
					{
						return result;
					}
					return 1;
				}
				num = 1;
				num2 = (flag4 ? 1 : 0);
			}
			else if (flag4)
			{
				if (!flag)
				{
					if (inv1.Arity == 2 && (result = Compare(inv0, inv1[1])) != 0)
					{
						return result;
					}
					return -1;
				}
				num = 0;
				num2 = 1;
			}
			else
			{
				bool flag5;
				if ((flag5 = HasExp(inv0)) != HasExp(inv1))
				{
					if (flag5)
					{
						if ((result = Compare(inv0[0], inv1)) != 0)
						{
							return result;
						}
						return 1;
					}
					if ((result = Compare(inv0, inv1[0])) != 0)
					{
						return result;
					}
					return -1;
				}
				if ((result = Compare(inv0.Head, inv1.Head)) != 0)
				{
					return result;
				}
				num = 0;
				num2 = 0;
			}
			result = Math.Sign(inv0.Arity - inv1.Arity - num + num2);
			if (result != 0)
			{
				return result;
			}
			for (int i = 0; i < inv0.Arity - num; i++)
			{
				if ((result = Compare(inv0[i + num], inv1[i + num2])) != 0)
				{
					return result;
				}
			}
			if (!flag3)
			{
				if (!flag4)
				{
					return 0;
				}
				return -1;
			}
			if (!flag4)
			{
				return 1;
			}
			return Compare(inv0[0], inv1[0]);
		}

		private static int CompareInvNon(Invocation inv, Expression expr)
		{
			if (inv.Arity == 2 && HasCoef(inv))
			{
				int num = Compare(inv[1], expr);
				if (num != 0)
				{
					return num;
				}
			}
			else if (HasExp(inv))
			{
				int num2 = Compare(inv[0], expr);
				if (num2 != 0)
				{
					return num2;
				}
			}
			return 1;
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2)
			{
				return null;
			}
			return new IntegerConstant(base.Rewrite, Compare(ib[0], ib[1]));
		}
	}
}
