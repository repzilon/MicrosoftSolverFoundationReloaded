namespace Microsoft.SolverFoundation.Solvers
{
	internal enum CompactQuasiNewtonErrorType
	{
		/// <summary>Chosen step is in non-descent direction, usually caused by mistake in users delegate.
		/// </summary>
		NonDescentDirection,
		/// <summary>Step length is too short.
		/// </summary>
		InsufficientSteplength,
		/// <summary>y vector and s vector are orthogonal.
		/// </summary>
		YIsOrthogonalToS,
		/// <summary>No delta for gradient. Can be linear function for example.
		/// </summary>
		GradientDeltaIsZero,
		/// <summary>When we exceeded the limits of double. Most likely that the function is unbounded.
		/// </summary>
		NumericLimitExceeded,
		/// <summary>When we exceeded the limit of number of iterations.
		/// </summary>
		MaxIterationExceeded,
		/// <summary>Stopped by the user. 
		/// </summary>
		Interrupted
	}
}
