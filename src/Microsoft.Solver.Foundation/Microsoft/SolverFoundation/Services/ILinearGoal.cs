namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Represent a linear goal variable. The Simplex solver supports multiple goals, each goal can have a priority
	/// </summary>
	public interface ILinearGoal : IGoal
	{
		/// <summary> the goal variable key 
		/// </summary>
		new object Key { get; }

		/// <summary> The variable index (vid) of this goal's row
		/// </summary>
		new int Index { get; }

		/// <summary> the goal priority. The lower the value, the higher the priority
		/// </summary>
		new int Priority { get; set; }

		/// <summary> whether the goal is to minimize the objective row 
		/// </summary>
		new bool Minimize { get; set; }

		/// <summary> whether the goal is enabled
		/// </summary>
		new bool Enabled { get; set; }
	}
}
