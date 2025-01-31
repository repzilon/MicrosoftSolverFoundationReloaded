using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>Solves the augmented system for SOCP.
	/// </summary>
	internal class SocpFactor : SymmetricIndefiniteFactor
	{
		/// <summary>Create a new instance.
		/// </summary>
		/// <param name="M">The coefficient matrix.</param>
		/// <param name="CheckAbort">CheckAbort.</param>
		/// <param name="factorParam">Factorization parameters.</param>
		public SocpFactor(SparseMatrixDouble M, Func<bool> CheckAbort, FactorizationParameters factorParam)
			: base(M, CheckAbort, factorParam)
		{
		}

		/// <summary>Set the diagonal values of the augmented system.
		/// </summary>
		public virtual void SetLinearSystemDiagonals(Vector Dprml, Vector DdualGKL, double[] thetaQ, double[] thetaR, List<Vector> wQ, List<Vector> wR, int minConeSizeLowRank)
		{
			Fill();
			for (int i = 0; i < Dprml.Length; i++)
			{
				int num = i;
				_M._values[_M._columnStarts[base.OuterToInner[num]]] = 0.0 - Dprml[i];
			}
			for (int j = 0; j < DdualGKL.Length; j++)
			{
				int num2 = Dprml.Length + j;
				_M._values[_M._columnStarts[base.OuterToInner[num2]]] = DdualGKL[j];
			}
			if (wQ.Count == 0 && wR.Count == 0)
			{
				return;
			}
			int num3 = Dprml.Length + DdualGKL.Length;
			for (int k = 0; k < wQ.Count; k++)
			{
				SubVector subVector = new SubVector(wQ[k], 0, wQ[k].Length);
				double num4 = thetaQ[k] * thetaQ[k];
				int num5 = num3;
				if (subVector.Length < minConeSizeLowRank)
				{
					for (int l = 0; l < subVector.Length; l++)
					{
						int num6 = base.OuterToInner[num3];
						for (int m = _M._columnStarts[num6]; m < _M._columnStarts[num6 + 1]; m++)
						{
							int num7 = _M._rowIndexes[m];
							int num8 = base.InnerToOuter[num7] - num5;
							if (l == num8)
							{
								if (l == 0)
								{
									_M._values[m] = num4 * (2.0 * subVector[l] * subVector[l] - 1.0);
								}
								else
								{
									_M._values[m] = num4 * (2.0 * subVector[l] * subVector[l] + 1.0);
								}
							}
							else if (num8 >= 0 && num8 < subVector.Length)
							{
								_M._values[m] = num4 * 2.0 * subVector[l] * subVector[num8];
							}
						}
						num3++;
					}
					continue;
				}
				for (int n = 0; n < subVector.Length; n++)
				{
					int num9 = base.OuterToInner[num3];
					int num10 = _M._columnStarts[num9];
					if (n == 0)
					{
						_M._values[num10] = 0.0 - num4;
					}
					else
					{
						_M._values[num10] = num4;
					}
					num3++;
				}
			}
			for (int num11 = 0; num11 < wR.Count; num11++)
			{
				SubVector subVector2 = new SubVector(wR[num11], 0, wR[num11].Length);
				double num12 = thetaR[num11] * thetaR[num11];
				int num13 = num3;
				if (subVector2.Length < minConeSizeLowRank)
				{
					for (int num14 = 0; num14 < subVector2.Length; num14++)
					{
						int num15 = base.OuterToInner[num3];
						for (int num16 = _M._columnStarts[num15]; num16 < _M._columnStarts[num15 + 1]; num16++)
						{
							int num17 = _M._rowIndexes[num16];
							int num18 = base.InnerToOuter[num17] - num13;
							if (num14 == num18)
							{
								if (num14 == 0 || num14 == 2)
								{
									_M._values[num16] = num12 * (2.0 * subVector2[num14] * subVector2[num14] - 1.0);
								}
								else
								{
									_M._values[num16] = num12 * (2.0 * subVector2[num14] * subVector2[num14] + 1.0);
								}
							}
							else if (num18 >= 0 && num18 < subVector2.Length)
							{
								if (num18 == 1)
								{
									_M._values[num16] = num12 * (2.0 * subVector2[num18] * subVector2[num14] - 1.0);
								}
								else
								{
									_M._values[num16] = num12 * 2.0 * subVector2[num18] * subVector2[num14];
								}
							}
						}
						num3++;
					}
					continue;
				}
				for (int num19 = 0; num19 < subVector2.Length; num19++)
				{
					int num20 = base.OuterToInner[num3];
					int num21 = _M._columnStarts[num20];
					switch (num19)
					{
					case 0:
						_M._values[num21++] = 0.0;
						_M._values[num21++] = 0.0 - num12;
						break;
					case 1:
						_M._values[num21++] = 0.0;
						break;
					default:
						_M._values[num21++] = num12;
						break;
					}
					num3++;
				}
			}
		}
	}
}
