using System;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> A column-compressed upper-triangular matrix (must be square).
	///           Note this is not simply a reflection of the lower
	///           triangle, since we remain column-compressed.
	/// </summary>
	internal class UpperSparseMatrix : SparseMatrixDouble
	{
		/// <summary> Instantiate an empty UpperSparseMatrix
		/// </summary>
		internal UpperSparseMatrix(int N, int count, double ratio)
			: base(N, N, count, ratio)
		{
		}

		/// <summary> Instantiate from a set of triples
		/// </summary>
		/// <param name="Ts"> (row, col, value) triples </param>
		/// <param name="N"> count of columns (= rows) </param>
		public UpperSparseMatrix(TripleList<double> Ts, int N)
			: base(N, N, Ts.Count, 1.0)
		{
			int num = -1;
			int num2 = -1;
			int num3 = 0;
			Ts.Sort();
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
					if (num2 > num)
					{
						throw new ArgumentException(Resources.LowerTriangularCoordinateSeen);
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

		internal override SparseMatrixByColumn<double> TransposeEmpty()
		{
			return new LowerSparseMatrix(base.ColumnCount, (int)Count, 1.0);
		}

		/// <summary> Check if the matrix is symmetric
		/// </summary>
		public override bool IsSymmetric()
		{
			return false;
		}

		/// <summary> this = A * B
		/// </summary>
		public void Product(UpperSparseMatrix A, UpperSparseMatrix B)
		{
			if (A == null)
			{
				throw new ArgumentNullException("A");
			}
			if (B == null)
			{
				throw new ArgumentNullException("B");
			}
			if (base.ColumnCount != A.ColumnCount || base.ColumnCount != B.ColumnCount)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "A", "B" }));
			}
			double[] array = new double[base.ColumnCount];
			int num = 0;
			int num2 = 0;
			_columnStarts[0] = 0;
			int num3 = 0;
			int num4 = B._columnStarts[1];
			while (num2 < base.ColumnCount)
			{
				int num5 = 0;
				while (num3 < num4)
				{
					int num6 = B._rowIndexes[num3];
					if (num2 < num6)
					{
						throw new ArgumentOutOfRangeException("B", Resources.NotTriangular);
					}
					num5 = Math.Min(num5, num6);
					double num7 = B._values[num3++];
					int num8 = A._columnStarts[num6];
					int num9 = A._columnStarts[num6 + 1];
					while (num8 < num9)
					{
						int num10 = A._rowIndexes[num8];
						if (num6 < num10)
						{
							throw new ArgumentOutOfRangeException("A", Resources.NotTriangular);
						}
						array[num10] += num7 * A._values[num8++];
					}
				}
				for (int i = num5; i <= num2; i++)
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
