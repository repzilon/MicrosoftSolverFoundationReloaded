namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Disjunction
	/// </summary>
	internal class DisolverOr : DisolverBooleanTerm
	{
		public DisolverOr(IntegerSolver sol, DisolverTerm[] subterms)
			: base(sol, subterms)
		{
		}
	}
}
