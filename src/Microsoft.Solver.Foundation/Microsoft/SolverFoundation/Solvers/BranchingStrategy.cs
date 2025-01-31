namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Lists the possible branching strategies for the MIP solver.
	/// </summary>
	public enum BranchingStrategy
	{
		/// <summary>
		/// Let the solver choose the strategy.
		/// </summary>
		Automatic,
		/// <summary>
		/// Selects the variable with the smallest pseudo-cost.
		/// </summary>
		SmallestPseudoCost,
		/// <summary>
		/// Selects the variable with the largest pseudo-cost.
		/// </summary>
		LargestPseudoCost,
		/// <summary>
		/// Selects the least fractional variable.
		/// </summary>
		LeastFractional,
		/// <summary>
		/// Selects the most fractional variable.
		/// </summary>
		MostFractional,
		/// <summary>
		/// Selects a variable with large pseudo-cost and expected large influence on other variables.
		/// </summary>
		VectorLength,
		/// <summary>
		/// Selects a variable with largest increase in objective value.
		/// </summary>
		StrongCost
	}
}
