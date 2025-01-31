namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Square i.e. sub-term multiplied by itself
	/// </summary>
	internal class DisolverSquare : DisolverIntegerTerm
	{
		public DisolverSquare(IntegerSolver s, long l, long r, DisolverTerm x)
			: base(s, l, r, new DisolverTerm[1] { x })
		{
		}
	}
}
