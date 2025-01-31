namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Properties that can be retrieved on the local search solver
	/// </summary>
	public static class HybridLocalSearchProperties
	{
		/// <summary>
		/// The Step property indicates the current count of 
		/// steps that the search has performed
		/// </summary>
		public const string Step = "Step";

		/// <summary>
		/// Constraints violation. Zero stands for no violation (feasible solution), 
		/// and the smaller Violation is the more solution tends toward feasibility.
		/// </summary>
		public const string Violation = "Violation";
	}
}
