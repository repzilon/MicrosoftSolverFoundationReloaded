using System;
using System.Diagnostics;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class LUFactorizationRational : LUFactorization<Rational>
	{
		private double[] _rgnumWorkApprox;

		public Rational Determinant
		{
			get
			{
				Rational result = 1;
				for (int i = 0; i < _rcLim; i++)
				{
					int num = _mprowslotU[i];
					result *= _rgnum[num];
				}
				return result;
			}
		}

		[Conditional("DEBUG")]
		private void VerifyWorkSpaceClearedApprox()
		{
		}

		protected override bool FuzzyVerify(Rational num1, Rational num2)
		{
			return num1 == num2;
		}

		protected override bool AddToMulPivot(ref Rational numDst, Rational num1, Rational num2)
		{
			numDst = Rational.AddMul(numDst, num1, num2);
			return numDst.IsZero;
		}

		protected override Rational GetValue(ref CoefMatrix.ColIter cit)
		{
			return cit.Exact;
		}

		protected override Rational GetValue(CoefMatrix mat, int row, int col)
		{
			return mat.GetCoefExact(row, col);
		}

		protected override int GetBestPivotInCol(int col)
		{
			return GetBestPivotInList(_mpcollin[col].slotFirst, _rgslotNextCol, _mprowlin, _rgrow);
		}

		protected override int GetBestPivotInRow(int row)
		{
			return GetBestPivotInList(_mprowlin[row].slotFirst, _rgslotNextRow, _mpcollin, _rgcol);
		}

		private int GetBestPivotInList(int slotFirst, int[] rgslotNext, ListInfo[] rglinOther, int[] rgrcOther)
		{
			int result = 0;
			int num = int.MaxValue;
			int num2 = int.MaxValue;
			for (int num3 = slotFirst; num3 > 0; num3 = rgslotNext[num3])
			{
				int num4 = rgrcOther[num3];
				int cslot = rglinOther[num4].cslot;
				if (cslot <= num)
				{
					int bitCount = _rgnum[num3].BitCount;
					if (cslot != num || bitCount < num2)
					{
						result = num3;
						num = cslot;
						num2 = bitCount;
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Solve L.U.x = b by morphing vec from b to x.
		/// </summary>
		public void SolveCol(Rational[] rgnum)
		{
			_permRow.ApplyInverse(rgnum);
			for (int i = 0; i < _rcLim; i++)
			{
				Rational num = rgnum[i];
				if (!num.IsZero)
				{
					Rational.Negate(ref num);
					for (int num2 = _mpcolslotL[i]; num2 > 0; num2 = _rgslotNextCol[num2])
					{
						ref Rational reference = ref rgnum[_rgrow[num2]];
						reference = Rational.AddMul(rgnum[_rgrow[num2]], num, _rgnum[num2]);
					}
				}
			}
			int num3 = _rcLim;
			while (--num3 >= 0)
			{
				Rational num4 = rgnum[num3];
				if (!num4.IsZero)
				{
					Rational rational = _rgnum[_mprowslotU[num3]];
					if (!rational.IsOne)
					{
						num4 /= rational;
						rgnum[num3] = num4;
					}
					Rational.Negate(ref num4);
					for (int num5 = _mpcolslotU[num3]; num5 > 0; num5 = _rgslotNextCol[num5])
					{
						ref Rational reference2 = ref rgnum[_rgrow[num5]];
						reference2 = Rational.AddMul(rgnum[_rgrow[num5]], num4, _rgnum[num5]);
					}
				}
			}
			_permCol.Apply(rgnum);
			SolveColEta(rgnum);
		}

		private void SolveColEta(Rational[] rgnum)
		{
			int etaCount = base.EtaCount;
			for (int i = 0; i < etaCount; i++)
			{
				int num = _mpetaslotFirst[i];
				int num2 = _rgrowEta[num];
				Rational num3 = rgnum[num2];
				if (!num3.IsZero)
				{
					int num4 = _mpetaslotFirst[i + 1];
					Rational rational = _rgnumEta[num];
					if (!rational.IsOne)
					{
						num3 /= rational;
						rgnum[num2] = num3;
					}
					Rational.Negate(ref num3);
					while (++num < num4)
					{
						int num5 = _rgrowEta[num];
						ref Rational reference = ref rgnum[num5];
						reference = Rational.AddMul(rgnum[num5], num3, _rgnumEta[num]);
					}
				}
			}
		}

		/// <summary>
		/// Solve L.U.x = b by morphing vec from b to x.
		/// </summary>
		public void SolveCol(VectorRational vec)
		{
			SolveColCore(vec);
			SolveColEta(vec);
		}

		private void SolveColCore(VectorRational vec)
		{
			_crcWork = 0;
			_crcStack = 0;
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(vec);
			while (iter.IsValid)
			{
				int num = _permRow.MapInverse(iter.Rc);
				ref Rational reference = ref _rgnumWork[num];
				reference = iter.Value;
				if (!_rgfWork[num])
				{
					_rgrcStack[0] = num;
					_rgslotNextStack[0] = _mpcolslotL[num];
					_crcStack = 1;
					ExecDfs(_mpcolslotL, _rgslotNextCol, _rgrow);
				}
				iter.Advance();
			}
			int num2 = _crcWork;
			while (--num2 >= 0)
			{
				int num3 = _rgrcWork[num2];
				_rgfWork[num3] = false;
				Rational ratMul = -_rgnumWork[num3];
				if (!ratMul.IsZero)
				{
					for (int num4 = _mpcolslotL[num3]; num4 > 0; num4 = _rgslotNextCol[num4])
					{
						int num5 = _rgrow[num4];
						ref Rational reference2 = ref _rgnumWork[num5];
						reference2 = Rational.AddMul(_rgnumWork[num5], ratMul, _rgnum[num4]);
					}
				}
			}
			int crcWork = _crcWork;
			Statics.Swap(ref _rgrcWork, ref _rgrcWork2);
			_crcWork = 0;
			_crcStack = 0;
			int num6 = crcWork;
			while (--num6 >= 0)
			{
				int num7 = _rgrcWork2[num6];
				if (!_rgnumWork[num7].IsZero && !_rgfWork[num7])
				{
					_rgrcStack[0] = num7;
					_rgslotNextStack[0] = _mpcolslotU[num7];
					_crcStack = 1;
					ExecDfs(_mpcolslotU, _rgslotNextCol, _rgrow);
				}
			}
			int num8 = _crcWork;
			while (--num8 >= 0)
			{
				int num9 = _rgrcWork[num8];
				_rgfWork[num9] = false;
				Rational num10 = _rgnumWork[num9];
				if (!num10.IsZero)
				{
					Rational rational = _rgnum[_mprowslotU[num9]];
					if (!rational.IsOne)
					{
						num10 /= rational;
						_rgnumWork[num9] = num10;
					}
					Rational.Negate(ref num10);
					for (int num11 = _mpcolslotU[num9]; num11 > 0; num11 = _rgslotNextCol[num11])
					{
						int num12 = _rgrow[num11];
						ref Rational reference3 = ref _rgnumWork[num12];
						reference3 = Rational.AddMul(_rgnumWork[num12], num10, _rgnum[num11]);
					}
				}
			}
			vec.Clear();
			int num13 = _crcWork;
			while (--num13 >= 0)
			{
				int num14 = _rgrcWork[num13];
				Rational num15 = _rgnumWork[num14];
				if (!num15.IsZero)
				{
					vec.SetCoefNonZero(_permCol.Map(num14), num15);
					_rgnumWork[num14] = default(Rational);
				}
			}
		}

		private void SolveColEta(VectorRational vec)
		{
			int etaCount = base.EtaCount;
			for (int i = 0; i < etaCount; i++)
			{
				int num = _mpetaslotFirst[i];
				int rc = _rgrowEta[num];
				Rational num2 = vec.GetCoef(rc);
				if (num2.IsZero)
				{
					continue;
				}
				int num3 = _mpetaslotFirst[i + 1];
				Rational rational = _rgnumEta[num];
				if (!rational.IsOne)
				{
					num2 /= rational;
					vec.SetCoefNonZero(rc, num2);
				}
				Rational.Negate(ref num2);
				while (++num < num3)
				{
					int rc2 = _rgrowEta[num];
					rational = Rational.AddMul(vec.GetCoef(rc2), num2, _rgnumEta[num]);
					if (!rational.IsZero)
					{
						vec.SetCoefNonZero(rc2, rational);
					}
					else
					{
						vec.RemoveCoef(rc2);
					}
				}
			}
		}

		/// <summary>
		/// Solve x.L.U = b by morphing rgnum from b to x.
		/// </summary>
		public void SolveRow(VectorRational vec)
		{
			SolveRowEta(vec);
			SolveRowCore(vec);
		}

		private void SolveRowCore(VectorRational vec)
		{
			_crcWork = 0;
			_crcStack = 0;
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(vec);
			while (iter.IsValid)
			{
				int num = _permCol.MapInverse(iter.Rc);
				ref Rational reference = ref _rgnumWork[num];
				reference = iter.Value;
				if (!_rgfWork[num])
				{
					_rgrcStack[0] = num;
					_rgslotNextStack[0] = _rgslotNextRow[_mprowslotU[num]];
					_crcStack = 1;
					ExecDfsSkip(_mprowslotU, _rgslotNextRow, _rgcol);
				}
				iter.Advance();
			}
			int num2 = _crcWork;
			while (--num2 >= 0)
			{
				int num3 = _rgrcWork[num2];
				_rgfWork[num3] = false;
				Rational num4 = _rgnumWork[num3];
				if (!num4.IsZero)
				{
					int num5 = _mprowslotU[num3];
					Rational rational = _rgnum[num5];
					if (!rational.IsOne)
					{
						num4 /= rational;
						_rgnumWork[num3] = num4;
					}
					Rational.Negate(ref num4);
					while ((num5 = _rgslotNextRow[num5]) > 0)
					{
						int num6 = _rgcol[num5];
						ref Rational reference2 = ref _rgnumWork[num6];
						reference2 = Rational.AddMul(_rgnumWork[num6], num4, _rgnum[num5]);
					}
				}
			}
			int crcWork = _crcWork;
			Statics.Swap(ref _rgrcWork, ref _rgrcWork2);
			_crcWork = 0;
			_crcStack = 0;
			int num7 = crcWork;
			while (--num7 >= 0)
			{
				int num8 = _rgrcWork2[num7];
				if (!_rgnumWork[num8].IsZero && !_rgfWork[num8])
				{
					_rgrcStack[0] = num8;
					_rgslotNextStack[0] = _mprowslotL[num8];
					_crcStack = 1;
					ExecDfs(_mprowslotL, _rgslotNextRow, _rgcol);
				}
			}
			int num9 = _crcWork;
			while (--num9 >= 0)
			{
				int num10 = _rgrcWork[num9];
				_rgfWork[num10] = false;
				Rational num11 = _rgnumWork[num10];
				if (!num11.IsZero)
				{
					Rational.Negate(ref num11);
					for (int num12 = _mprowslotL[num10]; num12 > 0; num12 = _rgslotNextRow[num12])
					{
						int num13 = _rgcol[num12];
						ref Rational reference3 = ref _rgnumWork[num13];
						reference3 = Rational.AddMul(_rgnumWork[num13], num11, _rgnum[num12]);
					}
				}
			}
			vec.Clear();
			int num14 = _crcWork;
			while (--num14 >= 0)
			{
				int num15 = _rgrcWork[num14];
				if (!_rgnumWork[num15].IsZero)
				{
					vec.SetCoefNonZero(_permRow.Map(num15), _rgnumWork[num15]);
					_rgnumWork[num15] = default(Rational);
				}
			}
		}

		private void SolveRowEta(VectorRational vec)
		{
			int num = base.EtaCount;
			while (--num >= 0)
			{
				Rational rational = 0;
				int num2 = _mpetaslotFirst[num];
				int num3 = _mpetaslotFirst[num + 1];
				int rc = _rgrowEta[num2];
				for (int i = num2 + 1; i < num3; i++)
				{
					int rc2 = _rgrowEta[i];
					Rational coef = vec.GetCoef(rc2);
					if (!coef.IsZero)
					{
						rational += coef * _rgnumEta[i];
					}
				}
				Rational rational2 = vec.GetCoef(rc) - rational;
				if (rational2.IsZero)
				{
					vec.RemoveCoef(rc);
				}
				else
				{
					vec.SetCoefNonZero(rc, rational2 / _rgnumEta[num2]);
				}
			}
		}

		/// <summary>
		/// Solve x.L.U = b by morphing rgnum from b to x.
		/// </summary>
		public void SolveApproxRow(VectorDouble vec, float numEps)
		{
			if (_rgnumWorkApprox == null || _rgnumWorkApprox.Length < _rcLim)
			{
				_rgnumWorkApprox = new double[_rcLim];
			}
			SolveApproxRowEta(vec, numEps);
			SolveApproxRowCore(vec, numEps);
		}

		private void SolveApproxRowCore(VectorDouble vec, float numEps)
		{
			_crcWork = 0;
			_crcStack = 0;
			Vector<double>.Iter iter = new Vector<double>.Iter(vec);
			while (iter.IsValid)
			{
				int num = _permCol.MapInverse(iter.Rc);
				_rgnumWorkApprox[num] = iter.Value;
				if (!_rgfWork[num])
				{
					_rgrcStack[0] = num;
					_rgslotNextStack[0] = _rgslotNextRow[_mprowslotU[num]];
					_crcStack = 1;
					ExecDfsSkip(_mprowslotU, _rgslotNextRow, _rgcol);
				}
				iter.Advance();
			}
			int num2 = _crcWork;
			while (--num2 >= 0)
			{
				int num3 = _rgrcWork[num2];
				_rgfWork[num3] = false;
				double num4 = _rgnumWorkApprox[num3];
				if (num4 == 0.0)
				{
					continue;
				}
				int num5 = _mprowslotU[num3];
				Rational rational = _rgnum[num5];
				if (!rational.IsOne)
				{
					num4 /= (double)rational;
					_rgnumWorkApprox[num3] = num4;
				}
				num4 = 0.0 - num4;
				while ((num5 = _rgslotNextRow[num5]) > 0)
				{
					int num6 = _rgcol[num5];
					double num7 = _rgnumWorkApprox[num6];
					double num8 = num7 + num4 * (double)_rgnum[num5];
					if (Math.Abs(num8) > (double)numEps * Math.Abs(num7))
					{
						_rgnumWorkApprox[num6] = num8;
					}
					else
					{
						_rgnumWorkApprox[num6] = 0.0;
					}
				}
			}
			int crcWork = _crcWork;
			Statics.Swap(ref _rgrcWork, ref _rgrcWork2);
			_crcWork = 0;
			_crcStack = 0;
			int num9 = crcWork;
			while (--num9 >= 0)
			{
				int num10 = _rgrcWork2[num9];
				if (_rgnumWorkApprox[num10] != 0.0 && !_rgfWork[num10])
				{
					_rgrcStack[0] = num10;
					_rgslotNextStack[0] = _mprowslotL[num10];
					_crcStack = 1;
					ExecDfs(_mprowslotL, _rgslotNextRow, _rgcol);
				}
			}
			int num11 = _crcWork;
			while (--num11 >= 0)
			{
				int num12 = _rgrcWork[num11];
				_rgfWork[num12] = false;
				double num13 = 0.0 - _rgnumWorkApprox[num12];
				if (num13 == 0.0)
				{
					continue;
				}
				for (int num14 = _mprowslotL[num12]; num14 > 0; num14 = _rgslotNextRow[num14])
				{
					int num15 = _rgcol[num14];
					double num16 = _rgnumWorkApprox[num15];
					double num17 = num16 + num13 * (double)_rgnum[num14];
					if (Math.Abs(num17) > (double)numEps * Math.Abs(num16))
					{
						_rgnumWorkApprox[num15] = num17;
					}
					else
					{
						_rgnumWorkApprox[num15] = 0.0;
					}
				}
			}
			vec.Clear();
			int num18 = _crcWork;
			while (--num18 >= 0)
			{
				int num19 = _rgrcWork[num18];
				if (_rgnumWorkApprox[num19] != 0.0)
				{
					vec.SetCoefNonZero(_permRow.Map(num19), _rgnumWorkApprox[num19]);
					_rgnumWorkApprox[num19] = 0.0;
				}
			}
		}

		private void SolveApproxRowEta(VectorDouble vec, float numEps)
		{
			int num = base.EtaCount;
			while (--num >= 0)
			{
				double num2 = 0.0;
				int num3 = _mpetaslotFirst[num];
				int num4 = _mpetaslotFirst[num + 1];
				int rc = _rgrowEta[num3];
				for (int i = num3 + 1; i < num4; i++)
				{
					int rc2 = _rgrowEta[i];
					double coef = vec.GetCoef(rc2);
					if (coef != 0.0)
					{
						double value = num2;
						num2 += coef * (double)_rgnumEta[i];
						if (Math.Abs(num2) <= (double)numEps * Math.Abs(value))
						{
							num2 = 0.0;
						}
					}
				}
				double num5 = vec.GetCoef(rc) - num2;
				if (Math.Abs(num5) <= (double)numEps * Math.Abs(num2))
				{
					vec.RemoveCoef(rc);
				}
				else
				{
					vec.SetCoefNonZero(rc, num5 / (double)_rgnumEta[num3]);
				}
			}
		}

		public void Update(int ivar, VectorRational vec)
		{
			UpdateCore(ivar, vec);
		}
	}
}
