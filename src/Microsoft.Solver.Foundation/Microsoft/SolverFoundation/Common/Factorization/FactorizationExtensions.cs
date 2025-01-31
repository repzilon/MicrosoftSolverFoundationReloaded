using System;

namespace Microsoft.SolverFoundation.Common.Factorization
{
	/// <summary> Utilities and extension methods that are useful for factorization.
	/// </summary>
	internal static class FactorizationExtensions
	{
		/// <summary>Change the number of nonzeros allocated to a SparseMatrixDouble.
		/// </summary>
		public static void Resize(this SparseMatrixDouble A, int newNzmax)
		{
			if (newNzmax <= 0)
			{
				newNzmax = (int)A.Count;
			}
			Array.Resize(ref A._rowIndexes, newNzmax);
			if (A._values != null)
			{
				Array.Resize(ref A._values, newNzmax);
			}
		}

		/// <summary>Set all values to a constant.
		/// </summary>
		public static void ConstantFill(this int[] p, int value)
		{
			if (p != null)
			{
				for (int i = 0; i < p.Length; i++)
				{
					p[i] = value;
				}
			}
		}

		/// <summary>Allocate a new array with all values set to a constant.
		/// </summary>
		public static int[] ConstantFill(long n, int value)
		{
			int[] array = new int[n];
			array.ConstantFill(value);
			return array;
		}

		/// <summary>Create the identity permutation.
		/// </summary>
		public static int[] IdentityPermutation(int n)
		{
			int[] array = new int[n];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = i;
			}
			return array;
		}

		/// <summary>Invert a permutation.
		/// </summary>
		public static int[] InvertPermutation(int[] p)
		{
			int[] array = new int[p.Length];
			for (int i = 0; i < p.Length; i++)
			{
				array[p[i]] = i;
			}
			return array;
		}

		/// <summary>Mark an entry in the array.
		/// </summary>
		public static void Mark(this int[] w, int k)
		{
			w[k] = Flip(w[k]);
		}

		/// <summary>"Flip" a value.  (This function is its own inverse.)
		/// </summary>
		public static int Flip(int n)
		{
			return -n - 2;
		}

