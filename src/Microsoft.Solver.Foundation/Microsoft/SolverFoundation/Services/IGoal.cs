namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Represent a goal variable. For solver that support multiple goals, each goal can have a priority.
	/// </summary>
	public interface IGoal
	{
		/// <summary> The goal variable key. 
		/// </summary>
		object Key { get; }

		/// <summary> The variable index (vid) of this goal's row
		/// </summary>
		int Index { get; }

		/// <summary> The goal priority. The lower the value, the higher the priority.
		/// </summary>
		int Priority { get; set; }

		/// <summary> Whether the goal is to minimize the objective row. 
		/// </summary>
		bool Minimize { get; set; }

		/// <summary> Whether the goal is enabled.
		/// </summary>
		bool Enabled { get; set; }
	}
}
