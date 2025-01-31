namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>Define parameters with data binding
	/// </summary>
	internal sealed class ParametersSymbol : Symbol
	{
		internal ParametersSymbol(RewriteSystem rs)
			: base(rs, "Parameters")
		{
			AddAttributes(rs.Attributes.HoldAll);
			AddAttributes(rs.Attributes.HoldSplice);
		}
	}
}
