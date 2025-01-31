namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Variable Ordering meant to be used jointly with a Value ordering
	///   heuristic. Can be stateful, will be backtracked.
	/// </summary>
	internal abstract class VariableSelector
	{
		protected readonly TreeSearchAlgorithm _treeSearch;

		protected readonly Problem _problem;

		public VariableSelector(TreeSearchAlgorithm algo)
		{
			_treeSearch = algo;
			_problem = algo.Problem;
		}

		/// <summary>
		///   Returns a variable that is not instantiated and should
		///   be branched-on. Null value indicates that all variables
		///   are instantiated.
		/// </summary>
		public abstract DiscreteVariable DecideNextVariable();
	}
}
