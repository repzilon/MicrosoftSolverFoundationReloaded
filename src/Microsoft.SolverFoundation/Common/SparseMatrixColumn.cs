namespace Microsoft.SolverFoundation.Common
{
	/// <summary> An abstraction for a column of the matrix
	/// </summary>
	internal struct SparseMatrixColumn<Number>
	{
		internal SparseMatrix<Number> M;

		internal int Column;

		internal SparseMatrixColumn(SparseMatrix<Number> m, int column)
		{
			M = m;
			Column = column;
		}

		/// <summary> x = v[i]·M[i,column]
		/// </summary>
		public Number Product(Number[] v)
		{
			return M.Product(v, this);
		}

		/// <summary> x = v[i]·M[i,column]
		/// </summary>
		public static Number operator *(Number[] v, SparseMatrixColumn<Number> column)
		{
			return column.Product(v);
		}
	}
}
