namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Indicate the row/variable value status 
	/// </summary>
	public enum LinearValueState
	{
		/// <summary> the value is not finite 
		/// </summary>
		Invalid,
		/// <summary> the value is below the lower bound 
		/// </summary>
		Below,
		/// <summary> the value is at the lower bound 
		/// </summary>
		AtLower,
		/// <summary> the value is in between the lower and upper bound 
		/// </summary>
		Between,
		/// <summary> the value is at the upper bound 
		/// </summary>
		AtUpper,
		/// <summary> the value is over the upper bound
		/// </summary>
		Above
	}
}
