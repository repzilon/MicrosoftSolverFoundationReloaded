namespace Microsoft.SolverFoundation.Common
{
	/// <summary> An abstraction for a row of the matrix
	/// </summary>
	internal struct SparseMatrixRow<Number>
	{
		internal SparseMatrix<Number> M;

		internal int Row;

		internal SparseMatrixRow(SparseMatrix<Number> m, int row)
		{
			M = m;
			Row = row;
		}

		/// <summary> x = M[row,j]·v[j]
		/// </summary>
		public Number Product(Number[] v)
		{
			return M.Product(this, v);
		}

		/// <summary> x = M[row,j]·v[j]
		/// </summary>
		public static Number operator *(SparseMatrixRow<Number> row, Number[] v)
		{
			return row.Product(v);
		}
	}
}
