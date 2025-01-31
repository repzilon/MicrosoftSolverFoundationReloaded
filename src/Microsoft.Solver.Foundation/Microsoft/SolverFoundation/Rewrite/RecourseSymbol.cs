namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>Recourse decisions.
	/// </summary>
	internal sealed class RecourseSymbol : Symbol
	{
		internal RecourseSymbol(RewriteSystem rs)
			: base(rs, "Recourse")
		{
		}
	}
}
