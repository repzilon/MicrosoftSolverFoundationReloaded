namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ConstraintsSymbol : Symbol
	{
		internal ConstraintsSymbol(RewriteSystem rs)
			: base(rs, "Constraints")
		{
			AddAttributes(rs.Attributes.HoldAll);
		}
	}
}
