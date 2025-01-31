using System;
using Microsoft.SolverFoundation.Common.Factorization;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Finds the AMD ordering for a matrix. 
	/// </summary>
	/// <remarks> Unlike the minimize local fill code, this method does NOT compute the structure of L.
	/// </remarks>
	internal class AMD
	{
		private SparseMatrixDouble _A;

		private int[] _order;

		private int[] _head;

		private int[] _next;

		private int[] _last;

		private int[] _superCount;

		private int[] _degree;

		private int _denseColumnThreshhold = int.MaxValue;

		private int[] _hashHead;

		private long[] _hash;

		private int[] _count;

		private int _minDegree;

		/// <summary>An ordering on the columns of A that results in reduced fill.
		/// </summary>
		public int[] Order => _order;

		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="M">The matrix.</param>
		public AMD(SparseMatrixDouble M)
		{
			_A = M;
		}

		/// <summary> Permute the symbolics to choose an order with low fill.
		/// </summary>
		public void PlanBestOrder()
		{
			int num = 0;
			int columnCount = _A.ColumnCount;
			_order = new int[columnCount + 1];
			_denseColumnThreshhold = (int)Math.Max(16.0, 10.0 * Math.Sqrt(1.0 * (double)columnCount));
			_denseColumnThreshhold = Math.Min(columnCount - 2, _denseColumnThreshhold);
			SparseMatrixByColumn<double> b = _A.Transpose();
			SparseMatrixDouble sparseMatrixDouble = _A.Add(b, 0.0, 0.0);
			b = null;
			sparseMatrixDouble.RemoveDiagonal();
			int[] columnStarts = sparseMatrixDouble._columnStarts;
			int[] rowIndexes = sparseMatrixDouble._rowIndexes;
			int num2 = columnStarts[columnCount];
			int newNzmax = num2 + num2 / 5 + 2 * columnCount;
			sparseMatrixDouble.Resize(newNzmax);
			_superCount = FactorizationExtensions.ConstantFill(columnCount + 1, 1);
			_next = FactorizationExtensions.ConstantFill(columnCount + 1, -1);
			_head = FactorizationExtensions.ConstantFill(columnCount + 1, -1);
			_last = FactorizationExtensions.ConstantFill(columnCount + 1, -1);
			int[] array = new int[columnCount + 1];
			_degree = new int[columnCount + 1];
			int[] array2 = new int[columnCount + 1];
			_hashHead = FactorizationExtensions.ConstantFill(columnCount + 1, -1);
			_hash = new long[columnCount + 1];
			CountColumnSlots(sparseMatrixDouble);
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = 1;
				array[i] = 0;
				_degree[i] = _count[i];
			}
			int mark = ClearWork(0, 0, array2, columnCount);
			array[columnCount] = -2;
			columnStarts[columnCount] = -1;
			array2[columnCount] = 0;
			int num3 = InitDegree(columnStarts, array, array2);
			while (num3 < columnCount)
			{
				int num4 = FindPivot(columnStarts, columnCount);
				int num5 = array[num4];
				int num6 = _superCount[num4];
				num3 += num6;
				if (num5 > 0 && num2 + _minDegree >= rowIndexes.Length)
				{
					num2 = CompressMatrix(columnStarts, ref rowIndexes, _count, num2, _minDegree);
				}
				int num7 = 0;
				_superCount[num4] = -num6;
				int num8 = columnStarts[num4];
				int num9 = ((num5 == 0) ? num8 : num2);
				int num10 = num9;
				for (int j = 1; j <= num5 + 1; j++)
				{
					int num11;
					int num12;
					int num13;
					if (j > num5)
					{
						num11 = num4;
						num12 = num8;
						num13 = _count[num4] - num5;
					}
					else
					{
						num11 = rowIndexes[num8++];
						num12 = columnStarts[num11];
						num13 = _count[num11];
					}
					for (int k = 0; k < num13; k++)
					{
						int num14 = rowIndexes[num12++];
						int num15;
						if ((num15 = _superCount[num14]) > 0)
						{
							num7 += num15;
							_superCount[num14] = -num15;
							rowIndexes[num10++] = num14;
							if (_next[num14] != -1)
							{
								_last[_next[num14]] = _last[num14];
							}
							if (_last[num14] != -1)
							{
								_next[_last[num14]] = _next[num14];
							}
							else
							{
								_head[_degree[num14]] = _next[num14];
							}
						}
					}
					if (num11 != num4)
					{
						columnStarts[num11] = FactorizationExtensions.Flip(num4);
						array2[num11] = 0;
					}
				}
				if (num5 != 0)
				{
					num2 = num10;
				}
				_degree[num4] = num7;
				columnStarts[num4] = num9;
				_count[num4] = num10 - num9;
				array[num4] = -2;
				mark = ClearWork(mark, num, array2, columnCount);
				for (int l = num9; l < num10; l++)
				{
					int num16 = rowIndexes[l];
					int num17;
					if ((num17 = array[num16]) <= 0)
					{
						continue;
					}
					int num15 = -_superCount[num16];
					int num18 = mark - num15;
					for (num8 = columnStarts[num16]; num8 <= columnStarts[num16] + num17 - 1; num8++)
					{
						int num19 = rowIndexes[num8];
						if (array2[num19] >= mark)
						{
							array2[num19] -= num15;
						}
						else if (array2[num19] != 0)
						{
							array2[num19] = _degree[num19] + num18;
						}
					}
				}
				for (int m = num9; m < num10; m++)
				{
					int num20 = rowIndexes[m];
					int num21 = columnStarts[num20];
					int num22 = num21 + array[num20] - 1;
					int num23 = num21;
					long num24 = 0L;
					int num25 = 0;
					for (num8 = num21; num8 <= num22; num8++)
					{
						int num26 = rowIndexes[num8];
						if (array2[num26] != 0)
						{
							int num27 = array2[num26] - mark;
							if (num27 > 0)
							{
								num25 += num27;
								rowIndexes[num23++] = num26;
								num24 += num26;
							}
							else
							{
								columnStarts[num26] = FactorizationExtensions.Flip(num4);
								array2[num26] = 0;
							}
						}
					}
					array[num20] = num23 - num21 + 1;
					int num28 = num23;
					int num29 = num21 + _count[num20];
					for (num8 = num22 + 1; num8 < num29; num8++)
					{
						int num30 = rowIndexes[num8];
						int num31;
						if ((num31 = _superCount[num30]) > 0)
						{
							num25 += num31;
							rowIndexes[num23++] = num30;
							num24 += num30;
						}
					}
					if (num25 == 0)
					{
						columnStarts[num20] = FactorizationExtensions.Flip(num4);
						int num15 = -_superCount[num20];
						num7 -= num15;
						num6 += num15;
						num3 += num15;
						_superCount[num20] = 0;
						array[num20] = -1;
					}
					else
					{
						_degree[num20] = Math.Min(_degree[num20], num25);
						rowIndexes[num23] = rowIndexes[num28];
						rowIndexes[num28] = rowIndexes[num21];
						rowIndexes[num21] = num4;
						_count[num20] = num23 - num21 + 1;
						num24 %= columnCount;
						_next[num20] = _hashHead[num24];
						_hashHead[num24] = num20;
						_hash[num20] = num24;
					}
				}
				_degree[num4] = num7;
				num = Math.Max(num, num7);
				mark = ClearWork(mark + num, num, array2, columnCount);
				RemoveSupernodes(columnStarts, rowIndexes, num9, num10, array, array2, ref mark);
				num8 = num9;
				for (int n = num9; n < num10; n++)
				{
					int num32 = rowIndexes[n];
					int num15;
					if ((num15 = -_superCount[num32]) > 0)
					{
						_superCount[num32] = num15;
						int val = _degree[num32] + num7 - num15;
						val = Math.Min(val, columnCount - num3 - num15);
						if (_head[val] != -1)
						{
							_last[_head[val]] = num32;
						}
						_next[num32] = _head[val];
						_last[num32] = -1;
						_head[val] = num32;
						_minDegree = Math.Min(_minDegree, val);
						_degree[num32] = val;
						rowIndexes[num8++] = num32;
					}
				}
				_superCount[num4] = num6;
				if ((_count[num4] = num8 - num9) == 0)
				{
					columnStarts[num4] = -1;
					array2[num4] = 0;
				}
				if (num5 != 0)
				{
					num2 = num8;
				}
			}
			Postorder(columnStarts, array2);
			Array.Resize(ref _order, _order.Length - 1);
		}

		private int FindPivot(int[] columnStarts, int n)
		{
			int num = -1;
			while (_minDegree < n && (num = _head[_minDegree]) == -1)
			{
				_minDegree++;
			}
			if (_next[num] != -1)
			{
				_last[_next[num]] = -1;
			}
			_head[_minDegree] = _next[num];
			return num;
		}

		private void CountColumnSlots(SparseMatrixDouble C)
		{
			_count = new int[C.ColumnCount + 1];
			for (int i = 0; i < C.ColumnCount; i++)
			{
				_count[i] = C.CountColumnSlots(i);
			}
			_count[_count.Length - 1] = 0;
		}

		private int InitDegree(int[] columnStarts, int[] elen, int[] work)
		{
			int num = columnStarts.Length - 1;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			for (int i = 0; i < num; i++)
			{
				int num5 = _degree[i];
				num4 = ((num4 < num5) ? num5 : num4);
				if (num5 == 0)
				{
					elen[i] = -2;
					num2++;
					columnStarts[i] = -1;
					work[i] = 0;
				}
				else if (num5 > _denseColumnThreshhold)
				{
					num3++;
					columnStarts[i] = FactorizationExtensions.Flip(num);
					_superCount[num]++;
					num2++;
					_superCount[i] = 0;
					elen[i] = -1;
				}
				else
				{
					if (_head[num5] != -1)
					{
						_last[_head[num5]] = i;
					}
					_next[i] = _head[num5];
					_head[num5] = i;
				}
			}
			return num2;
		}

		private static int CompressMatrix(int[] columnStarts, ref int[] rowIndexes, int[] count, int nonzeroCount, int newSpace)
		{
			for (int i = 0; i < columnStarts.Length - 1; i++)
			{
				int num = columnStarts[i];
				if (num >= 0)
				{
					columnStarts[i] = rowIndexes[num];
					rowIndexes[num] = FactorizationExtensions.Flip(i);
				}
			}
			int num2 = 0;
			int num3 = 0;
			while (num3 < nonzeroCount)
			{
				int num4 = FactorizationExtensions.Flip(rowIndexes[num3++]);
				if (num4 >= 0)
				{
					rowIndexes[num2] = columnStarts[num4];
					columnStarts[num4] = num2++;
					for (int j = 0; j < count[num4] - 1; j++)
					{
						rowIndexes[num2++] = rowIndexes[num3++];
					}
				}
			}
			nonzeroCount = num2;
			if (nonzeroCount + newSpace >= rowIndexes.Length)
			{
				Array.Resize(ref rowIndexes, Math.Max((int)((double)rowIndexes.Length * 1.5), rowIndexes.Length + newSpace));
			}
			return nonzeroCount;
		}

		/// <summary>Detect and remove supernodes.
		/// </summary>
		private void RemoveSupernodes(int[] columnStarts, int[] rowIndexes, int pStart, int pEnd, int[] elen, int[] work, ref int mark)
		{
			for (int i = pStart; i < pEnd; i++)
			{
				int num = rowIndexes[i];
				if (_superCount[num] >= 0)
				{
					continue;
				}
				long num2 = _hash[num];
				num = _hashHead[num2];
				_hashHead[num2] = -1;
				while (num != -1 && _next[num] != -1)
				{
					int num3 = elen[num];
					for (int j = columnStarts[num] + 1; j <= columnStarts[num] + _count[num] - 1; j++)
					{
						work[rowIndexes[j]] = mark;
					}
					int num4 = num;
					int num5 = _next[num];
					while (num5 != -1)
					{
						bool flag = _count[num5] == _count[num] && elen[num5] == num3;
						int j = columnStarts[num5] + 1;
						while (flag && j <= columnStarts[num5] + _count[num] - 1)
						{
							if (work[rowIndexes[j]] != mark)
							{
								flag = false;
							}
							j++;
						}
						if (flag)
						{
							columnStarts[num5] = FactorizationExtensions.Flip(num);
							_superCount[num] += _superCount[num5];
							_superCount[num5] = 0;
							elen[num5] = -1;
							num5 = _next[num5];
							_next[num4] = num5;
						}
						else
						{
							num4 = num5;
							num5 = _next[num5];
						}
					}
					num = _next[num];
					mark++;
				}
			}
		}

		/// <summary> Determine the final AMD ordering based on the assembly tree.  The assembly tree
		/// is stored in columnStarts and is a tree that consists of all nodes/elements that were chosen 
		/// during the AMD procedure.  Nodes and elements that were absorbed into supernodes are not present
		/// in the tree.  Therefore we need to add them to the assembly tree before actually performing
		/// the postordering.  The postordering permutes the ordering in a way that preserves the nonzero
		/// structure, but gives better structure to the factor.
		/// </summary>
		private void Postorder(int[] columnStarts, int[] work)
		{
			for (int i = 0; i < columnStarts.Length - 1; i++)
			{
				columnStarts[i] = FactorizationExtensions.Flip(columnStarts[i]);
			}
			_head.ConstantFill(-1);
			for (int num = columnStarts.Length - 1; num >= 0; num--)
			{
				if (_superCount[num] <= 0)
				{
					_next[num] = _head[columnStarts[num]];
					_head[columnStarts[num]] = num;
				}
			}
			for (int num2 = columnStarts.Length - 1; num2 >= 0; num2--)
			{
				if (_superCount[num2] > 0 && columnStarts[num2] != -1)
				{
					_next[num2] = _head[columnStarts[num2]];
					_head[columnStarts[num2]] = num2;
				}
			}
			int k = 0;
			for (int j = 0; j < columnStarts.Length; j++)
			{
				if (columnStarts[j] == -1)
				{
					k = FactorizationExtensions.TraverseDFS(j, k, _head, _next, _order, work);
				}
			}
		}

		private static int ClearWork(int mark, int lemax, int[] work, int n)
		{
			if (mark < 2 || mark + lemax < 0)
			{
				for (int i = 0; i < n; i++)
				{
					if (work[i] != 0)
					{
						work[i] = 1;
					}
					mark = 2;
				}
			}
			return mark;
		}
	}
}
