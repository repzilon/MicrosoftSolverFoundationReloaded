using System;
using Microsoft.SolverFoundation.Common.Factorization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> A supernodal approximate minimum degree ordering algorithm.
	/// </summary>
	/// <remarks> Read Timothy Davis's book on Direct Methods for Sparse Matrices for details.
	/// </remarks>
	internal class ApproximateMinDegreeWorkspace : SymbolicFactorWorkspace
	{
		internal class AMDResult
		{
			public int[] InnerToOuter;

			public int[] OuterToInner;

			public SparseMatrixDouble C;

			public int[] q;

			public int[] parent;

			public int[] ColumnStarts;

			public int[] leftmost;
		}

		/// <summary> Create a new instance.
		/// </summary>
		/// <param name="M">The matrix.</param>
		/// <param name="colWeights">Column weights.</param>
		/// <param name="factorizationParameters">Factorization parameters</param>
		/// <param name="abort">Abort delegate.</param>
		public ApproximateMinDegreeWorkspace(SparseMatrixDouble M, double[] colWeights, FactorizationParameters factorizationParameters, Func<bool> abort)
			: base(M, colWeights, factorizationParameters, abort)
		{
		}

		/// <summary>Computes the symbolic factorization, modifying M.
		/// </summary>
		/// <returns>Symbolic factorization information, including mapping from original to permuted columns.</returns>
		public override SymbolicFactorResult Factorize()
		{
			if (CheckAbort())
			{
				throw new TimeLimitReachedException();
			}
			SymbolicFactorResult symbolicFactorResult = new SymbolicFactorResult();
			AMDResult aMDResult = InitSymbolic();
			symbolicFactorResult.InnerToOuter = aMDResult.InnerToOuter;
			symbolicFactorResult.OuterToInner = aMDResult.OuterToInner;
			if (CheckAbort())
			{
				throw new TimeLimitReachedException();
			}
			SparseMatrixDouble sparseMatrixDouble = FactorCore(aMDResult, symbolicFactorResult);
			_M._columnStarts = sparseMatrixDouble._columnStarts;
			_M._rowIndexes = sparseMatrixDouble._rowIndexes;
			_M._values = new double[sparseMatrixDouble.Count];
			return symbolicFactorResult;
		}

		private AMDResult InitSymbolic()
		{
			AMDResult aMDResult = new AMDResult();
			AMD aMD = new AMD(_M);
			aMD.PlanBestOrder();
			aMDResult.InnerToOuter = aMD.Order;
			aMDResult.OuterToInner = FactorizationExtensions.InvertPermutation(aMDResult.InnerToOuter);
			aMD = null;
			SparseMatrixDouble a = _M.PermuteUpperTriangular(aMDResult.OuterToInner, values: false);
			aMDResult.parent = a.EliminationTree();
			int[] post = aMDResult.parent.Postorder();
			int[] c = a.FactorColumnCounts(aMDResult.parent, post);
			aMDResult.C = null;
			aMDResult.ColumnStarts = new int[_M.ColumnCount + 1];
			aMDResult.ColumnStarts.CumulativeSum(c);
			return aMDResult;
		}

		private SparseMatrixDouble FactorCore(AMDResult sym, SymbolicFactorResult result)
		{
			int columnCount = _M.ColumnCount;
			int[] array = new int[columnCount];
			int[] array2 = new int[columnCount];
			if (sym.C == null)
			{
				sym.C = _M.PermuteUpperTriangular(sym.OuterToInner, values: false);
			}
			_M._rowIndexes = (_M._columnStarts = null);
			_M._values = null;
			SparseMatrixDouble sparseMatrixDouble = new SparseMatrixDouble(columnCount, columnCount, sym.ColumnStarts[columnCount], initValues: true);
			Array.Copy(sym.ColumnStarts, array, array.Length);
			Array.Copy(sym.ColumnStarts, sparseMatrixDouble._columnStarts, sparseMatrixDouble._columnStarts.Length);
			for (int i = 0; i < columnCount; i++)
			{
				for (int j = sym.C.Reach(i, sym.parent, array2, array); j < columnCount; j++)
				{
					int num = array2[j];
					int num2 = array[num]++;
					sparseMatrixDouble._rowIndexes[num2] = i;
				}
				int num3 = array[i]++;
				sparseMatrixDouble._rowIndexes[num3] = i;
			}
			sparseMatrixDouble._columnStarts[columnCount] = sym.ColumnStarts[columnCount];
			if (CheckAbort())
			{
				throw new TimeLimitReachedException();
			}
			FindFirstDenseColumn(sparseMatrixDouble, result);
			if (sparseMatrixDouble._columnStarts[sparseMatrixDouble.ColumnCount] > sparseMatrixDouble._rowIndexes.Length)
			{
				sparseMatrixDouble.Resize(sparseMatrixDouble._columnStarts[sparseMatrixDouble.ColumnCount]);
			}
			SortRowIndexes(sparseMatrixDouble, result);
			return sparseMatrixDouble;
		}

		private void FindFirstDenseColumn(SparseMatrixDouble L, SymbolicFactorResult result)
		{
			int num = -1;
			int num2 = -1;
			for (int i = 0; i < L._columnStarts.Length - 1; i++)
			{
				int num3 = L._columnStarts[i + 1] - L._columnStarts[i];
				if (num < 0 && (double)num3 > base.Parameters.DenseWindowThreshhold * (double)(L.ColumnCount - i))
				{
					num = i;
				}
				if (num >= 0 && i >= num)
				{
					num3 = L.ColumnCount - i;
					L._columnStarts[i + 1] = L._columnStarts[i] + num3;
				}
				num2 = Math.Max(num2, num3);
			}
			result.FirstDenseColumn = num;
			result.MaxColumnCount = num2;
		}

		private static void SortRowIndexes(SparseMatrixDouble L, SymbolicFactorResult result)
		{
			for (int i = 0; i < L._columnStarts.Length - 1; i++)
			{
				int num = L._columnStarts[i + 1] - L._columnStarts[i];
				if (result.FirstDenseColumn >= 0 && i >= result.FirstDenseColumn)
				{
					int num2 = L._columnStarts[i];
					for (int j = 0; j < num; j++)
					{
						L._rowIndexes[num2 + j] = i + j;
					}
				}
				else if (num > 1)
				{
					Array.Sort(L._rowIndexes, L._columnStarts[i], num);
				}
			}
		}
	}
}
