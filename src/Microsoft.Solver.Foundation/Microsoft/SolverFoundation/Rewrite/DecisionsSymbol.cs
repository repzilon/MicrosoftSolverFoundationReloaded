namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class DecisionsSymbol : Symbol
	{
		internal DecisionsSymbol(RewriteSystem rs)
			: base(rs, "Decisions")
		{
			AddAttributes(rs.Attributes.HoldSplice);
			AddAttributes(rs.Attributes.HoldAll);
		}
	}
}
