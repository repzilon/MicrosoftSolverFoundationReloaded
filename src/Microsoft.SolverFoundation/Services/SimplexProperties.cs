namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Properties that can be retrieved by events raised by the Simplex solver.
	/// </summary> 
	public static class SimplexProperties
	{
		/// <summary>
		/// The pivot count property indicates the number of simplex pivots performed as an Int32.
		/// Generally these include both major and minor pivots.
		/// </summary>
		public static readonly string PivotCount = "PivotCount";

		/// <summary>
		/// The factor count property indicates the number of basis matrix LU factorizations performed as an Int32.
		/// </summary>
		public static readonly string FactorCount = "FactorCount";

		/// <summary>
		/// The BranchCount property indicates the number of branches performed when applying the branch and bound algorithm to a MILP. 
		/// The value is returned as an Int32.
		/// </summary>
		/// <remarks>If the model has no integer variables, this will be zero.
		/// </remarks>
		public static readonly string BranchCount = "BranchCount";

		/// <summary>
		/// Used by MIP to indicate the difference between an integer solution to a relaxed solution as a Double.
		/// </summary>
		public static readonly string MipGap = "MipGap";
	}
}
