namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ClearAttributesSymbol : ClearBaseSymbol
	{
		internal ClearAttributesSymbol(RewriteSystem rs)
			: base(rs, "ClearAttributes")
		{
		}

		protected override Expression CheckForLocked(Symbol sym)
		{
			return base.Rewrite.FailOnAttributesLocked(sym);
		}

		protected override void ClearSymbol(Symbol sym)
		{
			sym.ClearAttributes();
		}
	}
}
