using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// This and SetDelayed are almost identical. SetDelayed has HoldAll while Set has HoldFirst.
	/// SetDelayed returns Null when successful and this returns the rhs.
	/// </summary>
	internal abstract class SetBaseSymbol : Symbol
	{
		internal SetBaseSymbol(RewriteSystem rs, string strName, ParseInfo pi)
			: base(rs, strName, pi)
		{
		}

		protected Expression AccumulateConditions(Expression expr, ref List<Expression> rgexprCond)
		{
			int index = ((rgexprCond != null) ? rgexprCond.Count : 0);
			while (expr.Head == base.Rewrite.Builtin.Condition && expr.Arity == 2)
			{
				if (rgexprCond == null)
				{
					rgexprCond = new List<Expression>();
				}
				rgexprCond.Insert(index, expr[1]);
				expr = expr[0];
			}
			return expr;
		}

		protected bool GetAssignmentParts(InvocationBuilder ib, out Symbol sym, out Expression exprLeft, out Expression exprRight, out Expression exprCond, out Expression exprFail)
		{
			exprCond = null;
			List<Expression> rgexprCond = null;
			exprLeft = AccumulateConditions(ib[0], ref rgexprCond);
			if (ib.Count < 2)
			{
				exprRight = null;
			}
			else
			{
				exprRight = AccumulateConditions(ib[1], ref rgexprCond);
			}
			if (exprLeft is Invocation invSrc)
			{
				exprLeft = base.Rewrite.EvaluateHeadAndArgs(invSrc);
			}
			sym = exprLeft.FirstSymbolHead;
			exprFail = base.Rewrite.FailOnValuesLocked(sym);
			if (exprFail != null)
			{
				return false;
			}
			if (rgexprCond != null && rgexprCond.Count > 0)
			{
				if (rgexprCond.Count == 1)
				{
					exprCond = rgexprCond[0];
				}
				else
				{
					exprCond = base.Rewrite.Builtin.And.Invoke(rgexprCond.ToArray());
				}
			}
			return true;
		}
	}
}
