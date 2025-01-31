using System;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> A column-compressed lower-triangular matrix (must be square).
	/// </summary>
	internal class LowerSparseMatrix : SparseMatrixDouble
	{
		/// <summary> Instantiate an empty LowerSparseMatrix
		/// </summary>
		internal LowerSparseMatrix(int N, int count, double ratio)
			: base(N, N, count, ratio)
		{
		}

		/// <summary> Instantiate an identity matrix size rowColumnCount (square)
		/// </summary>
		public static LowerSparseMatrix Identity(int rowColumnCount)
		{
			LowerSparseMatrix lowerSparseMatrix = new LowerSparseMatrix(rowColumnCount, rowColumnCount, 1.0);
			for (int i = 0; i < rowColumnCount; i++)
			{
				lowerSparseMatrix._columnStarts[i] = i;
				lowerSparseMatrix._rowIndexes[i] = i;
				lowerSparseMatrix._values[i] = 1.0;
			}
			lowerSparseMatrix._columnStarts[rowColumnCount] = rowColumnCount;
			return lowerSparseMatrix;
		}

		/// <summary> Instantiate from a set of triples
		/// </summary>
		/// <param name="Ts"></param>
		/// <param name="N"></param>
		public LowerSparseMatrix(TripleList<double> Ts, int N)
			: base(N, N, Ts.Count, 1.0)
		{
			int num = -1;
			int num2 = -1;
			int num3 = 0;
			Ts.SortUnique(null);
			foreach (Triple<double> T in Ts)
			{
				if (num != T.Column)
				{
					if (T.Column < num)
					{
						throw new ArgumentException(Resources.ColumnIndexesDidNotSortCorrectly);
					}
					while (num < T.Column)
					{
						_columnStarts[++num] = num3;
					}
					num2 = -1;
				}
				if (T.Row < num2)
				{
					throw new ArgumentException(Resources.RowIndexesDidNotSortCorrectly);
				}
				if (num2 < T.Row)
				{
					num2 = T.Row;
					if (num2 < num)
					{
						throw new ArgumentException(Resources.UpperTriangularCoordinateSeen);
					}
					_values[num3] = T.Value;
					_rowIndexes[num3++] = T.Row;
				}
			}
			while (num < base.ColumnCount)
			{
				_columnStarts[++num] = num3;
			}
		}

		/// <summary> Instantiate a pre-planned ColumnCompressedSparseMatrix.
		/// </summary>
		/// <param name="N"> the number of columns and rows </param>
		/// <param name="starts"> the starting positions of columns </param>
		/// <param name="indexes"> the row indexes </param>
		/// <param name="values"> optional: the values </param>
		internal LowerSparseMatrix(int N, int[] starts, int[] indexes, double[] values)
			: base(N, N, starts, indexes, values)
		{
		}

		/// <summary> Instantiate a pre-planned ColumnCompressedSparseMatrix.
		/// </summary>
		/// <param name="N"> the number of columns and rows </param>
		internal LowerSparseMatrix(int N)
			: base(N, N)
		{
		}

		internal override SparseMatrixByColumn<double> TransposeEmpty()
		{
			return new UpperSparseMatrix(base.ColumnCount, (int)Count, 1.0);
		}

		/// <summary> Check if the matrix is symmetric
		/// </summary>
		public override bool IsSymmetric()
		{
			return false;
		}

		/// <summary> this = A * B
		/// </summary>
		public void Product(LowerSparseMatrix A, LowerSparseMatrix B)
		{
			if (A == null)
			{
				throw new ArgumentNullException("A");
			}
			if (B == null)
			{
				throw new ArgumentNullException("B");
			}
			if (base.RowCount != A.RowCount || base.ColumnCount != B.ColumnCount)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "A", "B" }));
			}
			double[] array = new double[base.ColumnCount];
			int num = 0;
			int num2 = 0;
			_columnStarts[0] = 0;
			int num3 = 0;
			while (num2 < base.ColumnCount)
			{
				int num4 = B._columnStarts[num2 + 1];
				int num5 = 0;
				while (num3 < num4)
				{
					int num6 = B._rowIndexes[num3];
					if (num6 < num2)
					{
						throw new ArgumentOutOfRangeException("B", Resources.NotTriangular);
					}
					double num7 = B._values[num3++];
					int num8 = A._columnStarts[num6];
					int num9 = A._columnStarts[num6 + 1];
					int num10 = 0;
					while (num8 < num9)
					{
						num10 = A._rowIndexes[num8];
						if (num10 < num6)
						{
							throw new ArgumentOutOfRangeException("A", Resources.NotTriangular);
						}
						array[num10] += num7 * A._values[num8++];
					}
					num5 = Math.Max(num5, num10);
				}
				for (int i = num2; i <= num5; i++)
				{
					if (0.0 != array[i])
					{
						_values[num] = array[i];
						array[i] = 0.0;
						_rowIndexes[num++] = i;
					}
				}
				_columnStarts[++num2] = num;
			}
		}
	}
}
