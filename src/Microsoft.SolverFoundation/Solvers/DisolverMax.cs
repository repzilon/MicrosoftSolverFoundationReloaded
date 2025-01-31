namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Maximum of an arbitrary number of terms
	/// </summary>
	internal class DisolverMax : DisolverIntegerTerm
	{
		public DisolverMax(IntegerSolver s, long l, long r, params DisolverTerm[] args)
			: base(s, l, r, args)
		{
		}
	}
}
