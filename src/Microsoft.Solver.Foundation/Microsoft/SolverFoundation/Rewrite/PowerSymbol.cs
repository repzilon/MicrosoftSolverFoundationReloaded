namespace Microsoft.SolverFoundation.Rewrite
{
	internal class PowerSymbol : PowerSymbolBase
	{
		internal PowerSymbol(RewriteSystem rs)
			: base(rs, "Power", new ParseInfo("^", Precedence.Unary, Precedence.Power, ParseInfoOptions.VaryadicInfix))
		{
			AddAttributes(rs.Attributes.Listable, rs.Attributes.UnaryIdentity);
		}
	}
}
