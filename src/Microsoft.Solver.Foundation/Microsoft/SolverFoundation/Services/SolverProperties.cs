namespace Microsoft.SolverFoundation.Services
{
	/// <summary>The names of properties that can be retrieved by events
	/// </summary> 
	public static class SolverProperties
	{
		/// <summary>The name of the solver class.
		/// </summary>
		public static readonly string SolverName = "SolverName";

		/// <summary>Lower bound on the variable as a Double.
		/// </summary>
		public static readonly string VariableLowerBound = "VariableLowerBound";

		/// <summary>Lower bound on the variable as a Double.
		/// </summary>
		public static readonly string VariableUpperBound = "VariableUpperBound";

		/// <summary>The reason why the solver raised the Solve event as a String.
		/// </summary>
		public static readonly string SolveState = "SolveState";

		/// <summary>The current best value for the first goal in the model as a Double.
		/// </summary>
		/// <remarks>For goaless model returns NaN.</remarks>
		public static readonly string GoalValue = "GoalValue";

		/// <summary>Iteration count as an Int32.
		/// </summary>
		public static readonly string IterationCount = "IterationCount";

		/// <summary>The initial value for a variable as a Double.
		/// </summary>
		public static readonly string VariableStartValue = "VariableStartValue";
	}
}
