using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class AlgorithmBase<Number>
	{
		protected const int kcnumMin = 20;

		protected int _cpiv1;

		protected int _cpiv2;

		protected int _cpivDegen;

		protected int _cvFactor;

		protected SimplexTask _thd;

		protected LogSource _log;

		protected SimplexReducedModel _mod;

		internal int _varLim;

		internal int _rowLim;

		protected SimplexFactoredBasis _bas;

		protected bool _fShiftBounds;

		protected Number[] _rgnumBasic;

		protected Number[] _mpvarnumLower;

		protected Number[] _mpvarnumUpper;

		public int PivotCount => _cpiv1 + _cpiv2;

		public int PivotCountPhaseOne => _cpiv1;

		public int PivotCountPhaseTwo => _cpiv2;

		public int PivotCountDegenerate => _cpivDegen;

		public int FactorCount => _cvFactor;

		public SimplexFactoredBasis Basis => _bas;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="thd"></param>
		protected AlgorithmBase(SimplexTask thd)
		{
			_thd = thd;
			_log = _thd.Solver.Logger;
			_fShiftBounds = _thd.ShiftBounds;
		}

		public virtual void Init(SimplexReducedModel mod, SimplexFactoredBasis bas)
		{
			_mod = mod;
			_varLim = _mod.VarLim;
			_rowLim = _mod.RowLim;
			_bas = bas;
			_cpiv1 = 0;
			_cpiv2 = 0;
			_cvFactor = 0;
			EnsureSize(ref _rgnumBasic, _rowLim);
			EnsureSize(ref _mpvarnumLower, _varLim);
			EnsureSize(ref _mpvarnumUpper, _varLim);
		}

		public Number GetLowerBound(int var)
		{
			return _mpvarnumLower[var];
		}

		public Number GetUpperBound(int var)
		{
			return _mpvarnumUpper[var];
		}

		public Number GetBasicValue(int ivar)
		{
			return _rgnumBasic[ivar];
		}

		public Number GetNonBasicValue(int var)
		{
			return GetVarBound(var, _bas.GetVvk(var));
		}

		public Number GetVarBound(int var, SimplexVarValKind vvk)
		{
			switch (vvk)
			{
			default:
				return default(Number);
			case SimplexVarValKind.Fixed:
			case SimplexVarValKind.Lower:
				return _mpvarnumLower[var];
			case SimplexVarValKind.Upper:
				return _mpvarnumUpper[var];
			}
		}

		public Number GetVarValue(int var)
		{
			switch (_bas.GetVvk(var))
			{
			default:
				return default(Number);
			case SimplexVarValKind.Basic:
				return _rgnumBasic[_bas.GetBasisSlot(var)];
			case SimplexVarValKind.Fixed:
			case SimplexVarValKind.Lower:
				return _mpvarnumLower[var];
			case SimplexVarValKind.Upper:
				return _mpvarnumUpper[var];
			}
		}

		private static void EnsureSize(ref Number[] rgnum, int cnum)
		{
			if (rgnum == null)
			{
				rgnum = new Number[cnum + 20];
			}
			else if (rgnum.Length < cnum)
			{
				rgnum = new Number[Math.Max(cnum + 20, rgnum.Length + rgnum.Length / 2)];
			}
		}
	}
}
