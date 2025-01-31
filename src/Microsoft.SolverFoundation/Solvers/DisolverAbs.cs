namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Absolute value
	/// </summary>
	internal class DisolverAbs : DisolverIntegerTerm
	{
		public DisolverAbs(IntegerSolver s, long l, long r, DisolverTerm x)
			: base(s, l, r, new DisolverTerm[1] { x })
		{
		}
	}
}
