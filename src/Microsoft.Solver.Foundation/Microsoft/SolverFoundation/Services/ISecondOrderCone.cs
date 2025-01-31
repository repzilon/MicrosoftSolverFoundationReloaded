using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Second order cone information.
	/// </summary>
	public interface ISecondOrderCone
	{
		/// <summary> The cone key.
		/// </summary>
		object Key { get; }

		/// <summary> The cone index (cid) of this cone.
		/// </summary>
		int Index { get; }

		/// <summary> The second order cone type.
		/// </summary>
		SecondOrderConeType ConeType { get; }

		/// <summary> The primary conic variable.  For a quadratic cone it is x1 where
		/// x1 &gt;= || x2 ||, x1 &gt; 0.
		/// </summary>
		/// <remarks>
		/// -1 means there is no primary conic variable - the cone is empty.
		/// </remarks>
		int PrimaryVid1 { get; set; }

		/// <summary> The secondary conic variable.  For a rotated quadratic cone it is x2 where
		/// x1 x2 &gt;= || x3 ||, x1, s2 &gt; 0.
		/// </summary>
		/// <remarks>
		/// -1 means either the cone is not rotated, or there are fewer than 2 conic vars.
		/// </remarks>
		int PrimaryVid2 { get; set; }

		/// <summary> The VIDs that belong to this cone.
		/// </summary>
		IEnumerable<int> Vids { get; }

		/// <summary> The number of VIDs that belong to this cone.
		/// </summary>
		int VidCount { get; }
	}
}
