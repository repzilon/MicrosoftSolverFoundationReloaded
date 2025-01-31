using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ListSymbol : Symbol
	{
		internal ListSymbol(RewriteSystem rs)
			: base(rs, "List")
		{
		}

		public override Expression EvaluateInvocationArgsNested(Invocation invHead, InvocationBuilder ib)
		{
			if (invHead.Head == this && ib.Count == 1 && ib[0].GetValue(out int val) && val >= 0 && val < invHead.Arity)
			{
				return invHead[val];
			}
			return base.EvaluateInvocationArgsNested(invHead, ib);
		}

		public override void FormatInvocation(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			sb.Append('{');
			formatter.BeginInvocationArgs(sb, inv);
			for (int i = 0; i < inv.Arity; i++)
			{
				formatter.BeginOneArg(sb, inv);
				sb.Append(inv[i]);
				if (i < inv.Arity - 1)
				{
					sb.Append(',');
				}
				formatter.EndOneArg(sb, i >= inv.Arity - 1, inv);
			}
			formatter.EndInvocationArgs(sb, inv);
			sb.Append('}');
			precLeft = (precRight = Precedence.Atom);
		}
	}
}
