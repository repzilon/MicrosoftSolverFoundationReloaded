using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> This is a report for sensitivity analysis  
	/// </summary>
	public interface ILinearSolverSensitivityReport : ILinearSolverReport
	{
		/// <summary> Return the dual value for a row constraint.
		/// </summary>
		/// <param name="vidRow">A row vid.</param>
		/// <returns>The dual value.</returns>
		Rational GetDualValue(int vidRow);

		/// <summary> Get the coefficient range on the first goal row   
		/// </summary>
		/// <param name="vid">A variable vid.</param>
		/// <returns>A LinearSolverSensitivityRange object.</returns>
		LinearSolverSensitivityRange GetObjectiveCoefficientRange(int vid);

		/// <summary> Get the coefficient range for a goal row.
		/// </summary>
		/// <param name="vid">A variable vid.</param>
		/// <param name="pri">The goal index.</param>
		/// <returns>A LinearSolverSensitivityRange object.</returns>
		LinearSolverSensitivityRange GetObjectiveCoefficientRange(int vid, int pri);

		/// <summary> Get the variable range.  
		/// </summary>
		/// <param name="vid">A variable vid.</param>
		/// <returns>A LinearSolverSensitivityRange object.</returns>
		LinearSolverSensitivityRange GetVariableRange(int vid);
	}
}
