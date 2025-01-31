namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///  Class capturing the result of a conflict analysis
	/// </summary>
	internal struct ConflictDiagnostic
	{
		/// <summary>
		///   false if the analysis was interrupted because of memory bounds
		/// </summary>
		public readonly bool Status;

		/// <summary>
		///   Variables involved in the conflict
		/// </summary>
		public readonly Cause Cause;

		/// <summary>
		///   Range of values for the conflict variable
		/// </summary>
		public readonly Interval Interval;

		public ConflictDiagnostic(bool status, Cause group, Interval itv)
		{
			Status = status;
			Cause = group;
			Interval = itv;
		}
	}
}
