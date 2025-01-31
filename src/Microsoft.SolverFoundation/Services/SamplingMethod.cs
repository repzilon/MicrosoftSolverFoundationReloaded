namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Sampling method: Monte Carlo or Latin Hypercube.
	/// In Automatic mode the sampling method will be selected automatically 
	/// </summary>
	public enum SamplingMethod
	{
		/// <summary>Automatic.
		/// </summary>
		Automatic,
		/// <summary>No sampling.
		/// </summary>
		NoSampling,
		/// <summary>Monte Carlo
		/// </summary>
		MonteCarlo,
		/// <summary>Latin Hypercube
		/// </summary>
		LatinHypercube
	}
}
