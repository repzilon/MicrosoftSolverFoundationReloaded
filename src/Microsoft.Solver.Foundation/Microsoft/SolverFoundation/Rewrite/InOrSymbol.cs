using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class InOrSymbol : SetOpSymbol
	{
		internal InOrSymbol(RewriteSystem rs)
			: base(rs, "InOr")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count < 1)
			{
				return null;
			}
			if (ib.Count == 1)
			{
				return base.Rewrite.Builtin.Boolean.False;
			}
			int num = 1;
			Func<Expression, bool?> func;
			if (ib[0].GetNumericValue(out var num2))
			{
				func = (Expression expr) => IsValueInSet(num2, expr);
			}
			else
			{
				if (ib[0].Head != base.Rewrite.Builtin.Tuple)
				{
					return null;
				}
				Expression exprTuple = ib[0];
				func = (Expression expr) => IsTupleInSet(exprTuple, expr);
			}
			for (int i = 1; i < ib.Count; i++)
			{
				bool? flag = func(ib[i]);
				if (flag == true)
				{
					return GetExpr(true);
				}
				if (!flag.HasValue)
				{
					if (num < i)
					{
						ib[num] = ib[i];
					}
					num++;
				}
			}
			if (num == 1)
			{
				return base.Rewrite.Builtin.Boolean.False;
			}
			if (num < ib.Count)
			{
				ib.RemoveRange(num, ib.Count);
			}
			return null;
		}
	}
}
