using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> The SymmetricFactorWorkspace is a base class for problems which
	///           construct symmetric matrices and then factorizes them.
	/// </summary>
	internal abstract class SymmetricFactorWorkspace
	{
		/// <summary> When a zero pivot is discovered, the remedy depends upon the usage.
		/// </summary>
		public delegate double PivotPolicy(int col, double value, List<SparseVectorCell> perturbations);

		/// <summary> A context object used to control symbolic factorization threads
		/// </summary>
		internal class CholeskyParallelizer : Parallelizer
		{
			internal List<SparseVectorCell>[] perturbations;

			/// <summary> A context object used to control symbolic factorization threads
			/// </summary>
			internal CholeskyParallelizer()
			{
				perturbations = new List<SparseVectorCell>[base.ThreadCount];
				for (int i = 0; i < base.ThreadCount; i++)
				{
					perturbations[i] = new List<SparseVectorCell>();
				}
			}
		}

		/// <summary> The current set of coeffs in the workspace
		/// </summary>
		internal SparseMatrixDouble _M;

		/// <summary> The largest count of non-zeros in any column
		/// </summary>
		private int _maxColumnCount;

		/// <summary> If the matrix becomes dense we can switch algorithms for speed against space
		/// </summary>
		private int _firstDenseColumn;

		/// <summary> Diagonal inner array for L D L^T = M.  Values +1 or -1.  Indexed by Inner row/col!
		/// </summary>
		internal sbyte[] _diagonalFactor;

		/// <summary> The columns and values perturbed in M when forming Cholesky
		/// </summary>
		internal List<SparseVectorCell> _perturbList;

		/// <summary> This async callback returns true if a task needs to be stopped,
		///           for example by timeout, or if an exception in one thread needs
		///           to stop all the others.
		/// </summary>
		internal Func<bool> CheckAbort;

		/// <summary>Factorization parameters.
		/// </summary>
		public FactorizationParameters Parameters { get; set; }

		/// <summary> Count of columns in the workspace
		/// </summary>
		public int ColumnCount => _M.ColumnCount;

		/// <summary> Permutation from user's column to internal column
		/// </summary>
		public int[] OuterToInner { get; internal set; }

		/// <summary> Permutation from internal column to user's column
		/// </summary>
		public int[] InnerToOuter { get; internal set; }

		/// <summary> If the matrix becomes dense we can switch algorithms for speed against space
		/// </summary>
		private bool HasDenseColumns => _firstDenseColumn < ColumnCount;

		/// <summary> When a zero pivot is discovered, the remedy depends upon the usage.
		/// </summary>
		public PivotPolicy RefinePivot { get; set; }

		/// <summary> Count the non-zeros in a factor.
		/// </summary>
		public long Count => _M.Count;

		/// <summary> Initialize map indexes to -1 to trap misuse
		/// </summary>
		/// <param name="map"></param>
		[Conditional("DEBUG")]
		public static void SetMapHazard(int[] map)
		{
		}

		/// <summary> This hash function is used to cluster columns for supernode discovery
		/// </summary>
		internal static void Rehash(ref int a, int b)
		{
			a = (a << 19) ^ b ^ (int)((uint)a >> 13);
		}

		/// <summary> Quick and simple string view of one column
		/// </summary>
		internal string DisplayColumn(int col)
		{
			StringBuilder stringBuilder = new StringBuilder();
			int i = _M._columnStarts[col];
			int num = _M._columnStarts[col + 1];
			if (i < num)
			{
				stringBuilder.Append('[').Append(col).Append("]: ");
				for (; i < num; i++)
				{
					stringBuilder.Append(_M._rowIndexes[i]).Append(' ').Append(_M._values[i])
						.Append(", ");
				}
			}
			return stringBuilder.ToString();
		}

		/// <summary> Quick and simple string view, column-by-column, in internal order
		/// </summary>
		internal void DisplayColumns()
		{
			for (int i = 0; i < ColumnCount; i++)
			{
				string text = DisplayColumn(i);
				Console.WriteLine(text.ToString());
			}
			Console.WriteLine();
		}

		/// <summary> Quick and simple string view, column-by-column, in client's order.
		/// </summary>
		internal void DisplayMappedColumns()
		{
			for (int i = 0; i < ColumnCount; i++)
			{
				string text = DisplayColumn(OuterToInner[i]);
				Console.WriteLine(text.ToString());
			}
			Console.WriteLine();
		}

		internal void DisplayDiagonal()
		{
			for (int i = 0; i < ColumnCount; i++)
			{
				Console.WriteLine("[{0}]:{1}", i, _M[i, i]);
			}
		}

		/// <summary> Scan the diagonal and get the binary exponents limited to 0..255.
		/// </summary>
		/// <returns></returns>
		internal byte[] DiagonalExponents()
		{
			byte[] array = new byte[ColumnCount];
			for (int i = 0; i < array.Length; i++)
			{
				int num = 128 + new DoubleRaw(_M[i, i]).Exponent;
				array[InnerToOuter[i]] = (byte)((255 < num) ? 255u : ((num >= 0) ? ((uint)num) : 0u));
			}
			return array;
		}

		/// <summary>Performs the symbolic factorization for a matrix.
		/// </summary>
		/// <param name="PlanProductFillIn">A method that plans the fill-in.</param>
		/// <param name="columnCount">Column count for factorized matrix.</param>
		public void Factorize(Func<double[]> PlanProductFillIn, int columnCount)
		{
			double[] colWeights = PlanProductFillIn();
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			SymbolicFactorWorkspace symbolicFactorWorkspace;
			switch (Parameters.FactorizationMethod)
			{
			case SymbolicFactorizationMethod.LocalFill:
				symbolicFactorWorkspace = new MinimizeLocalFillWorkspace(_M, colWeights, Parameters, CheckAbort, InnerToOuter, columnCount);
				break;
			case SymbolicFactorizationMethod.Automatic:
			case SymbolicFactorizationMethod.AMD:
				symbolicFactorWorkspace = new ApproximateMinDegreeWorkspace(_M, colWeights, Parameters, CheckAbort);
				break;
			default:
				throw new NotImplementedException();
			}
			SymbolicFactorResult symbolicFactorResult = symbolicFactorWorkspace.Factorize();
			symbolicFactorWorkspace = null;
			_firstDenseColumn = symbolicFactorResult.FirstDenseColumn;
			_maxColumnCount = symbolicFactorResult.MaxColumnCount;
			stopwatch.Stop();
			InnerToOuter = symbolicFactorResult.InnerToOuter;
			OuterToInner = symbolicFactorResult.OuterToInner;
			_M._T = new SparseTransposeIndexes<double>(_M, _M.RowCounts());
		}

		/// <summary> The SymmetricFactorWorkspace is a base class for problems which
		///           construct symmetric matrices and then factorize them.
		/// </summary>
		/// <param name="rowColumnCount"> the rows count == columns count </param>
		/// <param name="factorParam">Symbolic factorization parameters.</param>
		internal SymmetricFactorWorkspace(int rowColumnCount, FactorizationParameters factorParam)
		{
			Parameters = factorParam;
			_M = new SparseMatrixDouble(rowColumnCount, rowColumnCount);
		}

		/// <summary> Construct a non-permuted copy of the current working matrix.
		///           It will be trimmed to skip zeroes.
		/// </summary>
		public LowerSparseMatrix UnpermutedLower()
		{
			LowerSparseMatrix lowerSparseMatrix = new LowerSparseMatrix(ColumnCount, (int)_M.Count, 1.0);
			int num = 0;
			for (int i = 0; i < ColumnCount; i++)
			{
				int num2 = num;
				lowerSparseMatrix._columnStarts[i] = num2;
				int column = OuterToInner[i];
				SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_M, column);
				while (colIter.IsValid)
				{
					int num3 = colIter.Row(_M);
					double num4 = colIter.Value(_M);
					if (0.0 != num4)
					{
						lowerSparseMatrix._values[num] = num4;
						lowerSparseMatrix._rowIndexes[num] = InnerToOuter[num3];
						num++;
					}
					colIter.Advance();
				}
				if (num2 + 1 < num)
				{
					Array.Sort(lowerSparseMatrix._rowIndexes, lowerSparseMatrix._values, num2, num - num2);
				}
			}
			lowerSparseMatrix._columnStarts[ColumnCount] = num;
			if (num < _M.Count)
			{
				double[] array = new double[num];
				Array.Copy(lowerSparseMatrix._values, array, num);
				lowerSparseMatrix._values = array;
				int[] array2 = new int[num];
				Array.Copy(lowerSparseMatrix._rowIndexes, array2, num);
				lowerSparseMatrix._rowIndexes = array2;
			}
			return lowerSparseMatrix;
		}

		/// <summary> Compute the Cmod and Cdiv operations for a single column.
		///           Use left-look in a multi-thread environment.
		/// </summary>
		/// <param name="jCol"> the column to be recalculated </param>
		/// <param name="lastRow"> the last row value is overwritten in indexes[] so true value is passed here </param>
		/// <param name="map"> a per-thread scratch array to be used for mapping into the array </param>
		/// <param name="sums"> the new values accumulated for the column </param>
		/// <param name="perturbations"> tracks perturbation information for use in pivot decisions </param>
		private void CholeskyLeftCmodCdivSparse(int jCol, int lastRow, int[] map, double[] sums, List<SparseVectorCell> perturbations)
		{
			int num = 0;
			SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_M, jCol);
			while (colIter.IsValid)
			{
				sums[num] = colIter.Value(_M);
				int num2 = colIter.Row(_M);
				if (num2 < 0)
				{
					num2 = lastRow;
				}
				map[num2] = num++;
				colIter.Advance();
			}
			BigSum bigSum = sums[0];
			SparseMatrixByColumn<double>.RowIter rowIter = new SparseMatrixByColumn<double>.RowIter(_M._T, jCol);
			while (rowIter.IsValid)
			{
				int num3 = rowIter.Column(_M._T);
				if (num3 == jCol)
				{
					break;
				}
				WaitUntilReady(num3);
				SparseMatrixByColumn<double>.ColIter colIter2 = new SparseMatrixByColumn<double>.ColIter(_M, num3, jCol);
				double num4 = (double)_diagonalFactor[num3] * colIter2.Value(_M);
				bigSum.Sub(num4 * colIter2.Value(_M));
				colIter2.Advance();
				while (colIter2.IsValid)
				{
					sums[map[colIter2.Row(_M)]] -= num4 * colIter2.Value(_M);
					colIter2.Advance();
				}
				rowIter.Advance();
			}
			num = 0;
			double value = (sums[0] = RefinePivot(jCol, bigSum.ToDouble(), perturbations));
			_diagonalFactor[jCol] = (sbyte)Math.Sign(value);
			value = (double)_diagonalFactor[jCol] * Math.Sqrt(Math.Abs(value));
			SparseMatrixByColumn<double>.ColIter colIter3 = new SparseMatrixByColumn<double>.ColIter(_M, jCol);
			while (colIter3.IsValid)
			{
				_M._values[colIter3.Slot] = sums[num] / value;
				int num5 = colIter3.Row(_M);
				if (num5 < 0)
				{
					num5 = lastRow;
				}
				map[num5] = -1;
				num++;
				colIter3.Advance();
			}
		}

		/// <summary> Compute the Cmod and Cdiv operations for a single column.
		///           Use left-look.
		/// </summary>
		/// <param name="jCol"> the column to be recalculated </param>
		/// <param name="sums"> the new values accumulated for the column </param>
		/// <param name="perturbations"> tracks perturbation information for use in pivot decisions </param>
		private void CholeskyLeftCmodCdivDense(int jCol, double[] sums, List<SparseVectorCell> perturbations)
		{
			int num = 0;
			SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_M, jCol);
			while (colIter.IsValid)
			{
				sums[num++] = colIter.Value(_M);
				colIter.Advance();
			}
			BigSum bigSum = sums[0];
			SparseMatrixByColumn<double>.RowIter rowIter = new SparseMatrixByColumn<double>.RowIter(_M._T, jCol);
			while (rowIter.IsValid)
			{
				int num2 = rowIter.Column(_M._T);
				if (num2 == jCol)
				{
					break;
				}
				WaitUntilReady(num2);
				if (num2 >= _firstDenseColumn)
				{
					int num3 = _M._columnStarts[num2] + jCol - num2;
					double num4 = (double)_diagonalFactor[num2] * _M._values[num3];
					bigSum.Sub(num4 * _M._values[num3]);
					num3++;
					num = 1;
					while (num3 < _M._columnStarts[num2 + 1])
					{
						sums[num] -= num4 * _M._values[num3];
						num3++;
						num++;
					}
				}
				else
				{
					int slot = new SparseMatrixByColumn<double>.ColIter(_M, num2, jCol).Slot;
					double num5 = (double)_diagonalFactor[num2] * _M._values[slot];
					bigSum.Sub(num5 * _M._values[slot]);
					for (slot++; slot < _M._columnStarts[num2 + 1]; slot++)
					{
						sums[_M._rowIndexes[slot] - jCol] -= num5 * _M._values[slot];
					}
				}
				rowIter.Advance();
			}
			num = 0;
			double value = (sums[0] = RefinePivot(jCol, bigSum.ToDouble(), perturbations));
			_diagonalFactor[jCol] = (sbyte)Math.Sign(value);
			value = (double)_diagonalFactor[jCol] * Math.Sqrt(Math.Abs(value));
			SparseMatrixByColumn<double>.ColIter colIter2 = new SparseMatrixByColumn<double>.ColIter(_M, jCol);
			while (colIter2.IsValid)
			{
				_M._values[colIter2.Slot] = sums[num++] / value;
				colIter2.Advance();
			}
		}

		/// <summary> We use a word of the row indexes as a synchronization flag since this
		///           is both nicely scattered to minimize cache ping-pong and must be
		///           brought into cache anyway.
		/// </summary>
		private void MakeColumnsNotReady()
		{
			for (int i = 1; i <= ColumnCount; i++)
			{
				_M._rowIndexes[_M._columnStarts[i] - 1] ^= -1;
			}
		}

		/// <summary> Wait until a column we depend on has been processed by another thread.
		/// </summary>
		/// <param name="col"> the column we depend on </param>
		private void WaitUntilReady(int col)
		{
			if ((0xF & col) == 0 && (_M.Parallel.Failed || CheckAbort()))
			{
				throw new TimeLimitReachedException();
			}
			while (_M._rowIndexes[_M._columnStarts[col + 1] - 1] < 0)
			{
				Thread.Sleep(0);
				if (_M.Parallel.Failed || CheckAbort())
				{
					throw new TimeLimitReachedException();
				}
			}
		}

		/// <summary> The left-looking Cholesky formulated for multiple threads.
		/// </summary>
		/// <param name="state"> the thread state to be used to guide this thread </param>
		private void CholeskyLeftThread(object state)
		{
			ThreadState threadState = state as ThreadState;
			int[] map = new int[ColumnCount];
			double[] sums = new double[HasDenseColumns ? ColumnCount : _maxColumnCount];
			CholeskyParallelizer choleskyParallelizer = _M.Parallel as CholeskyParallelizer;
			bool flag = true;
			for (int i = 0; i < ColumnCount; i++)
			{
				if (!flag)
				{
					break;
				}
				int num = _M._rowIndexes[_M._columnStarts[i + 1] - 1];
				if (num < 0 && num != int.MinValue)
				{
					int num2 = Interlocked.CompareExchange(ref _M._rowIndexes[_M._columnStarts[i + 1] - 1], int.MinValue, num);
					if (num == num2)
					{
						flag = false;
						try
						{
							if (i < _firstDenseColumn)
							{
								CholeskyLeftCmodCdivSparse(i, ~num, map, sums, choleskyParallelizer.perturbations[threadState.threadIndex]);
							}
							else
							{
								CholeskyLeftCmodCdivDense(i, sums, choleskyParallelizer.perturbations[threadState.threadIndex]);
							}
							flag = true;
						}
						catch (TimeLimitReachedException value)
						{
							Interlocked.CompareExchange(ref _M.Parallel.innerException, value, null);
						}
						finally
						{
							if (!flag)
							{
								_M.Parallel.Failed = true;
							}
						}
						_M._rowIndexes[_M._columnStarts[i + 1] - 1] = ~num;
					}
				}
				if ((i & 0xF) == 0 && CheckAbort())
				{
					break;
				}
			}
			threadState.izer.FinishWorkItem(threadState.threadIndex);
		}

		/// <summary> Compute the Cholesky from the combined A D A* values.
		/// <remarks> Initializing the problem matrix is a prerequisite </remarks>
		/// </summary>
		/// <exception cref="T:System.DivideByZeroException"> Divide by zero may occur due to pivot policy </exception>
		/// <exception cref="T:System.TimeoutException"> Timeout may occur if CheckAbort reports true </exception>
		public void Cholesky()
		{
			_diagonalFactor = new sbyte[ColumnCount];
			MakeColumnsNotReady();
			CholeskyParallelizer choleskyParallelizer = new CholeskyParallelizer();
			_M.Parallel = choleskyParallelizer;
			bool flag = _M.Parallel.Run(CholeskyLeftThread, (int)(_M.Count / 5000));
			Exception innerException = _M.Parallel.innerException;
			_M.Parallel = null;
			if (CheckAbort())
			{
				throw new TimeLimitReachedException();
			}
			if (!flag)
			{
				throw new MsfException(Resources.ExceptionThrownFromCholeskyThread, innerException);
			}
			_perturbList = new List<SparseVectorCell>();
			for (int i = 0; i < choleskyParallelizer.ThreadCount; i++)
			{
				_perturbList.AddRange(choleskyParallelizer.perturbations[i]);
			}
			if (0 < _perturbList.Count)
			{
				_perturbList.Sort((SparseVectorCell left, SparseVectorCell right) => left.Index - right.Index);
			}
		}

		/// <summary> Solve Lx = y for x
		/// <code>
		///   y[0] = y[0] / L[0,0]
		///   for i = 1:n-1
		///     y[i] = (y[i] - L[i,0:i-1]*y[0:i-1])/L[i,i]
		/// </code>
		/// see Golub &amp; Van Loan, 3.1.1, modified not to overwrite y.
		/// <remarks> A complete solve may be written:
		///           x = BackwardSolve(ForwardSolve(y))
		/// </remarks>
		/// </summary>
		/// <param name="y"> rhs known values </param>
		/// <param name="reals"> space to accumulate Real parts </param>
		/// <returns> a new x </returns>
		internal Vector ForwardSolve(Vector y, double[] reals)
		{
			Vector vector = new Vector(y.Length);
			ForwardSolve(y, vector, reals);
			return vector;
		}

		public void ForwardSolve(Vector y, Vector x, double[] reals)
		{
			SparseMatrixByColumn<double>.RowSlots rowSlots = new SparseMatrixByColumn<double>.RowSlots(_M);
			double num = rowSlots.ValueAdvance(_M, 0, 0);
			x[0] = y[InnerToOuter[0]] / num;
			for (int i = 1; i < _M.RowCount; i++)
			{
				reals[0] = y[InnerToOuter[i]];
				int num2 = 1;
				double num3 = 0.0;
				SparseMatrixByColumn<double>.RowIter rowIter = new SparseMatrixByColumn<double>.RowIter(_M._T, i);
				while (rowIter.IsValid)
				{
					int num4 = rowIter.Column(_M._T);
					num3 = rowSlots.ValueAdvance(_M, i, num4);
					if (num4 < i && 0.0 != x[num4])
					{
						reals[num2++] = (0.0 - x[num4]) * num3;
					}
					rowIter.Advance();
				}
				double num5;
				if (num2 <= 2)
				{
					num5 = reals[0];
					if (num2 == 2)
					{
						num5 += reals[1];
					}
				}
				else
				{
					BigSum bigSum = reals[0];
					for (int j = 1; j < num2; j++)
					{
						bigSum.Add(reals[j]);
					}
					num5 = bigSum.ToDouble();
				}
				x[i] = num5 / num3;
			}
		}

		public void ForwardSolve(Matrix y, Matrix r)
		{
			y.VerifySameShape(r);
			Vector vector = new Vector(y.RowCount);
			Vector vector2 = new Vector(r.RowCount);
			double[] reals = new double[y.RowCount];
			for (int i = 0; i < y.ColumnCount; i++)
			{
				y.CopyColumnTo(vector, i);
				ForwardSolve(vector, vector2, reals);
				r.FillColumn(i, 0, 1.0, vector2);
			}
		}

		public int ForwardSolve(Vector y, Vector r)
		{
			y.VerifySameLength(r);
			double[] reals = new double[y.Length];
			ForwardSolve(y, r, reals);
			return 0;
		}

		/// <summary> Solve Ux = y for x, U is upper triangular (== L-transpose)
		/// <code>
		///   y[n-1] = y[n-1] / U[n-1,n-1]
		///   for i = n-2:0
		///     y[i] = (y[i] - U[i,i+1:n-1]*y[i+1:n-1])/U[i,i]
		/// </code>
		/// <remarks> A complete solve may be written:
		///           x = BackSolve(ForwardSolve(z))
		/// </remarks>
		/// </summary>
		/// see Golub &amp; Van Loan, 3.1.2
		/// <param name="y"> y is overwritten with the result </param>
		/// <param name="reals"> space to accumulate Real parts </param>
		/// <returns> the overwritten y </returns>
		internal Vector BackwardSolve(Vector y, double[] reals)
		{
			int num = ColumnCount;
			while (0 <= --num)
			{
				reals[0] = y[num];
				int num2 = 1;
				SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_M, num);
				double num3 = colIter.Value(_M);
				colIter.Advance();
				while (colIter.IsValid)
				{
					int i = colIter.Row(_M);
					if (0.0 != y[i])
					{
						reals[num2++] = (0.0 - y[i]) * colIter.Value(_M);
					}
					colIter.Advance();
				}
				double num4;
				if (num2 <= 2)
				{
					num4 = reals[0];
					if (num2 == 2)
					{
						num4 += reals[1];
					}
				}
				else
				{
					BigSum bigSum = reals[0];
					for (int j = 1; j < num2; j++)
					{
						bigSum.Add(reals[j]);
					}
					num4 = bigSum.ToDouble();
				}
				y[num] = num4 / num3;
			}
			y.Permute(OuterToInner);
			return y;
		}

		public int BackwardSolve(Vector y, Vector r)
		{
			y.VerifySameLength(r);
			r.CopyFrom(y);
			double[] reals = new double[r.Length];
			BackwardSolve(r, reals);
			return 0;
		}

		public int DiagonalSolve(Vector x, Vector y)
		{
			for (int i = 0; i < x.Length; i++)
			{
				y[i] = x[i] * (double)_diagonalFactor[i];
			}
			return 0;
		}

		public int DiagonalSolve(Matrix x, Matrix y)
		{
			for (int i = 0; i < x.ColumnCount; i++)
			{
				for (int j = 0; j < x.RowCount; j++)
				{
					y[j, i] = x[j, i] * (double)_diagonalFactor[j];
				}
			}
			return 0;
		}

		/// <summary> Solve FactoredSymmetricM * x = z for unknown x
		/// </summary>
		/// <returns> x </returns>
		public abstract Vector Solve(Vector y);
	}
}
