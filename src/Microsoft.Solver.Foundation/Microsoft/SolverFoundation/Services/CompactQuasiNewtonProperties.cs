namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Properties that can be retrieved by events raised by the Compact Quasi Newton (CQN) solver.
	/// </summary>
	public static class CompactQuasiNewtonProperties
	{
		/// <summary>Number of calls to the function and gradient evaluators as a long.
		/// </summary>
		public static readonly string EvaluationCount = "EvaluationCount";

		/// <summary>Current tolerance calculated as a double.
		/// </summary>
		public static readonly string CurrentTerminationCriterion = "CurrentTerminationCriterion";
	}
}
