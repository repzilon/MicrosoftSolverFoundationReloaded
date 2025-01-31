using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>Solves systems of the form
	///  ⌠  a dP_x        Ax*              │ ⌠Δx │
	///  │   Ax     b dD - As dP_s^-1 As*  ⌡ │Δy ⌡
	/// </summary>
	internal class GeneralModelBlendedFactor : KktBlendedFactor
	{
		private SparseMatrixDouble _A;

		private Vector _rhsXY;

		private SubVector _rhsX;

		private SubVector _rhsY;

		private int[] _map;

		private int _zeroPivots;

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
		public GeneralModelBlendedFactor(SparseMatrixDouble A, Func<bool> CheckAbort, FactorizationParameters factorParam)
			: base(A, null, null, CheckAbort, factorParam)
		{
			_A = A;
			_rhsXY = new Vector(base.AugCount + _A.RowCount);
			_rhsX = new SubVector(_rhsXY, 0, base.AugCount);
			_rhsY = new SubVector(_rhsXY, base.AugCount, _A.RowCount);
			_map = new int[base.AugCount + base.Ycount];
			base.RefinePivot = ZeroPivotRepair;
		}

		/// <summary> Compute the blended matrix:
		///
		///  ⌠  a dP_x        Ax*              │ ⌠Δx │
		///  │   Ax     b dD - As dP_s^-1 As*  ⌡ │Δy ⌡
		/// </summary>
		/// <param name="alpha">The multiplier for the primal diagonal perturbation</param>
		/// <param name="dPrml">Primal diagonal perturbation.</param>
		/// <param name="beta">The multiplier for the dual diagonal perturbation.</param>
		/// <param name="dDual">Dual diagonal perturbation.</param>
		/// <param name="setA">Whether the entire factor needs to be set, or only the perturbations.
		/// This implementation does not use setA, because the blended factor combines A with the 
		/// perturbations - therefore it is impossible to update the perturbations without looking
		/// at A.
		/// </param>
		public virtual void SetBlendedValues(double alpha, Vector dPrml, double beta, Vector dDual, bool setA)
		{
			_M._values.ZeroFill();
			for (int i = 0; i < base.AugCount; i++)
			{
				int num = base.OuterToInner[i];
				SparseMatrixByColumn<double>.ColIter colIter = new SparseMatrixByColumn<double>.ColIter(_M, num);
				while (colIter.IsValid)
				{
					_map[colIter.Row(_M)] = colIter.Slot;
					colIter.Advance();
				}
				if (_A_x != null)
				{
					SparseMatrixByColumn<double>.ColIter colIter2 = new SparseMatrixByColumn<double>.ColIter(_A_x, i);
					while (colIter2.IsValid)
					{
						int num2 = base.OuterToInner[base.AugCount + colIter2.Row(_A_x)];
						if (num <= num2)
						{
							double num3 = colIter2.Value(_A_x);
							_M._values[_map[num2]] = num3;
						}
						colIter2.Advance();
					}
				}
				_M._values[_map[num]] += alpha * dPrml[base.AugToPrimal[i]];
				SparseMatrixByColumn<double>.ColIter colIter3 = new SparseMatrixByColumn<double>.ColIter(_M, num);
				while (colIter3.IsValid)
				{
					_map[colIter3.Row(_M)] = -1;
					colIter3.Advance();
				}
			}
			SparseMatrixByColumn<double>.RowSlots rowSlots = default(SparseMatrixByColumn<double>.RowSlots);
			if (_A_x != null)
			{
				rowSlots = new SparseMatrixByColumn<double>.RowSlots(_A_x);
			}
			for (int j = 0; j < base.Ycount; j++)
			{
				int num4 = base.OuterToInner[base.AugCount + j];
				SparseMatrixByColumn<double>.ColIter colIter4 = new SparseMatrixByColumn<double>.ColIter(_M, num4);
				while (colIter4.IsValid)
				{
					_map[colIter4.Row(_M)] = colIter4.Slot;
					colIter4.Advance();
				}
				if (_A_x != null)
				{
					SparseMatrixByColumn<double>.RowIter rowIter = new SparseMatrixByColumn<double>.RowIter(_At_x, j);
					while (rowIter.IsValid)
					{
						int num5 = rowIter.Column(_At_x);
						int num6 = base.OuterToInner[num5];
						double num7 = rowSlots.ValueAdvance(_A_x, j, num5);
						if (num4 <= num6)
						{
							_M._values[_map[num6]] = num7;
						}
						rowIter.Advance();
					}
				}
				_M._values[_map[num4]] += beta * dDual[j];
				if (_A_s != null)
				{
					SparseMatrixByColumn<double>.RowIter rowIter2 = new SparseMatrixByColumn<double>.RowIter(_At_s, j);
					while (rowIter2.IsValid)
					{
						int num8 = rowIter2.Column(_At_s);
						double num9 = alpha * dPrml[base.SimpleToPrimal[num8]];
						double num10 = _A_s[j, num8] / num9;
						SparseMatrixByColumn<double>.ColIter colIter5 = new SparseMatrixByColumn<double>.ColIter(_A_s, num8);
						while (colIter5.IsValid)
						{
							int num11 = base.OuterToInner[base.AugCount + colIter5.Row(_A_s)];
							if (num4 <= num11)
							{
								int num12 = _map[num11];
								_M._values[num12] -= num10 * colIter5.Value(_A_s);
							}
							colIter5.Advance();
						}
						rowIter2.Advance();
					}
				}
				SparseMatrixByColumn<double>.ColIter colIter6 = new SparseMatrixByColumn<double>.ColIter(_M, num4);
				while (colIter6.IsValid)
				{
					_map[colIter6.Row(_M)] = -1;
					colIter6.Advance();
				}
			}
			double num13 = 0.0;
			for (int k = 0; k < _M.ColumnCount; k++)
			{
				num13 = Math.Max(num13, Math.Abs(_M[k, k]));
			}
			_zeroPivotTolerance = 1E-30 * num13;
			if (_printM)
			{
				Console.WriteLine(_M.ToString("mm", null));
			}
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

		/// <summary>
		/// Calculate the residue r* = Mx - r using the augmented equations:
		///
		///        M          x       r
		///  ⌠  a dP  A*  │ ⌠x_p│ = ⌠r_p│
		///  │   A   b dD ⌡ │x_d⌡   │r_d⌡
		/// </summary>
		/// <returns>The 2-norm of the residue.</returns>
		public virtual double ComputeResidue(double alpha, Vector dPrml, double beta, Vector dDual, Vector x, Vector r)
		{
			SubVector subVector = new SubVector(r, 0, dPrml.Length);
			SubVector subVector2 = new SubVector(x, 0, dPrml.Length);
			SubVector subVector3 = new SubVector(r, subVector.Length, dDual.Length);
			SubVector subVector4 = new SubVector(x, subVector2.Length, dDual.Length);
			_A.SumLeftProduct(-1.0, subVector4, 1.0, subVector);
			for (int i = 0; i < subVector.Length; i++)
			{
				subVector[i] -= alpha * dPrml[i] * subVector2[i];
			}
			_A.SumProductRight(-1.0, subVector2, 1.0, subVector3);
			for (int j = 0; j < subVector3.Length; j++)
			{
				subVector3[j] -= beta * dDual[j] * subVector4[j];
			}
			return r.Norm2();
		}

		/// <summary> solve for vectors gfx, gfy, using prepared blended factors.
		/// gfx and gfy are additive so that caller may use a residual correction loop
		/// </summary>
		public Vector SolveSystem(double alpha, Vector dPrml, double beta, Vector dDual, Vector r)
		{
			SubVector subVector = new SubVector(r, 0, dPrml.Length);
			SubVector y = new SubVector(r, dPrml.Length, dDual.Length);
			Vector vector = new Vector(base.SimpleCount);
			for (int i = 0; i < base.AugCount; i++)
			{
				_rhsX[i] = subVector[base.AugToPrimal[i]];
			}
			_rhsY.CopyFrom(y);
			if (_A_s != null)
			{
				for (int j = 0; j < base.SimpleCount; j++)
				{
					int i2 = base.SimpleToPrimal[j];
					vector[j] = subVector[i2] / (alpha * dPrml[i2]);
				}
				_A_s.SumProductRight(-1.0, vector, 1.0, _rhsY);
			}
			Vector vector2 = Solve(_rhsXY);
			Vector vector3 = new Vector(r.Length);
			SubVector subVector2 = new SubVector(vector3, 0, dPrml.Length);
			SubVector subVector3 = new SubVector(vector3, dPrml.Length, dDual.Length);
			for (int k = 0; k < base.AugCount; k++)
			{
				subVector2[base.AugToPrimal[k]] = vector2[k];
			}
			SubVector subVector4 = new SubVector(vector2, base.AugCount, base.Ycount);
			subVector3.CopyFrom(subVector4);
			if (_A_s != null)
			{
				for (int l = 0; l < base.SimpleCount; l++)
				{
					vector[l] = subVector[base.SimpleToPrimal[l]];
				}
				_A_s.SumLeftProduct(-1.0, subVector4, 1.0, vector);
				for (int m = 0; m < vector.Length; m++)
				{
					int i3 = base.SimpleToPrimal[m];
					subVector2[i3] = vector[m] / (alpha * dPrml[i3]);
				}
			}
			return vector3;
		}
	}
}
