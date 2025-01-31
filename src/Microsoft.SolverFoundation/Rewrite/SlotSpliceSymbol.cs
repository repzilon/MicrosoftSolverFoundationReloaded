using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class SlotSpliceSymbol : Symbol
	{
		internal SlotSpliceSymbol(RewriteSystem rs)
			: base(rs, "SlotSplice")
		{
		}

		public override void FormatInvocation(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			if (inv.Arity == 1 && inv[0].GetValue(out int val) && val >= 0)
			{
				sb.AppendFormat("##{0}", val);
				precLeft = (precRight = Precedence.Atom);
			}
			else
			{
				FormatInvocationPlain(sb, inv, out precLeft, out precRight, formatter);
			}
		}
	}
}
