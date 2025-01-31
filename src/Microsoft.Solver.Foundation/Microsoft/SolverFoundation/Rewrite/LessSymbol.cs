namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class LessSymbol : CompareSymbol
	{
		internal override Direction Dir => Direction.Less;

		internal LessSymbol(RewriteSystem rs)
			: base(rs, "Less", new ParseInfo("<", Precedence.Compare, Precedence.Plus, ParseInfoOptions.VaryadicInfix | ParseInfoOptions.Comparison))
		{
		}
	}
}
