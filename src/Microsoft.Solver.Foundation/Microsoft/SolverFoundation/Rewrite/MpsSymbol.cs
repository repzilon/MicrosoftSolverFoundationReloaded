namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Symbol for MPS files.
	/// </summary>
	internal class MpsSymbol : BaseSolveSymbol
	{
		internal MpsSymbol(SolveRewriteSystem rs)
			: base(rs, "Mps")
		{
		}
	}
}
