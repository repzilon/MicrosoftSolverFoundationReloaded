namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class MaximizeSymbol : Symbol
	{
		internal MaximizeSymbol(RewriteSystem rs)
			: base(rs, "Maximize")
		{
			AddAttributes(rs.Attributes.HoldAll);
		}
	}
}
