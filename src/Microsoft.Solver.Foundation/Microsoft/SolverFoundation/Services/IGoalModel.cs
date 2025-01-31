using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// IGoalModel represents an optimization model with goals.
	/// </summary>
	/// <remarks>
	/// This interface is inherited by model interfaces such as ILinearModel and INonlinearModel.
	/// Goals are identified using integer indexes (vids). Multiple goals can be specified
	/// using multiple calls to AddGoal.
	/// </remarks>
	public interface IGoalModel
	{
		/// <summary>
		/// Return the goal collection of this model. 
		/// </summary>
		IEnumerable<IGoal> Goals { get; }

		/// <summary>
		/// The number of goals in this model.
		/// </summary>
		int GoalCount { get; }

		/// <summary>Mark a row as a goal.
		/// </summary>
		/// <param name="vid">A row id.</param>
		/// <param name="pri">The priority of the goal (smaller values are prioritized first).</param>
		/// <param name="minimize">Whether to minimize the goal row.</param>
		/// <returns>An IGoal object representing the goal.</returns>
		IGoal AddGoal(int vid, int pri, bool minimize);

		/// <summary>
		/// Check if a row id is a goal row.
		/// </summary>
		/// <param name="vid">A row id.</param>
		/// <returns>True if this a goal row, otherwise false.</returns>
		bool IsGoal(int vid);

		/// <summary>
		/// Check if a row id is a goal and retreive the associated IGoal. 
		/// </summary>
		/// <param name="vid">A row id.</param>
		/// <param name="goal">The IGoal corresponding to the vid.</param>
		/// <returns>True if this a goal row, otherwise false.</returns>
		bool IsGoal(int vid, out IGoal goal);

		/// <summary>
		/// Remove a goal row.
		/// </summary>
		/// <param name="vid">A row id.</param>
		/// <returns>True if the goal was removed, otherwise false.</returns>
		bool RemoveGoal(int vid);

		/// <summary>
		/// Clear all the goals .
		/// </summary>
		void ClearGoals();

		/// <summary>
		/// Return a goal entry if the row id is a goal
		/// </summary>
		/// <param name="vid">A variable id.</param>
		/// <returns>A goal entry. Null if the vid does not correspond to a goal.</returns>
		IGoal GetGoalFromIndex(int vid);
	}
}