		/// <summary>Compute the cumulative sum of c, storing in "this". c is also modified.
		/// </summary>
		public static int CumulativeSum(this int[] p, int[] c)
		{
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < c.Length; i++)
			{
				p[i] = num;
				num += c[i];
				num2 += c[i];
				c[i] = p[i];
			}
			p[c.Length] = num;
			return num2;
		}

		/// <summary>Add sparse matrices.
		/// </summary>
		public static SparseMatrixDouble Add(this SparseMatrixByColumn<double> A, SparseMatrixByColumn<double> B, double alpha, double beta)
		{
			int num = 0;
			int num2 = A._columnStarts[A.ColumnCount];
			int num3 = B._columnStarts[A.ColumnCount];
			int[] filter = new int[A.RowCount];
			bool flag = A._values != null && B._values != null;
			double[] array = null;
			if (flag)
			{
				array = new double[A.RowCount];
			}
			SparseMatrixDouble sparseMatrixDouble = new SparseMatrixDouble(A.RowCount, B.ColumnCount, num2 + num3, flag);
			for (int i = 0; i < B.ColumnCount; i++)
			{
				sparseMatrixDouble._columnStarts[i] = num;
				num = A.Scatter(i, alpha, filter, array, i + 1, sparseMatrixDouble, num);
				num = B.Scatter(i, beta, filter, array, i + 1, sparseMatrixDouble, num);
				if (flag)
				{
					for (int j = sparseMatrixDouble._columnStarts[i]; j < num; j++)
					{
						sparseMatrixDouble._values[j] = array[sparseMatrixDouble._rowIndexes[j]];
					}
				}
			}
			sparseMatrixDouble._columnStarts[A.ColumnCount] = num;
			sparseMatrixDouble.Resize(-1);
			return sparseMatrixDouble;
		}

		/// <summary> Computes the elimination tree of a sparse matrix.  If we regard the
		/// columns of A as nodes, the elimination tree represents the nonzero structure
		/// of the Cholesky factor L.  The elimination tree is a pruned version of the
		/// graph of the nonzeros of L.
		/// </summary>
		/// <param name="A">A sparse matrix.</param>
		/// <returns>The parent of each node in the elimination tree.</returns>
		public static int[] EliminationTree(this SparseMatrixDouble A)
		{
			int[] array = ConstantFill(A.ColumnCount, -1);
			int[] array2 = ConstantFill(A.ColumnCount, -1);
			for (int i = 0; i < A.ColumnCount; i++)
			{
				for (int j = A._columnStarts[i]; j < A._columnStarts[i + 1]; j++)
				{
					int num = A._rowIndexes[j];
					int num2 = -1;
					while (num != -1 && num < i)
					{
						num2 = array2[num];
						array2[num] = i;
						if (num2 == -1)
						{
							array[num] = i;
						}
						num = num2;
					}
				}
			}
			return array;
		}

		/// <summary> Postorder the tree given by parent. The postordering permutes the matrix in a way that 
		/// preserves nonzero structure, but gives better structure.
		/// </summary>
		public static int[] Postorder(this int[] parent)
		{
			int num = parent.Length;
			int[] array = ConstantFill(num, -1);
			int[] array2 = new int[num];
			int[] array3 = new int[num];
			int[] stack = new int[num];
			for (int num2 = parent.Length - 1; num2 >= 0; num2--)
			{
				if (parent[num2] != -1)
				{
					array3[num2] = array[parent[num2]];
					array[parent[num2]] = num2;
				}
			}
			int k = 0;
			for (int i = 0; i < num; i++)
			{
				if (parent[i] == -1)
				{
					k = TraverseDFS(i, k, array, array3, array2, stack);
				}
			}
			return array2;
		}

		/// <summary> Postorder traversal starting at node j.  Write indexes in post starting at index k.
		/// </summary>
		public static int TraverseDFS(int j, int k, int[] head, int[] next, int[] post, int[] stack)
		{
			stack[0] = j;
			int num = 0;
			while (num >= 0)
			{
				int num2 = stack[num];
				int num3 = head[num2];
				if (num3 == -1)
				{
					num--;
					post[k++] = num2;
				}
				else
				{
					head[num2] = next[num3];
					stack[++num] = num3;
				}
			}
			return k;
		}

		/// <summary> Compute the nonzero pattern of one row in the factorization.
		/// Use the elimination tree (parent) to traverse the kth row subtree.
		/// </summary>
		/// <remarks>w is temporary storage which is restored on output.</remarks>
		public static int Reach(this SparseMatrixDouble A, int row, int[] parent, int[] reach, int[] work)
		{
			int num = A.ColumnCount;
			work.Mark(row);
			for (int i = A._columnStarts[row]; i < A._columnStarts[row + 1]; i++)
			{
				int num2 = A._rowIndexes[i];
				if (num2 <= row)
				{
					int num3 = 0;
					while (work[num2] >= 0)
					{
						reach[num3++] = num2;
						work.Mark(num2);
						num2 = parent[num2];
					}
					while (num3 > 0)
					{
						reach[--num] = reach[--num3];
					}
				}
			}
			for (int j = num; j < A.ColumnCount; j++)
			{
				work.Mark(reach[j]);
			}
			work.Mark(row);
			return num;
		}

		/// <summary>Scatters the elements of column col into a dense vector x, scaling by beta and filtering using (filter, limit).
		/// </summary>
		public static int Scatter(this SparseMatrixByColumn<double> A, int col, double beta, int[] filter, double[] x, int limit, SparseMatrixDouble C, int nz)
		{
			for (int i = A._columnStarts[col]; i < A._columnStarts[col + 1]; i++)
			{
				int num = A._rowIndexes[i];
				if (filter[num] < limit)
				{
					filter[num] = limit;
					C._rowIndexes[nz++] = num;
					if (x != null)
					{
						x[num] = beta * A._values[i];
					}
				}
				else if (x != null)
				{
					x[num] += beta * A._values[i];
				}
			}
			return nz;
		}

		/// <summary> Remove diagonal entries.
		/// </summary>
		/// <param name="A">The matrix.</param>
		/// <returns>The number of nonzeros in the matrix after removal.</returns>
		public static int RemoveDiagonal(this SparseMatrixDouble A)
		{
			int num = 0;
			for (int i = 0; i < A.ColumnCount; i++)
			{
				int j = A._columnStarts[i];
				A._columnStarts[i] = num;
				for (; j < A._columnStarts[i + 1]; j++)
				{
					if (A._rowIndexes[j] != i)
					{
						if (A._values != null)
						{
							A._values[num] = A._values[j];
						}
						A._rowIndexes[num++] = A._rowIndexes[j];
					}
				}
			}
			A._columnStarts[A.ColumnCount] = num;
			A.Resize(-1);
			return num;
		}

		/// <summary> Compute the number of nonzero entries in each column of the Cholesky factorization of A.
		/// </summary>
		/// <param name="A">The matrix.</param>
		/// <param name="parent">The elimination tree of A.</param>
		/// <param name="post">The postordering of A.</param>
		/// <returns></returns>
		public static int[] FactorColumnCounts(this SparseMatrixDouble A, int[] parent, int[] post)
		{
			int columnCount = A.ColumnCount;
			SparseMatrixByColumn<double> sparseMatrixByColumn = A.Transpose();
			int[] array = new int[columnCount];
			int[] array2 = array;
			int[] maxfirst = ConstantFill(columnCount, -1);
			int[] prevleaf = ConstantFill(columnCount, -1);
			int[] array3 = ConstantFill(columnCount, -1);
			for (int i = 0; i < post.Length; i++)
			{
				int num = post[i];
				array[num] = ((array3[num] == -1) ? 1 : 0);
				while (num != -1 && array3[num] == -1)
				{
					array3[num] = i;
					num = parent[num];
				}
			}
			int[] columnStarts = sparseMatrixByColumn._columnStarts;
			int[] rowIndexes = sparseMatrixByColumn._rowIndexes;
			int[] array4 = IdentityPermutation(columnCount);
			foreach (int num2 in post)
			{
				if (parent[num2] != -1)
				{
					array[parent[num2]]--;
				}
				for (int k = columnStarts[num2]; k < columnStarts[num2 + 1]; k++)
				{
					int i2 = rowIndexes[k];
					IsLeafInEliminationSubtree(i2, num2, array3, maxfirst, prevleaf, array4, out var isLeaf, out var jLCA);
					if (isLeaf >= 1)
					{
						array[num2]++;
					}
					if (isLeaf == 2)
					{
						array[jLCA]--;
					}
				}
				if (parent[num2] != -1)
				{
					array4[num2] = parent[num2];
				}
			}
			for (int l = 0; l < parent.Length; l++)
			{
				if (parent[l] != -1)
				{
					array2[parent[l]] += array2[l];
				}
			}
			return array2;
		}

		/// <summary> Determine if j is a leaf of the ith row elimination subtree.  (Section 4.4 of Davis)
		/// </summary>
		/// <param name="i">The index of the elimination subtree</param>
		/// <param name="j">The index of the node to test.</param>
		/// <param name="first">First[j] is the first descendant of j in the subtree.</param>
		/// <param name="maxfirst"></param>
		/// <param name="prevleaf"></param>
		/// <param name="ancestor">The ancestor of each node.  -1 for root nodes.</param>
		/// <param name="isLeaf">Is 0 if j is not a leaf, is 1 if it is the first leaf, 2 if a subsequent leaf. (See Figure 4.9 in Davis.)</param>
		/// <param name="jLCA">The least common ancestor of j and its previous leaf, if j is a leaf.</param>
		private static void IsLeafInEliminationSubtree(int i, int j, int[] first, int[] maxfirst, int[] prevleaf, int[] ancestor, out int isLeaf, out int jLCA)
		{
			isLeaf = 0;
			if (i <= j || first[j] <= maxfirst[i])
			{
				jLCA = -1;
				return;
			}
			maxfirst[i] = first[j];
			int num = prevleaf[i];
			prevleaf[i] = j;
			isLeaf = ((num == -1) ? 1 : 2);
			if (isLeaf == 1)
			{
				jLCA = i;
				return;
			}
			for (jLCA = num; jLCA != ancestor[jLCA]; jLCA = ancestor[jLCA])
			{
			}
			int num2 = -1;
			for (int num3 = num; num3 != jLCA; num3 = num2)
			{
				num2 = ancestor[num3];
				ancestor[num3] = jLCA;
			}
		}

		/// <summary> For upper-triangular A, compute C = A(p, p), given the inverse of p.
		/// </summary>
		public static SparseMatrixDouble PermuteUpperTriangular(this SparseMatrixDouble A, int[] pinv, bool values)
		{
			SparseMatrixDouble sparseMatrixDouble = new SparseMatrixDouble(A.ColumnCount, A.ColumnCount, A._columnStarts[A.ColumnCount], values && A._values != null);
			int[] array = new int[A.ColumnCount];
			for (int i = 0; i < A.ColumnCount; i++)
			{
				int val = pinv[i];
				for (int j = A._columnStarts[i]; j < A._columnStarts[i + 1]; j++)
				{
					int num = A._rowIndexes[j];
					if (num <= i)
					{
						int val2 = pinv[num];
						array[Math.Max(val2, val)]++;
					}
				}
			}
			sparseMatrixDouble._columnStarts.CumulativeSum(array);
			for (int k = 0; k < A.ColumnCount; k++)
			{
				int val3 = pinv[k];
				for (int l = A._columnStarts[k]; l < A._columnStarts[k + 1]; l++)
				{
					int num2 = A._rowIndexes[l];
					if (num2 <= k)
					{
						int val4 = pinv[num2];
						int num3 = array[Math.Max(val4, val3)]++;
						sparseMatrixDouble._rowIndexes[num3] = Math.Min(val4, val3);
						if (sparseMatrixDouble._values != null)
						{
							sparseMatrixDouble._values[num3] = A._values[l];
						}
					}
				}
			}
			sparseMatrixDouble.Resize(-1);
			return sparseMatrixDouble;
		}
	}
}
