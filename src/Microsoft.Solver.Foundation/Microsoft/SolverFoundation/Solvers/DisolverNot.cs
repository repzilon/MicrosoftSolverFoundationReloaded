namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   negation of a term
	/// </summary>
	internal class DisolverNot : DisolverBooleanTerm
	{
		public DisolverBooleanTerm Subterm => base.SubTerms[0] as DisolverBooleanTerm;

		public DisolverNot(IntegerSolver sol, DisolverTerm subterm)
			: base(sol, new DisolverTerm[1] { subterm })
		{
		}
	}
}
