using System;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// ValueFeasibilityKnown event data.
	/// </summary>
	public sealed class ValueFeasibilityKnownEventArgs : EventArgs
	{
		/// <summary>
		/// Return whether the sender object (a value) is feasible or not
		/// </summary>
		public bool IsFeasible { get; set; }

		/// <summary>Construct a ValueFeasibilityKnown event argument.
		/// </summary>
		public ValueFeasibilityKnownEventArgs(bool isFeasible)
		{
			IsFeasible = isFeasible;
		}
	}
}
