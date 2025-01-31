namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class LessEqualSymbol : CompareSymbol
	{
		internal override Direction Dir => Direction.LessEqual;

		internal LessEqualSymbol(RewriteSystem rs)
			: base(rs, "LessEqual", new ParseInfo("<=", Precedence.Compare, Precedence.Plus, ParseInfoOptions.VaryadicInfix | ParseInfoOptions.Comparison))
		{
		}
	}
}
