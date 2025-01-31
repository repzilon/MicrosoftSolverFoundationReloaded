namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Membership of a variable in a domain
	/// </summary>
	internal class DisolverMember : DisolverBooleanTerm
	{
		public readonly CspDomain _values;

		public DisolverMember(IntegerSolver s, DisolverTerm x, CspDomain d)
			: base(s, new DisolverTerm[1] { x })
		{
			_values = d;
		}
	}
}
