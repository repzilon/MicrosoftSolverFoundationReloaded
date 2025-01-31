namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Types of basic search strategies that can be used to parametrise
	///   the tree-search solver
	/// </summary>
	/// <remarks>
	///   So far there is little choice but we make it possible to have
	///   later strategies that would not be variable/value ordering heuristics
	/// </remarks>
	internal enum SearchStrategyFlag
	{
		VariableValueHeuristic
	}
}
