namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// terms for boolean constants
	/// </summary>
	internal class DisolverBooleanConstant : DisolverBooleanTerm
	{
		public DisolverBooleanConstant(IntegerSolver s)
			: base(s, null)
		{
		}
	}
}
