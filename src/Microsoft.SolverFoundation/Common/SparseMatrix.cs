using System;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> General base generic class for a sparse matrix of numbers
	/// </summary>
	/// <typeparam name="Number"> a type which behaves as a number, like double or Rational </typeparam>
	internal abstract class SparseMatrix<Number>
	{
		private Parallelizer _parallel;

		/// <summary> The number of columns
		/// </summary>
		public int ColumnCount { get; internal set; }

		/// <summary> The number of rows
		/// </summary>
		public int RowCount { get; internal set; }

		/// <summary> Get or set a position in the matrix.
		///           There may be restrictions on the order of setting contents.
		/// </summary>
		/// <param name="row"> 0 &lt;= row &lt; N </param>
		/// <param name="col"> 0 &lt;= col &lt; N </param>
		/// <returns> the element at [row, col] </returns>
		public abstract Number this[int row, int col] { get; internal set; }

		/// <summary> The (fast) count of potentially non-zero elements.
		///           This is whatever least upper bound is cheap to compute.
		/// </summary>
		public abstract long Count { get; }

		internal Parallelizer Parallel
		{
			get
			{
				return _parallel;
			}
			set
			{
				if (value != null && Parallel != null)
				{
					throw new InvalidOperationException();
				}
				_parallel = value;
			}
		}

		/// <summary> abstract a row of the matrix
		/// </summary>
		public SparseMatrixRow<Number> Row(int row)
		{
			return new SparseMatrixRow<Number>(this, row);
		}

		/// <summary> abstract a column of the matrix
		/// </summary>
		public SparseMatrixColumn<Number> Column(int column)
		{
			return new SparseMatrixColumn<Number>(this, column);
		}

		/// <summary> x = M[row,j]·v[j]
		/// </summary>
		public abstract Number Product(SparseMatrixRow<Number> row, Number[] v);

		/// <summary> x = v[i]·M[i,column]
		/// </summary>
		public abstract Number Product(Number[] v, SparseMatrixColumn<Number> column);

		/// <summary> A[,] = this[:,j]·M[j,:]
		/// </summary>
		public abstract SparseMatrix<Number> Product(SparseMatrix<Number> M);

		/// <summary> y[] = a·THIS[:,j]·x[j] + b·y[]
		/// </summary>
		public abstract void SumProductRight(Number a, Number[] x, Number b, Number[] y);

		/// <summary> y[] = a·M[:,j]·x[j] + b·y[]
		/// </summary>
		public static void SumProductRight(Number a, SparseMatrix<Number> M, Number[] x, Number b, Number[] y)
		{
			M.SumProductRight(a, x, b, y);
		}

		/// <summary> y[] = a·x[i]·THIS[i,:] + b·y[]
		/// </summary>
		public abstract void SumLeftProduct(Number a, Number[] x, Number b, Number[] y);

		/// <summary> y[] = a·x[i]·M[i,:] + b·y[]
		/// </summary>
		public static void SumLeftProduct(Number a, Number[] x, SparseMatrix<Number> M, Number b, Number[] y)
		{
			M.SumLeftProduct(a, x, b, y);
		}

		/// <summary> Throw an exception the matrix is not square.
		/// </summary>
		public void VerifySquare()
		{
			if (RowCount != ColumnCount)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "RowCount", "ColumnCount" }));
			}
		}
	}
}
