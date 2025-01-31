using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Interface for solvers of ITermModel instances.
	/// </summary>
	public interface ITermSolver : IRowVariableSolver, ISolver, ITermModel, IRowVariableModel, IGoalModel
	{
		/// <summary>
		/// Gets the operations supported by the solver.
		/// </summary>
		/// <returns>All the TermModelOperations supported by the solver.</returns>
		IEnumerable<TermModelOperation> SupportedOperations { get; }
	}
}
