using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Manages the bounds during branch and bound.
	/// </summary>
	internal class BoundManager
	{
		private readonly SimplexTask _thread;

		private SimplexReducedModel _model;

		private int _lowerBoundsCount;

		private int _upperBoundsCount;

		private Rational[] _lowerBounds;

		private Rational[] _upperBounds;

		private double[] _lowerBoundsDouble;

		private double[] _upperBoundsDouble;

		/// <summary>
		/// Gets the number of upper bounds that have been modified from the original reduced model.
		/// </summary>
		internal int UpperBoundsCount => _upperBoundsCount;

		/// <summary>
		/// Gets the number of lower bounds that have been modified from the original reduced model.
		/// </summary>
		internal int LowerBoundsCount => _lowerBoundsCount;

		private SimplexTask Thread => _thread;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="thread"></param>
		public BoundManager(SimplexTask thread)
		{
			_thread = thread;
			_model = _thread.Model;
		}

		/// <summary> Clone
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="cloneTask"></param>
		public BoundManager(SimplexTask thread, SimplexTask cloneTask)
		{
			_thread = thread;
			_model = _thread.Model;
			int varLim = _model.VarLim;
			_lowerBounds = new Rational[varLim];
			_lowerBoundsDouble = new double[varLim];
			_upperBounds = new Rational[varLim];
			_upperBoundsDouble = new double[varLim];
			BoundManager boundManager = cloneTask.BoundManager;
			if (boundManager != null)
			{
				_lowerBoundsCount = boundManager._lowerBoundsCount;
				_upperBoundsCount = boundManager._upperBoundsCount;
				if (_lowerBoundsCount != 0)
				{
					if (boundManager._lowerBounds != null)
					{
						Array.Copy(boundManager._lowerBounds, _lowerBounds, boundManager._lowerBounds.Length);
						for (int i = boundManager._lowerBounds.Length; i < _model._varLim; i++)
						{
							ref Rational reference = ref _lowerBounds[i];
							reference = _model.GetLowerBound(i);
						}
					}
					if (boundManager._lowerBoundsDouble != null)
					{
						Array.Copy(boundManager._lowerBoundsDouble, _lowerBoundsDouble, boundManager._lowerBoundsDouble.Length);
						for (int j = boundManager._lowerBoundsDouble.Length; j < _model._varLim; j++)
						{
							_lowerBoundsDouble[j] = _model.GetLowerBoundDbl(j);
						}
					}
				}
				else
				{
					_model.GetLowerBounds(_lowerBounds);
					_model.GetLowerBoundsDbl(_lowerBoundsDouble);
				}
				if (_upperBoundsCount != 0)
				{
					if (boundManager._upperBounds != null)
					{
						Array.Copy(boundManager._upperBounds, _upperBounds, boundManager._upperBounds.Length);
						for (int k = boundManager._upperBounds.Length; k < _model._varLim; k++)
						{
							ref Rational reference2 = ref _upperBounds[k];
							reference2 = _model.GetUpperBound(k);
						}
					}
					if (boundManager._upperBoundsDouble != null)
					{
						Array.Copy(boundManager._upperBoundsDouble, _upperBoundsDouble, boundManager._upperBoundsDouble.Length);
						for (int l = boundManager._upperBoundsDouble.Length; l < _model._varLim; l++)
						{
							_upperBoundsDouble[l] = _model.GetUpperBoundDbl(l);
						}
					}
				}
				else
				{
					_model.GetUpperBounds(_upperBounds);
					_model.GetUpperBoundsDbl(_upperBoundsDouble);
				}
			}
			else
			{
				InitBounds(thread);
			}
		}

		/// <summary>
		/// Resets the bounds.
		/// </summary>
		public void InitBounds(SimplexTask thread)
		{
			if (thread.Model != _model)
			{
				_model = thread.Model;
			}
			_lowerBounds = new Rational[_model.VarLim];
			_model.GetLowerBounds(_lowerBounds);
			if (thread.Params.UseDouble)
			{
				_lowerBoundsDouble = new double[_model.VarLim];
				_model.GetLowerBoundsDbl(_lowerBoundsDouble);
			}
			_lowerBoundsCount = 0;
			_upperBounds = new Rational[_model.VarLim];
			_model.GetUpperBounds(_upperBounds);
			if (thread.Params.UseDouble)
			{
				_upperBoundsDouble = new double[_model.VarLim];
				_model.GetUpperBoundsDbl(_upperBoundsDouble);
			}
			_upperBoundsCount = 0;
		}

		/// <summary>
		/// Gets a variable's bound.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="vvk"></param>
		/// <returns></returns>
		public Rational GetVarBound(int variable, SimplexVarValKind vvk)
		{
			switch (vvk)
			{
			default:
				return default(Rational);
			case SimplexVarValKind.Fixed:
			case SimplexVarValKind.Lower:
				return GetLowerBound(variable);
			case SimplexVarValKind.Upper:
				return GetUpperBound(variable);
			}
		}

		public Rational GetLowerBound(int variable)
		{
			if (_lowerBoundsCount <= 0)
			{
				return _model.GetLowerBound(variable);
			}
			return _lowerBounds[variable];
		}

		public Rational GetUpperBound(int variable)
		{
			if (_upperBoundsCount <= 0)
			{
				return _model.GetUpperBound(variable);
			}
			return _upperBounds[variable];
		}

		public double GetLowerBoundDbl(int variable)
		{
			if (_lowerBoundsCount <= 0)
			{
				return _model.GetLowerBoundDbl(variable);
			}
			return _lowerBoundsDouble[variable];
		}

		public double GetUpperBoundDbl(int variable)
		{
			if (_upperBoundsCount <= 0)
			{
				return _model.GetUpperBoundDbl(variable);
			}
			return _upperBoundsDouble[variable];
		}

		public void GetLowerBounds(Rational[] rgnum)
		{
			if (_lowerBoundsCount > 0)
			{
				Array.Copy(_lowerBounds, rgnum, _model.VarLim);
			}
			else
			{
				_model.GetLowerBounds(rgnum);
			}
		}

		public void GetUpperBounds(Rational[] rgnum)
		{
			if (_upperBoundsCount > 0)
			{
				Array.Copy(_upperBounds, rgnum, _model.VarLim);
			}
			else
			{
				_model.GetUpperBounds(rgnum);
			}
		}

		/// <summary>
		/// Gets all the lower bounds. 
		/// </summary>
		/// <param name="rgnum"></param>
		/// <remarks>
		/// This method may be called before InitBounds. In this case we
		/// return the model's lower bounds.
		/// </remarks>
		public void GetLowerBoundsDbl(double[] rgnum)
		{
			if (_lowerBoundsCount > 0)
			{
				Array.Copy(_lowerBoundsDouble, rgnum, _model.VarLim);
			}
			else
			{
				_model.GetLowerBoundsDbl(rgnum);
			}
		}

		/// <summary>
		/// Gets all the upper bounds. 
		/// </summary>
		/// <param name="rgnum"></param>
		/// <remarks>
		/// This method may be called before InitBounds. In this case we
		/// return the model's upper bounds.
		/// </remarks>
		public void GetUpperBoundsDbl(double[] rgnum)
		{
			if (_upperBoundsCount > 0)
			{
				Array.Copy(_upperBoundsDouble, rgnum, _model.VarLim);
			}
			else
			{
				_model.GetUpperBoundsDbl(rgnum);
			}
		}

		/// <summary>
		/// Sets the lower bound of a variable to a specific value.
		/// </summary>
		/// <param name="variable">The variable whose lower bound is set.</param>
		/// <param name="bound">The value of the upper bound (in the reduced model).</param>
		internal void SetLowerBound(int variable, Rational bound)
		{
			_lowerBoundsCount++;
			_lowerBounds[variable] = bound;
			if (_lowerBoundsDouble != null)
			{
				_lowerBoundsDouble[variable] = _model.MapValueFromExactToDouble(variable, bound);
			}
			SimplexVarValKind vvk = _thread.Basis.GetVvk(variable);
			if (_upperBounds[variable] == _lowerBounds[variable] && vvk != 0)
			{
				_thread.Basis.MinorPivot(variable, SimplexVarValKind.Fixed);
			}
			else if (vvk == SimplexVarValKind.Zero)
			{
				_thread.Basis.MinorPivot(variable, SimplexVarValKind.Lower);
			}
		}

		/// <summary>
		/// Sets the lower bound of a variable to a specific value.
		/// </summary>
		/// <param name="variable">The variable whose lower bound is set.</param>
		/// <param name="bound">The value of the upper bound (in the reduced model).</param>
		internal void SetLowerBoundDbl(int variable, double bound)
		{
			_lowerBoundsCount++;
			Rational rational = ((!(Math.Abs(bound - GetUpperBoundDbl(variable)) <= _thread.AlgorithmDouble.VarEpsilon)) ? _model.MapValueFromDoubleToExact(variable, bound) : GetUpperBound(variable));
			_lowerBounds[variable] = rational;
			if (_lowerBoundsDouble != null)
			{
				_lowerBoundsDouble[variable] = bound;
			}
			SimplexVarValKind vvk = _thread.Basis.GetVvk(variable);
			if (_upperBounds[variable] == _lowerBounds[variable] && vvk != 0)
			{
				_thread.Basis.MinorPivot(variable, SimplexVarValKind.Fixed);
			}
			else if (vvk == SimplexVarValKind.Zero)
			{
				_thread.Basis.MinorPivot(variable, SimplexVarValKind.Lower);
			}
		}

		/// <summary>
		/// Resets the lower bound to its original value.
		/// </summary>
		/// <param name="variable"></param>
		internal void ResetLowerBound(int variable)
		{
			_lowerBoundsCount--;
			Rational lowerBound = _model.GetLowerBound(variable);
			_lowerBounds[variable] = lowerBound;
			if (_lowerBoundsDouble != null)
			{
				_lowerBoundsDouble[variable] = _model.MapValueFromExactToDouble(variable, lowerBound);
			}
		}

		/// <summary>
		/// Sets the upper bound of a variable to a specific value.
		/// </summary>
		/// <remarks>This is currently used just for sos</remarks>
		/// <param name="variable">The variable whose upper bound is set.</param>
		/// <param name="bound">The value of the upper bound (in the reduced model).</param>
		internal bool SetUpperBound(int variable, Rational bound)
		{
			if (bound < _lowerBounds[variable])
			{
				return false;
			}
			_upperBoundsCount++;
			_upperBounds[variable] = bound;
			if (_upperBoundsDouble != null)
			{
				_upperBoundsDouble[variable] = _model.MapValueFromExactToDouble(variable, bound);
			}
			SimplexVarValKind vvk = _thread.Basis.GetVvk(variable);
			if (_upperBounds[variable] == _lowerBounds[variable] && vvk != 0)
			{
				_thread.Basis.MinorPivot(variable, SimplexVarValKind.Fixed);
			}
			else if (vvk == SimplexVarValKind.Zero)
			{
				_thread.Basis.MinorPivot(variable, SimplexVarValKind.Upper);
			}
			return true;
		}

		/// <summary>
		/// Sets the upper bound of a variable to a specific value.
		/// </summary>
		/// <param name="variable">The variable whose upper bound is set.</param>
		/// <param name="bound">The value of the upper bound (in the reduced model).</param>
		internal void SetUpperBoundDbl(int variable, double bound)
		{
			_upperBoundsCount++;
			Rational rational = ((!(Math.Abs(bound - GetLowerBoundDbl(variable)) <= _thread.AlgorithmDouble.VarEpsilon)) ? _model.MapValueFromDoubleToExact(variable, bound) : GetLowerBound(variable));
			_upperBounds[variable] = rational;
			if (_upperBoundsDouble != null)
			{
				_upperBoundsDouble[variable] = bound;
			}
			SimplexVarValKind vvk = _thread.Basis.GetVvk(variable);
			if (_upperBounds[variable] == _lowerBounds[variable] && vvk != 0)
			{
				_thread.Basis.MinorPivot(variable, SimplexVarValKind.Fixed);
			}
			else if (vvk == SimplexVarValKind.Zero)
			{
				_thread.Basis.MinorPivot(variable, SimplexVarValKind.Upper);
			}
		}

		/// <summary>
		/// Resets the upper bound to its original value.
		/// </summary>
		/// <param name="variable"></param>
		internal void ResetUpperBound(int variable)
		{
			_upperBoundsCount--;
			Rational upperBound = _model.GetUpperBound(variable);
			_upperBounds[variable] = upperBound;
			if (_upperBoundsDouble != null)
			{
				_upperBoundsDouble[variable] = _model.MapValueFromExactToDouble(variable, upperBound);
			}
		}

		internal void TrimVariableRange(int variable, Rational lowerBound, Rational upperBound)
		{
			if (GetLowerBound(variable) < lowerBound)
			{
				SetLowerBound(variable, lowerBound);
			}
			if (GetUpperBound(variable) > upperBound)
			{
				SetUpperBound(variable, upperBound);
			}
		}

		/// <summary>
		/// Gets the bounds of a row.
		/// </summary>
		/// <param name="row">The row whose bounds are sought.</param>
		/// <param name="lowerBound">The lower bound of the row.</param>
		/// <param name="upperBound">The upper bound of the row.</param>
		internal void GetRowBounds(int row, out Rational lowerBound, out Rational upperBound)
		{
			int slackVarForRow = _model.GetSlackVarForRow(row);
			Rational rational = -_thread.Model.Matrix.GetCoefExact(row, slackVarForRow);
			if (rational.IsOne)
			{
				lowerBound = GetLowerBound(slackVarForRow);
				upperBound = GetUpperBound(slackVarForRow);
				return;
			}
			if (rational.Sign < 0)
			{
				upperBound = GetLowerBound(slackVarForRow);
				lowerBound = GetUpperBound(slackVarForRow);
			}
			else
			{
				lowerBound = GetLowerBound(slackVarForRow);
				upperBound = GetUpperBound(slackVarForRow);
			}
			lowerBound *= rational;
			upperBound *= rational;
		}

		/// <summary>
		/// Sets the bounds of a row.
		/// </summary>
		/// <param name="row">The row whose bounds are set.</param>
		/// <param name="lowerBound">The lower bound of the row.</param>
		/// <param name="upperBound">The upper bound of the row.</param>
		internal void SetRowBounds(int row, Rational lowerBound, Rational upperBound)
		{
			int slackVarForRow = _model.GetSlackVarForRow(row);
			Rational rational = -_thread.Model.Matrix.GetCoefExact(row, slackVarForRow);
			if (rational.IsOne)
			{
				SetLowerBound(slackVarForRow, lowerBound);
				SetUpperBound(slackVarForRow, upperBound);
				return;
			}
			lowerBound /= rational;
			upperBound /= rational;
			if (rational.Sign < 0)
			{
				SetLowerBound(slackVarForRow, upperBound);
				SetUpperBound(slackVarForRow, lowerBound);
			}
			else
			{
				SetLowerBound(slackVarForRow, lowerBound);
				SetUpperBound(slackVarForRow, upperBound);
			}
		}

		internal bool IsFixed(int variable)
		{
			return GetLowerBound(variable) == GetUpperBound(variable);
		}

		internal bool IsBinary(int variable)
		{
			if (!_model.IsVarInteger(variable))
			{
				return false;
			}
			if ((!(GetLowerBound(variable) == 0) || !(GetUpperBound(variable) == 1)) && (!(GetLowerBound(variable) == 1) || !(GetUpperBound(variable) == 1)))
			{
				if (GetLowerBound(variable) == 0)
				{
					return GetUpperBound(variable) == 0;
				}
				return false;
			}
			return true;
		}

		internal void GetVidBounds(int vid, out Rational lower, out Rational upper)
		{
			Thread.Solver.GetBounds(vid, out lower, out upper);
		}

		private static void UpdateImpliedBounds(SimplexTask thread, int vidVar, Rational coef, ref Rational impliedLowerBound, ref Rational impliedUpperBound)
		{
			thread.BoundManager.GetVidBounds(vidVar, out var lower, out var upper);
			if (coef < 0)
			{
				impliedLowerBound += coef * upper;
				impliedUpperBound += coef * lower;
			}
			else
			{
				impliedLowerBound += coef * lower;
				impliedUpperBound += coef * upper;
			}
		}

		/// <summary>
		/// Compute the implied bounds of vidRow.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="vidRow"></param>
		/// <param name="impliedLowerBound"></param>
		/// <param name="impliedUpperBound"></param>
		internal static void ComputeImpliedBounds(SimplexTask thread, int vidRow, out Rational impliedLowerBound, out Rational impliedUpperBound)
		{
			SimplexSolver solver = thread.Solver;
			impliedLowerBound = 0;
			impliedUpperBound = 0;
			foreach (LinearEntry rowEntry in solver.GetRowEntries(vidRow))
			{
				if (rowEntry.Index != vidRow)
				{
					UpdateImpliedBounds(thread, rowEntry.Index, rowEntry.Value, ref impliedLowerBound, ref impliedUpperBound);
				}
			}
		}

		/// <summary>
		/// Compute the implied bounds of vector row, whose column vars must have been defined in the solver.
		/// </summary>
		/// <param name="thread"></param>
		/// <param name="row"></param>
		/// <param name="impliedLowerBound"></param>
		/// <param name="impliedUpperBound"></param>
		internal static void ComputeImpliedBounds(SimplexTask thread, VectorRational row, out Rational impliedLowerBound, out Rational impliedUpperBound)
		{
			impliedLowerBound = 0;
			impliedUpperBound = 0;
			Vector<Rational>.Iter iter = new Vector<Rational>.Iter(row);
			while (iter.IsValid)
			{
				UpdateImpliedBounds(thread, iter.Rc, iter.Value, ref impliedLowerBound, ref impliedUpperBound);
				iter.Advance();
			}
		}
	}
}
