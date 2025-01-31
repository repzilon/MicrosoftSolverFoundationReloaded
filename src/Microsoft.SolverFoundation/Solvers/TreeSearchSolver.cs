using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class TreeSearchSolver : CspSolver
	{
		internal int _cBacktracks;

		protected CspSolverTerm[] _searchVars;

		protected int _goalCount;

		protected int _userVarCount;

		protected int _searchFirstCount;

		protected Stack<BranchingDecision> _decisions;

		protected int _depth;

		protected int _priorCoverage;

		protected int _backTrackCtr;

		protected int _lastRestartAt;

		protected List<CspSolverTerm> _initSearchVarList;

		protected AdaptiveLocalPropagation _propagator;

		internal TreeSearchSolver(ConstraintSystem model)
			: base(model)
		{
			_cBacktracks = 0;
			_propagator = _model._propagator;
			_initSearchVarList = new List<CspSolverTerm>();
			Dictionary<CspSolverTerm, int> dictionary = new Dictionary<CspSolverTerm, int>();
			int value;
			foreach (CspSolverTerm minimizationGoal in _model._minimizationGoals)
			{
				if (0 < minimizationGoal.Count)
				{
					if (!dictionary.TryGetValue(minimizationGoal, out value))
					{
						dictionary.Add(minimizationGoal, 0);
						_initSearchVarList.Add(minimizationGoal);
					}
					continue;
				}
				throw new InvalidOperationException(Resources.GoalWithEmptyDomain);
			}
			_goalCount = _initSearchVarList.Count;
			foreach (CspUserVariable userVar in _model._userVars)
			{
				if (0 < userVar.Count && !dictionary.TryGetValue(userVar, out value))
				{
					dictionary.Add(userVar, 0);
					_initSearchVarList.Add(userVar);
				}
			}
			_userVarCount = _initSearchVarList.Count;
			ReadOnlyCollection<CspTerm> userOrderVariables = _model.Parameters.UserOrderVariables;
			foreach (CspTerm item in userOrderVariables)
			{
				if (!(item is CspSolverTerm cspSolverTerm))
				{
					throw new ArgumentNullException(Resources.SearchFirstTermError);
				}
				if (1 < cspSolverTerm.Count && 0 < cspSolverTerm.Dependents.Count && !dictionary.TryGetValue(cspSolverTerm, out value))
				{
					dictionary.Add(cspSolverTerm, 0);
					_initSearchVarList.Add(cspSolverTerm);
				}
			}
			_searchFirstCount = _initSearchVarList.Count;
			foreach (CspSolverTerm variable in _model._variables)
			{
				if (1 < variable.Count && 0 < variable.Dependents.Count && !dictionary.TryGetValue(variable, out value))
				{
					dictionary.Add(variable, 0);
					_initSearchVarList.Add(variable);
				}
			}
			_searchVars = _initSearchVarList.ToArray();
			_decisions = new Stack<BranchingDecision>();
		}

		/// <summary> Choose the next undecided Decision Variable
		/// </summary>
		/// <returns> false if no decision variables remain to be tried </returns>
		internal abstract bool NextDecisionVariable();

		internal abstract bool NextDecisionVariableYieldOnSuboptimal();

		/// <summary> With the current decision variable, narrow its value set
		/// </summary>
		/// <returns> False when all values of this variable have been exhausted </returns>
		internal abstract bool TryNextValue();

		internal virtual bool TakeNecessarySnapshots()
		{
			return true;
		}

		internal override IEnumerable<int> Search(bool yieldSuboptimals)
		{
			if (yieldSuboptimals && _model._minimizationGoals.Count == 0)
			{
				yieldSuboptimals = false;
			}
			int loops = 0;
			while (!_model.CheckAbort())
			{
				if (!((!yieldSuboptimals) ? NextDecisionVariable() : NextDecisionVariableYieldOnSuboptimal()))
				{
					yield return loops;
					if (_model._fIsInModelingPhase)
					{
						throw new InvalidOperationException(Resources.SolverResetDuringSolve);
					}
					if (yieldSuboptimals && TakeNecessarySnapshots())
					{
						continue;
					}
				}
				CspSolverTerm conflict;
				do
				{
					_cBacktracks++;
					conflict = null;
					if (TryNextValue())
					{
						loops++;
						continue;
					}
					yield break;
				}
				while (!_propagator.Propagate(out conflict) && !_model.CheckAbort());
				_cBacktracks--;
			}
		}

		internal override object GetValue(CspTerm variable)
		{
			if (variable is CspSolverTerm cspSolverTerm && _model.AllTerms.Contains(cspSolverTerm))
			{
				return cspSolverTerm.GetValue();
			}
			CspCompositeVariable cspCompositeVariable = variable as CspCompositeVariable;
			if (_model._compositeVariables != null && cspCompositeVariable != null && _model._compositeVariables.Contains(cspCompositeVariable) && (cspCompositeVariable.DomainComposite is CspPowerSet || cspCompositeVariable.DomainComposite is CspPowerList))
			{
				return CspSetListHelper.GetSetListVarValue(cspCompositeVariable);
			}
			throw new ArgumentException(Resources.UnknownVariable + variable.ToString());
		}

		internal override Dictionary<CspTerm, int> SnapshotVariablesIntegerValues()
		{
			Dictionary<CspTerm, int> dictionary = new Dictionary<CspTerm, int>();
			foreach (CspSolverTerm variable in _model._variables)
			{
				dictionary.Add(variable, variable.FiniteValue.First);
			}
			return dictionary;
		}

		internal override Dictionary<CspTerm, object> SnapshotVariablesValues()
		{
			Dictionary<CspTerm, object> dictionary = new Dictionary<CspTerm, object>();
			foreach (CspSolverTerm variable in _model._variables)
			{
				dictionary.Add(variable, variable.GetValue());
			}
			foreach (CspCompositeVariable compositeVariable in _model._compositeVariables)
			{
				if (compositeVariable.DomainComposite is CspPowerSet || compositeVariable.DomainComposite is CspPowerList)
				{
					dictionary.Add(compositeVariable, CspSetListHelper.GetSetListVarValue(compositeVariable));
				}
			}
			return dictionary;
		}
	}
}
