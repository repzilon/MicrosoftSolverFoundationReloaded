using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> A compact representation of a symmetric matrix.
	/// </summary>
	internal class SymmetricSparseMatrix : LowerSparseMatrix
	{
		/// <summary> Access the value at [row, col]
		/// </summary>
		public override double this[int row, int col]
		{
			get
			{
				if (row >= col)
				{
					return base[row, col];
				}
				return base[col, row];
			}
			internal set
			{
				throw new IndexOutOfRangeException(Resources.SymmetricSparseMatrixDoesNotSupportElementInsertion);
			}
		}

		/// <summary> Instantiate an empty SymmetricSparseMatrix
		/// </summary>
		internal SymmetricSparseMatrix(int N, int count, double ratio)
			: base(N, count, ratio)
		{
		}

		/// <summary> Instantiate an empty SymmetricSparseMatrix
		/// </summary>
		internal SymmetricSparseMatrix(int N, int[] starts, int[] indexes, double[] values)
			: base(N, starts, indexes, values)
		{
		}

		/// <summary> Instantiate from a set of triples
		/// </summary>
		public SymmetricSparseMatrix(TripleList<double> Ts, int N)
			: base(Ts, N)
		{
		}

		internal override SparseMatrixByColumn<double> TransposeEmpty()
		{
			return new SymmetricSparseMatrix(base.ColumnCount, (int)Count, 1.0);
		}

		/// <summary> Enumerate all the rows, above and below the diagonal.
		/// </summary>
		/// <param name="col"></param>
		/// <returns></returns>
		public override IEnumerable<int> AllRowsInColumn(int col)
		{
			if (_T == null)
			{
				_T = new SparseTransposeIndexes<double>(this, RowCounts());
			}
			RowIter rIter = new RowIter(_T, col);
			while (rIter.IsValid)
			{
				yield return rIter.Column(_T);
				rIter.Advance();
			}
			ColIter cIter = new ColIter(this, col);
			while (cIter.IsValid)
			{
				int row = cIter.Row(this);
				if (col < row)
				{
					yield return row;
				}
				cIter.Advance();
			}
		}

		/// <summary> Enumerate all non-zero values, above and below the diagonal.
		/// </summary>
		/// <param name="col"></param>
		/// <returns> pairs of row,value for the non-zeros </returns>
		public override IEnumerable<KeyValuePair<int, double>> AllValuesInColumn(int col)
		{
			if (_T == null)
			{
				_T = new SparseTransposeIndexes<double>(this, RowCounts());
			}
			RowIter rIter = new RowIter(_T, col);
			while (rIter.IsValid)
			{
				int row = rIter.Column(_T);
				if (row < col)
				{
					double num = this[row, col];
					if (0.0 != num)
					{
						yield return new KeyValuePair<int, double>(row, num);
					}
				}
				rIter.Advance();
			}
			ColIter cIter = new ColIter(this, col);
			while (cIter.IsValid)
			{
				double num2 = cIter.Value(this);
				if (0.0 != num2)
				{
					yield return new KeyValuePair<int, double>(cIter.Row(this), num2);
				}
				cIter.Advance();
			}
		}

		/// <summary> Check if the matrix is symmetric
		/// </summary>
		public override bool IsSymmetric()
		{
			return true;
		}

		/// <summary> Check if the matrix is equal to within the specified tolerance
		/// </summary>
		public override bool EqualTo(SparseMatrixByColumn<double> M, double tolerance)
		{
			if (M is SymmetricSparseMatrix m)
			{
				return base.EqualTo(m, tolerance);
			}
			throw new NotImplementedException();
		}

		/// <summary> Check if the matrix is same as an unpermuted version
		/// </summary>
		internal override bool EqualTo(SparseMatrixByColumn<double> M, int[] newToOld)
		{
			if (M is SymmetricSparseMatrix m)
			{
				return base.EqualTo(m, newToOld);
			}
			throw new NotImplementedException();
		}

		/// <summary> x = v[i]·M[i,column]
		/// </summary>
		public override double Product(double[] v, SparseMatrixColumn<double> column)
		{
			throw new NotImplementedException();
		}

		/// <summary> A[,] = this[:,j]·M[j,:].
		/// </summary>
		public override SparseMatrix<double> Product(SparseMatrix<double> M)
		{
			throw new NotImplementedException();
		}

		/// <summary> x = M[row,j]·v[j]
		/// </summary>
		public override double Product(SparseMatrixRow<double> row, double[] v)
		{
			throw new NotImplementedException();
		}

		/// <summary> y[] = a·x[i]·THIS[i,:] + b·y[]
		/// </summary>
		public override void SumLeftProduct(double a, double[] x, double b, double[] y)
		{
			throw new NotImplementedException();
		}

		/// <summary> y[] = a·THIS[:,j]·x[j] + b·y[]
		/// </summary>
		public override void SumProductRight(double a, double[] x, double b, double[] y)
		{
			if (x == null)
			{
				throw new ArgumentNullException("x");
			}
			if (y == null)
			{
				throw new ArgumentNullException("y");
			}
			if (x.Length != base.ColumnCount)
			{
				throw new ArgumentOutOfRangeException("x");
			}
			if (y.Length != base.RowCount)
			{
				throw new ArgumentOutOfRangeException("y");
			}
			y.ScaleBy(b);
			if (0.0 == a)
			{
				return;
			}
			if (_T == null)
			{
				_T = new SparseTransposeIndexes<double>(this, RowCounts());
			}
			RowSlots rowSlots = new RowSlots(this);
			for (int i = 0; i < base.ColumnCount; i++)
			{
				RowIter rowIter = new RowIter(_T, i);
				while (rowIter.IsValid)
				{
					int num = rowIter.Column(_T);
					double num2 = rowSlots.ValueAdvance(this, i, num);
					if (0.0 != num2)
					{
						y[num] += a * x[i] * num2;
					}
					rowIter.Advance();
				}
				ColIter colIter = new ColIter(this, i);
				while (colIter.IsValid)
				{
					int num3 = colIter.Row(this);
					if (i < num3)
					{
						double num4 = x[i];
						if (0.0 != num4)
						{
							y[num3] += a * num4 * colIter.Value(this);
						}
					}
					colIter.Advance();
				}
			}
		}

		/// <summary> y[] = a·THIS[:,j]·x[j] + b·y[]
		/// </summary>
		public override void SumProductRight(double a, Vector x, double b, Vector y)
		{
			if (x == null)
			{
				throw new ArgumentNullException("x");
			}
			if (y == null)
			{
				throw new ArgumentNullException("y");
			}
			if (x.Length != base.ColumnCount)
			{
				throw new ArgumentOutOfRangeException("x");
			}
			if (y.Length != base.RowCount)
			{
				throw new ArgumentOutOfRangeException("y");
			}
			y.ScaleBy(b);
			if (0.0 == a)
			{
				return;
			}
			if (_T == null)
			{
				_T = new SparseTransposeIndexes<double>(this, RowCounts());
			}
			RowSlots rowSlots = new RowSlots(this);
			for (int i = 0; i < base.ColumnCount; i++)
			{
				RowIter rowIter = new RowIter(_T, i);
				while (rowIter.IsValid)
				{
					int num = rowIter.Column(_T);
					double num2 = rowSlots.ValueAdvance(this, i, num);
					if (0.0 != num2)
					{
						y[num] += a * x[i] * num2;
					}
					rowIter.Advance();
				}
				ColIter colIter = new ColIter(this, i);
				while (colIter.IsValid)
				{
					int num3 = colIter.Row(this);
					if (i < num3)
					{
						double num4 = x[i];
						if (0.0 != num4)
						{
							y[num3] += a * num4 * colIter.Value(this);
						}
					}
					colIter.Advance();
				}
			}
		}
	}
}
