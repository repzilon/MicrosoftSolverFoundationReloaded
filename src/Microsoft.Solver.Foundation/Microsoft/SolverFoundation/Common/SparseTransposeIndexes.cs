using System;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Indexes for row-by-row structure of a column-compressed matrix
	/// </summary>
	internal class SparseTransposeIndexes<Number>
	{
		/// <summary> Starts of the compressed row indexes for the transpose view of A
		/// </summary>
		public int[] _rowStarts;

		/// <summary> Compressed row indexes for the transpose view of A
		/// </summary>
		public int[] _colIndexes;

		public int _maxRowLength;

		/// <summary> We will use the transpose indexes of the A matrix for faster multiplication.
		/// </summary>
		/// <param name="A"> The matrix on which to build transpose indexes </param>
		/// <param name="rowCounts"> a count of the number of non-zeroes in each row </param>
		public SparseTransposeIndexes(SparseMatrixByColumn<Number> A, int[] rowCounts)
		{
			int rowCount = A.RowCount;
			_rowStarts = new int[rowCount + 1];
			int num = 0;
			int num2 = 0;
			while (num2 < rowCount)
			{
				_maxRowLength = Math.Max(_maxRowLength, rowCounts[num2]);
				num += rowCounts[num2];
				_rowStarts[++num2] = num;
			}
			_colIndexes = new int[A.Count];
			for (int i = 0; i < A.ColumnCount; i++)
			{
				int j = A._columnStarts[i];
				for (int num3 = A._columnStarts[i + 1]; j < num3; j++)
				{
					int num4 = A._rowIndexes[j];
					_colIndexes[_rowStarts[num4]] = i;
					_rowStarts[num4]++;
				}
			}
			int num5 = rowCount;
			while (0 <= --num5)
			{
				_rowStarts[num5 + 1] = _rowStarts[num5];
			}
			_rowStarts[0] = 0;
		}

		public int RowCount(int row)
		{
			return _rowStarts[row + 1] - _rowStarts[row];
		}
	}
}
