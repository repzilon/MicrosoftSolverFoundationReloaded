namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Properties that can be retrieved by events raised by the NelderMeadSolver.
	/// </summary>
	public static class NelderMeadProperties
	{
		/// <summary>Number of calls to the evaluation function as a long.
		/// </summary>
		public static readonly string EvaluationCount = "EvaluationCount";

		/// <summary>Current tolerance calculated as a double.
		/// </summary>
		public static readonly string CurrentTerminationCriterion = "CurrentTerminationCriterion";
	}
}
