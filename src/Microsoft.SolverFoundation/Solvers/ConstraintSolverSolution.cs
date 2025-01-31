using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Represent a solution computed by a ConstraintSystem
	/// </summary>
	public sealed class ConstraintSolverSolution : ISolverSolution
	{
		/// <summary>
		/// Solution quality
		/// </summary>
		public enum SolutionQuality
		{
			/// <summary>
			/// Search was interrupted and solver hasn't found a feasible solution
			/// </summary>
			Unknown,
			/// <summary>
			/// Model is infeasible
			/// </summary>
			Infeasible,
			/// <summary>
			/// Solver found a feasible solution
			/// </summary>
			Feasible,
			/// <summary>
			/// Solver found an optimal solution
			/// </summary>
			Optimal
		}

		private ConstraintSolverParams _param;

		private SolutionQuality _quality;

		private ConstraintSystem _cspSolver;

		private IntegerSolver _intSolver;

		private bool _hasGoals;

		private int _solutionCount;

		private bool _hasFoundSolution;

		private Dictionary<CspTerm, object> _lastSolution;

		private IEnumerator<Dictionary<CspTerm, object>> _solutionEnumerator;

		internal ConstraintSolverParams SolverParams
		{
			get
			{
				return _param;
			}
			set
			{
				_param = value;
			}
		}

		/// <summary>
		/// Get the solution quality 
		/// </summary>
		public SolutionQuality Quality => _quality;

		/// <summary>
		/// Return true if and only if no more solutions exist
		/// </summary>
		public bool HasFoundSolution => _hasFoundSolution;

		internal bool IsInterrupted
		{
			get
			{
				if (_cspSolver != null)
				{
					return _cspSolver.IsInterrupted;
				}
				if (_intSolver != null)
				{
					return _intSolver.IsInterrupted;
				}
				throw new ArgumentException(Resources.InvalidSolverInstance);
			}
		}

		/// <summary>
		/// Overload [] for convenience
		/// </summary>
		public object this[CspTerm variable] => GetValueCore(variable);

		internal ConstraintSystem MyCspSolver => _cspSolver;

		internal IntegerSolver MyIntSolver => _intSolver;

		/// <summary>
		/// Internal constructor to create a ConstraintSolverSolution instance. Also forbid outsiders to create ConstraintSolverSolution instances.
		/// </summary>
		/// <param name="solver">A ConstraintSystem instance that does the actual solving of the model</param>
		internal ConstraintSolverSolution(ConstraintSystem solver)
		{
			Initialize();
			InitializeSolutionEnumerator(solver);
		}

		/// <summary>
		/// Internal constructor to create a ConstraintSolverSolution instance. Also forbid outsiders to create ConstraintSolverSolution instances.
		/// </summary>
		/// <param name="solver">A ConstraintSystem instance that does the actual solving of the model</param>
		internal ConstraintSolverSolution(IntegerSolver solver)
		{
			Initialize();
			InitializeSolutionEnumerator(solver);
		}

		internal void Initialize()
		{
			_solutionCount = 0;
			_hasFoundSolution = false;
			_lastSolution = null;
			_quality = SolutionQuality.Unknown;
		}

		internal void InitializeSolutionEnumerator(ConstraintSystem solver)
		{
			_cspSolver = solver;
			_intSolver = null;
			if (solver.Parameters.EnumerateInterimSolutions)
			{
				_solutionEnumerator = solver.EnumerateInterimSolutions().GetEnumerator();
			}
			else
			{
				_solutionEnumerator = solver.EnumerateSolutions().GetEnumerator();
			}
			_hasGoals = solver.GoalCount > 0;
		}

		internal void InitializeSolutionEnumerator(IntegerSolver solver)
		{
			_intSolver = solver;
			_cspSolver = null;
			if (solver.Parameters.EnumerateInterimSolutions)
			{
				_solutionEnumerator = solver.EnumerateInterimSolutions().GetEnumerator();
			}
			else
			{
				_solutionEnumerator = solver.EnumerateSolutions().GetEnumerator();
			}
			_hasGoals = solver.GoalCount > 0;
		}

		internal void UpdateQueryAbort(Func<bool> fnQueryAbort)
		{
			if (fnQueryAbort != null)
			{
				if (_cspSolver != null)
				{
					_cspSolver.Parameters.QueryAbort = fnQueryAbort;
				}
				else if (_intSolver != null)
				{
					_intSolver.Parameters.QueryAbort = fnQueryAbort;
				}
			}
		}

		/// <summary>
		/// Compute the next solution and update the solution.
		/// </summary>
		/// <returns>Returns true if and only if a solution is found.</returns>
		public bool GetNext()
		{
			return GetNext(null);
		}

		internal bool GetNext(Func<bool> newQueryAbort)
		{
			UpdateQueryAbort(newQueryAbort);
			_hasFoundSolution = _solutionEnumerator.MoveNext();
			if (_hasFoundSolution)
			{
				_lastSolution = _solutionEnumerator.Current;
				if (_hasGoals && ((_cspSolver != null && !_cspSolver.Parameters.EnumerateInterimSolutions) || (_intSolver != null && !_intSolver.Parameters.EnumerateInterimSolutions)))
				{
					_quality = SolutionQuality.Optimal;
				}
				else
				{
					_quality = SolutionQuality.Feasible;
				}
				_solutionCount++;
			}
			else if (_solutionCount == 0)
			{
				if (IsInterrupted)
				{
					_quality = SolutionQuality.Unknown;
				}
				else
				{
					_quality = SolutionQuality.Infeasible;
				}
			}
			if (_param != null)
			{
				if (_cspSolver != null)
				{
					_param.SetElapsed(_cspSolver.Parameters.ElapsedMilliSec);
				}
				else if (_intSolver != null)
				{
					_param.SetElapsed(_intSolver.Parameters.ElapsedMilliSec);
				}
			}
			return _hasFoundSolution;
		}

		/// <summary>
		/// Retrieve the value of the variable in this solution
		/// </summary>
		public int GetIntegerValue(CspTerm variable)
		{
			object valueCore = GetValueCore(variable);
			if (valueCore is int)
			{
				return (int)valueCore;
			}
			throw new ArgumentException(Resources.WrongVariableType + variable.ToString());
		}

		/// <summary>
		/// Retrieve the value of the variable in this solution
		/// </summary>
		public double GetDoubleValue(CspTerm variable)
		{
			object valueCore = GetValueCore(variable);
			if (valueCore is double)
			{
				return (double)valueCore;
			}
			throw new ArgumentException(Resources.WrongVariableType + variable.ToString());
		}

		/// <summary>
		/// Retrieve the value of the variable in this solution
		/// </summary>
		public string GetSymbolValue(CspTerm variable)
		{
			object valueCore = GetValueCore(variable);
			if (valueCore is string result)
			{
				return result;
			}
			throw new ArgumentException(Resources.WrongVariableType + variable.ToString());
		}

		/// <summary>
		/// Retrieve the value of the variable in this solution
		/// </summary>
		public int[] GetIntegerSetListValue(CspTerm variable)
		{
			object valueCore = GetValueCore(variable);
			if (valueCore is int[] result)
			{
				return result;
			}
			throw new ArgumentException(Resources.WrongVariableType + variable.ToString());
		}

		/// <summary>
		/// Retrieve the value of the variable in this solution
		/// </summary>
		public double[] GetDoubleSetListValue(CspTerm variable)
		{
			object valueCore = GetValueCore(variable);
			if (valueCore is double[] result)
			{
				return result;
			}
			throw new ArgumentException(Resources.WrongVariableType + variable.ToString());
		}

		/// <summary>
		/// Retrieve the value of the variable in this solution
		/// </summary>
		public string[] GetSymbolSetListValue(CspTerm variable)
		{
			object valueCore = GetValueCore(variable);
			if (valueCore is string[] result)
			{
				return result;
			}
			throw new ArgumentException(Resources.WrongVariableType + variable.ToString());
		}

		/// <summary>
		/// Try get the value of the given variable. This method never fails
		/// </summary>
		/// <returns>True if and only if the variable exists and its value is successfully retrieved</returns>
		public bool TryGetValue(CspTerm variable, out object val)
		{
			bool result = true;
			try
			{
				val = GetValueCore(variable);
			}
			catch (InvalidOperationException)
			{
				val = 0;
				result = false;
			}
			catch (ArgumentException)
			{
				val = 0;
				result = false;
			}
			return result;
		}

		private object GetValueCore(CspTerm variable)
		{
			if (_lastSolution == null)
			{
				throw new InvalidOperationException(Resources.ModelHasNoSolution);
			}
			CspCompositeVariable cspCompositeVariable = variable as CspCompositeVariable;
			if (cspCompositeVariable != null && (cspCompositeVariable.DomainComposite is CspPowerSet || cspCompositeVariable.DomainComposite is CspPowerList) && _cspSolver != null)
			{
				return CspSetListHelper.GetSetListVarValue(cspCompositeVariable, _cspSolver.TermMap);
			}
			if (cspCompositeVariable != null)
			{
				throw new InvalidOperationException(Resources.CompositeVarGetValueNotSupported + variable.ToString());
			}
			CspTerm value;
			if (_cspSolver != null && _cspSolver.TermMap != null)
			{
				if (!_cspSolver.TermMap.TryGetValue(variable, out value))
				{
					throw new ArgumentException(Resources.UnknownVariable + variable.ToString());
				}
			}
			else if (_intSolver != null && _intSolver.TermMap != null)
			{
				if (!_intSolver.TermMap.TryGetValue(variable, out value))
				{
					throw new ArgumentException(Resources.UnknownVariable + variable.ToString());
				}
			}
			else
			{
				value = variable;
			}
			if (variable is CspVariable && _lastSolution.TryGetValue(value, out var value2))
			{
				return value2;
			}
			if (_cspSolver != null)
			{
				return _cspSolver.GetValue(value);
			}
			if (_intSolver != null && _lastSolution.ContainsKey(value))
			{
				return _lastSolution[value];
			}
			throw new ArgumentException(Resources.UnknownVariable + variable.ToString());
		}
	}
}
