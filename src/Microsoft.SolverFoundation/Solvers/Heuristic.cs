namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Root class for heuristics. A heuristics is an object that parametrizes
	///   a tree-search solver by indicating which decisions to make.
	/// </summary>
	internal abstract class Heuristic
	{
		protected readonly TreeSearchAlgorithm _treeSearch;

		protected readonly Problem _problem;

		protected Heuristic(TreeSearchAlgorithm algo)
		{
			_treeSearch = algo;
			_problem = algo.Problem;
		}

		/// <summary>
		///   Picks a well-chosen decision that should be done by a tree search
		///   algorithm. The decision might be to tighten a value (e.g. by
		///   imposing a new lower bound) or something else, e.g. to restart or
		///   to stop because a solution is found.
		///
		///   If the decision is to tighen a variable then note that (1) the 
		///   variable should not already be instantiated; (2) the decision
		///   should indeed reduce its domain; (3) the decision cannot be an
		///   instantiation to an arbitrary value, i.e. it has to preserve the
		///   convexity of any convex domain (ImposeLowerBound/ImposeUpperBound).
		///   This allows refutation to work robustly.
		/// </summary>
		public DisolverDecision Decide()
		{
			return NextDecision();
		}

		/// <summary>
		///   Actual implementation of the decision method
		///   depending on the concrete type of the heuristic
		/// </summary>
		public abstract DisolverDecision NextDecision();
	}
}
