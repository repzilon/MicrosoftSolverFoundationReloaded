namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Lists the possible search strategies for the MIP solver.
	/// </summary>
	public enum SearchStrategy
	{
		/// <summary>
		/// Selects the node with the smallest bound. 
		/// </summary>
		BestBound,
		/// <summary>
		/// Selects the node with the best objective value estimate.
		/// </summary>
		BestEstimate,
		/// <summary>
		/// Selects the node in depth first manner.
		/// </summary>
		DepthFirst
	}
}
