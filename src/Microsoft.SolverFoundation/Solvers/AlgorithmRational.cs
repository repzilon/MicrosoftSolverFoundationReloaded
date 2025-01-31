using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class AlgorithmRational : AlgorithmBase<Rational>
	{
		protected VectorRational _vecCost;

		protected bool _fValidValues;

		protected bool _fValidReducedCosts;

		protected AlgorithmRational(SimplexTask thd)
			: base(thd)
		{
		}

		public override void Init(SimplexReducedModel mod, SimplexFactoredBasis bas)
		{
			base.Init(mod, bas);
			_vecCost = new VectorRational(_mod.VarLim);
			_fValidValues = false;
			_fValidReducedCosts = false;
		}

		public bool TryForValidValues(bool fPermute)
		{
			if (!_fValidValues)
			{
				return false;
			}
			if (!_bas.IsExactFactorizationValid)
			{
				return false;
			}
			if (_bas.IsExactNonBasicChanged)
			{
				return false;
			}
			if (_bas.IsExactBasisPermuted)
			{
				if (!fPermute)
				{
					return false;
				}
				_bas.RepairPermutedExactBasis();
			}
			return true;
		}

		public Rational Cost(int var)
		{
			return _vecCost.GetCoef(var);
		}

		public VectorRational GetCostVector()
		{
			return _vecCost;
		}

		public virtual void ComputeExactVars()
		{
			InitBounds(null, out var _, out var _);
			EnsureExactVars();
		}

		protected virtual void EnsureFactorization()
		{
			_thd.CheckDone();
			if (_bas.IsExactFactorizationValid)
			{
				if (_bas.IsExactBasisPermuted)
				{
					_bas.RepairPermutedExactBasis();
				}
				if (_bas.IsExactNonBasicChanged)
				{
					_fValidValues = false;
				}
			}
			else
			{
				_fValidValues = false;
				_fValidReducedCosts = false;
				FactorBasis(fForce: true);
			}
		}

		protected virtual void EnsureExactVars()
		{
			EnsureFactorization();
			if (!_fValidValues)
			{
				ComputeBasicValues();
			}
		}

		protected virtual void InitBounds(OptimalGoalValues ogvMin, out int igiCur, out bool fStrict)
		{
			_thd.CheckDone();
			_thd.BoundManager.GetLowerBounds(_mpvarnumLower);
			_thd.BoundManager.GetUpperBounds(_mpvarnumUpper);
			InitGoalBounds(ogvMin, out igiCur, out fStrict);
		}

		protected virtual void InitGoalBounds(int igiCur)
		{
			InitGoalBounds(null, out var _, out var _);
		}

		protected virtual void InitGoalBounds(OptimalGoalValues ogvMin, out int igiCur, out bool fStrict)
		{
			fStrict = ogvMin == null;
			for (igiCur = 0; igiCur < _mod.GoalCount; igiCur++)
			{
				Rational num = _thd.OptimalGoalValues[igiCur];
				if (num.IsIndeterminate)
				{
					break;
				}
				if (!fStrict)
				{
					int num2 = num.CompareTo(ogvMin[igiCur]);
					if (num2 < 0)
					{
						fStrict = true;
					}
				}
				int goalVar = _mod.GetGoalVar(igiCur);
				if (!_mod.IsGoalMinimize(igiCur))
				{
					Rational.Negate(ref num);
				}
				ref Rational reference = ref _mpvarnumLower[goalVar];
				reference = (_mpvarnumUpper[goalVar] = num);
			}
		}

		public abstract bool RunSimplex(int cpivMax, bool fStopAtNextGoal, OptimalGoalValues ogvMin, out LinearResult res);

		public bool IsPrimalFeasible()
		{
			_thd.CheckDone();
			for (int i = 0; i < _mod.RowLim; i++)
			{
				int basicVar = _bas.GetBasicVar(i);
				Rational rational = _rgnumBasic[i];
				if (!(_mpvarnumLower[basicVar] <= rational) || !(rational <= _mpvarnumUpper[basicVar]))
				{
					return false;
				}
			}
			return true;
		}

		protected void ComputeBasicValues()
		{
			ComputeBasicValues(_rgnumBasic);
			_fValidValues = true;
			_bas.IsExactNonBasicChanged = false;
		}

		protected void ComputeBasicValues(Rational[] rgnumDst)
		{
			_thd.CheckDone();
			Array.Clear(rgnumDst, 0, _mod.RowLim);
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_mod.GetRhs());
			while (iter.IsValid)
			{
				ref Rational reference = ref rgnumDst[iter.Rc];
				reference = iter.Value;
				iter.Advance();
			}
			for (int i = 0; i < _mod.VarLim; i++)
			{
				Rational num;
				switch (_bas.GetVvk(i))
				{
				case SimplexVarValKind.Fixed:
				case SimplexVarValKind.Lower:
					num = _mpvarnumLower[i];
					break;
				case SimplexVarValKind.Upper:
					num = _mpvarnumUpper[i];
					break;
				default:
					continue;
				}
				if (!num.IsZero)
				{
					Rational.Negate(ref num);
					CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mod.Matrix, i);
					while (colIter.IsValid)
					{
						int row = colIter.Row;
						ref Rational reference2 = ref rgnumDst[row];
						reference2 = Rational.AddMul(rgnumDst[row], num, colIter.Exact);
						colIter.Advance();
					}
				}
			}
			_bas.InplaceSolveCol(rgnumDst);
		}

		protected SimplexVarValKind DetermineVvk(int var, Rational numSrc)
		{
			if (_mpvarnumLower[var] == _mpvarnumUpper[var])
			{
				return SimplexVarValKind.Fixed;
			}
			if (numSrc - _mpvarnumLower[var] < _mpvarnumUpper[var] - numSrc)
			{
				return SimplexVarValKind.Lower;
			}
			if (!_mpvarnumUpper[var].IsFinite)
			{
				return SimplexVarValKind.Zero;
			}
			return SimplexVarValKind.Upper;
		}

		protected FactorResultFlags FactorBasis(bool fForce)
		{
			_thd.CheckDone();
			FactorResultFlags factorResultFlags = _bas.RefactorExactBasis(_rgnumBasic, fForce);
			if ((factorResultFlags & FactorResultFlags.Completed) != 0)
			{
				PostFactor(factorResultFlags);
			}
			return factorResultFlags;
		}

		protected virtual void PostFactor(FactorResultFlags flags)
		{
			_thd.CheckDone();
			if ((flags & FactorResultFlags.Substituted) != 0)
			{
				LUFactorizationRational exactFactorization = _bas.GetExactFactorization();
				int eliminatedColumnCount = exactFactorization.EliminatedColumnCount;
				_log.LogEvent(4, Resources.FactoringFailedSubstituted0SlackVariables, eliminatedColumnCount);
				for (int i = 0; i < eliminatedColumnCount; i++)
				{
					exactFactorization.GetEliminatedEntry(i, out var row, out var col);
					int slackVarForRow = _mod.GetSlackVarForRow(row);
					int basicVar = _bas.GetBasicVar(col);
					SimplexVarValKind vvkLeave = ((basicVar < 0) ? SimplexVarValKind.Zero : DetermineVvk(basicVar, _rgnumBasic[col]));
					_log.LogEvent(4, Resources.Replacing0With1InBasis, basicVar, slackVarForRow);
					_bas.RepairBasis(slackVarForRow, col, basicVar, vvkLeave);
					Rational coefExact = _mod.Matrix.GetCoefExact(row, slackVarForRow);
					exactFactorization.SetEliminatedEntryValue(i, coefExact);
				}
			}
			_cvFactor++;
		}

		protected static void AddToScale(Rational[] rgnumDst, int cnum, Rational num, VectorRational rgnumSrc)
		{
			if (!num.IsZero)
			{
				Vector<Rational>.Iter iter = new Vector<Rational>.Iter(rgnumSrc);
				while (iter.IsValid)
				{
					rgnumDst[iter.Rc] += num * iter.Value;
					iter.Advance();
				}
			}
		}

		protected void VerifyBasisSolveCol(Rational[] rgnumSrc, Rational[] rgnumDst)
		{
			Rational[] rgnum = _thd.GetTempArrayExact(_mod.RowLim, fClear: true);
			for (int i = 0; i < _mod.RowLim; i++)
			{
				Rational ratMul = rgnumSrc[i];
				if (!ratMul.IsZero)
				{
					int basicVar = _bas.GetBasicVar(i);
					CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mod.Matrix, basicVar);
					while (colIter.IsValid)
					{
						ref Rational reference = ref rgnum[colIter.Row];
						reference = Rational.AddMul(rgnum[colIter.Row], ratMul, colIter.Exact);
						colIter.Advance();
					}
				}
			}
			_thd.Solver.VerifySame(rgnumDst, rgnum, _mod.RowLim, 1, Resources.ErrorInBasisSolveOperation012);
			_thd.ReleaseTempArray(ref rgnum);
		}

		protected void VerifyBasisSolveRow(Rational[] rgnumSrc, Rational[] rgnumDst)
		{
			for (int i = 0; i < _mod.RowLim; i++)
			{
				int basicVar = _bas.GetBasicVar(i);
				Rational rational = 0;
				CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mod.Matrix, basicVar);
				while (colIter.IsValid)
				{
					rational = Rational.AddMul(rational, rgnumSrc[colIter.Row], colIter.Exact);
					colIter.Advance();
				}
				if (rational != rgnumDst[i])
				{
					_log.LogEvent(1, "Error in basis row solve operation! {0} {1} {2}", i, rgnumDst[i], rational);
					break;
				}
			}
		}
	}
}
