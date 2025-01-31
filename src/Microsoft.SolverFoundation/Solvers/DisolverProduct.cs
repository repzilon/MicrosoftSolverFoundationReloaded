namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   product, i.e. sub-terms are multiplied
	/// </summary>
	internal class DisolverProduct : DisolverIntegerTerm
	{
		public DisolverProduct(IntegerSolver s, long l, long r, params DisolverTerm[] args)
			: base(s, l, r, args)
		{
		}
	}
}
