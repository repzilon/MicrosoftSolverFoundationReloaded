namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class MinimizeSymbol : Symbol
	{
		internal MinimizeSymbol(RewriteSystem rs)
			: base(rs, "Minimize")
		{
			AddAttributes(rs.Attributes.HoldAll);
		}
	}
}
