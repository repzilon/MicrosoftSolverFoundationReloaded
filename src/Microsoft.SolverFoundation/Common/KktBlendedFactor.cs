using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> The Karush-Kuhn-Tucker conditions on linear or quadratic
	///           systems result in symmetric matrices which can be factored.
	///           Blend uses a hybrid of Augmented and Normal forms.
	/// </summary>
	internal class KktBlendedFactor : SymmetricFactorWorkspace
	{
		/// <summary> The linear coefficient matrix for x
		/// </summary>
		internal SparseMatrixDouble _A_x;

		/// <summary> The transpose view of Ax
		/// </summary>
		protected SparseTransposeIndexes<double> _At_x;

		/// <summary> The linear coefficient matrix for s
		/// </summary>
		internal SparseMatrixDouble _A_s;

		/// <summary> The transpose view of As
		/// </summary>
		protected SparseTransposeIndexes<double> _At_s;

		/// <summary> The quadratic coefficients for x are in Q_x.
		/// </summary>
		internal SymmetricSparseMatrix _Q_x;

		/// <summary> The quadratic coefficients for s are in Q_s.
		/// </summary>
		internal Vector _Q_s;

		/// <summary> Track augmented outer columns which are unbounded.
		/// </summary>
		private VarSpecial[] _varInfo;

		/// <summary>Diagonal values smaller than this value are handled specially.
		/// </summary>
		internal double _zeroPivotTolerance = 1E-30;

		internal bool _printM;

		/// <summary> If the model is decided during construction, we set this.
		/// </summary>
		internal bool Decided { get; private set; }

		/// <summary> Count of columns/rows in the left/top blocks of the Blended form
		/// </summary>
		public int AugCount { get; private set; }

		/// <summary> Count of columns/rows in the right/lower blocks of the Blended form
		/// </summary>
		public int Ycount { get; private set; }

		/// <summary> Count of Simple columns in the Blended form
		/// </summary>
		public int SimpleCount { get; private set; }

		/// <summary> Permute the variables into hard (augmented) columns preceding simple columns.
		/// </summary>
		public int[] PrimalToBlend { get; private set; }

		/// <summary> Permute from the augmented (hard) to the original primal.
		/// </summary>
		public int[] AugToPrimal { get; private set; }

		/// <summary> Permute from the simple to the original primal.
		/// </summary>
		public int[] SimpleToPrimal { get; private set; }

		/// <summary> Report if an outer column corresponds to an unbounded variable.
		/// </summary>
		/// <param name="outerCol"></param>
		/// <returns> true iff the column is unbounded </returns>
		public bool IsUnbounded(int outerCol)
		{
			return 0 != (_varInfo[outerCol] & VarSpecial.Unbounded);
		}

		/// <summary> Report if an outer column corresponds to a quadratic variable.
		/// </summary>
		/// <param name="outerCol"></param>
		/// <returns> true iff the column is quadratic </returns>
		public bool IsQuadratic(int outerCol)
		{
			return 0 != (_varInfo[outerCol] & (VarSpecial)6);
		}

		public bool IsPurelyQuadraticBlendedSystem()
		{
			if (_A_x == null && _A_s == null && _Q_s == null && _Q_x != null)
			{
				for (int i = 0; i < _varInfo.Length; i++)
				{
					if (!IsUnbounded(i))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		/// <summary> Map the augmented and normal parts of a blend back into original vector
		/// </summary>
		public void MapToPrimal(Vector xAug, Vector xNorm, ref double[] xPrimal)
		{
			if (xPrimal == null || PrimalToBlend.Length != xPrimal.Length)
			{
				xPrimal = new double[PrimalToBlend.Length];
			}
			for (int i = 0; i < PrimalToBlend.Length; i++)
			{
				int num = PrimalToBlend[i];
				if (num < AugCount)
				{
					xPrimal[i] = xAug[num];
				}
				else
				{
					xPrimal[i] = xNorm[num - AugCount];
				}
			}
		}

		/// <summary> Map a primal vector into the separate parts of the blend
		/// </summary>
		public void MapToBlend(Vector xPrimal, ref Vector xAug, ref Vector xSimple)
		{
			xAug = new Vector(AugCount);
			xSimple = new Vector(SimpleCount);
			for (int i = 0; i < PrimalToBlend.Length; i++)
			{
				int num = PrimalToBlend[i];
				if (num < AugCount)
				{
					xAug[num] = xPrimal[i];
				}
				else
				{
					xSimple[num - AugCount] = xPrimal[i];
				}
			}
		}

		/// <summary> Construct the row indexes and values for a partial subset of an existing matrix.
		/// </summary>
		/// <param name="slotCount"> count slots which will allow non-zeros within the matrix </param>
		/// <param name="starts"> where within indexes to find the start of each column </param>
		/// <param name="mask"> select augmented (complement, ~0) or normal (unchanged, 0) index values </param>
		/// <param name="colMap"> map for primal column to blend column </param>
		/// <param name="mapRows"> should mask apply to row values too ? </param>
		/// <param name="X"> the full matrix from which we are selecting either augmented or simple values </param>
		/// <param name="indexes"> the indexes for the new, partial matrix </param>
		/// <param name="values"> the values for the new, partial matrix </param>
		private static void BuildPartialMatrix(int slotCount, List<int> starts, int mask, int[] colMap, bool mapRows, SparseMatrixDouble X, out int[] indexes, out double[] values)
		{
			starts.Add(slotCount);
			int num = 0;
			indexes = new int[slotCount];
			values = new double[slotCount];
			for (int i = 0; i < X.ColumnCount; i++)
			{
				int num2 = mask ^ colMap[i];
				if (0 > num2)
				{
					continue;
				}
				SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(X, i);
				while (colIter.IsValid)
				{
					int num3 = colIter.Row(X);
					if (mapRows)
					{
						num3 = mask ^ colMap[num3];
					}
					indexes[num] = num3;
					values[num++] = colIter.Value(X);
					colIter.Advance();
				}
			}
		}

		/// <summary> Distinguish between the complicated and the simple columns and
		///     initialize the matrices and column transforms accordingly.  We want to
		///     separate the columns which are suitable for Normal form, from those where
		///     we are better to use Augmented form.
		/// </summary>
		/// <param name="A"> the existing, combined A matrix to be analyzed </param>
		/// <param name="Q"> the existing Q matrix to be analyzed </param>
		private void SeparateColumns(SparseMatrixDouble A, SymmetricSparseMatrix Q)
		{
			int num = A?.ColumnCount ?? Q.ColumnCount;
			PrimalToBlend = new int[num];
			List<int> list = new List<int>();
			int num2 = 0;
			List<int> list2 = new List<int>();
			int num3 = 0;
			List<int> list3 = null;
			int num4 = 0;
			List<double> list4 = null;
			int num5 = 0;
			if (Q != null)
			{
				list3 = new List<int>();
				list4 = new List<double>();
			}
			int num6 = 8;
			if (A != null)
			{
				num6 += (int)A.Count / (4 + A.ColumnCount / 4);
			}
			if (!base.Parameters.AllowNormal)
			{
				num6 = 0;
			}
			if (_varInfo == null)
			{
				_varInfo = new VarSpecial[num];
			}
			if (Q != null)
			{
				for (int i = 0; i < Q.ColumnCount; i++)
				{
					SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(Q, i);
					while (colIter.IsValid)
					{
						int num7 = colIter.Row(Q);
						if (num7 == i)
						{
							_varInfo[i] |= VarSpecial.QDiagonal;
						}
						else
						{
							_varInfo[num7] |= VarSpecial.NotSeparable;
							_varInfo[i] |= VarSpecial.NotSeparable;
						}
						colIter.Advance();
					}
				}
			}
			if (A != null)
			{
				for (int j = 0; j < num; j++)
				{
					if (A.CountColumnSlots(j) < num6 && _varInfo[j].MayBeNormal())
					{
						PrimalToBlend[j] = list2.Count;
						list2.Add(num3);
						num3 += A.CountColumnSlots(j);
						if (Q != null)
						{
							list4.Add(Q[j, j]);
							if (Q[j, j] != 0.0)
							{
								num5++;
							}
						}
					}
					else
					{
						PrimalToBlend[j] = ~list.Count;
						list.Add(num2);
						num2 += A.CountColumnSlots(j);
						if (Q != null)
						{
							list3.Add(num4);
							num4 += Q.CountColumnSlots(j);
						}
					}
				}
			}
			else
			{
				for (int k = 0; k < num; k++)
				{
					if (_varInfo[k].MayBeNormal())
					{
						PrimalToBlend[k] = list2.Count;
						list2.Add(0);
						list4.Add(Q[k, k]);
						if (Q[k, k] != 0.0)
						{
							num5++;
						}
					}
					else
					{
						PrimalToBlend[k] = ~list.Count;
						list.Add(0);
						list3.Add(num4);
						num4 += Q.CountColumnSlots(k);
					}
				}
			}
			AugCount = list.Count;
			list.Add(num2);
			if (Q != null)
			{
				list3.Add(num4);
			}
			SimpleCount = list2.Count;
			list2.Add(num3);
			Ycount = A?.RowCount ?? 0;
			if (num2 != 0)
			{
				BuildPartialMatrix(num2, list, -1, PrimalToBlend, mapRows: false, A, out var indexes, out var values);
				_A_x = new SparseMatrixDouble(Ycount, AugCount, list.ToArray(), indexes, values);
				list = null;
				_At_x = new SparseTransposeIndexes<double>(_A_x, _A_x.RowCounts());
			}
			if (num3 != 0)
			{
				BuildPartialMatrix(num3, list2, 0, PrimalToBlend, mapRows: false, A, out var indexes2, out var values2);
				_A_s = new SparseMatrixDouble(Ycount, SimpleCount, list2.ToArray(), indexes2, values2);
				list2 = null;
				_At_s = new SparseTransposeIndexes<double>(_A_s, _A_s.RowCounts());
			}
			if (num4 != 0)
			{
				BuildPartialMatrix(num4, list3, -1, PrimalToBlend, mapRows: true, Q, out var indexes3, out var values3);
				_Q_x = new SymmetricSparseMatrix(AugCount, list3.ToArray(), indexes3, values3);
				list3 = null;
			}
			if (num5 != 0)
			{
				_Q_s = new Vector(list4.ToArray());
			}
			int num8 = 0;
			int num9 = 0;
			AugToPrimal = new int[AugCount];
			SimpleToPrimal = new int[SimpleCount];
			for (int l = 0; l < num; l++)
			{
				int num10 = PrimalToBlend[l];
				if (0 <= num10)
				{
					num10 += AugCount;
					SimpleToPrimal[num9++] = l;
				}
				else
				{
					num10 = ~num10;
					AugToPrimal[num8++] = l;
				}
				PrimalToBlend[l] = num10;
			}
		}

		/// <summary> Plan the fillin of the blended &lt;A,Q,Z/X&gt; matrix.
		/// <remarks> This block fill is both below and above diagonal.  It is reduced to
		///           below diagonal by symbolic factorization.
		/// </remarks>
		/// </summary>
		/// <returns> initial weighting combines integer degree with a fractional pattern-hash </returns>
		public double[] PlanProductFillin()
		{
			int num = AugCount + Ycount;
			int[] array = new int[num + 1];
			int num2 = 0;
			int num3 = (int)((_A_x != null) ? _A_x.Count : 0);
			int num4 = 0;
			if (_Q_x != null && 0 < _Q_x.Count)
			{
				for (int i = 0; i < AugCount; i++)
				{
					int num5 = ((0.0 == _Q_x[i, i]) ? 1 : 0);
					num4 += 2 * (_Q_x.CountColumnSlots(i) + num5) - 1;
				}
			}
			else
			{
				num2 += AugCount;
			}
			int num6 = num3 + num4 + num2;
			int num7 = 0;
			List<int> list;
			if (_A_s == null)
			{
				list = new List<int>();
				num7 = Ycount;
			}
			else
			{
				AccumulateUnique<int> accumulateUnique = new AccumulateUnique<int>(2 * _A_s.RowCount);
				list = new List<int>(3 * (int)_A_s.Count / 2);
				for (int j = 0; j < Ycount; j++)
				{
					int num8;
					if (0 < _At_s.RowCount(j))
					{
						SparseMatrixByColumn<double>.RowIter rowIter = new SparseMatrixByColumn<double>.RowIter(_At_s, j);
						while (rowIter.IsValid)
						{
							SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_A_s, rowIter.Column(_At_s));
							while (colIter.IsValid)
							{
								accumulateUnique.Add(colIter.Row(_A_s));
								colIter.Advance();
							}
							rowIter.Advance();
						}
						num8 = accumulateUnique.Count();
						for (int k = 0; k < num8; k++)
						{
							list.Add(accumulateUnique[k]);
						}
					}
					else
					{
						list.Add(j);
						num8 = 1;
					}
					array[AugCount + j + 1] = array[AugCount + j] + num8;
					accumulateUnique.Clear();
				}
			}
			int num9 = num6 + array[num] + num7;
			if (_A_x != null)
			{
				num9 += (int)_A_x.Count;
			}
			int[] array2 = new int[num9];
			int num10 = 0;
			base.InnerToOuter = new int[num];
			int l;
			for (l = 0; l < AugCount; l++)
			{
				base.InnerToOuter[l] = l;
				array[l] = num10;
				bool flag = false;
				if (_Q_x != null)
				{
					foreach (int item in _Q_x.AllRowsInColumn(l))
					{
						flag = flag || l == item;
						if (!flag && l < item)
						{
							flag = true;
							array2[num10++] = l;
						}
						array2[num10++] = item;
					}
				}
				if (!flag)
				{
					array2[num10++] = l;
				}
				if (_A_x != null)
				{
					SparseMatrixByColumn<double>.ColIter colIter2 = new SparseMatrixByColumn<double>.ColIter(_A_x, l);
					while (colIter2.IsValid)
					{
						array2[num10++] = colIter2.Row(_A_x) + AugCount;
						colIter2.Advance();
					}
				}
			}
			int num11 = 0;
			while (num11 < Ycount)
			{
				base.InnerToOuter[l] = l;
				int num12 = array[l];
				int num13 = array[l + 1];
				array[l] = num10;
				if (_A_x != null)
				{
					SparseMatrixByColumn<double>.RowIter rowIter2 = new SparseMatrixByColumn<double>.RowIter(_At_x, num11);
					while (rowIter2.IsValid)
					{
						array2[num10++] = rowIter2.Column(_At_x);
						rowIter2.Advance();
					}
				}
				if (num12 == num13)
				{
					array2[num10++] = AugCount + num11;
				}
				else
				{
					for (int m = num12; m < num13; m++)
					{
						array2[num10++] = AugCount + list[m];
					}
				}
				num11++;
				l++;
			}
			array[num] = num10;
			list = null;
			double[] array3 = new double[num];
			_M = new SparseMatrixDouble(num, num, array, array2, null);
			for (l = 0; l < num; l++)
			{
				int a = _M.CountColumnSlots(l);
				SparseMatrixByColumn<double>.ColIter colIter3 = new SparseMatrixByColumn<double>.ColIter(_M, l);
				while (colIter3.IsValid)
				{
					SymmetricFactorWorkspace.Rehash(ref a, colIter3.Row(_M));
					colIter3.Advance();
				}
				array3[l] = (double)a / -2147483648.0;
			}
			return array3;
		}

		/// <summary> Cause a zero pivot's row and column to be ignored.
		/// </summary>
		/// <returns> throw a DivideByZeroException </returns>
		private double BlendedPivotRepair(int innerCol, double value, List<SparseVectorCell> perturbations)
		{
			if (double.IsNaN(value))
			{
				return 1E+150;
			}
			double num = Math.Abs(value);
			if (num < _zeroPivotTolerance)
			{
				return 1E-08;
			}
			return value;
		}

		/// <summary> Construct a KKT context for the Blended form equations in Central Path IPM.
		/// </summary>
		/// <param name="A"> The (required) linear coefficients' matrix </param>
		/// <param name="Q"> The (optional) quadratic coefficients' matrix </param>
		/// <param name="varInfo"> indicates glitches affecting each variable </param>
		/// <param name="CheckAbort"> predicate returns false for continue, true if stop required </param>
		/// <param name="factorParam">Symbolic factorization parameters.</param>
		public KktBlendedFactor(SparseMatrixDouble A, SymmetricSparseMatrix Q, VarSpecial[] varInfo, Func<bool> CheckAbort, FactorizationParameters factorParam)
			: base(1, factorParam)
		{
			Decided = false;
			base.RefinePivot = BlendedPivotRepair;
			base.CheckAbort = CheckAbort;
			if (A == null && Q == null)
			{
				throw new ArgumentNullException("A", string.Format(CultureInfo.InvariantCulture, Resources.TwoAreNull01, new object[2] { "A", "Q" }));
			}
			if (A != null && Q != null && A.ColumnCount != Q.ColumnCount)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SizesDoNotMatch, new object[2] { "A", "Q" }));
			}
			int num = A?.ColumnCount ?? Q.ColumnCount;
			if (varInfo == null)
			{
				varInfo = new VarSpecial[num];
			}
			_varInfo = varInfo;
			SeparateColumns(A, Q);
			if (_A_x == null && _A_s == null && _Q_x == null)
			{
				Decided = true;
			}
			else
			{
				Factorize(PlanProductFillin, AugCount + Ycount);
			}
		}

		/// <summary> Prepare the blended matrix using A, Q, Z/X, and Z/S.
		/// </summary>
		/// <param name="z_x"> quadratic complementarity Z/X numerator </param>
		/// <param name="x"> quadratic complementarity Z/X denominator </param>
		/// <param name="z_s"> quadratic complementarity Z/S numerator </param>
		/// <param name="s"> quadratic complementarity Z/S denominator </param>
		/// <param name="perturbation"> perturbation to use on zero diagonals </param>
		public void SetBlendedValues(Vector z_x, Vector x, Vector z_s, Vector s, double perturbation)
		{
			int num = AugCount + Ycount;
			int[] array = new int[num];
			_M._values.ZeroFill();
			for (int i = 0; i < AugCount; i++)
			{
				int num2 = base.OuterToInner[i];
				SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_M, num2);
				while (colIter.IsValid)
				{
					array[colIter.Row(_M)] = colIter.Slot;
					colIter.Advance();
				}
				if (_Q_x != null)
				{
					foreach (KeyValuePair<int, double> item in _Q_x.AllValuesInColumn(i))
					{
						int num3 = base.OuterToInner[item.Key];
						if (num2 <= num3)
						{
							double value = item.Value;
							_M._values[array[num3]] = 0.0 - value;
						}
					}
				}
				double num4 = _M._values[array[num2]];
				if (0.0 != z_x[i])
				{
					num4 -= z_x[i] / x[i];
				}
				_M._values[array[num2]] = num4;
				if (_A_x != null)
				{
					SparseMatrixByColumn<double>.ColIter colIter2 = new SparseMatrixByColumn<double>.ColIter(_A_x, i);
					while (colIter2.IsValid)
					{
						int num5 = base.OuterToInner[AugCount + colIter2.Row(_A_x)];
						if (num2 <= num5)
						{
							double num6 = colIter2.Value(_A_x);
							_M._values[array[num5]] = num6;
						}
						colIter2.Advance();
					}
				}
				SparseMatrixByColumn<double>.ColIter colIter3 = new SparseMatrixByColumn<double>.ColIter(_M, num2);
				while (colIter3.IsValid)
				{
					array[colIter3.Row(_M)] = -1;
					colIter3.Advance();
				}
			}
			SparseMatrixByColumn<double>.RowSlots rowSlots = default(SparseMatrixByColumn<double>.RowSlots);
			if (_A_x != null)
			{
				rowSlots = new SparseMatrixByColumn<double>.RowSlots(_A_x);
			}
			for (int j = 0; j < Ycount; j++)
			{
				int num7 = base.OuterToInner[AugCount + j];
				SparseMatrixByColumn<double>.ColIter colIter4 = new SparseMatrixByColumn<double>.ColIter(_M, num7);
				while (colIter4.IsValid)
				{
					array[colIter4.Row(_M)] = colIter4.Slot;
					colIter4.Advance();
				}
				if (_A_x != null)
				{
					SparseMatrixByColumn<double>.RowIter rowIter = new SparseMatrixByColumn<double>.RowIter(_At_x, j);
					while (rowIter.IsValid)
					{
						int num8 = rowIter.Column(_At_x);
						int num9 = base.OuterToInner[num8];
						double num10 = rowSlots.ValueAdvance(_A_x, j, num8);
						if (num7 <= num9)
						{
							_M._values[array[num9]] = num10;
						}
						rowIter.Advance();
					}
				}
				if (_A_s != null)
				{
					SparseMatrixByColumn<double>.RowIter rowIter2 = new SparseMatrixByColumn<double>.RowIter(_At_s, j);
					while (rowIter2.IsValid)
					{
						int num11 = rowIter2.Column(_At_s);
						double num12 = ((z_s[num11] == 0.0) ? 0.0 : (z_s[num11] / s[num11]));
						if (_Q_s != null)
						{
							num12 += _Q_s[num11];
						}
						if (0.0 == num12)
						{
							num12 = perturbation;
						}
						double num13 = _A_s[j, num11] / num12;
						SparseMatrixByColumn<double>.ColIter colIter5 = new SparseMatrixByColumn<double>.ColIter(_A_s, num11);
						while (colIter5.IsValid)
						{
							int num14 = base.OuterToInner[AugCount + colIter5.Row(_A_s)];
							if (num7 <= num14)
							{
								int num15 = array[num14];
								_M._values[num15] += num13 * colIter5.Value(_A_s);
							}
							colIter5.Advance();
						}
						rowIter2.Advance();
					}
				}
				SparseMatrixByColumn<double>.ColIter colIter6 = new SparseMatrixByColumn<double>.ColIter(_M, num7);
				while (colIter6.IsValid)
				{
					array[colIter6.Row(_M)] = -1;
					colIter6.Advance();
				}
			}
			if (_printM)
			{
				Console.WriteLine(_M.ToString("mm", null));
			}
		}

		/// <summary> A complete solution of the system A*D*At x = z
		/// </summary>
		/// <returns> x = BackSubstitution(ForwardSubstitution(z)) </returns>
		public override Vector Solve(Vector y)
		{
			double[] reals = new double[_M._T._maxRowLength];
			Vector vector = ForwardSolve(y, reals);
			for (int i = 0; i < vector.Length; i++)
			{
				vector[i] *= _diagonalFactor[i];
			}
			return BackwardSolve(vector, reals);
		}
	}
}
