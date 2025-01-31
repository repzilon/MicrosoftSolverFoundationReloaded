namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Conjunction
	/// </summary>
	internal class DisolverAnd : DisolverBooleanTerm
	{
		public DisolverAnd(IntegerSolver sol, DisolverTerm[] subterms)
			: base(sol, subterms)
		{
		}
	}
}
