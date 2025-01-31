using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class HoleSymbol : Symbol
	{
		internal HoleSymbol(RewriteSystem rs)
			: base(rs, "Hole")
		{
		}

		public override void FormatInvocation(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			if (inv.Arity == 0)
			{
				sb.Append('_');
				precLeft = (precRight = Precedence.Atom);
			}
			else
			{
				FormatInvocationPlain(sb, inv, out precLeft, out precRight, formatter);
			}
		}
	}
}
