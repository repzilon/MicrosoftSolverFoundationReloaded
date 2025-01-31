namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Possible reasons for InvalidSolverPropertyException.
	/// </summary>
	public enum InvalidSolverPropertyReason
	{
		/// <summary>
		/// This solver don't support event handling at all
		/// </summary>
		SolverDoesNotSupportEvents,
		/// <summary>
		/// While solving, this solver does not support setting operation
		/// </summary>
		EventDoesNotSupportSetProperty,
		/// <summary>
		/// The specific property is not supported by this solver
		/// </summary>
		InvalidPropertyName,
		/// <summary>
		/// This property cannot be used with the current event
		/// </summary>
		EventDoesNotSupportProperty
	}
}
