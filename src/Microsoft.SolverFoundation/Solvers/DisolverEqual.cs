namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Equality
	/// </summary>
	internal class DisolverEqual : DisolverBooleanTerm
	{
		public DisolverEqual(IntegerSolver sol, DisolverTerm[] subterms)
			: base(sol, subterms)
		{
		}
	}
}
