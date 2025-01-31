namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   ordering between two variables
	/// </summary>
	internal class DisolverLessEqual : DisolverBooleanTerm
	{
		public DisolverLessEqual(IntegerSolver sol, DisolverTerm[] subterms)
			: base(sol, subterms)
		{
		}
	}
}
