using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Factorizes and solves symmetric indefinite sparse systems.
	/// </summary>
	internal class SymmetricIndefiniteFactor : SymmetricFactorWorkspace
	{
		private int _zeroPivots;

		private SparseMatrixDouble _A;

		/// <summary>Diagonal values smaller than this value are handled specially.
		/// </summary>
		internal double _zeroPivotTolerance = 1E-30;

		private int[] _map;

		/// <summary>Number of zero pivots encountered during factorization.
		/// </summary>
		public int ZeroPivots
		{
			get
			{
				return _zeroPivots;
			}
			set
			{
				_zeroPivots = value;
			}
		}

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="M">The coefficient matrix.</param>
		/// <param name="CheckAbort">CheckAbort.</param>
		/// <param name="factorParam">Factorization parameters.</param>
		public SymmetricIndefiniteFactor(SparseMatrixDouble M, Func<bool> CheckAbort, FactorizationParameters factorParam)
			: base(M.RowCount, factorParam)
		{
			M.VerifySquare();
			base.CheckAbort = CheckAbort;
			base.RefinePivot = ZeroPivotRepair;
			_M = M;
			_map = new int[base.ColumnCount];
			base.InnerToOuter = IdentityPermutation(_M.ColumnCount);
			base.OuterToInner = IdentityPermutation(_M.ColumnCount);
			Factorize(PlanProductFillin, _M.ColumnCount);
		}

		/// <summary> Cause a zero pivot's row and column to be ignored.
		/// </summary>
		/// <returns> throw a DivideByZeroException </returns>
		private double ZeroPivotRepair(int innerCol, double value, List<SparseVectorCell> perturbations)
		{
			if (double.IsNaN(value))
			{
				_zeroPivots++;
				return 1E+150;
			}
			double num = Math.Abs(value);
			if (num < 1E-30)
			{
				_zeroPivots++;
				return 1E-08;
			}
			if (num < _zeroPivotTolerance)
			{
				_zeroPivots++;
				return _zeroPivotTolerance;
			}
			return value;
		}

		/// <summary> Solves the linear system without iterative refinement.
		/// </summary>
		/// <param name="rhs">The righthand side.</param>
		/// <returns>The solution vector.</returns>
		public override Vector Solve(Vector rhs)
		{
			Vector vector = new Vector(_M.ColumnCount);
			Solve(rhs, vector, 0);
			return vector;
		}

		/// <summary> Solves the linear system without iterative refinement.
		/// </summary>
		/// <param name="rhs">The righthand side.</param>
		/// <param name="solution">The preallocated solution vector.</param>
		/// <param name="maxIR">The maximum number of iterative refinements (currently ignored)</param>
		/// <returns>The number of iterative refinements performed (currently zero).</returns>
		public int Solve(Vector rhs, Vector solution, int maxIR)
		{
			maxIR = 0;
			double[] reals = new double[_M._T._maxRowLength];
			Vector vector = ForwardSolve(rhs, reals);
			DiagonalSolve(vector);
			Vector y = BackwardSolve(vector, reals);
			solution.CopyFrom(y);
			return maxIR;
		}

		private void DiagonalSolve(Vector intermediates)
		{
			for (int i = 0; i < intermediates.Length; i++)
			{
				intermediates[i] *= _diagonalFactor[i];
			}
		}

		private double[] PlanProductFillin()
		{
			_A = new SparseMatrixDouble(_M.RowCount, _M.ColumnCount, (int[])_M._columnStarts.Clone(), (int[])_M._rowIndexes.Clone(), (double[])_M._values.Clone());
			int columnCount = _M.ColumnCount;
			double[] array = new double[columnCount];
			for (int i = 0; i < columnCount; i++)
			{
				int a = _M.CountColumnSlots(i);
				SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_M, i);
				while (colIter.IsValid)
				{
					SymmetricFactorWorkspace.Rehash(ref a, colIter.Row(_M));
					colIter.Advance();
				}
				array[i] = (double)a / -2147483648.0;
			}
			return array;
		}

		/// <summary>Fill the coefficient matrix in preparation for a solve.
		/// </summary>
		public virtual void Fill()
		{
			_M._values.ZeroFill();
			for (int i = 0; i < _M.ColumnCount; i++)
			{
				int num = base.OuterToInner[i];
				SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_M, num);
				while (colIter.IsValid)
				{
					_map[colIter.Row(_M)] = colIter.Slot;
					colIter.Advance();
				}
				SparseMatrixByColumn<double>.ColIter colIter2 = new SparseMatrixByColumn<double>.ColIter(_A, i);
				while (colIter2.IsValid)
				{
					int num2 = base.OuterToInner[colIter2.Row(_A)];
					if (num <= num2)
					{
						_M._values[_map[num2]] = colIter2.Value(_A);
					}
					colIter2.Advance();
				}
				SparseMatrixByColumn<double>.ColIter colIter3 = new SparseMatrixByColumn<double>.ColIter(_M, num);
				while (colIter3.IsValid)
				{
					_map[colIter3.Row(_M)] = -1;
					colIter3.Advance();
				}
			}
		}

		private static int[] IdentityPermutation(int length)
		{
			int[] array = new int[length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = i;
			}
			return array;
		}
	}
}
