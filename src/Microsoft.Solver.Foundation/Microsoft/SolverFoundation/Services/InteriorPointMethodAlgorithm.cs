namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Algorithm types for IPM.
	/// </summary>
	public enum InteriorPointMethodAlgorithm
	{
		/// <summary> Use whatever algorithm the solver thinks is best
		/// </summary>
		Default,
		/// <summary> Use Predictor Corrector
		/// </summary>
		PredictorCorrector,
		/// <summary> Use Homogeneous Self Dual
		/// </summary>
		HomogeneousSelfDual,
		/// <summary> Use Predictor multi-corrector
		/// </summary>
		PredictorMultiCorrector
	}
}
