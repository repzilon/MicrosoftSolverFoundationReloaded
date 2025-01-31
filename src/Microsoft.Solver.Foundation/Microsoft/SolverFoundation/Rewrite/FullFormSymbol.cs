using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class FullFormSymbol : Symbol
	{
		internal FullFormSymbol(RewriteSystem rs)
			: base(rs, "FullForm")
		{
		}

		public override void FormatInvocation(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			if (inv.Arity == 1)
			{
				inv[0].FormatFull(sb, formatter);
			}
			else
			{
				inv.FormatFull(sb, formatter);
			}
			precLeft = Precedence.Invocation;
			precRight = Precedence.Atom;
		}
	}
}
