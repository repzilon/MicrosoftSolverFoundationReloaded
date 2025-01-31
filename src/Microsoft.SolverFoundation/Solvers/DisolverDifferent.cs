namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Difference, aka "unequality"
	/// </summary>
	internal class DisolverDifferent : DisolverBooleanTerm
	{
		public DisolverDifferent(IntegerSolver sol, DisolverTerm[] subterms)
			: base(sol, subterms)
		{
		}
	}
}
