using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class PatternSymbol : Symbol
	{
		internal PatternSymbol(RewriteSystem rs)
			: base(rs, "Pattern", new ParseInfo(ParseInfoOptions.CreateVariable))
		{
		}

		public override void FormatInvocation(StringBuilder sb, Invocation inv, out Precedence precLeft, out Precedence precRight, IExpressionFormatter formatter)
		{
			if (inv.Arity != 2 || !(inv[0] is Symbol))
			{
				FormatInvocationPlain(sb, inv, out precLeft, out precRight, formatter);
				return;
			}
			if (inv[1].Head == base.Rewrite.Builtin.Hole && inv[1].Arity == 0)
			{
				sb.AppendFormat("{0}_", inv[0]);
				precLeft = (precRight = Precedence.Atom);
				return;
			}
			if (inv[1].Head == base.Rewrite.Builtin.HoleSplice && inv[1].Arity == 0)
			{
				sb.AppendFormat("{0}___", inv[0]);
				precLeft = (precRight = Precedence.Atom);
				return;
			}
			sb.Append(inv[0]);
			sb.Append(':');
			inv[1].Format(sb, out precLeft, out precRight, formatter);
			precLeft = Precedence.Atom;
		}
	}
}
