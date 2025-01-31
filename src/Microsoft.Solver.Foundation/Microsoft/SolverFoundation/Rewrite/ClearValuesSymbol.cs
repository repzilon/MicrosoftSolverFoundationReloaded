namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ClearValuesSymbol : ClearBaseSymbol
	{
		internal ClearValuesSymbol(RewriteSystem rs)
			: base(rs, "ClearValues")
		{
		}

		protected override Expression CheckForLocked(Symbol sym)
		{
			return base.Rewrite.FailOnValuesLocked(sym);
		}

		protected override void ClearSymbol(Symbol sym)
		{
			sym.ClearValues();
		}
	}
}
