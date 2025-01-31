using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> An inexact minimum local fill ordering algorithm.
	/// </summary>
	/// <remarks>
	/// Inexact minimum local fill uses the External Degree ordering as a starting point.
	/// This is a variation on the technique described by Meszaros in 1996.
	/// </remarks>
	internal class MinimizeLocalFillWorkspace : SymbolicFactorWorkspace
	{
		/// <summary> SymbolicCount[ColumnCount] array is used for minimum fill work
		/// </summary>
		internal struct SymbolicColumn
		{
			/// <summary> the number of entries used in _indexes.
			/// </summary>
			public int _indexCount;

			/// <summary> the row indexes current for this column
			/// </summary>
			public int[] _indexes;

			/// <summary> List of outer columns belonging to supernode, represented by this one.
			///           The others just ride along until placement then follow the same pattern.
			/// </summary>
			public int[] _superNode;

			/// <summary> The most dense columns are tracked with a bit map
			/// </summary>
			public int _mapBit;

			/// <summary> Use the count of nodes taken to label when this node was last fill-calculated
			/// </summary>
			public int _lastTaken;

			/// <summary> Exact nearest fill, -1 if not up to date.  Only small values are closely
			///           inspected, for big values it means "we don't want this column yet", so
			///           save space by using a float.
			/// </summary>
			public float _fill;

			/// <summary> Each column may pass through stages while order is being calculated.
			/// </summary>
			public SymbolicStage _stage;

			/// <summary> A handicap is used to control stable pivot order.
			/// </summary>
			public sbyte _handicap;

			/// <summary> 1 + _superNode.Length
			/// </summary>
			public byte _degree;

			public override string ToString()
			{
				return string.Format(CultureInfo.InvariantCulture, "[{0}] {1}", new object[2] { _indexCount, _fill });
			}
		}

		/// <summary> Each column may pass through stages while order is being calculated.
		/// <remarks>
		///           Ordering is important:
		///           - pending column states must be less than Candidate
		///           - Candidate less than Taken
		///           - Taken less than Super
		/// </remarks>
		/// </summary>
		internal enum SymbolicStage : byte
		{
			/// <summary> The initial stage for most columns
			/// </summary>
			Default,
			/// <summary> A column for which final placement has been decided
			/// </summary>
			Taken,
			/// <summary> A column which is part of the supernodal set of some other column
			/// </summary>
			Super
		}

		/// <summary> A context object used to control symbolic factorization threads
		/// </summary>
		private class CalculateFillParallelizer : Parallelizer
		{
			private Action<int, byte[]> _CalcFill;

			internal byte[][] _inUse;

			/// <summary> Deferred fill calculations are multithreaded
			/// </summary>
			internal List<int> _deferredFill;

			/// <summary> Variable used to coordinate multithreaded calculation
			/// </summary>
			private int _prevDeferredFill;

			internal CalculateFillParallelizer(int columnCount, Action<int, byte[]> CalcFill)
			{
				_CalcFill = CalcFill;
				_deferredFill = new List<int>();
				_inUse = new byte[base.ThreadCount][];
				for (int i = 0; i < base.ThreadCount; i++)
				{
					_inUse[i] = new byte[columnCount];
				}
			}

			internal void CalcFillAction(object state)
			{
				ThreadState threadState = state as ThreadState;
				int threadIndex = threadState.threadIndex;
				int index;
				while (!Failed && (index = Interlocked.Increment(ref _prevDeferredFill)) < _deferredFill.Count)
				{
					_CalcFill(_deferredFill[index], _inUse[threadIndex]);
				}
				threadState.izer.FinishWorkItem(threadState.threadIndex);
			}

			/// <summary> Run deferred fill calculations
			/// </summary>
			internal void RunDeferred()
			{
				_prevDeferredFill = -1;
				Run(CalcFillAction, _deferredFill.Count / 2);
				_deferredFill.Clear();
			}
		}

		/// <summary> [outerCol].  if not null, influences stable permutation order.
		///   A value of -128 indicates a zero diagonal.
		///   Other values are capped at -127..127 representing the binary exponent of the diagonal element.
		///   A 0 value (default) is thus for a diagonal value in the range 0.5 .. 1.0.
		/// </summary>
		private sbyte[] _diagonalExponents;

		/// <summary> The largest count of non-zeros in any column
		/// </summary>
		private int _maxColumnCount;

		/// <summary> If the matrix becomes dense we can switch algorithms for speed against space
		/// </summary>
		private int _firstDenseColumn;

		/// <summary> Permutation from user's column to internal column
		/// </summary>
		private int[] OuterToInner;

		/// <summary> Permutation from internal colum to user's column
		/// </summary>
		private int[] InnerToOuter;

		/// <summary> [ColumnCount] During symbolic phase, track symbolic work on columns and supernodes
		/// </summary>
		private SymbolicColumn[] _symbolics;

		/// <summary> The most dense columns are tracked with a bit position in these maps.
		/// </summary>
		private List<int[]> _denseMaps;

		/// <summary> Only one thread may extend the maps at a time
		/// </summary>
		private int _denseMapLock;

		/// <summary> The dense columns are tracked with a bit position in this map.
		/// </summary>
		private int _denseMapCount;

		/// <summary> The Symbolics are sorted in minimum degree order from which the low become candidates
		/// </summary>
		private SortedDictionary<double, int> _minDegrees;

		private int _columnCount;

		/// <summary> Count of columns in the workspace
		/// </summary>
		public int ColumnCount => _M.ColumnCount;

		/// <summary> Create a new instance.
		/// </summary>
		public MinimizeLocalFillWorkspace(SparseMatrixDouble M, double[] colWeights, FactorizationParameters factorizationParameters, Func<bool> abort, int[] innerToOuter, int columnCount)
			: base(M, colWeights, factorizationParameters, abort)
		{
			InnerToOuter = innerToOuter;
			_colWeights = colWeights;
			_columnCount = columnCount;
		}

		/// <summary>Computes the symbolic factorization, modifying M.
		/// </summary>
		/// <returns>Symbolic factorization information, including mapping from original to permuted columns.</returns>
		public override SymbolicFactorResult Factorize()
		{
			_diagonalExponents = new sbyte[_columnCount];
			for (int i = 0; i < _diagonalExponents.Length; i++)
			{
				_diagonalExponents[i] = 0;
			}
			CreateSymbolics(_colWeights);
			_colWeights = null;
			_M._rowIndexes = null;
			int[] takenOrder = PlanBestOrder();
			FinalizeNewOrder(takenOrder, out var columnIndexes, out var isSupernodal);
			BuildCholeskyLayout(columnIndexes, isSupernodal);
			columnIndexes = null;
			isSupernodal = null;
			_M._values = new double[_M.Count];
			SymbolicFactorResult symbolicFactorResult = new SymbolicFactorResult();
			symbolicFactorResult.InnerToOuter = InnerToOuter;
			symbolicFactorResult.OuterToInner = OuterToInner;
			symbolicFactorResult.MaxColumnCount = _maxColumnCount;
			symbolicFactorResult.FirstDenseColumn = _firstDenseColumn;
			return symbolicFactorResult;
		}

		/// <summary> Verify the equivalence of the fill pattern of two columns.
		///           Up to now we used a hash in the weights, but that is subject
		///           to false matches.  This is used to eliminate the false matches.
		/// </summary>
		internal bool VerifyEquivalence(int newColA, int newColB)
		{
			SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_M, InnerToOuter[newColA]);
			SparseMatrixByColumn<double>.ColIter colIter2 = new SparseMatrixByColumn<double>.ColIter(_M, InnerToOuter[newColB]);
			while (colIter.IsValid && colIter2.IsValid)
			{
				if (colIter.Row(_M) != colIter2.Row(_M))
				{
					return false;
				}
				colIter.Advance();
				colIter2.Advance();
			}
			if (!colIter.IsValid)
			{
				return !colIter2.IsValid;
			}
			return false;
		}

		/// <summary> The first published and still best static estimate of fill degree.
		///           Markowitz degree is (colCount-1) * (rowCount-1) but in the case
		///           of symmetric matrix we can use just (colCount-1) called "true degree".
		/// </summary>
		/// <returns></returns>
		internal int TrueDegree(int col)
		{
			return _M.CountColumnSlots(col) - 1;
		}

		/// <summary> Calculate the fill weight to be attributed to a handicap
		/// </summary>
		private double EvaluateHandicap(int handicap, int indexLength)
		{
			if (127 != handicap)
			{
				return (double)handicap / 1.0;
			}
			return ColumnCount * Math.Min(128, indexLength - 1);
		}

		/// <summary> count the neighbor-fill it will cause
		/// </summary>
		/// <param name="col"> the index of the candidate </param>
		/// <param name="inUse">  map of rows used by current candidate </param>
		private void CalculateFill(int col, byte[] inUse)
		{
			int[] indexes = _symbolics[col]._indexes;
			int indexCount = _symbolics[col]._indexCount;
			double num = 0.0;
			if (2 < indexCount)
			{
				for (int i = 0; i < indexCount; i++)
				{
					inUse[indexes[i]] = 1;
				}
				for (int j = 0; j < indexCount; j++)
				{
					int num2 = indexes[j];
					int indexCount2 = _symbolics[num2]._indexCount;
					if (col == num2 || (int)_symbolics[num2]._stage >= 1)
					{
						continue;
					}
					int mapBit = _symbolics[num2]._mapBit;
					int num3;
					if (mapBit != 0 && indexCount < indexCount2)
					{
						num3 = indexCount;
						int[] array = _denseMaps[mapBit - 1 >> 5];
						int num4 = (mapBit - 1) & 0x1F;
						for (int k = 0; k < indexCount; k++)
						{
							num3 -= 1 & (array[indexes[k]] >> num4);
						}
					}
					else
					{
						int[] indexes2 = _symbolics[num2]._indexes;
						num3 = indexCount;
						if (256 < indexCount2 && mapBit == 0 && Interlocked.Exchange(ref _denseMapLock, 1) == 0)
						{
							if (_denseMaps == null)
							{
								_denseMaps = new List<int[]>();
							}
							if ((_denseMapCount & 0x1F) == 0)
							{
								_denseMaps.Add(new int[ColumnCount]);
							}
							int[] array2 = _denseMaps[_denseMapCount >> 5];
							int num5 = 1 << _denseMapCount;
							for (int l = 0; l < indexCount2; l++)
							{
								int num6 = indexes2[l];
								array2[num6] |= num5;
								num3 -= inUse[num6];
							}
							_denseMapCount++;
							_symbolics[num2]._mapBit = _denseMapCount;
							Interlocked.Exchange(ref _denseMapLock, 0);
						}
						else
						{
							for (int m = 0; m < indexCount2; m++)
							{
								int num7 = indexes2[m];
								num3 -= inUse[num7];
							}
						}
					}
					num += (double)(num3 * _symbolics[num2]._degree);
				}
				for (int n = 0; n < indexCount; n++)
				{
					inUse[indexes[n]] = 0;
				}
			}
			double num8 = EvaluateHandicap(_symbolics[col]._handicap, _symbolics[col]._indexCount);
			num = ((!(0.0 <= num8)) ? Math.Max(3.0, num + num8) : (num + num8));
			_symbolics[col]._fill = (float)num;
		}

		/// <summary> count the neighbor-fill it will cause
		/// </summary>
		private int CalculateFillDeferred(int col, CalculateFillParallelizer FillIzer, int limit)
		{
			if (_symbolics[col]._indexCount - _symbolics[col]._degree < 2)
			{
				_symbolics[col]._fill = (float)EvaluateHandicap(Math.Max(0, (int)_symbolics[col]._handicap), _symbolics[col]._indexCount);
			}
			else if (FillIzer._deferredFill.Count == 0)
			{
				FillIzer._deferredFill.Add(col);
				limit -= _symbolics[col]._indexCount - _symbolics[col]._degree;
			}
			else if (0 < limit)
			{
				int num = FillIzer._deferredFill[0];
				int num2 = _symbolics[num]._indexCount - _symbolics[num]._degree;
				num2 += num2 / 8 + 2;
				int num3 = _symbolics[col]._indexCount - _symbolics[col]._degree;
				if (num3 <= num2)
				{
					limit -= num3;
					FillIzer._deferredFill.Add(col);
				}
			}
			return limit;
		}

		/// <summary> MinDegree calculates the keys used to order the minDegree sorted dictionary
		/// </summary>
		private double MinDegree(int indexLength, int nodeDegree, int handicap, int col)
		{
			if (127 == handicap)
			{
				handicap = ColumnCount * Math.Min(64, indexLength - 1);
			}
			return (double)((indexLength - nodeDegree) * 8 + handicap) + (double)col / (double)ColumnCount;
		}

		/// <summary> recalculate the external degree for otherCol after a col has been taken
		/// </summary>
		/// <param name="otherCol"> The column affected by taken columns </param>
		/// <param name="prior"> The prior count of indexes for otherSym </param>
		/// <param name="revised"> The new count of indexes for otherSym </param>
		private void UpdateMinDegree(int otherCol, int prior, int revised)
		{
			int num = _symbolics[otherCol]._handicap;
			if (prior != revised || 127 == num || !(_symbolics[otherCol]._fill < 0f))
			{
				int degree = _symbolics[otherCol]._degree;
				if (!_minDegrees.Remove(MinDegree(prior, degree, num, otherCol)))
				{
					throw new MsfException(Resources.MinimumDegreeKeyUnmatched);
				}
				if (127 == num)
				{
					num = (_symbolics[otherCol]._handicap = 126);
				}
				_minDegrees.Add(MinDegree(revised, degree, num, otherCol), otherCol);
			}
		}

		/// <summary> Calculate the coincidences and fill cost of a newColumn,
		///           counting only fill to columns not yet taken.
		/// <remarks> The other column must not be a supernode </remarks>
		/// </summary>
		/// <returns> Number of fills caused </returns>
		private void ApplyFill(int col, int taken, bool[] inUse, int[] tempIndex)
		{
			int[] indexes = _symbolics[col]._indexes;
			int indexCount = _symbolics[col]._indexCount;
			for (int i = 0; i < indexCount; i++)
			{
				int num = indexes[i];
				if ((int)_symbolics[num]._stage >= 1)
				{
					continue;
				}
				int num2 = 0;
				int[] indexes2 = _symbolics[num]._indexes;
				int indexCount2 = _symbolics[num]._indexCount;
				int mapBit = _symbolics[num]._mapBit;
				bool flag;
				if (mapBit != 0)
				{
					int[] array = _denseMaps[mapBit - 1 >> 5];
					int num3 = 1 << ((mapBit - 1) & 0x1F);
					for (int j = 0; j < indexCount; j++)
					{
						int num4 = indexes[j];
						if ((array[num4] & num3) == 0 && _symbolics[num4]._stage != SymbolicStage.Taken)
						{
							tempIndex[num2++] = num4;
							array[num4] |= num3;
						}
					}
					flag = 0 == num2;
					if (flag && _symbolics[num]._fill < 0f && -6f < _symbolics[num]._fill)
					{
						_symbolics[num]._fill -= 1f;
						continue;
					}
					if (taken - 1 <= _symbolics[num]._lastTaken)
					{
						for (int k = 0; k < indexCount2; k++)
						{
							int num5 = indexes2[k];
							if (col != num5)
							{
								tempIndex[num2++] = num5;
							}
						}
					}
					else
					{
						for (int l = 0; l < indexCount2; l++)
						{
							int num6 = indexes2[l];
							if (_symbolics[num6]._stage != SymbolicStage.Taken)
							{
								tempIndex[num2++] = num6;
							}
						}
					}
				}
				else
				{
					for (int m = 0; m < indexCount2; m++)
					{
						int num7 = indexes2[m];
						if (_symbolics[num7]._stage != SymbolicStage.Taken)
						{
							inUse[num7] = true;
							tempIndex[num2++] = num7;
						}
					}
					flag = num2 == indexCount2;
					for (int n = 0; n < indexCount; n++)
					{
						int num8 = indexes[n];
						if (!inUse[num8] && _symbolics[num8]._stage != SymbolicStage.Taken)
						{
							tempIndex[num2++] = num8;
						}
					}
					for (int num9 = 0; num9 < indexCount2; num9++)
					{
						int num10 = indexes2[num9];
						inUse[num10] = false;
					}
				}
				if (!(flag && num2 == indexCount2))
				{
					if (num2 > indexes2.Length)
					{
						indexes2 = new int[num2];
						Array.Copy(tempIndex, indexes2, num2);
						_symbolics[num]._indexes = indexes2;
					}
					else
					{
						Array.Copy(tempIndex, indexes2, num2);
					}
					_symbolics[num]._indexCount = num2;
					UpdateMinDegree(num, indexCount2, num2);
				}
				_symbolics[num]._fill = -1f;
				_symbolics[num]._lastTaken = taken;
			}
		}

		/// <summary> Calculate the coincidences and fill cost of a newColumn,
		///           counting only fill to columns not yet taken.
		/// <remarks> The other column must not be a supernode </remarks>
		/// </summary>
		/// <returns> Number of fills caused </returns>
		private void ApplyDeferredReduction(int col)
		{
			int num = 0;
			int[] array = _symbolics[col]._indexes;
			int indexCount = _symbolics[col]._indexCount;
			for (int i = 0; i < indexCount; i++)
			{
				int num2 = array[i];
				if (_symbolics[num2]._stage != SymbolicStage.Taken)
				{
					array[num++] = num2;
				}
			}
			if (num != indexCount)
			{
				if (num > indexCount)
				{
					Array.Resize(ref array, num);
					_symbolics[col]._indexes = array;
				}
				_symbolics[col]._indexCount = num;
				UpdateMinDegree(col, indexCount, num);
			}
			_symbolics[col]._fill = -1f;
		}

		/// <summary> The symbolic arrays track the columns during minimum fill calculations
		/// </summary>
		/// <param name="colWeights"> fractional value per column, derived from hashing the non-zero pattern </param>
		/// <returns> the recommended ordering of _symbolics </returns>
		private void CreateSymbolics(double[] colWeights)
		{
			for (int i = 0; i < ColumnCount; i++)
			{
				int num = InnerToOuter[i];
				colWeights[i] += TrueDegree(num);
				if (_diagonalExponents != null)
				{
					if (_diagonalExponents[num] == sbyte.MinValue)
					{
						colWeights[i] += 10000 + ColumnCount / 20;
					}
					else
					{
						colWeights[i] += -_diagonalExponents[num] / 4;
					}
				}
			}
			Array.Sort(colWeights, InnerToOuter);
			OuterToInner = new int[ColumnCount];
			for (int j = 0; j < ColumnCount; j++)
			{
				OuterToInner[InnerToOuter[j]] = j;
			}
			_symbolics = new SymbolicColumn[ColumnCount];
			for (int k = 0; k < ColumnCount; k++)
			{
				int num2 = InnerToOuter[k];
				if (_symbolics[num2]._stage == SymbolicStage.Default)
				{
					SymbolicColumn symbolicColumn = default(SymbolicColumn);
					symbolicColumn._fill = -1f;
					int num3 = _M.CountColumnSlots(num2);
					symbolicColumn._indexes = new int[num3];
					symbolicColumn._indexCount = num3;
					Array.Copy(_M._rowIndexes, _M._columnStarts[num2], symbolicColumn._indexes, 0, num3);
					int num4 = k;
					while (++num4 < ColumnCount && colWeights[num4] == colWeights[k] && VerifyEquivalence(k, num4))
					{
						_symbolics[InnerToOuter[num4]]._stage = SymbolicStage.Super;
					}
					int num5 = num4 - k;
					if (1 < num5)
					{
						symbolicColumn._superNode = new int[num5 - 1];
						Array.Copy(InnerToOuter, k + 1, symbolicColumn._superNode, 0, num5 - 1);
					}
					symbolicColumn._degree = (byte)Math.Min(255, num5);
					_symbolics[num2] = symbolicColumn;
				}
			}
			_denseMapLock = 0;
			_denseMapCount = 0;
			_denseMaps = null;
		}

		/// <summary> Change a symbol (and its supernode) to Taken status
		/// </summary>
		/// <returns> the symbol index of the candidate </returns>
		private void TakeSym(int bestCol)
		{
			_symbolics[bestCol]._stage = SymbolicStage.Taken;
			if (_symbolics[bestCol]._superNode != null)
			{
				int[] superNode = _symbolics[bestCol]._superNode;
				foreach (int num in superNode)
				{
					_symbolics[num]._stage = SymbolicStage.Taken;
				}
			}
			if (_symbolics[bestCol]._indexCount != _symbolics[bestCol]._indexes.Length)
			{
				Array.Resize(ref _symbolics[bestCol]._indexes, _symbolics[bestCol]._indexCount);
			}
		}

		/// <summary> Permute the symbolics to choose an order with low fill.
		/// </summary>
		/// <returns> the recommended ordering of _symbolics </returns>
		protected int[] PlanBestOrder()
		{
			_minDegrees = new SortedDictionary<double, int>();
			for (int i = 0; i < ColumnCount; i++)
			{
				if (_symbolics[i]._stage != SymbolicStage.Super)
				{
					if (_diagonalExponents != null)
					{
						_symbolics[i]._handicap = (sbyte)(~_diagonalExponents[i]);
					}
					_minDegrees.Add(MinDegree(_symbolics[i]._indexCount, _symbolics[i]._degree, _symbolics[i]._handicap, i), i);
				}
			}
			int num = 0;
			Queue<KeyValuePair<double, int>> queue = new Queue<KeyValuePair<double, int>>();
			int[] array = new int[_minDegrees.Count];
			bool[] inUse = new bool[ColumnCount];
			int[] tempIndex = new int[ColumnCount];
			CalculateFillParallelizer calculateFillParallelizer = new CalculateFillParallelizer(ColumnCount, CalculateFill);
			int count = 256;
			int num2 = 0;
			while (0 < _minDegrees.Count)
			{
				if (CheckAbort())
				{
					throw new TimeLimitReachedException();
				}
				int limit = 2048 + ColumnCount;
				KeyValuePair<double, int>[] array2 = _minDegrees.Take(count).ToArray();
				KeyValuePair<double, int>[] array3 = array2;
				foreach (KeyValuePair<double, int> keyValuePair in array3)
				{
					int value = keyValuePair.Value;
					if (_symbolics[value]._fill < 0f)
					{
						if (-1f == _symbolics[value]._fill)
						{
							limit = CalculateFillDeferred(value, calculateFillParallelizer, limit);
						}
						else
						{
							ApplyDeferredReduction(value);
						}
					}
				}
				int count2 = calculateFillParallelizer._deferredFill.Count;
				num2 += count2;
				calculateFillParallelizer.RunDeferred();
				float num3 = float.MaxValue;
				KeyValuePair<double, int> item = new KeyValuePair<double, int>(0.0, 0);
				if (queue.Count == 0)
				{
					foreach (KeyValuePair<double, int> item2 in _minDegrees.Take(count))
					{
						int value2 = item2.Value;
						float fill = _symbolics[value2]._fill;
						if (0f == fill)
						{
							TakeSym(value2);
							queue.Enqueue(item2);
							num3 = 0f;
						}
						else if (0f < fill && fill <= num3)
						{
							item = item2;
							num3 = fill;
						}
					}
					if (num3 > 0f && num3 < 2.1474836E+09f)
					{
						TakeSym(item.Value);
						queue.Enqueue(item);
					}
				}
				while (0 < queue.Count)
				{
					KeyValuePair<double, int> keyValuePair2 = queue.Dequeue();
					_minDegrees.Remove(keyValuePair2.Key);
					int value3 = keyValuePair2.Value;
					array[num++] = value3;
					if ((double)_symbolics[value3]._indexCount >= base.Parameters.DenseWindowThreshhold * (double)(array.Length - num))
					{
						TakeDenseColumns(array, num);
						num = array.Length;
						break;
					}
					for (int k = 0; k < _symbolics[value3]._indexCount; k++)
					{
						int num4 = _symbolics[value3]._indexes[k];
						if (_symbolics[num4]._stage != SymbolicStage.Taken)
						{
							ApplyFill(value3, num, inUse, tempIndex);
							break;
						}
					}
					if (8000 < _symbolics[value3]._indexCount && num + 12000 < ColumnCount)
					{
						throw new ModelTooLargeException();
					}
				}
			}
			_minDegrees = null;
			_denseMaps = null;
			return array;
		}

		private void TakeDenseColumns(int[] takenOrder, int nextPlace)
		{
			_firstDenseColumn = nextPlace;
			foreach (KeyValuePair<double, int> minDegree in _minDegrees)
			{
				takenOrder[nextPlace++] = minDegree.Value;
				if (_symbolics[minDegree.Value]._stage != SymbolicStage.Taken)
				{
					TakeSym(minDegree.Value);
				}
			}
			_minDegrees.Clear();
		}

		/// <summary> Convert symbolics to a final order, expanding the supernodes
		/// </summary>
		/// <param name="takenOrder"> the order yielded by PlanBestOrder </param>
		/// <param name="columnIndexes"> the per-column vectors which will become the new compressed column matrix </param>
		/// <param name="isSupernodal"> a map of which columns are in supernodes </param>
		/// <returns> The list of column indexes, to be used for building the Cholesky </returns>
		internal void FinalizeNewOrder(int[] takenOrder, out int[][] columnIndexes, out bool[] isSupernodal)
		{
			columnIndexes = new int[ColumnCount][];
			isSupernodal = new bool[ColumnCount];
			bool flag = false;
			int num = 0;
			for (int i = 0; i < takenOrder.Length; i++)
			{
				int num2 = takenOrder[i];
				if (i >= _firstDenseColumn && !flag)
				{
					flag = true;
					_firstDenseColumn = num;
				}
				int[] indexes = _symbolics[num2]._indexes;
				OuterToInner[num2] = num;
				columnIndexes[num] = indexes;
				isSupernodal[num] = false;
				InnerToOuter[num++] = num2;
				if (_symbolics[num2]._superNode != null)
				{
					int[] superNode = _symbolics[num2]._superNode;
					foreach (int num3 in superNode)
					{
						OuterToInner[num3] = num;
						columnIndexes[num] = indexes;
						isSupernodal[num] = true;
						InnerToOuter[num++] = num3;
					}
				}
				if ((num2 & 0xF) == 0 && CheckAbort())
				{
					throw new TimeLimitReachedException();
				}
			}
			_symbolics = null;
			if (!flag)
			{
				_firstDenseColumn = -1;
			}
		}

		/// <summary> The final reorg is to predict all the coeff locations for the Cholesky factor.
		/// <remarks> PlanBestOrder is a prerequisite </remarks>
		/// </summary>
		/// <param name="columnIndexes"> the per-column vectors which will become the new compressed column matrix </param>
		/// <param name="isSupernodal"> a map of which columns are in supernodes </param>
		protected void BuildCholeskyLayout(int[][] columnIndexes, bool[] isSupernodal)
		{
			int num = 0;
			for (int i = 0; i < ColumnCount; i++)
			{
				int[] array = columnIndexes[i];
				if (!isSupernodal[i])
				{
					for (int j = 0; j < array.Length; j++)
					{
						array[j] = OuterToInner[array[j]];
					}
					Array.Sort(array);
				}
				int k;
				for (k = 0; i != array[k]; k++)
				{
				}
				_M._columnStarts[i] = k;
				int num2 = array.Length - k;
				int num3 = ColumnCount - i;
				if (_firstDenseColumn >= 0 && _firstDenseColumn <= i)
				{
					num2 = num3;
				}
				num += num2;
			}
			if (_firstDenseColumn < 0)
			{
				_firstDenseColumn = ColumnCount;
			}
			_M._rowIndexes = new int[num];
			num = 0;
			for (int l = 0; l < ColumnCount; l++)
			{
				int num4 = _M._columnStarts[l];
				_M._columnStarts[l] = num;
				int[] array2 = columnIndexes[l];
				int num5 = array2.Length - num4;
				if (l >= _firstDenseColumn)
				{
					num5 = ColumnCount - l;
					for (int m = l; m < ColumnCount; m++)
					{
						_M._rowIndexes[num++] = m;
					}
				}
				else
				{
					Array.Copy(array2, num4, _M._rowIndexes, num, num5);
					num += num5;
				}
				_maxColumnCount = Math.Max(num5, _maxColumnCount);
			}
			_M._columnStarts[ColumnCount] = num;
		}
	}
}
