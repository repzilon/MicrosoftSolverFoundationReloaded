using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Callbacks called periodically during solving
	/// </summary>
	public interface ISolverEvents
	{
		/// <summary>
		/// Callback called periodically during solving
		/// </summary>
		Action Solving { get; set; }
	}
}
