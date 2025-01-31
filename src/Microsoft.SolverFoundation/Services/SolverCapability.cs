namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Supported capabilities of solvers that can be plugged into Foundation
	/// </summary>
	public enum SolverCapability
	{
		/// <summary>
		/// Default that has value 0
		/// </summary>
		Undefined,
		/// <summary>
		/// Linear programming
		/// </summary>
		LP,
		/// <summary>
		/// Quadratic programming
		/// </summary>
		QP,
		/// <summary>
		/// Mixed integer linear programming
		/// </summary>
		MILP,
		/// <summary>
		/// Constraint programming
		/// </summary>
		CP,
		/// <summary>
		/// General nonlinear programming
		/// </summary>
		NLP,
		/// <summary>
		/// General nonlinear programming
		/// </summary>
		MINLP,
		/// <summary>
		/// Mixed integer quadratic programming
		/// </summary>
		MIQP
	}
}
