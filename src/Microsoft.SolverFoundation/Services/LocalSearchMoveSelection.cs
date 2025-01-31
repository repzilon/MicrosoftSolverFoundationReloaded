namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Heuristic to use for move selection in CSP
	/// </summary>
	public enum LocalSearchMoveSelection
	{
		/// <summary>
		/// Use whatever heuristic the solver thinks is best
		/// </summary>
		Default,
		/// <summary>
		/// Violation-guided greedy move
		/// </summary>
		Greedy,
		/// <summary>
		/// Simulated annealing
		/// </summary>
		SimulatedAnnealing,
		/// <summary>
		/// Violation-guided greedy with noise
		/// </summary>
		GreedyNoise,
		/// <summary>
		/// Violation-guided greedy with noise and tabu
		/// </summary>
		Tabu,
		/// <summary>
		/// Gradient-guided with tabu and escape strategy
		/// </summary>
		Gradients
	}
}
