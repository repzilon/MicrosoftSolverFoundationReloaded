namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class HoldSymbol : Symbol
	{
		internal HoldSymbol(RewriteSystem rs)
			: base(rs, "Hold")
		{
			AddAttributes(rs.Attributes.HoldAll);
		}
	}
}
