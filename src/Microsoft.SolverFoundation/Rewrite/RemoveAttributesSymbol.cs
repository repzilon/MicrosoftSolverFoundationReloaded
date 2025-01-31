namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class RemoveAttributesSymbol : AdjustAttributesSymbol
	{
		internal RemoveAttributesSymbol(RewriteSystem rs)
			: base(rs, "RemoveAttributes", fAdd: false)
		{
		}
	}
}
