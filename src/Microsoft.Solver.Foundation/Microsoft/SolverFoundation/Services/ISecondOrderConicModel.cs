using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Encapsulates a second order conic optimization problem.
	/// </summary>
	/// <remarks>
	/// Second order conic (SOCP) models are distinguished from linear models by
	/// the use of conic constraints.  Cones come in two types: quadratic and rotated
	/// quadratic.
	/// </remarks>
	public interface ISecondOrderConicModel : ILinearModel
	{
		/// <summary>Return the cone collection of this model. 
		/// </summary>
		IEnumerable<ISecondOrderCone> Cones { get; }

		/// <summary> Indicates whether the model contains any second order cones.
		/// </summary>
		bool IsSocpModel { get; }

		/// <summary> Add a reference row for a second order cone. Each cone has one reference row.
		/// </summary>
		/// <param name="key">A second order cone key</param>
		/// <param name="coneType">Second order cone type</param>
		/// <param name="vidRow">the vid of the reference row</param>
		/// <returns></returns>
		bool AddRow(object key, SecondOrderConeType coneType, out int vidRow);

		/// <summary> Specifies a primary variable for a cone.  
		/// </summary>
		/// <param name="vidRow">The reference row for the cone.</param>
		/// <param name="vid">The vid of the variable.</param>
		/// <returns></returns>
		/// <remarks>
		/// Quadratic cones have one primary variable.  SetPrimaryConic must be called twice for rotated quadratic cones
		/// because they have two primary variables.
		/// </remarks>
		bool SetPrimaryConic(int vidRow, int vid);

		/// <summary>Gets cone information given a reference row vid.
		/// </summary>
		bool TryGetConeFromIndex(int vidRow, out ISecondOrderCone cone);

		/// <summary>Adds a new conic row.
		/// </summary>
		bool AddRow(object key, int vidCone, SecondOrderConeRowType rowType, out int vidRow);

		/// <summary>Indicates whether a row is a conic row.
		/// </summary>
		bool IsConicRow(int vidRow);

		/// <summary> Return the rows for the specified cone.
		/// </summary>
		/// <param name="vidRow">The cone index.</param>
		IEnumerable<int> GetConicRowIndexes(int vidRow);

		/// <summary> Return the row count for the specified cone.
		/// </summary>
		/// <param name="vidRow">The cone index.</param>
		int GetConicRowCount(int vidRow);
	}
}
