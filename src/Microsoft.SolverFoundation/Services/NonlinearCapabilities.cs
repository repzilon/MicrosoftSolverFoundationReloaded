using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Capabilities related to non-linear solvers
	/// </summary>
	[Flags]
	public enum NonlinearCapabilities
	{
		/// <summary>
		/// Does not support any of the specified capabilities
		/// </summary>
		None = 0,
		/// <summary>
		/// Supports specify the linear terms explicitly
		/// </summary>
		/// <remarks>ILinearModel must be implemented for this capability</remarks>
		SupportsExplicitLinearTerms = 1,
		/// <summary>Supports model with constraints
		/// </summary>
		SupportsBoundedRows = 2,
		/// <summary>Supports model variable bounds (boxed model)
		/// </summary>
		SupportsBoundedVariables = 4
	}
}
