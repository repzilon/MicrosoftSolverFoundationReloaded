namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// An INonlinearSolution instance encapsulates the result of attempting to solve an 
	/// IRowVariableSolver (INonlinearSolver or ITermSolver). 
	/// </summary>
	/// <remarks>
	/// INonlinearSolution contains the following information:
	///    The solver’s final status, retrievable from Result property,
	///    The variable values, indicating the solver’s best attempt at an optimal feasible solution. The GetValue method provides this information.
	///    Detailed information on which goals were considered and solved to optimality.
	/// </remarks>
	public interface INonlinearSolution : ISolverSolution
	{
		/// <summary>Number of goals being solved.
		/// </summary>
		int SolvedGoalCount { get; }

		/// <summary>
		/// Indicates the type of result (e.g., LocalOptimal). 
		/// </summary>
		NonlinearResult Result { get; }

		/// <summary>
		/// Return the value for the variable (or optionally row) with the specified vid. 
		/// </summary>
		/// <param name="vid">A variable id.</param>
		/// <returns>The value of the variable as a double.</returns>
		/// <remarks>
		/// This method can always be called with variable vids. Some solvers support row vids as well.
		/// The value may be finite, infinite, or indeterminate depending on the solution status.
		/// </remarks>
		double GetValue(int vid);

		/// <summary>
		/// Get the objective value of a goal.
		/// </summary>
		/// <param name="goalIndex">A goal id.</param>
		double GetSolutionValue(int goalIndex);

		/// <summary> Get the information for a solved goal.
		/// </summary>
		/// <param name="goalIndex">The goal index: 0 &lt;= goal index &lt; SolvedGoalCount.</param>
		/// <param name="key">The goal row key.</param>
		/// <param name="vid">The goal row vid.</param>
		/// <param name="minimize">Whether the goal is minimization goal.</param>
		/// <param name="optimal">Whether the goal is optimal.</param>
		void GetSolvedGoal(int goalIndex, out object key, out int vid, out bool minimize, out bool optimal);
	}
}
