namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Minimum of an arbitrary number of terms
	/// </summary>
	internal class DisolverMin : DisolverIntegerTerm
	{
		public DisolverMin(IntegerSolver s, long l, long r, params DisolverTerm[] args)
			: base(s, l, r, args)
		{
		}
	}
}
