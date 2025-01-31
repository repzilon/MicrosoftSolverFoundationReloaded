using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> A column-compressed sparse matrix has the values in contiguous order.
	///           The elements are NOT guaranteed non-zero: the sparse pattern may be
	///           planned to accomodate possibly non-zero elements at some stage.
	/// </summary>
	internal abstract class SparseMatrixByColumn<Number> : SparseMatrix<Number>
	{
		/// <summary> A ColIter iterates the specified column for rows which have non-zeros,
		///           and also the values in those locations.
		/// </summary>
		public struct ColIter
		{
			private int _slotCur;

			private int _slotNext;

			/// <summary> Is the iterator positioned at a valid slot?
			/// </summary>
			public bool IsValid => _slotCur < _slotNext;

			/// <summary> Report the slot currently under the iterator.
			/// </summary>
			public int Slot => _slotCur;

			/// <summary> Creates a new iterator for the specified column.
			/// </summary>
			/// <param name="matrix">The matrix whose column is iterated.</param>
			/// <param name="column">The column to iterate.</param>
			public ColIter(SparseMatrixByColumn<Number> matrix, int column)
			{
				_slotCur = matrix._columnStarts[column];
				_slotNext = matrix._columnStarts[column + 1];
			}

			/// <summary> Creates a new iterator for the specified column starting at a given row.
			/// </summary>
			/// <param name="matrix">The matrix whose column is iterated.</param>
			/// <param name="column">The column to iterate.</param>
			/// <param name="fromRow"> The row at which we start. </param>
			public ColIter(SparseMatrixByColumn<Number> matrix, int column, int fromRow)
			{
				int num = matrix._columnStarts[column];
				int num2 = (_slotNext = matrix._columnStarts[column + 1]);
				while (true)
				{
					_slotCur = num + num2 >> 1;
					if (num == _slotCur || fromRow == matrix._rowIndexes[_slotCur])
					{
						break;
					}
					if (fromRow < matrix._rowIndexes[_slotCur])
					{
						num2 = _slotCur;
					}
					else
					{
						num = _slotCur + 1;
					}
				}
			}

			/// <summary> Move the iterator to the next slot.
			/// </summary>
			public void Advance()
			{
				_slotCur++;
			}

			/// <summary> Return the row index for the current slot in this column.
			/// </summary>
			/// <param name="matrix"> The matrix containing the column </param>
			/// <returns> The row index </returns>
			public int Row(SparseMatrixByColumn<Number> matrix)
			{
				return matrix._rowIndexes[_slotCur];
			}

			/// <summary> Return the numeric value for the current row in this column.
			/// </summary>
			/// <param name="matrix"> The matrix containing the column </param>
			/// <returns> The numeric value in the corrent row </returns>
			public Number Value(SparseMatrixByColumn<Number> matrix)
			{
				return matrix._values[_slotCur];
			}
		}

		/// <summary> A RowIter iterates the transposed index to give columns
		///           which have non-zeros in the specified row.  It does NOT
		///           iterate values.
		/// </summary>
		internal struct RowIter
		{
			private int _slotCur;

			private int _slotNext;

			/// <summary> Is the iterator positioned at a valid slot?
			/// </summary>
			public bool IsValid => _slotCur < _slotNext;

			/// <summary> Creates a new iterator for the specified row of a transpose index.
			/// </summary>
			/// <param name="matrixT">The matrix transpose index in which the row is iterated.</param>
			/// <param name="row">The row to iterate.</param>
			public RowIter(SparseTransposeIndexes<Number> matrixT, int row)
			{
				_slotCur = matrixT._rowStarts[row];
				_slotNext = matrixT._rowStarts[row + 1];
			}

			/// <summary> Move the iterator to the next slot.
			/// </summary>
			public void Advance()
			{
				_slotCur++;
			}

			/// <summary> Return the column index for the current slot in this row.
			/// </summary>
			/// <param name="matrixT"> The matrix transpose containing the row </param>
			/// <returns> The column index </returns>
			public int Column(SparseTransposeIndexes<Number> matrixT)
			{
				return matrixT._colIndexes[_slotCur];
			}
		}

		/// <summary> A RowSlots structure is used in conjunction with transposed indexes
		///           and RowIter to sweep once through the matrix values in row order.
		/// </summary>
		internal struct RowSlots
		{
			private int[] _slotCur;

			/// <summary> Creates a new vector of slot positions ready to comb down the matrix.
			///           It works only for a complete sweep starting from the first row, and
			///           should be discarded when finished.
			/// </summary>
			/// <param name="matrix"> The matrix in which rows will be iterated </param>
			public RowSlots(SparseMatrixByColumn<Number> matrix)
			{
				_slotCur = matrix._columnStarts.Clone() as int[];
			}

			/// <summary> Is the iterator positioned at a valid slot?
			/// </summary>
			public bool IsValid(SparseMatrixByColumn<Number> matrix, int column)
			{
				return _slotCur[column] < matrix._columnStarts[column + 1];
			}

			/// <summary> Move the iterator to the next slot.
			/// </summary>
			public Number ValueAdvance(SparseMatrixByColumn<Number> matrix, int row, int column)
			{
				return matrix._values[_slotCur[column]++];
			}
		}

		/// <summary> A context object used to control symbolic factorization threads
		/// </summary>
		internal class ProductParallelizer : Parallelizer
		{
			internal int[][] starts;

			internal List<int>[] indexes;

			internal List<Number>[] values;

			internal SparseMatrixByColumn<Number> M;

			internal ProductParallelizer(SparseMatrixByColumn<Number> m)
			{
				starts = new int[base.ThreadCount][];
				indexes = new List<int>[base.ThreadCount];
				values = new List<Number>[base.ThreadCount];
				M = m;
			}
		}

		/// <summary> A context object used to control symbolic factorization threads
		/// </summary>
		internal class ProductDiagonalParallelizer : ProductParallelizer
		{
			internal double[] diagonal;

			internal ProductDiagonalParallelizer(SparseMatrixByColumn<Number> m, double[] D)
				: base(m)
			{
				diagonal = D;
			}
		}

		/// <summary> The _n+1 position starts for values and indexes of a column.
		///           The _n+1th start is the final count.
		/// </summary>
		internal int[] _columnStarts;

		/// <summary> An index matched to every value identifies the row.
		/// </summary>
		internal int[] _rowIndexes;

		/// <summary> The values for each column are in a packed sequence located
		///           by the column start.
		/// </summary>
		internal Number[] _values;

		/// <summary> The transpose view of this
		/// </summary>
		internal SparseTransposeIndexes<Number> _T;

		/// <summary> The count of possibly non-zero elements.
		/// </summary>
		public override long Count => _columnStarts[base.ColumnCount];

		/// <summary> Instantiate an empty ColumnCompressedSparseMatrix
		/// </summary>
		internal SparseMatrixByColumn(int rowCount, int columnCount, int count, double ratio)
		{
			if (rowCount < 1)
			{
				throw new ArgumentOutOfRangeException("rowCount");
			}
			if (columnCount < 1)
			{
				throw new ArgumentOutOfRangeException("columnCount");
			}
			base.ColumnCount = columnCount;
			base.RowCount = rowCount;
			long val = (long)((double)count * ratio);
			val = Math.Min((long)rowCount * (long)columnCount, val);
			if (10000000000.0 < (double)val)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ActualSizeOfMatrixTooBig0, new object[1] { 10000000000.0 }));
			}
			_columnStarts = new int[base.ColumnCount + 1];
			_rowIndexes = new int[(int)val + 1];
			_values = new Number[(int)val + 1];
		}

		/// <summary> Instantiate from a set of triples
		/// </summary>
		internal SparseMatrixByColumn(TripleList<Number> tripleList, int rowCount, int columnCount, int[] rowCounts, TripleList<Number>.DuplicatePolicy duplicate)
			: this(rowCount, columnCount, tripleList.SortUnique(duplicate), 1.0)
		{
			int num = -1;
			int num2 = 0;
			foreach (Triple<Number> triple in tripleList)
			{
				if (num != triple.Column)
				{
					if (columnCount <= triple.Column)
					{
						throw new IndexOutOfRangeException();
					}
					while (num < triple.Column)
					{
						_columnStarts[++num] = num2;
					}
				}
				if (rowCount <= triple.Row)
				{
					throw new IndexOutOfRangeException();
				}
				_values[num2] = triple.Value;
				_rowIndexes[num2++] = triple.Row;
				if (rowCounts != null)
				{
					rowCounts[triple.Row]++;
				}
			}
			while (num < base.ColumnCount)
			{
				_columnStarts[++num] = num2;
			}
		}

		/// <summary> Instantiate from a set of triples
		/// </summary>
		public SparseMatrixByColumn(TripleList<Number> tripleList, int rowCount, int columnCount, TripleList<Number>.DuplicatePolicy duplicate)
			: this(tripleList, rowCount, columnCount, (int[])null, duplicate)
		{
		}

		/// <summary> Count the NZ slots in the column
		/// </summary>
		/// <param name="col"> which column to count </param>
		/// <returns> number of slots for NZs </returns>
		public int CountColumnSlots(int col)
		{
			return _columnStarts[col + 1] - _columnStarts[col];
		}

		/// <summary> Instantiate a pre-planned ColumnCompressedSparseMatrix with possibly null values
		/// </summary>
		/// <param name="nRow"> the number of rows </param>
		/// <param name="nCol"> the number of columns </param>
		/// <param name="starts"> the starting positions of columns </param>
		/// <param name="indexes"> the row indexes </param>
		/// <param name="values"> optional: the values </param>
		internal SparseMatrixByColumn(int nRow, int nCol, int[] starts, int[] indexes, Number[] values)
		{
			if (nRow < 1)
			{
				throw new ArgumentOutOfRangeException("nRow", string.Format(CultureInfo.InvariantCulture, Resources.XLessThanY01, new object[2] { "nRow", 1 }));
			}
			if (nCol < 1)
			{
				throw new ArgumentOutOfRangeException("nCol", string.Format(CultureInfo.InvariantCulture, Resources.XLessThanY01, new object[2] { "nCol", 1 }));
			}
			base.RowCount = nRow;
			base.ColumnCount = nCol;
			_columnStarts = starts;
			_rowIndexes = indexes;
			_values = values;
		}

		/// <summary> Instantiate a blank ColumnCompressedSparseMatrix which will be
		///           planned in detail.
		/// </summary>
		/// <param name="nRow"> the number of rows </param>
		/// <param name="nCol"> the number of columns </param>
		internal SparseMatrixByColumn(int nRow, int nCol)
		{
			if (nRow < 1)
			{
				throw new ArgumentOutOfRangeException("nRow", string.Format(CultureInfo.InvariantCulture, Resources.XLessThanY01, new object[2] { "nRow", 1 }));
			}
			if (nCol < 1)
			{
				throw new ArgumentOutOfRangeException("nCol", string.Format(CultureInfo.InvariantCulture, Resources.XLessThanY01, new object[2] { "nCol", 1 }));
			}
			base.RowCount = nRow;
			base.ColumnCount = nCol;
		}

		/// <summary> Create a RowIter based on the transpose indexes
		/// </summary>
		internal RowIter RowIterator(int row)
		{
			return new RowIter(_T, row);
		}

		internal abstract SparseMatrixByColumn<Number> TransposeEmpty();

		/// <summary> Instantiate the transpose of this matrix.
		/// </summary>
		public SparseMatrixByColumn<Number> Transpose()
		{
			int columnCount = base.ColumnCount;
			int rowCount = base.RowCount;
			SparseMatrixByColumn<Number> sparseMatrixByColumn = TransposeEmpty();
			int[] columnStarts = sparseMatrixByColumn._columnStarts;
			int[] array = RowCounts();
			int num = 0;
			int num2 = 0;
			while (num2 < rowCount)
			{
				num += array[num2];
				columnStarts[++num2] = num;
			}
			int[] rowIndexes = sparseMatrixByColumn._rowIndexes;
			Number[] values = sparseMatrixByColumn._values;
			for (int i = 0; i < columnCount; i++)
			{
				ColIter colIter = new ColIter(this, i);
				while (colIter.IsValid)
				{
					int num3 = colIter.Row(this);
					int num4 = columnStarts[num3]++;
					rowIndexes[num4] = i;
					if (_values != null)
					{
						values[num4] = colIter.Value(this);
					}
					colIter.Advance();
				}
			}
			int num5 = rowCount;
			while (0 <= --num5)
			{
				columnStarts[num5 + 1] = columnStarts[num5];
			}
			columnStarts[0] = 0;
			return sparseMatrixByColumn;
		}

		/// <summary> Tabulate a count of elements for each row.
		/// </summary>
		public int[] RowCounts()
		{
			int[] array = new int[base.RowCount];
			for (int i = 0; i < base.ColumnCount; i++)
			{
				ColIter colIter = new ColIter(this, i);
				while (colIter.IsValid)
				{
					array[colIter.Row(this)]++;
					colIter.Advance();
				}
			}
			return array;
		}

		/// <summary> Represent the matrix as a string
		/// </summary>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < base.ColumnCount; i++)
			{
				stringBuilder.Append('[').Append(i).Append("]: ");
				ColIter colIter = new ColIter(this, i);
				while (colIter.IsValid)
				{
					stringBuilder.Append(colIter.Row(this)).Append(" ").Append(colIter.Value(this))
						.Append(", ");
					colIter.Advance();
				}
				stringBuilder.AppendLine();
			}
			return stringBuilder.ToString();
		}

		/// <summary> Represent the matrix as a string
		/// </summary>
		public string ToStringByRow()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (_T == null)
			{
				_T = new SparseTransposeIndexes<Number>(this, RowCounts());
			}
			for (int i = 0; i < base.RowCount; i++)
			{
				stringBuilder.Append('[').Append(i).Append("]: ");
				RowIter rowIter = new RowIter(_T, i);
				while (rowIter.IsValid)
				{
					int num = rowIter.Column(_T);
					stringBuilder.Append(num).Append(" ").Append(this[i, num])
						.Append(", ");
					rowIter.Advance();
				}
				stringBuilder.Append('\n');
			}
			return stringBuilder.ToString();
		}
	}
}
