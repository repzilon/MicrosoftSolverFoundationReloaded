namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Extensional relation, i.e. the term is true iff a tuple of vars
	///   forms one of the combinations explicitly listed in a table.
	/// </summary>
	internal class DisolverPositiveTableTerm : DisolverBooleanTerm
	{
		public readonly int[][] _table;

		public readonly DisolverIntegerTerm[] _vars;

		public DisolverPositiveTableTerm(IntegerSolver s, DisolverIntegerTerm[] vars, int[][] table)
			: base(s, vars)
		{
			_table = table;
			_vars = vars;
		}
	}
}
