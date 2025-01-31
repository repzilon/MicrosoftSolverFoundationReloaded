using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class HoleSpliceSymbol : Symbol
	{
		internal HoleSpliceSymbol(RewriteSystem rs)
			: base(rs, "HoleSplice")
		{
		}

		public override void FormatInvocation(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			if (inv.Arity == 0)
			{
				sb.Append("___");
				precLeft = (precRight = Precedence.Atom);
			}
			else
			{
				FormatInvocationPlain(sb, inv, out precLeft, out precRight, formatter);
			}
		}
	}
}
