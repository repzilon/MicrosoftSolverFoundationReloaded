namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Sum of an arbitrary number of subterms
	/// </summary>
	internal class DisolverSum : DisolverIntegerTerm
	{
		public DisolverSum(IntegerSolver s, long l, long r, DisolverTerm[] args)
			: base(s, l, r, args)
		{
		}
	}
}
