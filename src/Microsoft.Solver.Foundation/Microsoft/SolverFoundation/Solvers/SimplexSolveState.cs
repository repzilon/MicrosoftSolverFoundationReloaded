namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> The current simplex solver state.
	/// </summary>
	internal enum SimplexSolveState
	{
		/// <summary>
		/// Before solve has been started.
		/// </summary>
		PreInit,
		/// <summary>
		/// Start. Currently the first event happens after this state is changed.
		/// </summary>
		Init,
		/// <summary>
		/// In presolve. Currently the first event happens after this state is changed.
		/// </summary>
		Presolve,
		/// <summary>
		/// During simplex solving.
		/// </summary>
		SimplexSolving,
		/// <summary>
		/// In mip solving.
		/// </summary>
		MipSolving,
		/// <summary>
		/// During mip, new best mip solution found.
		/// </summary>
		MipNewSolution,
		/// <summary>
		/// During mip, branch created.
		/// </summary>
		MipBranchCreated
	}
}
