using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Common.Factorization;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Creates the augmented system for SOCP.
	/// </summary>
	internal class SocpLinearSystem
	{
		private long nNzs;

		private int nRows;

		private SparseMatrixDouble _A;

		private Vector _Dprml;

		private Vector _DdualGKL;

		private double[] _thetaQ;

		private double[] _thetaR;

		private List<Vector> _wQ;

		private List<Vector> _wR;

		private int _minLowRankSize;

		public static SparseMatrixDouble Create(SparseMatrixDouble A, Vector Dprml, Vector DdualGKL, double[] thetaQ, double[] thetaR, List<Vector> wQ, List<Vector> wR, int minLowRankSize)
		{
			SocpLinearSystem socpLinearSystem = new SocpLinearSystem();
			socpLinearSystem._A = A;
			socpLinearSystem._Dprml = Dprml;
			socpLinearSystem._DdualGKL = DdualGKL;
			socpLinearSystem._thetaQ = thetaQ;
			socpLinearSystem._thetaR = thetaR;
			socpLinearSystem._wQ = wQ;
			socpLinearSystem._wR = wR;
			socpLinearSystem._minLowRankSize = minLowRankSize;
			socpLinearSystem.nNzs = A.Count + Dprml.Length + DdualGKL.Length;
			int num = 0;
			for (int i = 0; i < wQ.Count; i++)
			{
				int length = wQ[i].Length;
				num += length;
				if (length < minLowRankSize)
				{
					socpLinearSystem.nNzs += length * (length + 1) / 2;
				}
				else
				{
					socpLinearSystem.nNzs += length;
				}
			}
			for (int j = 0; j < wR.Count; j++)
			{
				int length2 = wR[j].Length;
				num += length2;
				if (length2 < minLowRankSize)
				{
					socpLinearSystem.nNzs += length2 * (length2 + 1) / 2;
				}
				else
				{
					socpLinearSystem.nNzs += length2 + 1;
				}
			}
			socpLinearSystem.nRows = Dprml.Length + DdualGKL.Length + num;
			return socpLinearSystem.ToMatrix();
		}

		private SparseMatrixDouble ToMatrix()
		{
			double[] values = new double[nNzs];
			int[] array = new int[nNzs];
			int[] array2 = new int[nRows + 1];
			Fill(values, array, array2);
			SparseMatrixDouble sparseMatrixDouble = new SparseMatrixDouble(nRows, nRows, array2, array, values);
			SparseMatrixByColumn<double> b = sparseMatrixDouble.Transpose();
			return sparseMatrixDouble.Add(b, 1.0, 1.0);
		}

		private void Fill(double[] values, int[] columns, int[] rowIndex)
		{
			int num;
			for (int i = 0; i < _Dprml.Length; i++)
			{
				num = (rowIndex[i] = _A._columnStarts[i] + i);
				columns[num] = i;
				values[num] = 0.0 - _Dprml[i];
				num++;
				for (int j = _A._columnStarts[i]; j < _A._columnStarts[i + 1]; j++)
				{
					int num2 = j - _A._columnStarts[i];
					columns[num + num2] = _A._rowIndexes[j] + _Dprml.Length;
					values[num + num2] = _A._values[j];
				}
			}
			num = (int)_A.Count + _Dprml.Length;
			int num3 = _Dprml.Length;
			for (int k = 0; k < _DdualGKL.Length; k++)
			{
				rowIndex[num3] = num;
				columns[num] = num3;
				values[num] = _DdualGKL[k];
				num3++;
				num++;
			}
			for (int l = 0; l < _wQ.Count; l++)
			{
				SubVector subVector = new SubVector(_wQ[l], 0, _wQ[l].Length);
				double num4 = _thetaQ[l] * _thetaQ[l];
				if (subVector.Length < _minLowRankSize)
				{
					for (int m = 0; m < subVector.Length; m++)
					{
						rowIndex[num3] = num;
						columns[num] = num3;
						if (m == 0)
						{
							values[num] = num4 * (2.0 * subVector[m] * subVector[m] - 1.0);
						}
						else
						{
							values[num] = num4 * (2.0 * subVector[m] * subVector[m] + 1.0);
						}
						for (int n = m + 1; n < subVector.Length; n++)
						{
							num++;
							columns[num] = num3 + (n - m);
							values[num] = num4 * 2.0 * subVector[m] * subVector[n];
						}
						num3++;
						num++;
					}
					continue;
				}
				for (int num5 = 0; num5 < subVector.Length; num5++)
				{
					rowIndex[num3] = num;
					columns[num] = num3;
					if (num5 == 0)
					{
						values[num] = 0.0 - num4;
					}
					else
					{
						values[num] = num4;
					}
					num3++;
					num++;
				}
			}
			for (int num6 = 0; num6 < _wR.Count; num6++)
			{
				SubVector subVector2 = new SubVector(_wR[num6], 0, _wR[num6].Length);
				double num7 = _thetaR[num6] * _thetaR[num6];
				if (subVector2.Length < _minLowRankSize)
				{
					for (int num8 = 0; num8 < subVector2.Length; num8++)
					{
						rowIndex[num3] = num;
						columns[num] = num3;
						if (num8 == 0 || num8 == 2)
						{
							values[num] = num7 * 2.0 * subVector2[num8] * subVector2[num8];
						}
						else
						{
							values[num] = num7 * (2.0 * subVector2[num8] * subVector2[num8] + 1.0);
						}
						for (int num9 = num8 + 1; num9 < subVector2.Length; num9++)
						{
							num++;
							columns[num] = num3 + (num9 - num8);
							if (num9 == 1)
							{
								values[num] = num7 * (2.0 * subVector2[num8] * subVector2[num9] - 1.0);
							}
							else
							{
								values[num] = num7 * 2.0 * subVector2[num8] * subVector2[num9];
							}
						}
						num3++;
						num++;
					}
					continue;
				}
				for (int num10 = 0; num10 < subVector2.Length; num10++)
				{
					rowIndex[num3] = num;
					columns[num] = num3;
					switch (num10)
					{
					case 0:
						values[num] = 0.0;
						num++;
						columns[num] = num3 + 1;
						values[num] = 0.0 - num7;
						break;
					case 1:
						values[num] = 0.0;
						break;
					default:
						values[num] = num7;
						break;
					}
					num3++;
					num++;
				}
			}
			rowIndex[nRows] = (int)nNzs;
		}
	}
}
