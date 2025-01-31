namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Terms representing constants
	/// </summary>
	internal class DisolverIntegerConstant : DisolverIntegerTerm
	{
		public DisolverIntegerConstant(IntegerSolver s, long k)
			: base(s, k, k, null)
		{
		}
	}
}
