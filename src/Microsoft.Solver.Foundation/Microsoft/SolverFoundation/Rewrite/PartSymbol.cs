using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class PartSymbol : Symbol
	{
		internal PartSymbol(RewriteSystem rs)
			: base(rs, "Part")
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
				return ib[0];
			}
			int[] array = new int[ib.Count - 1];
			for (int i = 0; i < array.Length; i++)
			{
				if (!ib[i + 1].GetValue(out array[i]))
				{
					return null;
				}
			}
			Expression expression = ib[0];
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j] == -1)
				{
					expression = expression.Head;
					continue;
				}
				if (array[j] < 0 || array[j] >= expression.Arity)
				{
					return null;
				}
				expression = expression[array[j]];
			}
			return expression;
		}

		public override void FormatInvocation(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			if (inv.Arity < 2)
			{
				base.FormatInvocation(sb, inv, out precLeft, out precRight, formatter);
				return;
			}
			int length = sb.Length;
			inv[0].Format(sb, out precLeft, out precRight, formatter);
			if ((int)precRight >= 2)
			{
				sb.Insert(length, '(');
				sb.Append(')');
			}
			sb.Append("[:");
			for (int i = 1; i < inv.Arity; i++)
			{
				if (i > 1)
				{
					sb.Append(',');
				}
				sb.Append(inv[i]);
			}
			sb.Append(":]");
			precLeft = Precedence.Invocation;
			precRight = Precedence.Atom;
		}
	}
}
