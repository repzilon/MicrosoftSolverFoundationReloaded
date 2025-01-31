using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Verbosity options for Solution.GetReport.
	/// </summary>
	[Flags]
	public enum ReportVerbosity
	{
		/// <summary>
		/// Include information about the algorithm used by the solver.
		/// </summary>
		SolverDetails = 1,
		/// <summary>
		/// Include the values of all decisions.
		/// </summary>
		Decisions = 2,
		/// <summary>
		/// Include sensitivity information if available.
		/// </summary>
		Sensitivity = 4,
		/// <summary>
		/// Include all directives passed to Solve.
		/// </summary>
		Directives = 8,
		/// <summary>
		/// Include infeasibility information (if available).
		/// </summary>
		Infeasibility = 0x10,
		/// <summary>
		/// Include everything.
		/// </summary>
		All = 0xFF
	}
}
