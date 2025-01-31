using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	internal class SimplexSensitivity : ILinearSolverSensitivityReport, ILinearSolverReport
	{
		private SimplexTask _thread;

		private VectorRational _vecReducedCost;

		private VectorRational _vecDualCost;

		private VectorRational _vecCost;

		private int _rowLim;

		private int _varLim;

		public SimplexSensitivity(SimplexTask thread)
		{
			_thread = thread;
		}

		public void Generate()
		{
			_vecReducedCost = ((DualExact)_thread.AlgorithmExact)._vecReducedCost;
			_rowLim = ((DualExact)_thread.AlgorithmExact)._rowLim;
			_varLim = ((DualExact)_thread.AlgorithmExact)._varLim;
			_vecDualCost = new VectorRational(_rowLim);
			_vecCost = new VectorRational(_varLim);
		}

		/// <summary> Return the dual value  
		/// </summary>
		/// <param name="vidRow">a row id</param>
		/// <returns>a number</returns>
		public Rational GetDualValue(int vidRow)
		{
			if (vidRow < 0 || vidRow >= _thread.Solver._vidLim || !_thread.Solver._mpvidvi[vidRow].IsRow)
			{
				throw new ArgumentException(Resources.InvalidRowId, "vidRow");
			}
			int rc = _thread.Model._mpvidvar[vidRow];
			Rational coef = _vecReducedCost.GetCoef(rc);
			if (coef.IsZero)
			{
				int rid = _thread.Solver._mpvidvi[vidRow].Rid;
				_thread.Solver.GetBounds(vidRow, out var lower, out var upper);
				if (lower == upper)
				{
					CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(_thread.Model.Matrix, rid);
					while (rowIter.IsValid)
					{
						coef += _vecReducedCost.GetCoef(rowIter.Column);
						rowIter.Advance();
					}
				}
			}
			return coef;
		}

		/// <summary> Get the coefficient range on the first objective row   
		/// </summary>
		/// <param name="vid">a vairable id</param>
		/// <returns>a range </returns>
		public LinearSolverSensitivityRange GetObjectiveCoefficientRange(int vid)
		{
			return GetObjectiveCoefficientRange(vid, 0);
		}

		/// <summary> Get the coefficient range on the first objective row   
		/// </summary>
		/// <param name="vid">a variable id</param>
		/// <param name="pri">a speific goal</param>
		/// <returns>a range </returns>
		public LinearSolverSensitivityRange GetObjectiveCoefficientRange(int vid, int pri)
		{
			if (vid < 0 || vid >= _thread.Solver._vidLim)
			{
				throw new ArgumentException(Resources.InvalidVariableId, "vid");
			}
			bool basic = _thread.Solver.GetBasic(vid);
			LinearSolverSensitivityRange numRange = default(LinearSolverSensitivityRange);
			numRange.Current = GetCoefficient(vid, pri);
			int num = _thread.Model._mpvidvar[vid];
			if (num < 0)
			{
				_thread.Solver.GetBounds(vid, out numRange.Lower, out numRange.Upper);
				return numRange;
			}
			numRange.Lower = numRange.Current;
			numRange.Upper = numRange.Current;
			if (!basic)
			{
				switch (_thread.Basis.GetVvk(num))
				{
				case SimplexVarValKind.Zero:
					numRange.Current = Rational.Zero;
					numRange.Lower = Rational.NegativeInfinity;
					numRange.Upper = Rational.PositiveInfinity;
					break;
				case SimplexVarValKind.Fixed:
					numRange.Current = _thread.Model.GetLowerBound(num);
					numRange.Lower = Rational.NegativeInfinity;
					numRange.Upper = Rational.PositiveInfinity;
					break;
				case SimplexVarValKind.Lower:
					numRange.Lower = numRange.Current - _vecReducedCost.GetCoef(num);
					numRange.Upper = Rational.PositiveInfinity;
					break;
				case SimplexVarValKind.Upper:
					numRange.Upper = numRange.Current - _vecReducedCost.GetCoef(num);
					numRange.Lower = Rational.NegativeInfinity;
					break;
				default:
					throw new ModelException();
				}
			}
			else
			{
				numRange.Lower = numRange.Current;
				numRange.Upper = numRange.Current;
				GetObjectiveCoefficientRange(num, ref numRange);
			}
			return numRange;
		}

		/// <summary> Get the rhs range on the specific row   
		/// </summary>
		/// <param name="vid">a variable id</param>
		/// <returns>a range</returns>
		public LinearSolverSensitivityRange GetVariableRange(int vid)
		{
			if (vid < 0 || vid >= _thread.Solver._vidLim || !_thread.Solver._mpvidvi[vid].IsRow || _thread.Solver.IsGoal(vid))
			{
				throw new ArgumentException(Resources.NotARow, "vid");
			}
			LinearSolverSensitivityRange numRange = default(LinearSolverSensitivityRange);
			if (_thread.Solver.GetBasic(vid))
			{
				numRange.Lower = Rational.NegativeInfinity;
				numRange.Upper = Rational.PositiveInfinity;
				Rational rational = _thread.Solver._mpvidnum[vid];
				_thread.Solver.GetBounds(vid, out var lower, out var upper);
				if (lower.IsNegativeInfinity)
				{
					numRange.Current = upper;
					numRange.Lower = rational;
				}
				else if (upper.IsPositiveInfinity)
				{
					numRange.Current = lower;
					numRange.Upper = rational;
				}
				else
				{
					numRange.Current = lower;
				}
			}
			else
			{
				numRange.Current = _thread.Solver._mpvidnum[vid];
				numRange.Lower = numRange.Current;
				numRange.Upper = numRange.Current;
				GetRhsRange(vid, ref numRange);
			}
			return numRange;
		}

		private Rational GetCoefficient(int vid, int pri)
		{
			foreach (ILinearGoal goal in _thread.Solver.Goals)
			{
				if (goal.Enabled && pri == goal.Priority)
				{
					int index = goal.Index;
					return _thread.Solver.GetCoefficient(index, vid);
				}
			}
			return Rational.Indeterminate;
		}

		private void GetObjectiveCoefficientRange(int var, ref LinearSolverSensitivityRange numRange)
		{
			SimplexFactoredBasis basis = _thread.Basis;
			_vecDualCost.Clear();
			_vecCost.Clear();
			int basisSlot = basis.GetBasisSlot(var);
			_vecDualCost.SetCoefNonZero(basisSlot, 1);
			basis.InplaceSolveRow(_vecDualCost);
			CoefMatrix matrix = _thread.Model.Matrix;
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(_vecDualCost);
			while (iter.IsValid)
			{
				Rational num = iter.Value;
				Rational.Negate(ref num);
				CoefMatrix.RowIter rowIter = new CoefMatrix.RowIter(matrix, iter.Rc);
				while (rowIter.IsValid)
				{
					int column = rowIter.Column;
					if (basis.GetVvk(column) >= SimplexVarValKind.Lower)
					{
						Rational num2 = Rational.AddMul(_vecCost.GetCoef(column), num, rowIter.Exact);
						if (num2.IsZero)
						{
							_vecCost.RemoveCoef(column);
						}
						else
						{
							_vecCost.SetCoefNonZero(column, num2);
						}
					}
					rowIter.Advance();
				}
				iter.Advance();
			}
			Rational rational = 0;
			Rational rational2 = 0;
			Vector<Rational>.Iter iter2 = new Vector<Rational>.Iter(_vecReducedCost);
			while (iter2.IsValid)
			{
				Rational num3 = iter2.Value;
				if (!num3.IsZero)
				{
					Rational.Negate(ref num3);
					Rational coef = _vecCost.GetCoef(iter2.Rc);
					Rational rational3 = coef / num3;
					rational3 = 1 / rational3;
					rational = ((rational3 < rational) ? rational3 : rational);
					rational2 = ((rational3 > rational2) ? rational3 : rational2);
				}
				iter2.Advance();
			}
			numRange.Lower += rational;
			numRange.Upper += rational2;
		}

		/// <summary>Gets the rhs range for a non-basic variables (active constraints)
		/// See chapter 7 in "Linear Programming, Foundation and Extensions, Second Edition, Robert J. Vanderbei"
		/// The code is a modified version of the book computation, as the book treat models in Standard Form.
		/// </summary>
		private void GetRhsRange(int vid, ref LinearSolverSensitivityRange numRange)
		{
			int row = _thread.Model.GetRow(vid);
			if (row < 0)
			{
				_thread.Solver.GetBounds(vid, out numRange.Lower, out numRange.Upper);
				return;
			}
			SimplexFactoredBasis basis = _thread.Basis;
			_vecDualCost.Clear();
			int basisSlot = basis.GetBasisSlot(vid);
			_vecDualCost.SetCoefNonZero(row, 1);
			basis.InplaceSolveCol(_vecDualCost);
			Rational rational = Rational.NegativeInfinity;
			Rational rational2 = Rational.PositiveInfinity;
			for (basisSlot = 0; basisSlot < _rowLim; basisSlot++)
			{
				int basicVar = basis.GetBasicVar(basisSlot);
				Rational lowerBound = _thread.AlgorithmExact.GetLowerBound(basicVar);
				Rational upperBound = _thread.AlgorithmExact.GetUpperBound(basicVar);
				if (_thread.Solver.IsGoal(basicVar) && lowerBound == upperBound)
				{
					continue;
				}
				Rational basicValue = _thread.AlgorithmExact.GetBasicValue(basisSlot);
				Rational coef = _vecDualCost.GetCoef(basisSlot);
				if (!(coef == Rational.Zero))
				{
					Rational rational3 = (lowerBound - basicValue) / coef;
					if (rational3.Sign == 1)
					{
						rational2 = ((rational3 < rational2) ? rational3 : rational2);
					}
					else if (rational3.Sign == -1)
					{
						rational = ((rational3 > rational) ? rational3 : rational);
					}
					rational3 = (upperBound - basicValue) / coef;
					if (rational3.Sign == 1)
					{
						rational2 = ((rational3 < rational2) ? rational3 : rational2);
					}
					else if (rational3.Sign == -1)
					{
						rational = ((rational3 > rational) ? rational3 : rational);
					}
				}
			}
			numRange.Lower += rational;
			numRange.Upper += rational2;
		}
	}
}
