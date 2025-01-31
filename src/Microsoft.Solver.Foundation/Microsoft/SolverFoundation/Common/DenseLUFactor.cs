using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Solves systems of linear equations using LU factorization.
	/// </summary>
	internal class DenseLUFactor
	{
		/// <summary>Diagonal values smaller than this value are handled specially.
		/// </summary>
		private double _zeroPivotTolerance = 1E-30;

		private int[] _pivot;

		private Matrix _M;

		/// <summary> The factorization (U in the upper triangular portion, L in lower).
		/// </summary>
		public Matrix M => _M;

		/// <summary> The row pivot vector.
		/// </summary>
		public int[] Pivot => _pivot;

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="M">The matrix to factor (rectangular is okay).</param>
		public DenseLUFactor(Matrix M)
		{
			_M = M;
			_pivot = new int[M.RowCount];
		}

		/// <summary> Solve U X = B.
		/// </summary>
		/// <param name="B">The right hand side.</param>
		public void ForwardSolve(Matrix B)
		{
			if (_M.ColumnCount != B.RowCount)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch);
			}
			for (int i = 0; i < B.ColumnCount; i++)
			{
				for (int num = B.RowCount - 1; num >= 0; num--)
				{
					if (B[num, i] != 0.0)
					{
						if (Math.Abs(_M[num, num]) < _zeroPivotTolerance)
						{
							throw new DivideByZeroException();
						}
						B[num, i] /= _M[num, num];
						double num2 = B[num, i];
						for (int j = 0; j < num; j++)
						{
							B[j, i] -= _M[j, num] * num2;
						}
					}
				}
			}
		}

		/// <summary> Solve U x = b.
		/// </summary>
		/// <param name="b">The right hand side (replaced with x).</param>
		public void ForwardSolve(Vector b)
		{
			if (_M.ColumnCount != b.Length)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch);
			}
			for (int num = b.Length - 1; num >= 0; num--)
			{
				if (b[num] != 0.0)
				{
					if (_M[num, num] == 0.0)
					{
						throw new DivideByZeroException();
					}
					b[num] /= _M[num, num];
					double num2 = b[num];
					for (int i = 0; i < num; i++)
					{
						b[i] -= _M[i, num] * num2;
					}
				}
			}
		}

		/// <summary> Solve L X = B.
		/// </summary>
		/// <param name="B">The right hand side (replaced with x).</param>
		public void BackwardSolve(Matrix B)
		{
			if (_M.ColumnCount != B.RowCount)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch);
			}
			for (int i = 0; i < B.ColumnCount; i++)
			{
				for (int j = 0; j < B.RowCount; j++)
				{
					double num = B[j, i];
					if (num != 0.0)
					{
						for (int k = j + 1; k < B.RowCount; k++)
						{
							B[k, i] -= _M[k, j] * num;
						}
					}
				}
			}
		}

		/// <summary> Solve L X = B.
		/// </summary>
		/// <param name="b">The right hand side (replaced with x).</param>
		public void BackwardSolve(Vector b)
		{
			if (_M.ColumnCount != b.Length)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch);
			}
			for (int i = 0; i < b.Length; i++)
			{
				double num = b[i];
				if (num != 0.0)
				{
					for (int j = i + 1; j < b.Length; j++)
					{
						b[j] -= _M[j, i] * num;
					}
				}
			}
		}

		/// <summary> Solve M X = B.
		/// </summary>
		/// <param name="B">The right hand side (replaced with X).</param>
		public void Solve(Matrix B)
		{
			if (_M.ColumnCount != B.RowCount)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch);
			}
			if (_pivot.Length != B.RowCount)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch);
			}
			B.PivotRows(_pivot);
			BackwardSolve(B);
			ForwardSolve(B);
		}

		/// <summary> Solve M x = b.
		/// </summary>
		/// <param name="b">The right hand side (replaced with X).</param>
		public void Solve(Vector b)
		{
			if (_M.ColumnCount != b.Length)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch);
			}
			if (_pivot.Length != b.Length)
			{
				throw new ArgumentException(Resources.MatrixDimensionsDoNotMatch);
			}
			b.Pivot(_pivot);
			BackwardSolve(b);
			ForwardSolve(b);
		}

		/// <summary> Find the LU factorization for the matrix.
		/// </summary>
		/// <remarks> Based on algorithm 3.2.1 in Golub and Van Loan.  It is also similar to the level-2 
		/// algorithm used by LAPACK.
		/// </remarks>
		public void Factor()
		{
			int num = Math.Min(_M.RowCount, _M.ColumnCount);
			for (int i = 0; i < num; i++)
			{
				int num2 = _M.IndexColMaxAbs(i, i);
				_pivot[i] = num2;
				if (_M.m[num2][i] != 0.0)
				{
					if (num2 != i)
					{
						_M.SwapRows(i, num2);
					}
					if (i < _M.RowCount)
					{
						_M.ScaleColumn(i, 1.0 / _M.m[i][i], i + 1);
					}
				}
				if (i < num - 1)
				{
					Matrix.RankOneUpdate(_M, i + 1, i + 1, _M.RowCount - i - 1, _M.ColumnCount - i - 1, -1.0, _M, i + 1, i, _M, i, i + 1);
				}
			}
		}
	}
}
