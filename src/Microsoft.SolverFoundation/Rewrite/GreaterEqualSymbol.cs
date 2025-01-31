namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class GreaterEqualSymbol : CompareSymbol
	{
		internal override Direction Dir => Direction.GreaterEqual;

		internal GreaterEqualSymbol(RewriteSystem rs)
			: base(rs, "GreaterEqual", new ParseInfo(">=", Precedence.Compare, Precedence.Plus, ParseInfoOptions.VaryadicInfix | ParseInfoOptions.Comparison))
		{
		}
	}
}
