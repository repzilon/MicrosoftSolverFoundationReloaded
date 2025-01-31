namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class AddAttributesSymbol : AdjustAttributesSymbol
	{
		internal AddAttributesSymbol(RewriteSystem rs)
			: base(rs, "AddAttributes", fAdd: true)
		{
		}
	}
}
