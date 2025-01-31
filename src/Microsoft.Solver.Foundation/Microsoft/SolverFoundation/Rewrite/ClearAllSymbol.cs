namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ClearAllSymbol : ClearBaseSymbol
	{
		internal ClearAllSymbol(RewriteSystem rs)
			: base(rs, "ClearAll")
		{
		}

		protected override Expression CheckForLocked(Symbol sym)
		{
			Expression expression = base.Rewrite.FailOnAttributesLocked(sym);
			if (expression == null)
			{
				expression = base.Rewrite.FailOnValuesLocked(sym);
			}
			return expression;
		}

		protected override void ClearSymbol(Symbol sym)
		{
			sym.ClearAttributes();
			sym.ClearValues();
		}
	}
}
