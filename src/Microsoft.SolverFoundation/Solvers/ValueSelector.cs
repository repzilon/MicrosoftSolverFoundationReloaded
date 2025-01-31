namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Value Ordering meant to be used jointly with a Variable ordering
	///   heuristic. Must be stateless.
	/// </summary>
	internal abstract class ValueSelector
	{
		protected readonly TreeSearchAlgorithm _treeSearch;

		protected readonly Problem _problem;

		public ValueSelector(TreeSearchAlgorithm algo)
		{
			_treeSearch = algo;
			_problem = algo.Problem;
		}

		/// <summary>
		///   Given a variable, returns a value for it that is
		///   within its domain and should be chosen for branching
		/// </summary>
		public abstract long DecideValue(DiscreteVariable v);
	}
}
