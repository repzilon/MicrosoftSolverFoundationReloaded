namespace Microsoft.SolverFoundation.Services
{
	/// <summary>The pricing strategy to use for simplex.
	/// </summary>
	public enum SimplexPricing
	{
		/// <summary> Use whatever pricing the solver thinks is best
		/// </summary>
		Default,
		/// <summary> Use steepest edge pricing
		/// </summary>
		SteepestEdge,
		/// <summary> Use reduced cost pricing
		/// </summary>
		ReducedCost,
		/// <summary> Use partial pricing
		/// </summary>
		Partial
	}
}
