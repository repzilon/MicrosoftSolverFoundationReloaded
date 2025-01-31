using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class SimplexFactoredBasis : SimplexBasis
	{
		protected const int kcetaMaxRat = 10;

		protected const int kcetaMaxDbl = 50;

		private readonly LUFactorizationRational _lufRat;

		private readonly LUFactorizationDouble _lufDbl;

		private bool _fValidRat;

		private bool _fValidDbl;

		private bool _fBasisPermutedRat;

		private bool _fBasisPermutedDbl;

		private bool _fNonBasicChangedRat;

		private bool _fNonBasicChangedDbl;

		private int[] _rgvarBasicOther;

		public bool IsExactFactorizationValid => _fValidRat;

		public bool IsDoubleFactorizationValid => _fValidDbl;

		public bool IsExactBasisPermuted => _fBasisPermutedRat;

		public bool IsDoubleBasisPermuted => _fBasisPermutedDbl;

		public bool IsExactNonBasicChanged
		{
			get
			{
				return _fNonBasicChangedRat;
			}
			set
			{
				_fNonBasicChangedRat = value;
			}
		}

		public bool IsDoubleNonBasicChanged
		{
			get
			{
				return _fNonBasicChangedDbl;
			}
			set
			{
				_fNonBasicChangedDbl = value;
			}
		}

		public SimplexFactoredBasis(SimplexTask thd, SimplexReducedModel mod)
			: base(thd, mod)
		{
			_rgvarBasicOther = new int[mod.RowLim];
			_lufDbl = new LUFactorizationDouble();
			_lufRat = new LUFactorizationRational();
		}

		public SimplexFactoredBasis(SimplexTask thd, SimplexBasis basis)
			: base(thd, thd.Model)
		{
			Array.Copy(basis._rgvarBasic, _rgvarBasic, _rgvarBasic.Length);
			Array.Copy(basis._mpvarivar, _mpvarivar, _mpvarivar.Length);
			_rgvarBasicOther = new int[thd.Model.RowLim];
			_lufDbl = new LUFactorizationDouble();
			_lufRat = new LUFactorizationRational();
		}

		public LUFactorizationRational GetExactFactorization()
		{
			return _lufRat;
		}

		public LUFactorizationDouble GetDoubleFactorization()
		{
			return _lufDbl;
		}

		public void MinorPivot(int var, SimplexVarValKind vvkNew)
		{
			_mpvarivar[var] = 0 - vvkNew;
			_fNonBasicChangedRat = (_fNonBasicChangedDbl = true);
		}

		public FactorResultFlags RefactorExactBasis(Rational[] rgnumBasic, bool fForce)
		{
			if (!_thd.Params.NotifyStartFactorization(_thd.Tid, _thd, fDouble: false) && !fForce)
			{
				return FactorResultFlags.Abort;
			}
			FactorResultFlags factorResultFlags = FactorResultFlags.Completed;
			if (!_lufRat.Factor(_mod.Matrix, _mod.RowLim, _rgvarBasic))
			{
				_fValidDbl = false;
				factorResultFlags |= FactorResultFlags.Substituted;
			}
			Permutation columnPermutation = _lufRat.GetColumnPermutation();
			if (!columnPermutation.IsIdentity())
			{
				if (_fValidDbl && !_fBasisPermutedDbl)
				{
					Array.Copy(_rgvarBasic, _rgvarBasicOther, _rgvarBasic.Length);
					_fBasisPermutedDbl = true;
				}
				Permute(columnPermutation);
				columnPermutation.ApplyInverse(rgnumBasic);
				columnPermutation.Clear();
				factorResultFlags |= FactorResultFlags.Permuted;
			}
			_fValidRat = true;
			_fBasisPermutedRat = false;
			if (!_thd.Params.NotifyEndFactorization(_thd.Tid, _thd, fDouble: false) && !fForce)
			{
				factorResultFlags |= FactorResultFlags.Abort;
			}
			return factorResultFlags;
		}

		public FactorResultFlags RefactorDoubleBasis(double[] rgnumBasic)
		{
			if (!_thd.Params.NotifyStartFactorization(_thd.Tid, _thd, fDouble: true))
			{
				return FactorResultFlags.Abort;
			}
			FactorResultFlags factorResultFlags = FactorResultFlags.Completed;
			if (!_lufDbl.Factor(_mod.Matrix, _mod.RowLim, _rgvarBasic))
			{
				_fValidRat = false;
				factorResultFlags |= FactorResultFlags.Substituted;
			}
			Permutation columnPermutation = _lufDbl.GetColumnPermutation();
			if (!columnPermutation.IsIdentity())
			{
				if (_fValidRat && !_fBasisPermutedRat)
				{
					Array.Copy(_rgvarBasic, _rgvarBasicOther, _rgvarBasic.Length);
					_fBasisPermutedRat = true;
				}
				Permute(columnPermutation);
				columnPermutation.ApplyInverse(rgnumBasic);
				columnPermutation.Clear();
				factorResultFlags |= FactorResultFlags.Permuted;
			}
			_fValidDbl = true;
			_fBasisPermutedDbl = false;
			if (!_thd.Params.NotifyEndFactorization(_thd.Tid, _thd, fDouble: true))
			{
				factorResultFlags |= FactorResultFlags.Abort;
			}
			return factorResultFlags;
		}

		public FactorResultFlags MajorPivot(int varEnter, int ivarLeave, int varLeave, SimplexVarValKind vvkLeave, VectorRational vecDelta, Rational[] rgnumBasic)
		{
			MajorPivot(varEnter, ivarLeave, varLeave, vvkLeave);
			_fValidDbl = false;
			if (_lufRat.EtaCount < 10)
			{
				_lufRat.Update(ivarLeave, vecDelta);
				return FactorResultFlags.None;
			}
			return RefactorExactBasis(rgnumBasic, fForce: false);
		}

		public FactorResultFlags MajorPivot(int varEnter, int ivarLeave, int varLeave, SimplexVarValKind vvkLeave, VectorDouble vecDelta, double[] rgnumBasic)
		{
			MajorPivot(varEnter, ivarLeave, varLeave, vvkLeave);
			_fValidRat = false;
			if (_lufDbl.EtaCount < 50)
			{
				_lufDbl.Update(ivarLeave, vecDelta);
				return FactorResultFlags.None;
			}
			return RefactorDoubleBasis(rgnumBasic);
		}

		public void SetDoubleFactorThresholds(double numZeroRatio, double numPivRatio, double numVarEpsilon)
		{
			_lufDbl.ZeroRatioThreshold = numZeroRatio;
			_lufDbl.PivotRatioThreshold = numPivRatio;
			_lufDbl.NonZeroThreshold = numVarEpsilon;
		}

		public void RepairPermutedExactBasis()
		{
			Statics.Swap(ref _rgvarBasic, ref _rgvarBasicOther);
			_fBasisPermutedDbl = true;
			_fBasisPermutedRat = false;
			int num = _mod.RowLim;
			while (--num >= 0)
			{
				int num2 = _rgvarBasic[num];
				_mpvarivar[num2] = num;
			}
		}

		public void RepairPermutedDoubleBasis()
		{
			Statics.Swap(ref _rgvarBasic, ref _rgvarBasicOther);
			_fBasisPermutedRat = true;
			_fBasisPermutedDbl = false;
			int num = _mod.RowLim;
			while (--num >= 0)
			{
				int num2 = _rgvarBasic[num];
				_mpvarivar[num2] = num;
			}
		}

		public void SetTo(SimplexBasis bas)
		{
			int num = SetTo(bas, _fValidRat | _fValidDbl);
			if (num == 1)
			{
				_fNonBasicChangedRat = (_fNonBasicChangedDbl = true);
			}
			else if (num >= 2)
			{
				_fValidRat = (_fValidDbl = false);
			}
		}

		public void InplaceSolveCol(Rational[] rgnum)
		{
			_lufRat.SolveCol(rgnum);
		}

		public void InplaceSolveCol(VectorRational vec)
		{
			_lufRat.SolveCol(vec);
		}

		public void InplaceSolveRow(VectorRational vec)
		{
			_lufRat.SolveRow(vec);
		}

		public void InplaceSolveApproxRow(VectorDouble vec)
		{
			_lufRat.SolveApproxRow(vec, 1E-12f);
		}

		/// <summary> Solve L.U.x = b by morphing rgnum from b to x.
		/// </summary>
		public void InplaceSolveCol(double[] rgnum)
		{
			_lufDbl.SolveCol(rgnum);
		}

		/// <summary> Solve L.U.x = b by morphing vec from b to x.
		/// </summary>
		public void InplaceSolveCol(VectorDouble vec)
		{
			_lufDbl.SolveCol(vec);
		}

		public void InplaceSolveRow(VectorDouble vec)
		{
			_lufDbl.SolveRow(vec);
		}
	}
}
