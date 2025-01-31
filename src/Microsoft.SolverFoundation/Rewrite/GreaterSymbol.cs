namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class GreaterSymbol : CompareSymbol
	{
		internal override Direction Dir => Direction.Greater;

		internal GreaterSymbol(RewriteSystem rs)
			: base(rs, "Greater", new ParseInfo(">", Precedence.Compare, Precedence.Plus, ParseInfoOptions.VaryadicInfix | ParseInfoOptions.Comparison))
		{
		}
	}
}
