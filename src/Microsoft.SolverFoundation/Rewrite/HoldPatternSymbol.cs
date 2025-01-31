namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class HoldPatternSymbol : Symbol
	{
		internal HoldPatternSymbol(RewriteSystem rs)
			: base(rs, "HoldPattern")
		{
			AddAttributes(rs.Attributes.HoldAll);
		}
	}
}
