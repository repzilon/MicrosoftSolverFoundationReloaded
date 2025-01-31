namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// INonlinearSolver represents a solver for INonlinearModel models.
	/// </summary>
	public interface INonlinearSolver : IRowVariableSolver, ISolver, INonlinearModel, IRowVariableModel, IGoalModel
	{
		/// <summary>
		/// The capabilities for this solver.
		/// </summary>
		NonlinearCapabilities NonlinearCapabilities { get; }

		/// <summary>
		/// Gradient related capabilities.
		/// </summary>
		DerivativeCapability GradientCapability { get; }

		/// <summary>
		/// Hessian related capabilities.
		/// </summary>
		DerivativeCapability HessianCapability { get; }
	}
}
