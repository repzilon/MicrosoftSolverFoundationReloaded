namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Select the interior point method algorithm.
	/// </summary>
	public enum InteriorPointAlgorithmKind
	{
		/// <summary> Mehrohtra style central path predictor-corrector
		/// </summary>
		PredictorCorrector,
		/// <summary> Homogeneous Self Dual form has advantages of feasibility certification
		/// </summary>
		HSD,
		/// <summary> SOCP solver (also HSD).
		/// </summary>
		SOCP,
		/// <summary> Undocumented test
		/// </summary>
		MKL
	}
}
