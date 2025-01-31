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
	internal class SparseMatrixDouble : SparseMatrixByColumn<double>, IFormattable
	{
		/// <summary> 1/2^36 (~ 1/6e10)
		/// </summary>
		internal const double _ratioEpsilon = 1.4551915228366852E-11;

		/// <summary> 1/2^40 (~ 1/1e12)
		/// </summary>
		internal const double _zeroEpsilon = 9.094947017729282E-13;

		/// <summary> Get (do not set) a position in the matrix.
		/// </summary>
		/// <param name="row"> 0 &lt;= row &lt; RowCount </param>
		/// <param name="col"> 0 &lt;= col &lt; ColumnCount </param>
		/// <returns> the element at [row, col] </returns>
		public override double this[int row, int col]
		{
			get
			{
				int num = _columnStarts[col];
				int num2 = _columnStarts[col + 1];
				while (num < num2)
				{
					int num3 = num + num2 >> 1;
					if (row == _rowIndexes[num3])
					{
						return _values[num3];
					}
					if (row < _rowIndexes[num3])
					{
						num2 = num3;
					}
					else
					{
						num = num3 + 1;
					}
				}
				if (row < 0 || base.RowCount <= row)
				{
					throw new IndexOutOfRangeException();
				}
				return 0.0;
			}
			internal set
			{
				throw new InvalidOperationException(Resources.ColumnCompressedSparseMatrixDoesNotSupportElementInsertion);
			}
		}

		/// <summary> Instantiate an empty SparseMatrixDouble
		/// </summary>
		internal SparseMatrixDouble(int rowCount, int columnCount, int count, double ratio)
			: base(rowCount, columnCount, count, ratio)
		{
		}

		/// <summary> Instantiate from a set of triples
		/// </summary>
		internal SparseMatrixDouble(TripleList<double> tripleList, int rowCount, int columnCount, int[] rowCounts, TripleList<double>.DuplicatePolicy duplicate)
			: base(tripleList, rowCount, columnCount, rowCounts, duplicate)
		{
		}

		/// <summary> Instantiate from a set of triples
		/// </summary>
		public SparseMatrixDouble(TripleList<double> tripleList, int rowCount, int columnCount)
			: this(tripleList, rowCount, columnCount, null, null)
		{
		}

		/// <summary> Instantiate from a set of triples
		/// </summary>
		public SparseMatrixDouble(TripleList<double> tripleList, int rowCount, int columnCount, TripleList<double>.DuplicatePolicy duplicate)
			: this(tripleList, rowCount, columnCount, null, duplicate)
		{
		}

		/// <summary> Instantiate a pre-planned SparseMatrixDouble with possibly null values
		/// </summary>
		/// <param name="nRow"> the number of rows </param>
		/// <param name="nCol"> the number of columns </param>
		/// <param name="starts"> the starting positions of columns </param>
		/// <param name="indexes"> the row indexes </param>
		/// <param name="values"> optional: the values </param>
		internal SparseMatrixDouble(int nRow, int nCol, int[] starts, int[] indexes, double[] values)
			: base(nRow, nCol, starts, indexes, values)
		{
		}

		/// <summary> Instantiate a pre-planned SparseMatrixDouble.
		/// </summary>
		/// <param name="nRow"> the number of rows </param>
		/// <param name="nCol"> the number of columns </param>
		internal SparseMatrixDouble(int nRow, int nCol)
			: base(nRow, nCol)
		{
		}

		/// <summary> Instantiate a pre-planned SparseMatrixDouble.
		/// </summary>
		internal SparseMatrixDouble(int nRow, int nCol, int count, bool initValues)
			: base(nRow, nCol)
		{
			_columnStarts = new int[nCol + 1];
			_rowIndexes = new int[count];
			_values = null;
			if (initValues)
			{
				_values = new double[count];
			}
		}

		/// <summary> Scan the diagonal and get the binary exponents limited to 0..255.
		/// </summary>
		/// <returns></returns>
		internal virtual byte[] DiagonalExponents()
		{
			byte[] array = new byte[base.ColumnCount];
			for (int i = 0; i < array.Length; i++)
			{
				int num = 128 + new DoubleRaw(this[i, i]).Exponent;
				array[i] = (byte)((255 < num) ? 255u : ((num >= 0) ? ((uint)num) : 0u));
			}
			return array;
		}

		internal override SparseMatrixByColumn<double> TransposeEmpty()
		{
			return new SparseMatrixDouble(base.ColumnCount, base.RowCount, (int)Count, 1.0);
		}

		/// <summary> Enumerate all the rows, above and below the diagonal.
		/// </summary>
		/// <param name="col"></param>
		/// <returns></returns>
		public virtual IEnumerable<int> AllRowsInColumn(int col)
		{
			ColIter cIter = new ColIter(this, col);
			while (cIter.IsValid)
			{
				yield return cIter.Row(this);
				cIter.Advance();
			}
		}

		/// <summary> Enumerate all non-zero values, above and below the diagonal.
		/// </summary>
		/// <param name="col"></param>
		/// <returns> pairs of row,value for the non-zeros </returns>
		public virtual IEnumerable<KeyValuePair<int, double>> AllValuesInColumn(int col)
		{
			ColIter cIter = new ColIter(this, col);
			while (cIter.IsValid)
			{
				int row = cIter.Row(this);
				double num = cIter.Value(this);
				if (0.0 != num)
				{
					yield return new KeyValuePair<int, double>(row, num);
				}
				cIter.Advance();
			}
		}

		/// <summary> x = M[row,j]·v[j]
		/// </summary>
		public override double Product(SparseMatrixRow<double> row, double[] v)
		{
			if (v == null)
			{
				throw new ArgumentNullException("v");
			}
			if (this != row.M)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.DoesNotBelongToMatrix0, new object[1] { "row" }), "row");
			}
			if (_T == null)
			{
				_T = new SparseTransposeIndexes<double>(this, RowCounts());
			}
			double num = 0.0;
			RowIter rowIter = new RowIter(_T, row.Row);
			while (rowIter.IsValid)
			{
				int num2 = rowIter.Column(_T);
				double num3 = v[num2];
				if (0.0 != num3)
				{
					num += num3 * this[row.Row, num2];
				}
				rowIter.Advance();
			}
			return num;
		}

		/// <summary> x = v[i]·M[i,column]
		/// </summary>
		public override double Product(double[] v, SparseMatrixColumn<double> column)
		{
			if (v == null)
			{
				throw new ArgumentNullException("v");
			}
			if (this != column.M)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.DoesNotBelongToMatrix0, new object[1] { "column" }), "column");
			}
			double num = 0.0;
			ColIter colIter = new ColIter(this, column.Column);
			while (colIter.IsValid)
			{
				num += v[colIter.Row(this)] * colIter.Value(this);
				colIter.Advance();
			}
			return num;
		}

		/// <summary> x = v[i]·M[i,column]
		/// </summary>
		public double Product(Vector v, SparseMatrixColumn<double> column)
		{
			if (v == null)
			{
				throw new ArgumentNullException("v");
			}
			if (this != column.M)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.DoesNotBelongToMatrix0, new object[1] { "column" }), "column");
			}
			double num = 0.0;
			ColIter colIter = new ColIter(this, column.Column);
			while (colIter.IsValid)
			{
				num += v[colIter.Row(this)] * colIter.Value(this);
				colIter.Advance();
			}
			return num;
		}

		/// <summary> x = v[vStart + i]·M[i,column]
		/// </summary>
		public double Product(double[] v, int vStart, SparseMatrixColumn<double> column)
		{
			if (this != column.M)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.DoesNotBelongToMatrix0, new object[1] { "column" }), "column");
			}
			double num = 0.0;
			ColIter colIter = new ColIter(this, column.Column);
			while (colIter.IsValid)
			{
				num += v[colIter.Row(this) + vStart] * colIter.Value(this);
				colIter.Advance();
			}
			return num;
		}

		/// <summary> A[,] = this[:,j]·M[j,:].  Sliced every n'th column, n == threadCount
		/// </summary>
		internal void ProductThread(object state)
		{
			ThreadState threadState = state as ThreadState;
			ProductParallelizer productParallelizer = base.Parallel as ProductParallelizer;
			int threadIndex = threadState.threadIndex;
			SparseMatrixByColumn<double> m = productParallelizer.M;
			double[] array = new double[base.RowCount];
			bool[] array2 = new bool[1 + (base.RowCount >> 5)];
			int[] array3 = new int[m.ColumnCount / productParallelizer.ThreadCount + 3];
			List<int> list = new List<int>(m.ColumnCount);
			List<double> list2 = new List<double>(m.ColumnCount);
			int num = 0;
			array3[num++] = 0;
			for (int i = threadIndex; i < m.ColumnCount; i += productParallelizer.ThreadCount)
			{
				ColIter colIter = new ColIter(m, i);
				while (colIter.IsValid)
				{
					double num2 = colIter.Value(m);
					if (0.0 != num2)
					{
						int column = colIter.Row(m);
						ColIter colIter2 = new ColIter(this, column);
						while (colIter2.IsValid)
						{
							int num3 = colIter2.Row(this);
							array[num3] += num2 * colIter2.Value(this);
							array2[num3 >> 5] = true;
							colIter2.Advance();
						}
					}
					colIter.Advance();
				}
				for (int j = 0; j <= base.RowCount >> 5; j++)
				{
					if (!array2[j])
					{
						continue;
					}
					int k = j << 5;
					for (int num4 = Math.Min(k + 32, base.RowCount); k < num4; k++)
					{
						if (0.0 != array[k])
						{
							list.Add(k);
							list2.Add(array[k]);
							array[k] = 0.0;
						}
					}
					array2[j] = false;
				}
				array3[num++] = list2.Count;
			}
			array3[num] = list2.Count;
			productParallelizer.starts[threadIndex] = array3;
			productParallelizer.indexes[threadIndex] = list;
			productParallelizer.values[threadIndex] = list2;
			productParallelizer.FinishWorkItem(threadIndex);
		}

		/// <summary> A[,] = this[:,j]·M[j,:].
		/// </summary>
		private SparseMatrixDouble CollectProductThreads(ProductParallelizer multiplizer, int resultColumnCount)
		{
			int num = 0;
			int num2 = multiplizer.ThreadCount;
			while (0 <= --num2)
			{
				num += multiplizer.indexes[num2].Count;
			}
			SparseMatrixDouble sparseMatrixDouble = new SparseMatrixDouble(base.RowCount, resultColumnCount, num, 1.0);
			int num3 = 0;
			int num4 = 0;
			num = 0;
			for (int i = 0; i < resultColumnCount; i++)
			{
				sparseMatrixDouble._columnStarts[i] = num;
				int num5 = multiplizer.starts[num3][num4];
				int num6 = multiplizer.starts[num3][num4 + 1] - num5;
				multiplizer.indexes[num3].CopyTo(num5, sparseMatrixDouble._rowIndexes, num, num6);
				multiplizer.values[num3].CopyTo(num5, sparseMatrixDouble._values, num, num6);
				num += num6;
				num3++;
				if (multiplizer.ThreadCount <= num3)
				{
					num3 = 0;
					num4++;
				}
			}
			sparseMatrixDouble._columnStarts[resultColumnCount] = num;
			return sparseMatrixDouble;
		}

		/// <summary> A[,] = this[:,j]·M[j,:].
		/// </summary>
		private SparseMatrixDouble Product(SparseMatrixDouble M)
		{
			if (M == null)
			{
				throw new ArgumentNullException("M");
			}
			if (base.ColumnCount != M.RowCount)
			{
				throw new ArgumentOutOfRangeException("M");
			}
			ProductParallelizer productParallelizer = (ProductParallelizer)(base.Parallel = new ProductParallelizer(M));
			if (productParallelizer.Run(ProductThread, (int)((Count + M.Count) / 5000)))
			{
				SparseMatrixDouble result = CollectProductThreads(productParallelizer, M.ColumnCount);
				base.Parallel = null;
				return result;
			}
			base.Parallel = null;
			throw new InvalidOperationException();
		}

		/// <summary> A[,] = this[:,j]·this[j,:].  Sliced every n'th column, n == threadCount
		/// </summary>
		internal void FusedProductTransposeThread(object state)
		{
			ThreadState threadState = state as ThreadState;
			ProductParallelizer productParallelizer = base.Parallel as ProductParallelizer;
			int threadIndex = threadState.threadIndex;
			double[] array = new double[base.RowCount];
			bool[] array2 = new bool[1 + (base.RowCount >> 5)];
			int[] array3 = new int[base.RowCount / productParallelizer.ThreadCount + 3];
			List<int> list = new List<int>(base.RowCount);
			List<double> list2 = new List<double>(base.RowCount);
			int num = 0;
			array3[num++] = 0;
			for (int i = threadIndex; i < base.RowCount; i += productParallelizer.ThreadCount)
			{
				RowIter rowIter = new RowIter(_T, i);
				while (rowIter.IsValid)
				{
					int num2 = rowIter.Column(_T);
					double num3 = this[i, num2];
					if (0.0 != num3)
					{
						ColIter colIter = new ColIter(this, num2);
						while (colIter.IsValid)
						{
							int num4 = colIter.Row(this);
							array[num4] += num3 * colIter.Value(this);
							array2[num4 >> 5] = true;
							colIter.Advance();
						}
					}
					rowIter.Advance();
				}
				for (int j = 0; j <= base.RowCount >> 5; j++)
				{
					if (!array2[j])
					{
						continue;
					}
					int k = j << 5;
					for (int num5 = Math.Min(k + 32, base.RowCount); k < num5; k++)
					{
						if (0.0 != array[k])
						{
							list.Add(k);
							list2.Add(array[k]);
							array[k] = 0.0;
						}
					}
					array2[j] = false;
				}
				array3[num++] = list2.Count;
			}
			array3[num] = list2.Count;
			productParallelizer.starts[threadIndex] = array3;
			productParallelizer.indexes[threadIndex] = list;
			productParallelizer.values[threadIndex] = list2;
			productParallelizer.FinishWorkItem(threadIndex);
		}

		/// <summary> result[,] = this[:,j]·this[j,:].
		/// </summary>
		public SparseMatrixDouble FusedProductTranspose()
		{
			if (_T == null)
			{
				_T = new SparseTransposeIndexes<double>(this, RowCounts());
			}
			ProductParallelizer productParallelizer = (ProductParallelizer)(base.Parallel = new ProductParallelizer(this));
			if (productParallelizer.Run(FusedProductTransposeThread, (int)(Count / 1000)))
			{
				SparseMatrixDouble result = CollectProductThreads(productParallelizer, base.RowCount);
				base.Parallel = null;
				return result;
			}
			base.Parallel = null;
			throw new InvalidOperationException();
		}

		/// <summary> A[,] = this[:,j]·diagonal[j,j]·this[j,:].  Sliced every n'th column, n == threadCount
		/// </summary>
		internal void FusedProductDiagonalTransposeThread(object state)
		{
			ThreadState threadState = state as ThreadState;
			ProductDiagonalParallelizer productDiagonalParallelizer = base.Parallel as ProductDiagonalParallelizer;
			int threadIndex = threadState.threadIndex;
			double[] array = new double[base.RowCount];
			bool[] array2 = new bool[1 + (base.RowCount >> 5)];
			int[] array3 = new int[base.RowCount / productDiagonalParallelizer.ThreadCount + 3];
			List<int> list = new List<int>(base.RowCount);
			List<double> list2 = new List<double>(base.RowCount);
			int num = 0;
			array3[num++] = 0;
			for (int i = threadIndex; i < base.RowCount; i += productDiagonalParallelizer.ThreadCount)
			{
				RowIter rowIter = new RowIter(_T, i);
				while (rowIter.IsValid)
				{
					int num2 = rowIter.Column(_T);
					double num3 = this[i, num2] * productDiagonalParallelizer.diagonal[num2];
					if (0.0 != num3)
					{
						ColIter colIter = new ColIter(this, num2);
						while (colIter.IsValid)
						{
							int num4 = colIter.Row(this);
							array[num4] += num3 * colIter.Value(this);
							array2[num4 >> 5] = true;
							colIter.Advance();
						}
					}
					rowIter.Advance();
				}
				for (int j = 0; j <= base.RowCount >> 5; j++)
				{
					if (!array2[j])
					{
						continue;
					}
					int k = j << 5;
					for (int num5 = Math.Min(k + 32, base.RowCount); k < num5; k++)
					{
						if (0.0 != array[k])
						{
							list.Add(k);
							list2.Add(array[k]);
							array[k] = 0.0;
						}
					}
					array2[j] = false;
				}
				array3[num++] = list2.Count;
			}
			array3[num] = list2.Count;
			productDiagonalParallelizer.starts[threadIndex] = array3;
			productDiagonalParallelizer.indexes[threadIndex] = list;
			productDiagonalParallelizer.values[threadIndex] = list2;
			productDiagonalParallelizer.FinishWorkItem(threadIndex);
		}

		/// <summary> result[,] = this[:,j]·D[j,j]·this[j,:].
		/// </summary>
		/// <param name="D"> Diagonal matrix, represented by a vector </param>
		public SparseMatrixDouble FusedADAtProduct(double[] D)
		{
			if (_T == null)
			{
				_T = new SparseTransposeIndexes<double>(this, RowCounts());
			}
			ProductDiagonalParallelizer productDiagonalParallelizer = (ProductDiagonalParallelizer)(base.Parallel = new ProductDiagonalParallelizer(this, D));
			if (productDiagonalParallelizer.Run(FusedProductDiagonalTransposeThread, (int)(Count / 1000)))
			{
				SparseMatrixDouble result = CollectProductThreads(productDiagonalParallelizer, base.RowCount);
				base.Parallel = null;
				return result;
			}
			base.Parallel = null;
			throw new InvalidOperationException();
		}

		/// <summary> A[,] = this[:,j]·M[j,:].
		/// </summary>
		public override SparseMatrix<double> Product(SparseMatrix<double> M)
		{
			if (!(M is SparseMatrixDouble m))
			{
				throw new NotImplementedException();
			}
			return Product(m);
		}

		/// <summary> A[,] = M[:,j]·N[j,:]
		/// </summary>
		public static SparseMatrixDouble operator *(SparseMatrixDouble M, SparseMatrixDouble N)
		{
			return M.Product(N);
		}

		/// <summary> y[] = a·THIS[:,j]·x[j] + b·y[]
		/// </summary>
		public override void SumProductRight(double a, double[] x, double b, double[] y)
		{
			if (x.Length != base.ColumnCount)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "x", "ColumnCount" }), "x");
			}
			if (y.Length != base.RowCount)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "y", "RowCount" }), "y");
			}
			y.ScaleBy(b);
			if (0.0 == a)
			{
				return;
			}
			for (int i = 0; i < base.ColumnCount; i++)
			{
				ColIter colIter = new ColIter(this, i);
				while (colIter.IsValid)
				{
					int num = colIter.Row(this);
					double num2 = x[i];
					if (0.0 != num2)
					{
						y[num] += a * num2 * colIter.Value(this);
					}
					colIter.Advance();
				}
			}
		}

		/// <summary> y[] = a·THIS[:,j]·x[j] + b·y[]
		/// </summary>
		public virtual void SumProductRight(double a, Vector x, double b, Vector y)
		{
			if (x.Length != base.ColumnCount)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "x", "ColumnCount" }), "x");
			}
			if (y.Length != base.RowCount)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "y", "RowCount" }), "y");
			}
			y.ScaleBy(b);
			if (0.0 == a)
			{
				return;
			}
			for (int i = 0; i < base.ColumnCount; i++)
			{
				ColIter colIter = new ColIter(this, i);
				while (colIter.IsValid)
				{
					int i2 = colIter.Row(this);
					double num = x[i];
					if (0.0 != num)
					{
						y[i2] += a * num * colIter.Value(this);
					}
					colIter.Advance();
				}
			}
		}

		/// <summary> y[] = M[,:] * x[]
		/// </summary>
		public static double[] operator *(SparseMatrixDouble M, double[] x)
		{
			double[] array = new double[M.RowCount];
			M.SumProductRight(1.0, x, 0.0, array);
			return array;
		}

		/// <summary> y[] = a·x[i]·THIS[i,:] + b·y[]
		/// </summary>
		public override void SumLeftProduct(double a, double[] x, double b, double[] y)
		{
			if (x == null)
			{
				throw new ArgumentNullException("x");
			}
			if (y == null)
			{
				throw new ArgumentNullException("y");
			}
			if (x.Length != base.RowCount)
			{
				throw new ArgumentOutOfRangeException("x", base.ColumnCount, string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "x", "ColumnCount" }));
			}
			if (y.Length != base.ColumnCount)
			{
				throw new ArgumentOutOfRangeException("y", base.RowCount, string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "y", "RowCount" }));
			}
			if (0.0 == a)
			{
				y.ScaleBy(b);
				return;
			}
			for (int i = 0; i < base.ColumnCount; i++)
			{
				y[i] = a * Column(i).Product(x) + b * y[i];
			}
		}

		/// <summary> z[] = a·x[i]·THIS[i,:] + b·y[]
		/// </summary>
		public void SumLeftProduct(double a, Vector x, double b, Vector y)
		{
			if (x == null)
			{
				throw new ArgumentNullException("x");
			}
			if (y == null)
			{
				throw new ArgumentNullException("y");
			}
			if (x.Length != base.RowCount)
			{
				throw new ArgumentOutOfRangeException("x", base.RowCount, string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "x", "RowCount" }));
			}
			if (y.Length != base.ColumnCount)
			{
				throw new ArgumentOutOfRangeException("y", base.ColumnCount, string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "y", "ColumnCount" }));
			}
			if (0.0 == a)
			{
				y.ScaleBy(b);
				return;
			}
			for (int i = 0; i < base.ColumnCount; i++)
			{
				y[i] = a * Product(x, Column(i)) + b * y[i];
			}
		}

		/// <summary> Check if the matrix is symmetric
		/// </summary>
		public virtual bool IsSymmetric()
		{
			if (base.ColumnCount != base.RowCount)
			{
				return false;
			}
			for (int i = 0; i < base.ColumnCount - 1; i++)
			{
				ColIter colIter = new ColIter(this, i);
				while (colIter.IsValid)
				{
					double num = colIter.Value(this);
					double num2 = this[i, colIter.Row(this)];
					if (num != num2)
					{
						double num3 = Math.Abs(num2 - num);
						if (9.094947017729282E-13 < num3 && (Math.Abs(num2) + Math.Abs(num)) * 1.4551915228366852E-11 < num3)
						{
							return false;
						}
					}
					colIter.Advance();
				}
			}
			return true;
		}

		/// <summary> Report the maximum absolute value of element differences
		/// </summary>
		public virtual double MaximumDifference(SparseMatrixByColumn<double> M)
		{
			if (base.ColumnCount != M.ColumnCount || base.RowCount != M.RowCount)
			{
				return double.NaN;
			}
			double num = 0.0;
			for (int i = 0; i < base.ColumnCount; i++)
			{
				ColIter colIter = new ColIter(this, i);
				ColIter colIter2 = new ColIter(M, i);
				while (colIter.IsValid || colIter2.IsValid)
				{
					if (!colIter.IsValid)
					{
						num = Math.Max(num, Math.Abs(colIter2.Value(M)));
						colIter2.Advance();
						continue;
					}
					if (!colIter2.IsValid)
					{
						num = Math.Max(num, Math.Abs(colIter.Value(this)));
						colIter.Advance();
						continue;
					}
					int num2 = colIter.Row(this);
					int num3 = colIter2.Row(M);
					if (num3 < num2)
					{
						num = Math.Max(num, Math.Abs(colIter2.Value(M)));
						colIter2.Advance();
						continue;
					}
					if (num2 < num3)
					{
						num = Math.Max(num, Math.Abs(colIter.Value(this)));
						colIter.Advance();
						continue;
					}
					double num4 = colIter.Value(this);
					double num5 = colIter2.Value(M);
					if (num4 != num5)
					{
						num = Math.Max(num, Math.Abs(num5 - num4));
					}
					colIter.Advance();
					colIter2.Advance();
				}
			}
			return num;
		}

		/// <summary> Check if the matrix is equal to within the specified tolerance
		/// </summary>
		public virtual bool EqualTo(SparseMatrixByColumn<double> M, double tolerance)
		{
			return MaximumDifference(M) <= tolerance;
		}

		/// <summary> Check if the matrix is equal, with the default tolerance 2^-40
		/// </summary>
		public virtual bool EqualTo(SparseMatrixByColumn<double> M)
		{
			return EqualTo(M, 9.094947017729282E-13);
		}

		/// <summary> Check if the matrix is same as an unpermuted version
		/// </summary>
		internal virtual bool EqualTo(SparseMatrixByColumn<double> M, int[] newToOld)
		{
			if (base.ColumnCount != M.ColumnCount || base.RowCount != M.RowCount)
			{
				return false;
			}
			for (int i = 0; i < base.ColumnCount; i++)
			{
				double[] array = new double[CountColumnSlots(i)];
				int[] array2 = new int[CountColumnSlots(i)];
				int num = 0;
				ColIter colIter = new ColIter(this, i);
				while (colIter.IsValid)
				{
					array[num] = colIter.Value(this);
					array2[num++] = newToOld[colIter.Row(this)];
					colIter.Advance();
				}
				Array.Sort(array2, array);
				num = 0;
				ColIter colIter2 = new ColIter(M, newToOld[i]);
				while (colIter2.IsValid || num < array.Length)
				{
					if (array.Length <= num)
					{
						if (9.094947017729282E-13 < Math.Abs(colIter2.Value(M)))
						{
							return false;
						}
						colIter2.Advance();
						continue;
					}
					if (!colIter2.IsValid)
					{
						if (9.094947017729282E-13 < Math.Abs(array[num]))
						{
							return false;
						}
						num++;
						continue;
					}
					int num2 = array2[num];
					int num3 = colIter2.Row(M);
					if (num2 < num3)
					{
						if (9.094947017729282E-13 < Math.Abs(array[num]))
						{
							return false;
						}
						num++;
						continue;
					}
					if (num3 < num2)
					{
						if (9.094947017729282E-13 < Math.Abs(colIter2.Value(M)))
						{
							return false;
						}
						colIter2.Advance();
						continue;
					}
					double num4 = array[num];
					double num5 = colIter2.Value(M);
					if (num4 != num5)
					{
						double num6 = Math.Abs(num5 - num4);
						if (9.094947017729282E-13 < num6 && (Math.Abs(num5) + Math.Abs(num4)) * 1.4551915228366852E-11 < num6)
						{
							return false;
						}
					}
					num++;
					colIter2.Advance();
				}
			}
			return true;
		}

		public string ToString(string format, IFormatProvider provider)
		{
			if (format == "mm")
			{
				return ToStringMatrixMarket();
			}
			return ToString();
		}

		private string ToStringMatrixMarket()
		{
			int num = 0;
			for (int i = 0; i < base.ColumnCount; i++)
			{
				ColIter colIter = new ColIter(this, i);
				while (colIter.IsValid)
				{
					if (colIter.Value(this) != 0.0)
					{
						num++;
					}
					colIter.Advance();
				}
			}
			StringBuilder stringBuilder = new StringBuilder(num * 8 + 40);
			stringBuilder.AppendLine("%%MatrixMarket matrix coordinate real symmetric");
			stringBuilder.Append(base.RowCount + " " + base.ColumnCount + " " + num);
			stringBuilder.AppendLine();
			for (int j = 0; j < base.ColumnCount; j++)
			{
				ColIter colIter2 = new ColIter(this, j);
				while (colIter2.IsValid)
				{
					if (colIter2.Value(this) != 0.0)
					{
						stringBuilder.Append(colIter2.Row(this) + 1).Append(" ").Append(j + 1)
							.Append(" ")
							.Append(colIter2.Value(this))
							.Append(" ")
							.AppendLine();
					}
					colIter2.Advance();
				}
			}
			return stringBuilder.ToString();
		}
	}
}
