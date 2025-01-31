namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> The current IPM solution state.
	/// </summary>
	internal enum InteriorPointSolveState
	{
		PreInit,
		Init,
		SymbolicFactorization,
		IterationStarted
	}
}
