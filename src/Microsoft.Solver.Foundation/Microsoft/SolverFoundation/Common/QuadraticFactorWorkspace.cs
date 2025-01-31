using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> The quadratic matrix is factored to check if it is positive semidefinite
	/// </summary>
	internal class QuadraticFactorWorkspace : SymmetricFactorWorkspace
	{
		private const double _zeroPivotValue = 1E+150;

		/// <summary> The quadratic coefficients are in Q.
		/// </summary>
		internal SymmetricSparseMatrix _Q;

		private double _negativePivotLimit = 1E-12;

		private double _zeroPivotLimit = 4.94E-322;

		/// <summary> Plan the fillin of the Q matrix.
		/// Each column is summarized as a Markowitz count (integer part) and
		///   a hash (fractional part) so that identical columns will cluster.
		/// <remarks> This block fill is both below and above diagonal. </remarks>
		/// </summary>
		/// <returns> initial weighting combines degree with a fractional pattern hash </returns>
		internal double[] PlanProductFillin()
		{
			int columnCount = _Q.ColumnCount;
			double[] array = new double[columnCount];
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < columnCount; i++)
			{
				if (0 < _Q.CountColumnSlots(i))
				{
					int num3 = ((i != _Q._rowIndexes[_Q._columnStarts[i]]) ? 1 : 0);
					num2 += 2 * (_Q.CountColumnSlots(i) + num3) - 1;
				}
				else
				{
					num2++;
				}
			}
			_M._rowIndexes = new int[num2 + num];
			int num4 = 0;
			_M._columnStarts = new int[columnCount + 1];
			base.InnerToOuter = new int[columnCount];
			for (int j = 0; j < columnCount; j++)
			{
				base.InnerToOuter[j] = j;
				_M._columnStarts[j] = num4;
				bool flag = false;
				foreach (int item in _Q.AllRowsInColumn(j))
				{
					flag = flag || j == item;
					if (!flag && j < item)
					{
						flag = true;
						_M._rowIndexes[num4++] = j;
					}
					_M._rowIndexes[num4++] = item;
				}
				if (!flag)
				{
					_M._rowIndexes[num4++] = j;
				}
			}
			_M._columnStarts[columnCount] = num4;
			for (int j = 0; j < columnCount; j++)
			{
				int a = _M.CountColumnSlots(j);
				SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_M, j);
				while (colIter.IsValid)
				{
					SymmetricFactorWorkspace.Rehash(ref a, colIter.Row(_M));
					colIter.Advance();
				}
				array[j] = (double)a / -2147483648.0;
			}
			double num5 = 0.0;
			for (int k = 0; k < _Q.RowCount; k++)
			{
				num5 += _Q[k, k] * _Q[k, k];
			}
			double num6 = _Q._values.InnerProduct(_Q._values);
			_negativePivotLimit = 0.0 - (_negativePivotLimit * num5 + num6 / 9.999999999999999E+151);
			_zeroPivotLimit *= Math.Max(num5, 1.0);
			return array;
		}

		/// <summary> Default behavior on an invalid pivot is to throw a DivideByZeroException.
		/// </summary>
		/// <returns> throw a DivideByZeroException </returns>
		private double PivotInspection(int col, double value, List<SparseVectorCell> perturbations)
		{
			if (value < _negativePivotLimit)
			{
				throw new DivideByZeroException();
			}
			if (value < _zeroPivotLimit)
			{
				return 1E+150;
			}
			return value;
		}

		/// <summary> Construct a KKT context for the Normal form equations in Central Path IPM.
		/// </summary>
		/// <param name="Q"> The (optional) quadratic coefficients' matrix </param>
		/// <param name="CheckAbort"> predicate returns false for continue, true if stop required </param>
		public QuadraticFactorWorkspace(SymmetricSparseMatrix Q, Func<bool> CheckAbort)
			: base(Q.ColumnCount, new FactorizationParameters(SymbolicFactorizationMethod.Automatic))
		{
			base.RefinePivot = PivotInspection;
			base.CheckAbort = CheckAbort;
			_Q = Q;
			Factorize(PlanProductFillin, base.ColumnCount);
		}

		/// <summary> Compute the augmented matrix using Q.
		/// </summary>
		public void SetWorkspaceValues()
		{
			int[] array = new int[base.ColumnCount];
			_M._values.ZeroFill();
			int columnCount = base.ColumnCount;
			for (int i = 0; i < columnCount; i++)
			{
				int num = base.OuterToInner[i];
				SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_M, num);
				while (colIter.IsValid)
				{
					array[colIter.Row(_M)] = colIter.Slot;
					colIter.Advance();
				}
				foreach (KeyValuePair<int, double> item in _Q.AllValuesInColumn(i))
				{
					int num2 = base.OuterToInner[item.Key];
					if (num <= num2)
					{
						_M._values[array[num2]] = item.Value;
					}
				}
			}
		}

		/// <summary> A complete solution of the system A*D*At x = z
		/// </summary>
		public override Vector Solve(Vector y)
		{
			throw new NotSupportedException("This class is only used to check convexity.");
		}
	}
}
