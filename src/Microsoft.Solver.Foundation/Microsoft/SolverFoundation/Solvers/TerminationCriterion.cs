namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>An object which is used to decide whether to stop optimization.
	/// </summary>
	internal abstract class TerminationCriterion
	{
		protected readonly double _tolerance;

		protected double _currentTolerance;

		public virtual double CurrentTolerance => _currentTolerance;

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="tolerance">The tolerance.</param>
		public TerminationCriterion(double tolerance)
		{
			_tolerance = tolerance;
		}

		/// <summary>Determines whether to stop optimization.
		/// </summary>
		/// <returns>Returns true if and only if the criterion is met, i.e. optimization should halt.</returns>
		public abstract bool CriterionMet(CompactQuasiNewtonSolverState state);
	}
}
