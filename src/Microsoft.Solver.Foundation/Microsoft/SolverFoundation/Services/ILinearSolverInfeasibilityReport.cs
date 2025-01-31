using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Infeasibility report
	/// </summary>
	public interface ILinearSolverInfeasibilityReport : ILinearSolverReport
	{
		/// <summary> return the infeasibility constraint set
		/// </summary>
		IEnumerable<int> IrreducibleInfeasibleSet { get; }
	}
}
