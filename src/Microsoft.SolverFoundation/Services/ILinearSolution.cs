using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// An ILinearSolution instance encapsulates the result of attempting to solve an ILinearModel. It contains the following information:
	///     . The solver’s final status, retrievable from the SolutionQuality, Result, LpResult, and MipResult properties.
	///     . The variable values, indicating the solver’s best attempt at an optimal feasible solution. The GetValue method provides this information.
	///     . Information on which variables are basic and which bounds are binding, non-binding or violated. The GetBasic and GetValueState methods provide this information.
	///     . Detailed information on which goals were considered and solved to optimality.
	/// </summary>
	public interface ILinearSolution : ISolverSolution
	{
		/// <summary>
		/// indicates the quality level of the solution.
		/// </summary>
		LinearSolutionQuality SolutionQuality { get; }

		/// <summary>
		/// indicates the result of solving the LP relaxation, which is essentially the model with its integrality conditions ignored
		/// </summary>
		LinearResult LpResult { get; }

		/// <summary>
		/// indicates the result of considering the integrality conditions
		/// </summary>
		LinearResult MipResult { get; }

		/// <summary>
		/// indicates the result of the solve attempt
		/// </summary>
		LinearResult Result { get; }

		/// <summary> the best result from the MIP solver 
		/// </summary>
		Rational MipBestBound { get; }

		/// <summary> number of goals being solved
		/// </summary>
		int SolvedGoalCount { get; }

		/// <summary>
		/// Return the value for the variable 
		/// </summary>
		/// <param name="vid">a variable id</param>
		/// <returns>the variable value</returns>
		Rational GetValue(int vid);

		/// <summary>
		/// Return the variable state
		/// </summary>
		/// <param name="vid">a variable id</param>
		/// <returns>the variable state</returns> 
		LinearValueState GetValueState(int vid);

		/// <summary>
		/// Check whehter a variable is a basic variable
		/// </summary>
		/// <param name="vid"></param>
		/// <returns></returns>
		bool GetBasic(int vid);

		/// <summary>
		/// get the objective value of a goal 
		/// </summary>
		/// <param name="goalIndex">goal id</param>
		Rational GetSolutionValue(int goalIndex);

		/// <summary> Get the information of a solved goal
		/// </summary>
		/// <param name="igoal"> 0 &lt;= goal index &lt; SolvedGoalCount </param>
		/// <param name="key">the goal row key</param>
		/// <param name="vid">the goal row vid</param>
		/// <param name="fMinimize">whether the goal is minimization</param>
		/// <param name="fOptimal">whether the goal is optimal</param>
		void GetSolvedGoal(int igoal, out object key, out int vid, out bool fMinimize, out bool fOptimal);
	}
}
