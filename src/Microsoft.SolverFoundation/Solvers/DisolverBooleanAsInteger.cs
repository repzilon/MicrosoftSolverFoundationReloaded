namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Integer term that corresponds to an explicit conversion of 
	///   Boolean term into a 0/1 value
	/// </summary>
	internal class DisolverBooleanAsInteger : DisolverIntegerTerm
	{
		public DisolverBooleanAsInteger(IntegerSolver s, DisolverBooleanTerm x)
			: base(s, 0L, 1L, new DisolverTerm[1] { x })
		{
		}
	}
}
