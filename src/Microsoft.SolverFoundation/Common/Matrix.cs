using System;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> A wrapper for double[][] which implements algebraic operators.
	/// </summary>
	/// <remarks>
	/// Dense matrix operations are not tuned for performance.  These routines should
	/// not be used for large matrices without further turning.
	/// </remarks>
	internal class Matrix : IFormattable
	{
		/// <summary> The contents of the Matrix.
		/// </summary>
		internal readonly double[][] m;

		private readonly int _columnCount;

		/// <summary> The contents of the Matrix.
		/// </summary>
		public double[][] M => m;

		/// <summary> Element accessor for the Matrix.
		/// </summary>
		public double this[int row, int col]
		{
			get
			{
				return m[row][col];
			}
			set
			{
				m[row][col] = value;
			}
		}

		/// <summary> The row count of the Matrix.
		/// </summary>
		public int RowCount => m.GetLength(0);

		/// <summary> The column count of the Matrix.
		/// </summary>
		public int ColumnCount => _columnCount;

		/// <summary> Is this Matrix empty?
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				if (m != null)
				{
					return 0 == m.Length;
				}
				return true;
			}
		}

		/// <summary> Construct an empty Matrix of specified length
		/// </summary>
		public Matrix(int rowCount, int columnCount)
		{
			_columnCount = columnCount;
			m = new double[rowCount][];
			for (int i = 0; i < m.Length; i++)
			{
				m[i] = new double[columnCount];
			}
		}

		/// <summary> Construct an empty Matrix of specified length
		/// </summary>
		internal Matrix(double[][] values)
		{
			_columnCount = ((values.Length != 0) ? values[0].Length : 0);
			m = values;
		}

		/// <summary> Shallow copy
		/// </summary>
		public Matrix Clone()
		{
			return new Matrix(m.Clone() as double[][]);
		}

		/// <summary> Assignment conversion
		/// </summary>
		public static implicit operator Matrix(double[][] values)
		{
			return new Matrix(values);
		}

		/// <summary> Throw an exception if the dimensions of the matrices do not match.
		/// </summary>
		internal void VerifySameShape(Matrix y)
		{
			if (RowCount != y.RowCount || ColumnCount != y.ColumnCount)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "x", "y" }), "y");
			}
		}

		/// <summary> Throw an exception the matrix is not square.
		/// </summary>
		internal void VerifySquare()
		{
			if (RowCount != ColumnCount)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "RowCount", "ColumnCount" }));
			}
		}

		private void VerifyRow(int row, string name)
		{
			if (row < 0 || row >= RowCount)
			{
				throw new ArgumentException("Invalid row index", name);
			}
		}

		private void VerifyColumn(int col, string name)
		{
			if (col < 0 || col >= ColumnCount)
			{
				throw new ArgumentException("Invalid column index", name);
			}
		}

		/// <summary> Throw an exeption if length of Matrix is 0, 
		/// </summary>
		internal void VerifyNonZeroLength()
		{
			if (m.Length == 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.LengthCanNotBeZero0, new object[1] { "x" }), "x");
			}
		}

		/// <summary> x[] = 0
		/// </summary>
		public Matrix ZeroFill()
		{
			ConstantFill(0.0);
			return this;
		}

		/// <summary> x[] = c
		/// </summary>
		public void ConstantFill(double c)
		{
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
				{
					m[i][j] = c;
				}
			}
		}

		/// <summary> x[] = c
		/// </summary>
		public void FillIdentity()
		{
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
				{
					m[i][j] = ((i == j) ? 1 : 0);
				}
			}
		}

		/// <summary> Fill a column of the matrix with a scaled vector.
		/// </summary>
		/// <param name="column">The column to fill.</param>
		/// <param name="rowStart">The row index where copying starts.</param>
		/// <param name="alpha">The scaling factor for v.</param>
		/// <param name="v">The input vector.</param>
		public void FillColumn(int column, int rowStart, double alpha, Vector v)
		{
			VerifyRow(rowStart, "rowStart");
			VerifyColumn(column, "column");
			for (int i = 0; i < v.Length; i++)
			{
				m[rowStart][column] = alpha * v[i];
				rowStart++;
			}
		}

		/// <summary> Scale a column of the matrix.
		/// </summary>
		/// <param name="column">The column to scale.</param>
		/// <param name="alpha">The scaling factor.</param>
		/// <param name="rowStart">The row index to start.</param>
		public void ScaleColumn(int column, double alpha, int rowStart)
		{
			VerifyColumn(column, "column");
			for (int i = rowStart; i < RowCount; i++)
			{
				m[i][column] *= alpha;
			}
		}

		/// <summary> Copy a column to a vector.
		/// </summary>
		/// <param name="v">The vector.</param>
		/// <param name="col">The column to copy from.</param>
		public void CopyColumnTo(Vector v, int col)
		{
			for (int i = 0; i < RowCount; i++)
			{
				v[i] = m[i][col];
			}
		}

		/// <summary> v[] += y[]
		/// </summary>
		public Matrix Plus(Matrix y)
		{
			VerifySameShape(y);
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
				{
					m[i][j] += y.m[i][j];
				}
			}
			return this;
		}

		/// <summary> v[] -= y[]
		/// </summary>
		public Matrix Minus(Matrix y)
		{
			VerifySameShape(y);
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
				{
					m[i][j] -= y.m[i][j];
				}
			}
			return this;
		}

		/// <summary> z[] = x[] + y[] -- (z is preallocated) pairwize (extension method)
		/// </summary>
		public static Matrix operator +(Matrix x, Matrix y)
		{
			x.VerifySameShape(y);
			Matrix matrix = new Matrix(x.RowCount, x.ColumnCount);
			for (int i = 0; i < x.RowCount; i++)
			{
				for (int j = 0; j < x.ColumnCount; j++)
				{
					matrix.m[i][j] = x.m[i][j] + y.m[i][j];
				}
			}
			return matrix;
		}

		/// <summary> z[] = x[] - y[] -- (z is preallocated) pairwize (extension method)
		/// </summary>
		public static Matrix operator -(Matrix x, Matrix y)
		{
			x.VerifySameShape(y);
			Matrix matrix = new Matrix(x.RowCount, x.ColumnCount);
			for (int i = 0; i < x.RowCount; i++)
			{
				for (int j = 0; j < x.ColumnCount; j++)
				{
					matrix.m[i][j] = x.m[i][j] - y.m[i][j];
				}
			}
			return matrix;
		}

		/// <summary> z[] = x[] * y[] -- (z is preallocated) pairwise (extension method)
		/// </summary>
		public static Matrix operator *(Matrix x, Matrix y)
		{
			if (x.ColumnCount != y.RowCount && 0 < x.RowCount && 0 < y.ColumnCount)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "x", "y" }), "y");
			}
			Matrix matrix = new Matrix(x.RowCount, y.ColumnCount);
			for (int i = 0; i < x.RowCount; i++)
			{
				for (int j = 0; j < y.ColumnCount; j++)
				{
					BigSum bigSum = 0.0;
					for (int k = 0; k < x.ColumnCount; k++)
					{
						bigSum += (BigSum)(x.m[i][k] * y.m[k][j]);
					}
					matrix.m[i][j] = bigSum.ToDouble();
				}
			}
			return matrix;
		}

		/// <summary> z[] = x[] * y[] -- (z is preallocated) pairwise (extension method)
		/// </summary>
		public static Vector operator *(Matrix x, Vector y)
		{
			if (x.ColumnCount != y.Length && 0 < x.RowCount && 0 < y.Length)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "x", "y" }), "y");
			}
			Vector vector = new Vector(x.RowCount);
			SumProductRight(1.0, x, y, 0.0, vector);
			return vector;
		}

		/// <summary> Compute alpha x[:,j] y[j] + beta z.
		/// </summary>
		public static Vector SumProductRight(double alpha, Matrix x, Vector y, double beta, Vector z)
		{
			if (x.RowCount != z.Length)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch, "z");
			}
			if (x.ColumnCount != y.Length)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch, "y");
			}
			for (int i = 0; i < x.RowCount; i++)
			{
				BigSum bigSum = 0.0;
				double[] array = x.m[i];
				for (int j = 0; j < y.Length; j++)
				{
					bigSum += (BigSum)(alpha * array[j] * y[j]);
				}
				z[i] = beta * z[i] + bigSum.ToDouble();
			}
			return z;
		}

		/// <summary> Compute alpha x[i] y[i,:] + beta z.
		/// </summary>
		public static Vector SumLeftProduct(double alpha, Matrix x, Vector y, double beta, Vector z)
		{
			if (x.RowCount != y.Length)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch, "y");
			}
			if (x.ColumnCount != z.Length)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch, "z");
			}
			for (int i = 0; i < x.ColumnCount; i++)
			{
				BigSum bigSum = 0.0;
				for (int j = 0; j < y.Length; j++)
				{
					bigSum += (BigSum)(alpha * y[j] * x.m[j][i]);
				}
				z[i] = beta * z[i] + bigSum.ToDouble();
			}
			return z;
		}

		/// <summary> Compute alpha x y + beta z.
		/// </summary>
		public static Matrix SumProductRight(double alpha, Matrix x, Matrix y, double beta, Matrix z)
		{
			if (x.RowCount != z.RowCount)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch, "z");
			}
			if (x.ColumnCount != y.RowCount)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch, "y");
			}
			for (int i = 0; i < y.ColumnCount; i++)
			{
				for (int j = 0; j < x.RowCount; j++)
				{
					BigSum bigSum = 0.0;
					for (int k = 0; k < y.ColumnCount; k++)
					{
						bigSum += (BigSum)(alpha * x.m[j][k] * y.m[k][i]);
					}
					z.m[j][i] = beta * z.m[j][i] + bigSum.ToDouble();
				}
			}
			return z;
		}

		/// <summary> Compute alpha y' x + beta z.
		/// </summary>
		public static Matrix SumLeftProduct(double alpha, Matrix x, Matrix y, double beta, Matrix z)
		{
			if (x.RowCount != y.RowCount)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch, "y");
			}
			if (x.ColumnCount != z.ColumnCount)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch, "z");
			}
			for (int i = 0; i < y.ColumnCount; i++)
			{
				for (int j = 0; j < x.ColumnCount; j++)
				{
					BigSum bigSum = 0.0;
					for (int k = 0; k < y.RowCount; k++)
					{
						bigSum += (BigSum)(alpha * y.m[k][i] * x.m[k][j]);
					}
					z.m[i][j] = beta * z.m[i][j] + bigSum.ToDouble();
				}
			}
			return z;
		}

		/// <summary> v[] += y
		/// </summary>
		public Matrix Plus(double y)
		{
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
				{
					m[i][j] += y;
				}
			}
			return this;
		}

		/// <summary> v[] -= y
		/// </summary>
		public Matrix Minus(double y)
		{
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
				{
					m[i][j] -= y;
				}
			}
			return this;
		}

		/// <summary> v[] *= y
		/// </summary>
		public Matrix Times(double y)
		{
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
				{
					m[i][j] *= y;
				}
			}
			return this;
		}

		/// <summary> v[] /= y
		/// </summary>
		public Matrix Over(double y)
		{
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
				{
					m[i][j] /= y;
				}
			}
			return this;
		}

		/// <summary> x[] == y[] -- pairwize 
		/// </summary>
		/// <returns>true if both are pairwize equal</returns>
		public bool AreExactlyTheSame(Matrix y)
		{
			VerifySameShape(y);
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
				{
					if (m[i][j] != y[i, j])
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>Find the minimum value in the matrix.
		/// </summary>
		/// <returns>The minimum value.</returns>
		public double Min()
		{
			double num = double.MaxValue;
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
				{
					if (num > m[i][j])
					{
						num = m[i][j];
					}
				}
			}
			return num;
		}

		/// <summary>Find the maximum value in the matrix.
		/// </summary>
		/// <returns>The maximum value.</returns>
		public double Max()
		{
			double num = double.MinValue;
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
				{
					if (num < m[i][j])
					{
						num = m[i][j];
					}
				}
			}
			return num;
		}

		/// <summary>Find the maximum magnitude value in a column.
		/// </summary>
		/// <returns>The maximum value in the column.</returns>
		public int IndexColMaxAbs(int column, int rowStart)
		{
			VerifyColumn(column, "column");
			int result = rowStart;
			double num = Math.Abs(m[rowStart][column]);
			for (int i = rowStart + 1; i < RowCount; i++)
			{
				double num2 = Math.Abs(m[i][column]);
				if (num < num2)
				{
					num = num2;
					result = i;
				}
			}
			return result;
		}

		/// <summary>Find the maximum magnitude value in a row.
		/// </summary>
		/// <returns>The maximum value in the row.</returns>
		public int IndexRowMaxAbs(int row, int colStart)
		{
			int result = colStart;
			double num = Math.Abs(m[row][colStart]);
			for (int i = colStart + 1; i < ColumnCount; i++)
			{
				double num2 = Math.Abs(m[row][i]);
				if (num < num2)
				{
					num = num2;
					result = i;
				}
			}
			return result;
		}

		/// <summary>Perform a rank-one update alpha * b * c' of a matrix, using a column in matrix B
		/// and row in matrix C.
		/// </summary>
		/// <param name="A">The matrix to update.</param>
		/// <param name="rowStart">The first row in the matrix to update.</param>
		/// <param name="colStart">The first column in the matrix to update.</param>
		/// <param name="rowCount">The number of rows to update.</param>
		/// <param name="colCount">The number of columns to update.</param>
		/// <param name="alpha">The scaling factor.</param>
		/// <param name="B">The matrix containing the column vector.</param>
		/// <param name="bRowStart">The first row in B.</param>
		/// <param name="b_col">The column to use in B.</param>
		/// <param name="C">The matrix containing the row vector.</param>
		/// <param name="c_row">The row to use in B.</param>
		/// <param name="cColStart">The first column in C.</param>
		public static void RankOneUpdate(Matrix A, int rowStart, int colStart, int rowCount, int colCount, double alpha, Matrix B, int bRowStart, int b_col, Matrix C, int c_row, int cColStart)
		{
			if (rowStart + rowCount > A.RowCount || colStart + colCount > A.ColumnCount)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch, "A");
			}
			if (bRowStart + rowCount > B.RowCount)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch, "B");
			}
			if (cColStart + colCount > C.ColumnCount)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch, "C");
			}
			for (int i = 0; i < rowCount; i++)
			{
				double num = alpha * B.m[i + bRowStart][b_col];
				for (int j = 0; j < colCount; j++)
				{
					A.m[i + rowStart][j + colStart] += num * C.m[c_row][j + cColStart];
				}
			}
		}

		/// <summary>Swap rows in a matrix.
		/// </summary>
		public void SwapRows(int row1, int row2)
		{
			VerifyRow(row1, "row1");
			VerifyRow(row2, "row2");
			for (int i = 0; i < ColumnCount; i++)
			{
				double num = m[row1][i];
				m[row1][i] = m[row2][i];
				m[row2][i] = num;
			}
		}

		/// <summary>Swap rows according to the specified pivot vector.
		/// </summary>
		/// <param name="pivot">Row i will be swapped with pivot[i].  pivot is NOT a permutation.</param>
		public void PivotRows(int[] pivot)
		{
			for (int i = 0; i < RowCount; i++)
			{
				if (i != pivot[i])
				{
					SwapRows(i, pivot[i]);
				}
			}
		}

		/// <summary>Returns a string representation of the matrix.
		/// </summary>
		/// <returns>A string representation of the matrix.</returns>
		public override string ToString()
		{
			return ToStringCore(5);
		}

		/// <summary>Returns a string representation of the matrix.
		/// </summary>
		/// <returns>A string representation of the matrix.</returns>
		public string ToString(string format, IFormatProvider formatProvider)
		{
			if (format != null && format.StartsWith("f", StringComparison.Ordinal))
			{
				return ToStringCore(int.MaxValue);
			}
			return ToString();
		}

		/// <summary>Returns a string representation of the matrix.
		/// </summary>
		/// <returns>A string representation of the matrix.</returns>
		private string ToStringCore(int printMax)
		{
			int num = ((RowCount > printMax) ? printMax : RowCount);
			int num2 = ((ColumnCount > printMax) ? printMax : ColumnCount);
			StringBuilder stringBuilder = new StringBuilder(num * num2 * 5);
			if (RowCount > printMax || ColumnCount > printMax)
			{
				stringBuilder.AppendFormat("r = {0}, c = {1} ", RowCount, ColumnCount);
			}
			stringBuilder.Append("{");
			for (int i = 0; i < num; i++)
			{
				stringBuilder.Append("{");
				for (int j = 0; j < num2; j++)
				{
					stringBuilder.Append(m[i][j]);
					if (j + 1 < num2)
					{
						stringBuilder.Append(", ");
					}
				}
				if (num2 < ColumnCount)
				{
					stringBuilder.Append("..");
				}
				stringBuilder.Append("} ");
				if (i + 1 < num)
				{
					stringBuilder.Append(", ");
				}
			}
			if (num < RowCount)
			{
				stringBuilder.Append("..");
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}
	}
}
