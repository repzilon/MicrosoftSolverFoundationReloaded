using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Composite decision, decision defined on a composite domain
	/// </summary>
	[DebuggerDisplay("{_name}")]
	public sealed class SubmodelInstance
	{
		private readonly Dictionary<Decision, Decision> _decisions;

		private readonly string _name;

		private readonly Dictionary<Parameter, Parameter> _parameters;

		private readonly Dictionary<RandomParameter, RandomParameter> _randomParameters;

		private readonly Dictionary<RecourseDecision, RecourseDecision> _recourseDecisions;

		private readonly Dictionary<SubmodelInstance, SubmodelInstance> _submodelInstances;

		internal Model _domain;

		internal Model _owningModel;

		internal SubmodelInstance _refKey;

		/// <summary>
		/// Return all parameter members directly defined in this submodel instance
		/// </summary>
		public IEnumerable<Parameter> Parameters => Model.GetTopLevelItems(_parameters, (KeyValuePair<Parameter, Parameter> p) => (object)p.Key._refKey == null, (KeyValuePair<Parameter, Parameter> p) => p.Value);

		/// <summary>
		/// Return all random parameter members directly defined in this submodel instance
		/// </summary>
		public IEnumerable<RandomParameter> RandomParameters => Model.GetTopLevelItems(_randomParameters, (KeyValuePair<RandomParameter, RandomParameter> rp) => (object)rp.Key._refKey == null, (KeyValuePair<RandomParameter, RandomParameter> rp) => rp.Value);

		/// <summary>
		/// Return all decision members directly defined in this submodel instance
		/// </summary>
		public IEnumerable<Decision> Decisions => Model.GetTopLevelItems(_decisions, (KeyValuePair<Decision, Decision> d) => (object)d.Key._refKey == null, (KeyValuePair<Decision, Decision> d) => d.Value);

		/// <summary>
		/// Return all recourse decision members directly defined in this submodel instance
		/// </summary>
		public IEnumerable<RecourseDecision> RecourseDecisions => Model.GetTopLevelItems(_recourseDecisions, (KeyValuePair<RecourseDecision, RecourseDecision> rd) => (object)rd.Key._refKey == null, (KeyValuePair<RecourseDecision, RecourseDecision> rd) => rd.Value);

		/// <summary>
		/// Return all submodel instance members directly defined in this submodel instance
		/// </summary>
		public IEnumerable<SubmodelInstance> SubmodelInstances => Model.GetTopLevelItems(_submodelInstances, (KeyValuePair<SubmodelInstance, SubmodelInstance> si) => si.Key._refKey == null, (KeyValuePair<SubmodelInstance, SubmodelInstance> si) => si.Value);

		/// <summary>
		/// Return all parameter members that are instantiated from this submodel instance
		/// </summary>
		internal IEnumerable<Parameter> AllParameters => _parameters.Values;

		/// <summary>
		/// Return all random parameter members that are instantiated from this submodel instance
		/// </summary>
		internal IEnumerable<RandomParameter> AllRandomParameters => _randomParameters.Values;

		/// <summary>
		/// Return all decision members that are instantiated from this submodel instance
		/// </summary>
		internal IEnumerable<Decision> AllDecisions => _decisions.Values;

		/// <summary>
		/// Return all recourse decision members that are instantiated from this submodel instance
		/// </summary>
		internal IEnumerable<RecourseDecision> AllRecourseDecisions => _recourseDecisions.Values;

		/// <summary>
		/// Return all submodel instance members that are instantiated from this submodel instance
		/// </summary>
		internal IEnumerable<SubmodelInstance> AllSubmodelInstances => _submodelInstances.Values;

		/// <summary>
		/// Get the name of the submodel decision
		/// </summary>
		public string Name => _name;

		/// <summary>
		/// Given the SubmodelInstance member defined in the domain, return the corresponding SubmodelInstance member of this SubmodelInstance
		/// </summary>
		/// <param name="key">The SubmodelInstance object in the domain of this SubmodelInstance</param>
		public SubmodelInstance this[SubmodelInstance key] => GetSubmodelInstance(key);

		/// <summary>
		/// Given the Decision member defined in the domain, return the corresponding Decision member of this SubmodelInstance
		/// </summary>
		/// <param name="key">The Decision object in the domain of this SubmodelInstance</param>
		public Decision this[Decision key] => GetDecision(key);

		/// <summary>
		/// Given the RecourseDecision member defined in the domain, return the corresponding RecourseDecision member of this SubmodelInstance
		/// </summary>
		/// <param name="key">The RecourseDecision object in the domain of this SubmodelInstance</param>
		public RecourseDecision this[RecourseDecision key] => GetDecision(key);

		/// <summary>
		/// Given the Parameter member defined in the domain, return the corresponding Parameter member of this SubmodelInstance
		/// </summary>
		/// <param name="key">The Parameter object in the domain of this SubmodelInstance</param>
		public Parameter this[Parameter key] => GetParameter(key);

		/// <summary>
		/// Given the RandomParameter member defined in the domain, return the corresponding RandomParameter member of this SubmodelInstance
		/// </summary>
		/// <param name="key">The RandomParameter object in the domain of this SubmodelInstance</param>
		public RandomParameter this[RandomParameter key] => GetParameter(key);

		/// <summary>
		/// Construct a submodel decision (singleton) on the submodel domain with the given name
		/// </summary>
		internal SubmodelInstance(Model domain, Model target, SubmodelInstance instance, string name)
			: this(domain, target, instance, name, new Set[0])
		{
		}

		/// <summary>
		/// Construct a submodel decision (table) on the submodel domain with the given name and index sets
		/// </summary>
		internal SubmodelInstance(Model domain, Model target, SubmodelInstance instance, string name, params Set[] indexSets)
		{
			_domain = domain;
			_owningModel = target;
			_name = name;
			if (_name == null)
			{
				_name = "submodel_decision_" + Model.UniqueSuffix();
			}
			_parameters = new Dictionary<Parameter, Parameter>();
			_randomParameters = new Dictionary<RandomParameter, RandomParameter>();
			_decisions = new Dictionary<Decision, Decision>();
			_recourseDecisions = new Dictionary<RecourseDecision, RecourseDecision>();
			_submodelInstances = new Dictionary<SubmodelInstance, SubmodelInstance>();
			CloneSubmodel(target, instance);
		}

		internal static SubmodelInstance FollowPath(Term.EvaluationContext context)
		{
			List<SubmodelInstance> list = null;
			if (context.Constraint != null)
			{
				list = context.Constraint._path;
			}
			else if (context.Goal != null)
			{
				list = context.Goal._path;
			}
			if (list != null)
			{
				SubmodelInstance submodelInstance = list[list.Count - 1];
				for (int num = list.Count - 2; num >= 0; num--)
				{
					submodelInstance = submodelInstance.GetSubmodelInstance(list[num]);
				}
				return submodelInstance;
			}
			return null;
		}

		private static void Clone<T>(T sourceRefKey, T source, T clone, Dictionary<T, T> dict, Action<T> targetModelAddAction)
		{
			if (sourceRefKey == null)
			{
				targetModelAddAction(clone);
				dict.Add(source, clone);
			}
		}

		private void Clone<T>(T clone, int targetLevel, Action<T> addAction)
		{
			if (_domain._level == targetLevel + 1)
			{
				addAction(clone);
			}
		}

		internal void CloneSubmodel(Model target, SubmodelInstance instance)
		{
			if (instance == null)
			{
				instance = this;
			}
			if (target == null)
			{
				target = _domain._parent;
			}
			foreach (SubmodelInstance allSubmodelInstance in _domain.AllSubmodelInstances)
			{
				if (allSubmodelInstance._refKey != null)
				{
					continue;
				}
				SubmodelInstance submodelInstance = allSubmodelInstance._domain.CreateInstance(Name, allSubmodelInstance.Name, target, instance);
				submodelInstance._refKey = allSubmodelInstance;
				target._submodelInstances.Add(submodelInstance);
				_submodelInstances.Add(allSubmodelInstance, submodelInstance);
				foreach (Parameter allParameter in allSubmodelInstance.AllParameters)
				{
					submodelInstance[allParameter._refKey]._refKey = allParameter;
					_parameters.Add(allParameter, submodelInstance[allParameter._refKey]);
				}
				foreach (RandomParameter allRandomParameter in allSubmodelInstance.AllRandomParameters)
				{
					submodelInstance[allRandomParameter._refKey]._refKey = allRandomParameter;
					_randomParameters.Add(allRandomParameter, submodelInstance[allRandomParameter._refKey]);
				}
				foreach (Decision allDecision in allSubmodelInstance.AllDecisions)
				{
					submodelInstance[allDecision._refKey]._refKey = allDecision;
					_decisions.Add(allDecision, submodelInstance[allDecision._refKey]);
				}
				foreach (RecourseDecision allRecourseDecision in allSubmodelInstance.AllRecourseDecisions)
				{
					submodelInstance[allRecourseDecision._refKey]._refKey = allRecourseDecision;
					_recourseDecisions.Add(allRecourseDecision, submodelInstance[allRecourseDecision._refKey]);
				}
			}
			foreach (Parameter allParameter2 in _domain.AllParameters)
			{
				Clone(allParameter2._refKey, allParameter2, ((object)allParameter2._refKey == null) ? (allParameter2.Clone(Name) as Parameter) : null, _parameters, target.AddParameter);
			}
			foreach (RandomParameter allRandomParameter2 in _domain.AllRandomParameters)
			{
				Clone(allRandomParameter2._refKey, allRandomParameter2, ((object)allRandomParameter2._refKey == null) ? (allRandomParameter2.Clone(Name) as RandomParameter) : null, _randomParameters, target.AddParameter);
			}
			foreach (Decision allDecision2 in _domain.AllDecisions)
			{
				Clone(allDecision2._refKey, allDecision2, ((object)allDecision2._refKey == null) ? (allDecision2.Clone(Name) as Decision) : null, _decisions, target.AddDecision);
			}
			foreach (RecourseDecision allRecourseDecision2 in _domain.AllRecourseDecisions)
			{
				Clone(allRecourseDecision2._refKey, allRecourseDecision2, ((object)allRecourseDecision2._refKey == null) ? (allRecourseDecision2.Clone(Name) as RecourseDecision) : null, _recourseDecisions, target.AddDecision);
			}
			foreach (Constraint allConstraint in _domain.AllConstraints)
			{
				Clone(allConstraint.Clone(Name, this), target._level, target._constraints.Add);
			}
			foreach (Goal allGoal in _domain.AllGoals)
			{
				Clone(allGoal.Clone(Name, this), target._level, target._goals.Add);
			}
		}

		private static bool TryGetItem<T>(Dictionary<T, T> dict, T key, out T val)
		{
			if (dict.TryGetValue(key, out val))
			{
				return true;
			}
			return false;
		}

		private static T GetItem<T>(Dictionary<T, T> dict, T key, string errorMessage)
		{
			if (TryGetItem(dict, key, out var val))
			{
				return val;
			}
			throw new InvalidOperationException(errorMessage);
		}

		internal bool TryGetSubmodelInstance(SubmodelInstance key, out SubmodelInstance val)
		{
			return TryGetItem(_submodelInstances, key, out val);
		}

		internal bool TryGetDecision(Decision key, out Decision val)
		{
			return TryGetItem(_decisions, key, out val);
		}

		internal bool TryGetRecourseDecision(RecourseDecision key, out RecourseDecision val)
		{
			return TryGetItem(_recourseDecisions, key, out val);
		}

		internal bool TryGetParameter(Parameter key, out Parameter val)
		{
			return TryGetItem(_parameters, key, out val);
		}

		internal bool TryGetRandomParameter(RandomParameter key, out RandomParameter val)
		{
			return TryGetItem(_randomParameters, key, out val);
		}

		/// <summary>
		/// Given the SubmodelInstance member defined in the domain, return the corresponding SubmodelInstance member of this SubmodelInstance
		/// </summary>
		/// <param name="key">The SubmodelInstance object in the domain of this SubmodelInstance</param>
		public SubmodelInstance GetSubmodelInstance(SubmodelInstance key)
		{
			return GetItem(_submodelInstances, key, Resources.SfsSubmodelSubmodelInstanceNotFound);
		}

		/// <summary>
		/// Given the Decision member defined in the domain, return the corresponding Decision member of this SubmodelInstance
		/// </summary>
		/// <param name="key">The Decision object in the domain of this SubmodelInstance</param>
		public Decision GetDecision(Decision key)
		{
			return GetItem(_decisions, key, Resources.SfsSubmodelInstanceNotFound);
		}

		/// <summary>
		/// Given the RecourseDecision member defined in the domain, return the corresponding RecourseDecision member of this SubmodelInstance
		/// </summary>
		/// <param name="key">The RecourseDecision object in the domain of this SubmodelInstance</param>
		public RecourseDecision GetDecision(RecourseDecision key)
		{
			return GetItem(_recourseDecisions, key, Resources.SfsSubmodelInstanceNotFound);
		}

		/// <summary>
		/// Given the Parameter member defined in the domain, return the corresponding Parameter member of this SubmodelInstance
		/// </summary>
		/// <param name="key">The Parameter object in the domain of this SubmodelInstance</param>
		public Parameter GetParameter(Parameter key)
		{
			return GetItem(_parameters, key, Resources.SfsSubmodelInstanceNotFound);
		}

		/// <summary>
		/// Given the RandomParameter member defined in the domain, return the corresponding RandomParameter member of this SubmodelInstance
		/// </summary>
		/// <param name="key">The RandomParameter object in the domain of this SubmodelInstance</param>
		public RandomParameter GetParameter(RandomParameter key)
		{
			return GetItem(_randomParameters, key, Resources.SfsSubmodelInstanceNotFound);
		}
	}
}
