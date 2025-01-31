namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// For now, this is the only type we support
	/// </summary>
	internal sealed class ExcelInputTypeSymbol : Symbol
	{
		internal ExcelInputTypeSymbol(RewriteSystem rs)
			: base(rs, "Excel")
		{
		}
	}
}
