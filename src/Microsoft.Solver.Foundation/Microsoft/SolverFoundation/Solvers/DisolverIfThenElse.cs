namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Conditional, i.e.   condition ? left : right
	/// </summary>
	internal class DisolverIfThenElse : DisolverIntegerTerm
	{
		public DisolverIfThenElse(IntegerSolver s, long l, long r, DisolverTerm cond, DisolverTerm case1, DisolverTerm case2)
			: base(s, l, r, new DisolverTerm[3] { cond, case1, case2 })
		{
		}
	}
}
