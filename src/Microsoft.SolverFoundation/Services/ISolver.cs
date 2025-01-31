namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Interface for defining solvers supported by Solver Foundation
	/// </summary>
	public interface ISolver
	{
		/// <summary> Shutdown the solver instance
		/// </summary>
		/// <remarks>Solver needs to dispose any unmanaged memory used upon this call.</remarks>
		void Shutdown();
	}
}
