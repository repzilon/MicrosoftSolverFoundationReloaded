using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Encapsulates presolve results.
	/// </summary>
	internal struct PresolveResult
	{
		public int ChangeCount { get; set; }

		public bool Terminate { get; set; }

		public LinearResult Status { get; set; }
	}
}
