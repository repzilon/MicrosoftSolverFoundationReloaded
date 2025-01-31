namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Heuristic to use for value selection in CSP
	/// </summary>
	public enum TreeSearchValueSelection
	{
		/// <summary>
		/// Use whatever heuristic the solver thinks is best
		/// </summary>
		Default,
		/// <summary>
		/// Value enumeration based on a prediction of the success
		/// </summary>
		SuccessPrediction,
		/// <summary>
		/// Value enumeration that follows the order of the values
		/// </summary>
		ForwardOrder,
		/// <summary>
		/// Value enumeration that picks uniformly at random
		/// </summary>
		RandomOrder
	}
}
