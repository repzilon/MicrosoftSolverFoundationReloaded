namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>Define input (data binding)
	/// </summary>
	internal sealed class InputSectionSymbol : Symbol
	{
		internal InputSectionSymbol(RewriteSystem rs)
			: base(rs, "Input")
		{
		}
	}
}
