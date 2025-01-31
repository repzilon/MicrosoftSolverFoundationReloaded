using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class AlgorithmDouble : AlgorithmBase<double>
	{
		protected VectorDouble _vecCost;

		protected bool _fValidValues;

		protected double _numCostEpsilon;

		protected double _numVarEpsilon;

		protected double _numZeroRatio;

		protected double _pricingEpsilon;

		public double CostEpsilon => _numCostEpsilon;

		public double VarEpsilon => _numVarEpsilon;

		public double VarZeroRatio => _numZeroRatio;

		protected AlgorithmDouble(SimplexTask thd)
			: base(thd)
		{
		}

		public override void Init(SimplexReducedModel mod, SimplexFactoredBasis bas)
		{
			base.Init(mod, bas);
			_vecCost = new VectorDouble(_varLim);
			_fValidValues = false;
		}

		public bool TryForValidValues(bool fPermute)
		{
			if (!_fValidValues)
			{
				return false;
			}
			if (!_bas.IsDoubleFactorizationValid)
			{
				return false;
			}
			if (_bas.IsDoubleNonBasicChanged)
			{
				return false;
			}
			if (_bas.IsDoubleBasisPermuted)
			{
				if (!fPermute)
				{
					return false;
				}
				_bas.RepairPermutedDoubleBasis();
			}
			return true;
		}

		public double Cost(int var)
		{
			return _vecCost.GetCoef(var);
		}

		public VectorDouble GetCostVector()
		{
			return _vecCost;
		}

		protected virtual void InitBounds(out int igiCur)
		{
			_thd.CheckDone();
			_thd.BoundManager.GetLowerBoundsDbl(_mpvarnumLower);
			_thd.BoundManager.GetUpperBoundsDbl(_mpvarnumUpper);
			InitGoalBounds(out igiCur);
		}

		protected virtual void InitGoalBounds(int igiCur)
		{
			InitGoalBounds(out var _);
		}

		protected virtual void InitGoalBounds(out int igiCur)
		{
			for (igiCur = 0; igiCur < _mod.GoalCount; igiCur++)
			{
				Rational num = _thd.OptimalGoalValues[igiCur];
				if (!num.IsFinite)
				{
					break;
				}
				if (!_mod.IsGoalMinimize(igiCur))
				{
					Rational.Negate(ref num);
				}
				int goalVar = _mod.GetGoalVar(igiCur);
				_mpvarnumUpper[goalVar] = (_mpvarnumLower[goalVar] = _mod.MapValueFromExactToDouble(goalVar, num));
			}
		}

		public void RunSimplex(bool fStopAtNextGoal, bool restart, out LinearResult res)
		{
			InitBounds(out var igiCur);
			_pricingEpsilon = 1E-26;
			do
			{
				_thd.CheckDone();
				PrimalDouble primalDouble = this as PrimalDouble;
				if (primalDouble != null)
				{
					_numCostEpsilon = 1.4901161193847656E-08;
					_numVarEpsilon = 2.384185791015625E-07;
				}
				else
				{
					_numCostEpsilon = 1E-28;
					_numVarEpsilon = 1.4901161193847656E-08;
				}
				if (_thd.Params.UserOveride)
				{
					if (_thd.Params.UserOverideCostEps)
					{
						_numCostEpsilon = _thd.Params.CostTolerance;
					}
					if (_thd.Params.UserOverideVarEps)
					{
						_numVarEpsilon = _thd.Params.VariableFeasibilityTolerance;
					}
				}
				double num = 0.125;
				int num2 = 0;
				while (true)
				{
					_numZeroRatio = _numVarEpsilon / 128.0;
					_bas.SetDoubleFactorThresholds(_numZeroRatio, num, _numVarEpsilon);
					res = RunSimplexCore(igiCur, restart);
					if (!SimplexSolver.IsComplete(res))
					{
						return;
					}
					if (++num2 > 2)
					{
						break;
					}
					if (!_thd.Params.UserOveride)
					{
						if (primalDouble != null)
						{
							_numCostEpsilon /= 16.0;
							_numVarEpsilon /= 64.0;
						}
						else
						{
							_numCostEpsilon /= 4.0;
							_numVarEpsilon /= 32.0;
						}
					}
					num *= 2.0;
					_fShiftBounds = false;
				}
				_thd.CheckDone();
				if (res != LinearResult.Optimal || igiCur >= _mod.GoalCount || fStopAtNextGoal)
				{
					break;
				}
				int goalVar = _mod.GetGoalVar(igiCur);
				double varValue = GetVarValue(goalVar);
				Rational num3 = _mod.MapValueFromDoubleToExact(goalVar, varValue);
				if (!_mod.IsGoalMinimize(igiCur))
				{
					Rational.Negate(ref num3);
				}
				_mpvarnumUpper[goalVar] = (_mpvarnumLower[goalVar] = varValue);
				_thd.OptimalGoalValues[igiCur] = num3;
			}
			while (++igiCur < _mod.GoalCount);
		}

		protected abstract LinearResult RunSimplexCore(int igiCur, bool restart);

		/// <summary> This computes basic variable values assuming _bas is set correctly
		///           and the basis is factored.  Results in _rgnumBasic.
		/// </summary>
		protected void ComputeBasicValues()
		{
			ComputeBasicValues(_rgnumBasic);
			_fValidValues = true;
			_bas.IsDoubleNonBasicChanged = false;
		}

		/// <summary> This computes basic variable values assuming _bas is set correctly
		///           and the basis is factored.
		///           rgnumDst must be values in order that they appear in the basis.
		/// </summary>
		protected void ComputeBasicValues(double[] rgnumDst)
		{
			_thd.CheckDone();
			Array.Clear(rgnumDst, 0, _mod.RowLim);
			Vector<double>.Iter iter = new Vector<double>.Iter(_mod.GetRhsDbl());
			while (iter.IsValid)
			{
				rgnumDst[iter.Rc] = iter.Value;
				iter.Advance();
			}
			for (int i = 0; i < _mod.VarLim; i++)
			{
				double num;
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
				if (num != 0.0)
				{
					CoefMatrix.ColIter colIter = new CoefMatrix.ColIter(_mod.Matrix, i);
					while (colIter.IsValid)
					{
						rgnumDst[colIter.Row] -= num * colIter.Approx;
						colIter.Advance();
					}
				}
			}
			_bas.InplaceSolveCol(rgnumDst);
		}

		internal SimplexVarValKind DetermineVvk(int var, double num)
		{
			if (_mpvarnumLower[var] == _mpvarnumUpper[var])
			{
				return SimplexVarValKind.Fixed;
			}
			if (num - _mpvarnumLower[var] < _mpvarnumUpper[var] - num)
			{
				return SimplexVarValKind.Lower;
			}
			if (!NumberUtils.IsFinite(_mpvarnumUpper[var]))
			{
				return SimplexVarValKind.Zero;
			}
			return SimplexVarValKind.Upper;
		}

		internal FactorResultFlags FactorBasis()
		{
			_thd.CheckDone();
			FactorResultFlags factorResultFlags = _bas.RefactorDoubleBasis(_rgnumBasic);
			if ((factorResultFlags & FactorResultFlags.Completed) != 0)
			{
				PostFactor(factorResultFlags);
			}
			return factorResultFlags;
		}

		internal void PostFactor(FactorResultFlags flags)
		{
			_thd.CheckDone();
			if ((flags & FactorResultFlags.Substituted) != 0)
			{
				LUFactorizationDouble doubleFactorization = _bas.GetDoubleFactorization();
				int eliminatedColumnCount = doubleFactorization.EliminatedColumnCount;
				_log.LogEvent(4, Resources.FactoringFailedSubstituted0SlackVariables, eliminatedColumnCount);
				for (int i = 0; i < eliminatedColumnCount; i++)
				{
					doubleFactorization.GetEliminatedEntry(i, out var row, out var col);
					int slackVarForRow = _mod.GetSlackVarForRow(row);
					int basicVar = _bas.GetBasicVar(col);
					SimplexVarValKind vvkLeave = ((basicVar < 0) ? SimplexVarValKind.Zero : DetermineVvk(basicVar, _rgnumBasic[col]));
					_log.LogEvent(4, Resources.Replacing0With1InBasis, basicVar, slackVarForRow);
					_bas.RepairBasis(slackVarForRow, col, basicVar, vvkLeave);
					double coefDouble = _mod.Matrix.GetCoefDouble(row, slackVarForRow);
					doubleFactorization.SetEliminatedEntryValue(i, coefDouble);
				}
				_thd.ResetIpiMin();
			}
			_cvFactor++;
		}

		protected bool SlamToZero(double numSrc, double numDst)
		{
			double num = Math.Abs(numSrc);
			double num2 = Math.Abs(numDst);
			if (!(num2 <= _numZeroRatio * num))
			{
				return num2 / num < _numZeroRatio;
			}
			return true;
		}
	}
}
