namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Opposite, i.e. -x
	/// </summary>
	internal class DisolverUnaryMinus : DisolverIntegerTerm
	{
		public DisolverUnaryMinus(IntegerSolver s, long l, long r, DisolverTerm x)
			: base(s, l, r, new DisolverTerm[1] { x })
		{
		}
	}
}
