namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Symbolic factorization information, including mapping from original to permuted columns.
	/// </summary>
	internal class SymbolicFactorResult
	{
		/// <summary> Permutation from internal colum to user's column
		/// </summary>
		public int[] InnerToOuter;

		/// <summary> Permutation from user's column to internal column
		/// </summary>
		public int[] OuterToInner;

		/// <summary> The first dense column (exploited by Cholesky).
		/// </summary>
		public int FirstDenseColumn;

		/// <summary> The maximum number of nonzeros in any column.
		/// </summary>
		public int MaxColumnCount;
	}
}
