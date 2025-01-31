using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal sealed class LUFactorizationDouble : LUFactorization<double>
	{
		private const double kepsVerify = 1E-06;

		private float _numMinNonZero;

		private float _numZeroRatio;

		private float _numPivRatio;

		private double[] _mprownumMax;

		public double NonZeroThreshold
		{
			get
			{
				return _numMinNonZero;
			}
			set
			{
				_numMinNonZero = (float)value;
			}
		}

		public double ZeroRatioThreshold
		{
			get
			{
				return _numZeroRatio;
			}
			set
			{
				_numZeroRatio = (float)value;
			}
		}

		public double PivotRatioThreshold
		{
			get
			{
				return _numPivRatio;
			}
			set
			{
				_numPivRatio = (float)value;
			}
		}

		public LUFactorizationDouble()
		{
			_numMinNonZero = 1E-09f;
			_numZeroRatio = 1E-12f;
			_numPivRatio = 0.01f;
		}

		protected override void Init(int rowLim, int cslotInit)
		{
			if (_mprownumMax == null || _mprownumMax.Length < rowLim)
			{
				_mprownumMax = new double[rowLim];
			}
			else
			{
				Array.Clear(_mprownumMax, 0, rowLim);
			}
			base.Init(rowLim, cslotInit);
		}

		protected override bool FuzzyVerify(double num1, double num2)
		{
			double num3 = Math.Abs(num1 - num2);
			if (num3 <= 1E-06)
			{
				return true;
			}
			double num4 = Math.Max(Math.Abs(num1), Math.Abs(num2));
			return num3 / num4 <= 1E-06;
		}

		protected override bool AddToMulPivot(ref double numDst, double num1, double num2)
		{
			if (SlamToZero(numDst, numDst += num1 * num2))
			{
				numDst = 0.0;
				return true;
			}
			return false;
		}

		private bool SlamToZero(double numSrc, double numDst)
		{
			return Math.Abs(numDst) <= (double)_numZeroRatio * Math.Abs(numSrc);
		}

		protected override double GetValue(ref CoefMatrix.ColIter cit)
		{
			return cit.Approx;
		}

		protected override double GetValue(CoefMatrix mat, int row, int col)
		{
			return mat.GetCoefDouble(row, col);
		}

		private double SetMaxForRow(int row)
		{
			double num = 0.0;
			for (int num2 = _mprowlin[row].slotFirst; num2 > 0; num2 = _rgslotNextRow[num2])
			{
				double num3 = Math.Abs(_rgnum[num2]);
				if (num < num3)
				{
					num = num3;
				}
			}
			return _mprownumMax[row] = num;
		}

		protected override void AddEntry(int row, int col, double num)
		{
			if (_mprowlin[row].slotFirst == 0)
			{
				_mprownumMax[row] = Math.Abs(num);
			}
			else if (_mprownumMax[row] > 0.0)
			{
				double num2 = Math.Abs(num);
				if (_mprownumMax[row] < num2)
				{
					_mprownumMax[row] = num2;
				}
			}
			base.AddEntry(row, col, num);
		}

		protected override void NotifyRowRecalc(int row)
		{
			_mprownumMax[row] = 0.0;
		}

		protected override int GetBestPivotInCol(int col)
		{
			int result = 0;
			int num = int.MaxValue;
			double num2 = 0.0;
			for (int num3 = _mpcollin[col].slotFirst; num3 > 0; num3 = _rgslotNextCol[num3])
			{
				int num4 = _rgrow[num3];
				int cslot = _mprowlin[num4].cslot;
				if (cslot <= num)
				{
					if (_mprownumMax[num4] == 0.0)
					{
						SetMaxForRow(num4);
					}
					double num5 = Math.Abs(_rgnum[num3]);
					if (!(num5 < (double)_numPivRatio * _mprownumMax[num4]) && (cslot != num || !(num5 <= num2)))
					{
						result = num3;
						num = cslot;
						num2 = num5;
					}
				}
			}
			return result;
		}

		protected override int GetBestPivotInRow(int row)
		{
			double num = _mprownumMax[row];
			if (num == 0.0)
			{
				num = SetMaxForRow(row);
			}
			double num2 = (double)_numPivRatio * num;
			int result = 0;
			int num3 = int.MaxValue;
			double num4 = 0.0;
			for (int num5 = _mprowlin[row].slotFirst; num5 > 0; num5 = _rgslotNextRow[num5])
			{
				int num6 = _rgcol[num5];
				int cslot = _mpcollin[num6].cslot;
				if (cslot <= num3)
				{
					double num7 = Math.Abs(_rgnum[num5]);
					if (!(num7 < num2) && (cslot != num3 || !(num7 <= num4)))
					{
						result = num5;
						num3 = cslot;
						num4 = num7;
					}
				}
			}
			return result;
		}

		/// <summary> Solve L.U.x = b by morphing rgnum from b to x.
		/// </summary>
		public void SolveCol(double[] rgnum)
		{
			_permRow.ApplyInverse(rgnum);
			for (int i = 0; i < _rcLim; i++)
			{
				double num = rgnum[i];
				if (num == 0.0)
				{
					continue;
				}
				for (int num2 = _mpcolslotL[i]; num2 > 0; num2 = _rgslotNextCol[num2])
				{
					int num3 = _rgrow[num2];
					if (SlamToZero(rgnum[num3], rgnum[num3] -= num * _rgnum[num2]))
					{
						rgnum[num3] = 0.0;
					}
				}
			}
			int num4 = _rcLim;
			while (--num4 >= 0)
			{
				double num5 = rgnum[num4];
				if (num5 == 0.0)
				{
					continue;
				}
				double num6 = _rgnum[_mprowslotU[num4]];
				if (num6 != 1.0)
				{
					num5 = (rgnum[num4] = num5 / num6);
				}
				for (int num7 = _mpcolslotU[num4]; num7 > 0; num7 = _rgslotNextCol[num7])
				{
					int num8 = _rgrow[num7];
					if (SlamToZero(rgnum[num8], rgnum[num8] -= num5 * _rgnum[num7]))
					{
						rgnum[num8] = 0.0;
					}
				}
			}
			_permCol.Apply(rgnum);
			SolveColEta(rgnum);
		}

		private void SolveColEta(double[] rgnum)
		{
			int etaCount = base.EtaCount;
			for (int i = 0; i < etaCount; i++)
			{
				int num = _mpetaslotFirst[i];
				int num2 = _rgrowEta[num];
				double num3 = rgnum[num2];
				if (num3 == 0.0)
				{
					continue;
				}
				int num4 = _mpetaslotFirst[i + 1];
				double num5 = _rgnumEta[num];
				if (num5 != 1.0)
				{
					num3 = (rgnum[num2] = num3 / num5);
				}
				while (++num < num4)
				{
					int num6 = _rgrowEta[num];
					if (SlamToZero(rgnum[num6], rgnum[num6] -= num3 * _rgnumEta[num]))
					{
						rgnum[num6] = 0.0;
					}
				}
			}
		}

		/// <summary> Solve L.U.x = b by morphing vec from b to x.
		/// </summary>
		public void SolveCol(VectorDouble vec)
		{
			SolveColCore(vec);
			SolveColEta(vec);
		}

		private void SolveColCore(VectorDouble vec)
		{
			_crcWork = 0;
			_crcStack = 0;
			Vector<double>.Iter iter = new Vector<double>.Iter(vec);
			while (iter.IsValid)
			{
				int num = _permRow.MapInverse(iter.Rc);
				_rgnumWork[num] = iter.Value;
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
				double num4 = _rgnumWork[num3];
				if (num4 == 0.0)
				{
					continue;
				}
				for (int num5 = _mpcolslotL[num3]; num5 > 0; num5 = _rgslotNextCol[num5])
				{
					int num6 = _rgrow[num5];
					if (SlamToZero(_rgnumWork[num6], _rgnumWork[num6] -= num4 * _rgnum[num5]))
					{
						_rgnumWork[num6] = 0.0;
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
				if (_rgnumWork[num8] != 0.0 && !_rgfWork[num8])
				{
					_rgrcStack[0] = num8;
					_rgslotNextStack[0] = _mpcolslotU[num8];
					_crcStack = 1;
					ExecDfs(_mpcolslotU, _rgslotNextCol, _rgrow);
				}
			}
			int num9 = _crcWork;
			while (--num9 >= 0)
			{
				int num10 = _rgrcWork[num9];
				_rgfWork[num10] = false;
				double num11 = _rgnumWork[num10];
				if (num11 == 0.0)
				{
					continue;
				}
				double num12 = _rgnum[_mprowslotU[num10]];
				if (num12 != 1.0)
				{
					num11 /= num12;
					_rgnumWork[num10] = num11;
				}
				for (int num13 = _mpcolslotU[num10]; num13 > 0; num13 = _rgslotNextCol[num13])
				{
					int num14 = _rgrow[num13];
					if (SlamToZero(_rgnumWork[num14], _rgnumWork[num14] -= num11 * _rgnum[num13]))
					{
						_rgnumWork[num14] = 0.0;
					}
				}
			}
			vec.Clear();
			int num15 = _crcWork;
			while (--num15 >= 0)
			{
				int num16 = _rgrcWork[num15];
				double num17 = _rgnumWork[num16];
				if (num17 != 0.0)
				{
					vec.SetCoefNonZero(_permCol.Map(num16), num17);
					_rgnumWork[num16] = 0.0;
				}
			}
		}

		private void SolveColEta(VectorDouble vec)
		{
			int etaCount = base.EtaCount;
			for (int i = 0; i < etaCount; i++)
			{
				int num = _mpetaslotFirst[i];
				int rc = _rgrowEta[num];
				double num2 = vec.GetCoef(rc);
				if (num2 == 0.0)
				{
					continue;
				}
				int num3 = _mpetaslotFirst[i + 1];
				double num4 = _rgnumEta[num];
				if (num4 != 1.0)
				{
					num2 /= num4;
					vec.SetCoefNonZero(rc, num2);
				}
				while (++num < num3)
				{
					int rc2 = _rgrowEta[num];
					num4 = num2 * _rgnumEta[num];
					double coef = vec.GetCoef(rc2);
					num4 = coef - num4;
					if (Math.Abs(num4) > (double)_numMinNonZero * Math.Abs(coef))
					{
						vec.SetCoefNonZero(rc2, num4);
					}
					else
					{
						vec.RemoveCoef(rc2);
					}
				}
			}
		}

		/// <summary>
		/// Solve x.L.U = b by morphing vec from b to x.
		/// </summary>
		public void SolveRow(VectorDouble vec)
		{
			SolveRowEta(vec);
			SolveRowCore(vec);
		}

		private void SolveRowCore(VectorDouble vec)
		{
			_crcWork = 0;
			_crcStack = 0;
			Vector<double>.Iter iter = new Vector<double>.Iter(vec);
			while (iter.IsValid)
			{
				int num = _permCol.MapInverse(iter.Rc);
				_rgnumWork[num] = iter.Value;
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
				double num4 = _rgnumWork[num3];
				if (num4 == 0.0)
				{
					continue;
				}
				int num5 = _mprowslotU[num3];
				double num6 = _rgnum[num5];
				if (num6 != 1.0)
				{
					num4 /= num6;
					_rgnumWork[num3] = num4;
				}
				while ((num5 = _rgslotNextRow[num5]) > 0)
				{
					int num7 = _rgcol[num5];
					double num8 = _rgnumWork[num7];
					double num9 = num8 - num4 * _rgnum[num5];
					if (Math.Abs(num9) > (double)_numMinNonZero * Math.Abs(num8))
					{
						_rgnumWork[num7] = num9;
					}
					else
					{
						_rgnumWork[num7] = 0.0;
					}
				}
			}
			int crcWork = _crcWork;
			Statics.Swap(ref _rgrcWork, ref _rgrcWork2);
			_crcWork = 0;
			_crcStack = 0;
			int num10 = crcWork;
			while (--num10 >= 0)
			{
				int num11 = _rgrcWork2[num10];
				if (_rgnumWork[num11] != 0.0 && !_rgfWork[num11])
				{
					_rgrcStack[0] = num11;
					_rgslotNextStack[0] = _mprowslotL[num11];
					_crcStack = 1;
					ExecDfs(_mprowslotL, _rgslotNextRow, _rgcol);
				}
			}
			int num12 = _crcWork;
			while (--num12 >= 0)
			{
				int num13 = _rgrcWork[num12];
				_rgfWork[num13] = false;
				double num14 = _rgnumWork[num13];
				if (num14 == 0.0)
				{
					continue;
				}
				for (int num15 = _mprowslotL[num13]; num15 > 0; num15 = _rgslotNextRow[num15])
				{
					int num16 = _rgcol[num15];
					double num17 = num14 * _rgnum[num15];
					double num18 = _rgnumWork[num16];
					num17 = num18 - num17;
					if (Math.Abs(num17) > (double)_numMinNonZero * Math.Abs(num18))
					{
						_rgnumWork[num16] = num17;
					}
					else
					{
						_rgnumWork[num16] = 0.0;
					}
				}
			}
			vec.Clear();
			int num19 = _crcWork;
			while (--num19 >= 0)
			{
				int num20 = _rgrcWork[num19];
				double num21 = _rgnumWork[num20];
				if (num21 != 0.0)
				{
					vec.SetCoefNonZero(_permRow.Map(num20), num21);
					_rgnumWork[num20] = 0.0;
				}
			}
		}

		private void SolveRowEta(VectorDouble vec)
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
						num2 += coef * _rgnumEta[i];
						if (Math.Abs(num2) <= (double)_numMinNonZero * Math.Abs(value))
						{
							num2 = 0.0;
						}
					}
				}
				double num5 = vec.GetCoef(rc) - num2;
				if (Math.Abs(num5) <= (double)_numMinNonZero * Math.Abs(num2))
				{
					vec.RemoveCoef(rc);
				}
				else
				{
					vec.SetCoefNonZero(rc, num5 / _rgnumEta[num3]);
				}
			}
		}

		public void Update(int ivar, VectorDouble vec)
		{
			UpdateCore(ivar, vec);
		}
	}
}
