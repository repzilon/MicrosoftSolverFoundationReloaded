#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> Model allows construction, elaboration, and modification of models with
	///           arithmetic and logical expressions and constraints expressed over domains
	///           of boolean, integer, and real variables.
	/// </summary>
	[DebuggerDisplay("{_name}")]
	public sealed class Model
	{
		private const int LEVELLIMIT = 5;

		internal readonly int _level;

		internal List<Constraint> _constraints = new List<Constraint>();

		internal bool _dataBound;

		internal ICollection<Decision> _decisions = new List<Decision>();

		internal object _decisionUpdateLock = new object();

		internal bool _fInstantiated;

		internal List<Goal> _goals = new List<Goal>();

		internal int _maxDecisionId;

		internal int _maxRecourseDecisionId;

		internal bool _hasValidSolution;

		internal string _name = "DefaultModel";

		internal ICollection<Parameter> _parameters = new List<Parameter>();

		internal Model _parent;

		private ICollection<RandomParameter> _randomParameters = new List<RandomParameter>();

		private ICollection<RecourseDecision> _recourseDecisions = new List<RecourseDecision>();

		internal ICollection<SubmodelInstance> _submodelInstances;

		internal ICollection<Model> _submodels;

		internal ICollection<Tuples> _tuples = new HashSet<Tuples>();

		private HashSet<string> _names = new HashSet<string>();

		internal ICollection<NamedConstantTerm> _namedConstants = new List<NamedConstantTerm>();

		internal SolverContext _context;

		private bool _validateNames = true;

		private bool IsFrozen
		{
			get
			{
				if (_level > 0)
				{
					return _fInstantiated;
				}
				return false;
			}
		}

		/// <summary>
		/// True if the model has no decisions, parameters, constraints, or goals
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				if (_decisions.Count == 0 && _recourseDecisions.Count == 0 && _parameters.Count == 0 && _randomParameters.Count == 0 && _constraints.Count == 0)
				{
					return _goals.Count == 0;
				}
				return false;
			}
		}

		/// <summary>
		/// Return whether this model is a root model
		/// </summary>
		internal bool IsRootModel
		{
			get
			{
				if (_parent == null)
				{
					return _level == 0;
				}
				return false;
			}
		}

		/// <summary>
		/// Return whether this submodel is already removed
		/// </summary>
		internal bool IsRemoved
		{
			get
			{
				if (_parent == null)
				{
					return _level > 0;
				}
				return false;
			}
		}

		/// <summary>The name of the model.
		/// </summary>
		public string Name
		{
			get
			{
				if (_name == null)
				{
					_name = "model" + UniqueSuffix();
				}
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		/// <summary>
		/// All decisions that are defined directly in the model
		/// </summary>
		public IEnumerable<Decision> Decisions => GetTopLevelItems(_decisions, (Decision d) => (object)d._refKey == null, (Decision d) => d);

		/// <summary>
		/// All recourse decisions that are defined directly in the model
		/// </summary>
		public IEnumerable<RecourseDecision> RecourseDecisions => GetTopLevelItems(_recourseDecisions, (RecourseDecision rd) => (object)rd._refKey == null, (RecourseDecision rd) => rd);

		/// <summary>
		/// All parameters that are defined directly in model
		/// </summary>
		public IEnumerable<Parameter> Parameters => GetTopLevelItems(_parameters, (Parameter p) => (object)p._refKey == null, (Parameter p) => p);

		/// <summary>
		/// All random parameters that are defined directly in the model
		/// </summary>
		public IEnumerable<RandomParameter> RandomParameters => GetTopLevelItems(_randomParameters, (RandomParameter rp) => (object)rp._refKey == null, (RandomParameter rp) => rp);

		/// <summary>
		/// All Tuples that are defined in model
		/// </summary>
		public IEnumerable<Tuples> Tuples => GetTopLevelItems(_tuples, (Tuples p) => true, (Tuples p) => p);

		/// <summary>
		/// All goals that are defined directly in the model
		/// </summary>
		public IEnumerable<Goal> Goals => GetTopLevelItems(_goals, (Goal g) => g._path == null, (Goal g) => g);

		/// <summary>
		/// All submodels created directly inside this model
		/// </summary>
		public IEnumerable<Model> Submodels => _submodels;

		/// <summary>
		/// All instantiations of submodels created directly inside this model
		/// </summary>
		public IEnumerable<SubmodelInstance> SubmodelInstances => GetTopLevelItems(_submodelInstances, (SubmodelInstance si) => si._refKey == null, (SubmodelInstance si) => si);

		/// <summary>
		/// All decisions that are defined directly in the model
		/// </summary>
		public IEnumerable<Constraint> Constraints => GetTopLevelItems(_constraints, (Constraint c) => c._path == null, (Constraint c) => c);

		/// <summary>
		/// All decisions that are part of the model
		/// </summary>
		internal IEnumerable<Decision> AllDecisions => _decisions;

		/// <summary>
		/// All recourse decisions that are part of the model
		/// </summary>
		internal IEnumerable<RecourseDecision> AllRecourseDecisions => _recourseDecisions;

		/// <summary>
		/// All parameters that are part of model
		/// </summary>
		internal IEnumerable<Parameter> AllParameters => _parameters;

		/// <summary>
		/// All random parameters that are part of the model
		/// </summary>
		internal IEnumerable<RandomParameter> AllRandomParameters => _randomParameters;

		/// <summary>
		/// All Tuples that are part of model
		/// </summary>
		internal IEnumerable<Tuples> AllTuples => _tuples;

		/// <summary>
		/// All goals that are part of the model
		/// </summary>
		internal IEnumerable<Goal> AllGoals => _goals;

		/// <summary>
		/// All instantiations of submodels that are part of this model
		/// </summary>
		internal IEnumerable<SubmodelInstance> AllSubmodelInstances => _submodelInstances;

		/// <summary>
		/// All decisions that are part of the model
		/// </summary>
		internal IEnumerable<Constraint> AllConstraints => _constraints;

		/// <summary>
		/// All named constants that are part of the model
		/// </summary>
		internal IEnumerable<NamedConstantTerm> AllNamedConstants => _namedConstants;

		/// <summary>
		/// All sets that are defined directly in the model.
		/// </summary>
		private IEnumerable<Set> Sets
		{
			get
			{
				HashSet<Set> hashSet = new HashSet<Set>();
				foreach (Decision decision in _decisions)
				{
					Set[] indexSets = ((IIndexable)decision).IndexSets;
					foreach (Set item in indexSets)
					{
						hashSet.Add(item);
					}
				}
				foreach (Parameter parameter in _parameters)
				{
					Set[] indexSets2 = ((IIndexable)parameter).IndexSets;
					foreach (Set item2 in indexSets2)
					{
						hashSet.Add(item2);
					}
				}
				foreach (RecourseDecision recourseDecision in _recourseDecisions)
				{
					Set[] indexSets3 = ((IIndexable)recourseDecision).IndexSets;
					foreach (Set item3 in indexSets3)
					{
						hashSet.Add(item3);
					}
				}
				foreach (RandomParameter randomParameter in _randomParameters)
				{
					Set[] indexSets4 = ((IIndexable)randomParameter).IndexSets;
					foreach (Set item4 in indexSets4)
					{
						hashSet.Add(item4);
					}
				}
				return hashSet;
			}
		}

		internal bool IsStochastic
		{
			get
			{
				if (_randomParameters.Count == 0)
				{
					return _recourseDecisions.Count != 0;
				}
				return true;
			}
		}

		/// <summary>
		/// This is true for valid stochastic (models with both recourse decisions and random parameters), 
		/// and false for either non-valid stochastic or non-stochastic models.
		/// </summary>
		internal bool IsValidStochastic
		{
			get
			{
				if (_randomParameters.Count != 0)
				{
					return _recourseDecisions.Count != 0;
				}
				return false;
			}
		}

		internal Model(SolverContext context)
			: this(context, 0)
		{
		}

		internal Model(SolverContext context, int level)
		{
			_context = context;
			_level = level;
			_submodels = new HashSet<Model>();
			_submodelInstances = new HashSet<SubmodelInstance>();
		}

		/// <summary>
		/// Construct a submodel inside the current Model
		/// </summary>
		public Model CreateSubModel(string name)
		{
			if (_level >= 5)
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.SfsSubmodelExceedNestingLimit, new object[1] { 5 }));
			}
			VerifyModelNotFrozen();
			Model model = new Model(_context, _level + 1);
			model._parent = this;
			model._name = name;
			if (model._name == null)
			{
				model._name = "submodel" + UniqueSuffix();
			}
			_submodels.Add(model);
			return model;
		}

		internal static string UniqueSuffix()
		{
			return OmlWriter.ReplaceInvalidChars(Guid.NewGuid().ToString()).ToString();
		}

		/// <summary>
		/// Remove a submodel from the current Model.
		/// </summary>
		/// <returns>Returns null if the given submodel does not exist in current Model,
		/// otherwise the given submodel.</returns>
		public Model RemoveSubModel(Model submodelToBeRemoved)
		{
			if (submodelToBeRemoved == null)
			{
				throw new ArgumentNullException("submodelToBeRemoved");
			}
			VerifyModelNotFrozen();
			if (submodelToBeRemoved.IsFrozen)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.SfsSubmodelCannotRemoveInstantiatedSubmodel, new object[1] { submodelToBeRemoved.Name }));
			}
			if (_submodels.Contains(submodelToBeRemoved))
			{
				_submodels.Remove(submodelToBeRemoved);
				submodelToBeRemoved._parent = null;
				return submodelToBeRemoved;
			}
			return null;
		}

		/// <summary>
		/// Create an instance of this Model.
		/// </summary>
		/// <param name="name">The name of the SubmodelInstance to be created.</param>
		/// <returns>An instantiation of the current Model. This instantiation inherits all Decisions, Constraints, and Goals
		/// defined in this Model</returns>
		/// <remarks>The root model created by SolverContext.CreateModel() cannot call CreateInstance</remarks>
		public SubmodelInstance CreateInstance(string name)
		{
			return CreateInstance(null, name, _parent, null);
		}

		/// <summary>
		/// Create an instance of this model and add the instance to the given model
		/// </summary>
		/// <param name="baseName">The base name for the instance</param>
		/// <param name="name">The name of the instance</param>
		/// <param name="model">To which the instance will be added</param>
		/// <param name="instance">The instance to which this newly created one is a member</param>
		/// <returns>An instance of this model</returns>
		internal SubmodelInstance CreateInstance(string baseName, string name, Model model, SubmodelInstance instance)
		{
			if (IsRootModel)
			{
				throw new InvalidOperationException(Resources.SfsSubmodelCannotInstantiateRootModel);
			}
			if (IsRemoved)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.SfsSubmodelCannotInstantiateRemovedSubmodel, new object[1] { Name }));
			}
			if (IsEmpty)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.SfsSubmodelCannotCreateInstanceOfEmptySubmodel, new object[1] { Name }));
			}
			if (model == _parent && model.IsFrozen)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.SfsSubmodelCannotModifyModels, new object[1] { Name }));
			}
			_fInstantiated = true;
			string name2 = (string.IsNullOrEmpty(baseName) ? name : (baseName + "." + name));
			SubmodelInstance submodelInstance = new SubmodelInstance(this, model, instance, name2);
			if (model == _parent)
			{
				model._submodelInstances.Add(submodelInstance);
			}
			return submodelInstance;
		}

		/// <summary>
		/// Replace a Set with another Set everywhere it appears in the model
		/// </summary>
		/// <param name="oldSet"></param>
		/// <param name="newSet"></param>
		internal void ReplaceSet(Set oldSet, Set newSet)
		{
			if (oldSet != newSet)
			{
				if (oldSet == null)
				{
					throw new ArgumentNullException("oldSet");
				}
				if (newSet == null)
				{
					throw new ArgumentNullException("newSet");
				}
				if (newSet.Name != oldSet.Name)
				{
					ValidateName(newSet.Name);
				}
				Action transform = delegate
				{
					ReplaceSetInner(oldSet, newSet);
				};
				Action undo = delegate
				{
					ReplaceSetInner(newSet, oldSet);
				};
				string name = newSet.Name;
				string name2 = oldSet.Name;
				ReplaceInner(name2, name, transform, undo);
			}
		}

		private void ReplaceSetInner(Set oldSet, Set newSet)
		{
			foreach (Decision decision in Decisions)
			{
				if (decision._indexSets == null)
				{
					continue;
				}
				for (int i = 0; i < decision.IndexSets.Count; i++)
				{
					if (decision.IndexSets[i] == oldSet)
					{
						decision.SetIndexSet(i, newSet);
					}
				}
			}
			foreach (RecourseDecision recourseDecision in RecourseDecisions)
			{
				if (recourseDecision.IndexSets == null)
				{
					continue;
				}
				for (int j = 0; j < recourseDecision.IndexSets.Count; j++)
				{
					if (recourseDecision.IndexSets[j] == oldSet)
					{
						recourseDecision.SetIndexSet(j, newSet);
					}
				}
			}
			foreach (Parameter parameter in Parameters)
			{
				if (parameter.IndexSets == null)
				{
					continue;
				}
				for (int k = 0; k < parameter.IndexSets.Count; k++)
				{
					if (parameter.IndexSets[k] == oldSet)
					{
						parameter.SetIndexSet(k, newSet);
					}
				}
			}
			foreach (RandomParameter randomParameter in RandomParameters)
			{
				if (randomParameter.IndexSets == null)
				{
					continue;
				}
				for (int l = 0; l < randomParameter.IndexSets.Count; l++)
				{
					if (randomParameter.IndexSets[l] == oldSet)
					{
						randomParameter.SetIndexSet(l, newSet);
					}
				}
			}
		}

		/// <summary>
		/// Add a Random parameter to the model (for stochastic programming).
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Thrown if the parameter has already been added to a different model.</exception>
		public void AddParameter(RandomParameter parameter)
		{
			if ((object)parameter == null)
			{
				throw new ArgumentNullException("parameter");
			}
			VerifyOwningModelIsEmpty(parameter);
			if ((object)parameter._refKey == null)
			{
				ValidateName(parameter.Name);
			}
			_randomParameters.Add(parameter);
			_hasValidSolution = false;
			parameter._owningModel = this;
			if (_validateNames)
			{
				_names.Add(parameter.Name);
			}
		}

		/// <summary>
		/// Add a parameter to the model.
		/// </summary>
		/// <param name="parameter">The parameter to add.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Thrown if the parameter has already been added to a different model.</exception>
		public void AddParameter(Parameter parameter)
		{
			VerifyModelNotFrozen();
			if ((object)parameter == null)
			{
				throw new ArgumentNullException("parameter");
			}
			VerifyOwningModelIsEmpty(parameter);
			if ((object)parameter._refKey == null)
			{
				ValidateName(parameter.Name);
			}
			_hasValidSolution = false;
			_parameters.Add(parameter);
			parameter._owningModel = this;
			if (_validateNames)
			{
				_names.Add(parameter.Name);
			}
		}

		/// <summary>
		/// Add a group of parameters to the model.
		/// </summary>
		/// <param name="parameters">The parameters to add.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Thrown if the parameter has already been added to a different model.</exception>
		public void AddParameters(params Parameter[] parameters)
		{
			if (parameters == null)
			{
				throw new ArgumentNullException("parameters");
			}
			foreach (Parameter parameter in parameters)
			{
				AddParameter(parameter);
			}
		}

		/// <summary>
		/// Add a group of random parameters to the model (for stochastic programming).
		/// </summary>
		/// <param name="parameters">The RandomParameters to add.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Thrown if the parameter has already been added to a different model.</exception>
		public void AddParameters(params RandomParameter[] parameters)
		{
			VerifyModelNotFrozen();
			if (parameters == null)
			{
				throw new ArgumentNullException("parameters");
			}
			foreach (RandomParameter parameter in parameters)
			{
				AddParameter(parameter);
			}
		}

		/// <summary>
		/// Replace a Parameter with another Parameter everywhere it appears in the model
		/// </summary>
		/// <param name="oldParameter"></param>
		/// <param name="newParameter"></param>
		internal void ReplaceParameter(Parameter oldParameter, Parameter newParameter)
		{
			if ((object)oldParameter == newParameter)
			{
				return;
			}
			if ((object)oldParameter == null)
			{
				throw new ArgumentNullException("oldParameter");
			}
			if ((object)newParameter == null)
			{
				throw new ArgumentNullException("newParameter");
			}
			VerifyOwningModelIsEmpty(newParameter);
			if (newParameter.Name != oldParameter.Name)
			{
				ValidateName(newParameter.Name);
			}
			PrepareCollectionForRemoval(ref _parameters);
			Action transform = delegate
			{
				_parameters.Remove(oldParameter);
				_parameters.Add(newParameter);
				if (_validateNames)
				{
					_names.Remove(oldParameter.Name);
					_names.Add(newParameter.Name);
				}
			};
			Action undo = delegate
			{
				_parameters.Remove(newParameter);
				_parameters.Add(oldParameter);
				if (_validateNames)
				{
					_names.Remove(newParameter.Name);
					_names.Add(oldParameter.Name);
				}
			};
			string name = newParameter._name;
			string name2 = oldParameter._name;
			newParameter._owningModel = this;
			ReplaceInner(name2, name, transform, undo);
			oldParameter._owningModel = null;
		}

		/// <summary>
		/// Replace a Parameter with another Parameter everywhere it appears in the model
		/// </summary>
		/// <param name="oldParameter"></param>
		/// <param name="newParameter"></param>
		internal void ReplaceParameter(RandomParameter oldParameter, RandomParameter newParameter)
		{
			if ((object)oldParameter == newParameter)
			{
				return;
			}
			if ((object)oldParameter == null)
			{
				throw new ArgumentNullException("oldParameter");
			}
			if ((object)newParameter == null)
			{
				throw new ArgumentNullException("newParameter");
			}
			VerifyOwningModelIsEmpty(newParameter);
			if (newParameter.Name != oldParameter.Name)
			{
				ValidateName(newParameter.Name);
			}
			PrepareCollectionForRemoval(ref _randomParameters);
			Action transform = delegate
			{
				_randomParameters.Remove(oldParameter);
				_randomParameters.Add(newParameter);
				if (_validateNames)
				{
					_names.Remove(oldParameter.Name);
					_names.Add(newParameter.Name);
				}
			};
			Action undo = delegate
			{
				_randomParameters.Remove(newParameter);
				_randomParameters.Add(oldParameter);
				if (_validateNames)
				{
					_names.Remove(newParameter.Name);
					_names.Add(oldParameter.Name);
				}
			};
			string name = newParameter._name;
			string name2 = oldParameter._name;
			newParameter._owningModel = this;
			ReplaceInner(name2, name, transform, undo);
			oldParameter._owningModel = null;
		}

		/// <summary>
		/// Remove an unused Parameter from the model
		/// </summary>
		/// <param name="parameter">The parameter to remove.</param>
		public void RemoveParameter(Parameter parameter)
		{
			PrepareCollectionForRemoval(ref _parameters);
			string name = parameter.Name;
			ValidateUnusedToken(name);
			_parameters.Remove(parameter);
			if (_validateNames)
			{
				_names.Remove(parameter.Name);
			}
		}

		/// <summary>
		/// Remove an unused RandomParameter from the model.
		/// </summary>
		/// <param name="parameter">The parameter to remove.</param>
		public void RemoveParameter(RandomParameter parameter)
		{
			PrepareCollectionForRemoval(ref _randomParameters);
			string name = parameter.Name;
			ValidateUnusedToken(name);
			_randomParameters.Remove(parameter);
			if (_validateNames)
			{
				_names.Remove(parameter.Name);
			}
		}

		private void ValidateUnusedToken(string name)
		{
			foreach (Constraint constraint in Constraints)
			{
				if (FindToken(constraint.Expression, name))
				{
					throw new MsfException(string.Format(CultureInfo.CurrentCulture, Resources.CannotRemove0BecauseItIsUsed, new object[1] { name }));
				}
			}
			foreach (Goal goal in Goals)
			{
				if (FindToken(goal.Expression, name))
				{
					throw new MsfException(string.Format(CultureInfo.CurrentCulture, Resources.CannotRemove0BecauseItIsUsed, new object[1] { name }));
				}
			}
		}

		private void ValidateName(string name)
		{
			if (name != null && _validateNames)
			{
				if (!OmlWriter.IsValidOmlName(name))
				{
					throw new MsfException(string.Format(CultureInfo.CurrentCulture, Resources.BadOMLName, new object[1] { name }));
				}
				if (_names.Contains(name))
				{
					throw new MsfException(string.Format(CultureInfo.CurrentCulture, Resources.DuplicateName0, new object[1] { name }));
				}
			}
		}

		/// <summary>
		/// Add a group of tuples to the model.
		/// </summary>
		/// <param name="tuples">The tuples to add.</param>
		public void AddTuples(params Tuples[] tuples)
		{
			VerifyModelNotFrozen();
			if (tuples == null)
			{
				throw new ArgumentNullException("tuples");
			}
			foreach (Tuples tuple in tuples)
			{
				AddTuple(tuple);
			}
		}

		/// <summary>
		/// Add a tuples object to the model.
		/// </summary>
		/// <param name="tuple">The tuple to add.</param>
		public void AddTuple(Tuples tuple)
		{
			ValidateName(tuple.Name);
			VerifyModelNotFrozen();
			if (tuple == null)
			{
				throw new ArgumentNullException("tuple");
			}
			VerifyOwningModelIsEmpty(tuple);
			_hasValidSolution = false;
			tuple._owningModel = this;
			_tuples.Add(tuple);
			if (_validateNames)
			{
				_names.Add(tuple.Name);
			}
		}

		/// <summary>
		/// Add a RecourseDecision to the model (for stochastic programming).
		/// </summary>
		/// <param name="decision">The RecourseDecision to add.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Thrown if the decision has already been added to a different model.</exception>
		public void AddDecision(RecourseDecision decision)
		{
			VerifyModelNotFrozen();
			if ((object)decision == null)
			{
				throw new ArgumentNullException("decision");
			}
			VerifyOwningModelIsEmpty(decision);
			if ((object)decision._refKey == null)
			{
				ValidateName(decision.Name);
			}
			_hasValidSolution = false;
			_recourseDecisions.Add(decision);
			decision._owningModel = this;
			if (_parent == null)
			{
				decision._id = _maxRecourseDecisionId++;
			}
			else
			{
				decision._id = -1;
			}
			if (_validateNames)
			{
				_names.Add(decision.Name);
			}
		}

		/// <summary>
		/// Add a decision to the model.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Thrown if the decision has already been added to a different model.</exception>
		public void AddDecision(Decision decision)
		{
			VerifyModelNotFrozen();
			if ((object)decision == null)
			{
				throw new ArgumentNullException("decision");
			}
			VerifyOwningModelIsEmpty(decision);
			if ((object)decision._refKey == null)
			{
				ValidateName(decision.Name);
			}
			_hasValidSolution = false;
			_decisions.Add(decision);
			decision._owningModel = this;
			if (_parent == null)
			{
				decision._id = _maxDecisionId++;
			}
			else
			{
				decision._id = -1;
			}
			if (_validateNames)
			{
				_names.Add(decision.Name);
			}
		}

		/// <summary>
		/// Add a group of decisions to the model.
		/// </summary>
		/// <param name="decisions">The decisions to add.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Thrown if the decision has already been added to a different model.</exception>
		public void AddDecisions(params Decision[] decisions)
		{
			if (decisions == null)
			{
				throw new ArgumentNullException("decisions");
			}
			foreach (Decision decision in decisions)
			{
				AddDecision(decision);
			}
		}

		/// <summary>
		/// Add a group of RecourseDecisions to the model (for stochastic programming)
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Thrown if the decision has already been added to a different model.</exception>
		public void AddDecisions(params RecourseDecision[] decisions)
		{
			if (decisions == null)
			{
				throw new ArgumentNullException("decisions");
			}
			foreach (RecourseDecision decision in decisions)
			{
				AddDecision(decision);
			}
		}

		/// <summary>
		/// Replace a Decision with another Decision everywhere it appears in the model
		/// </summary>
		/// <param name="oldDecision"></param>
		/// <param name="newDecision"></param>
		internal void ReplaceDecision(Decision oldDecision, Decision newDecision)
		{
			if ((object)oldDecision == newDecision)
			{
				return;
			}
			if ((object)oldDecision == null)
			{
				throw new ArgumentNullException("oldDecision");
			}
			if ((object)newDecision == null)
			{
				throw new ArgumentNullException("newDecision");
			}
			VerifyOwningModelIsEmpty(newDecision);
			if (newDecision.Name != oldDecision.Name)
			{
				ValidateName(newDecision.Name);
			}
			PrepareCollectionForRemoval(ref _decisions);
			newDecision._id = oldDecision._id;
			Action transform = delegate
			{
				_decisions.Remove(oldDecision);
				_decisions.Add(newDecision);
				if (_validateNames)
				{
					_names.Remove(oldDecision.Name);
					_names.Add(newDecision.Name);
				}
			};
			Action undo = delegate
			{
				_decisions.Remove(newDecision);
				_decisions.Add(oldDecision);
				if (_validateNames)
				{
					_names.Remove(newDecision.Name);
					_names.Add(oldDecision.Name);
				}
			};
			string name = newDecision._name;
			string name2 = oldDecision._name;
			newDecision._owningModel = this;
			ReplaceInner(name2, name, transform, undo);
			oldDecision._owningModel = null;
		}

		/// <summary>
		/// Replace a Decision with another Decision everywhere it appears in the model
		/// </summary>
		/// <param name="oldDecision"></param>
		/// <param name="newDecision"></param>
		internal void ReplaceDecision(RecourseDecision oldDecision, RecourseDecision newDecision)
		{
			if ((object)oldDecision == newDecision)
			{
				return;
			}
			if ((object)oldDecision == null)
			{
				throw new ArgumentNullException("oldDecision");
			}
			if ((object)newDecision == null)
			{
				throw new ArgumentNullException("newDecision");
			}
			VerifyOwningModelIsEmpty(newDecision);
			if (newDecision.Name != oldDecision.Name)
			{
				ValidateName(newDecision.Name);
			}
			PrepareCollectionForRemoval(ref _recourseDecisions);
			newDecision._id = oldDecision._id;
			Action transform = delegate
			{
				_recourseDecisions.Remove(oldDecision);
				_recourseDecisions.Add(newDecision);
				if (_validateNames)
				{
					_names.Remove(oldDecision.Name);
					_names.Add(newDecision.Name);
				}
			};
			Action undo = delegate
			{
				_recourseDecisions.Remove(newDecision);
				_recourseDecisions.Add(oldDecision);
				if (_validateNames)
				{
					_names.Remove(newDecision.Name);
					_names.Add(oldDecision.Name);
				}
			};
			string name = newDecision._name;
			string name2 = oldDecision._name;
			newDecision._owningModel = this;
			ReplaceInner(name2, name, transform, undo);
			oldDecision._owningModel = null;
		}

		private void ReplaceInner(string oldName, string newName, Action transform, Action undo)
		{
			Dictionary<Constraint, string> dictionary = new Dictionary<Constraint, string>();
			Dictionary<Constraint, Term> dictionary2 = new Dictionary<Constraint, Term>();
			Dictionary<Goal, string> dictionary3 = new Dictionary<Goal, string>();
			Dictionary<Goal, Term> dictionary4 = new Dictionary<Goal, Term>();
			bool flag = false;
			try
			{
				transform();
				SolveRewriteSystem rs = new SolveRewriteSystem();
				OmlParser omlParser = new OmlParser(rs, _context, new OmlLexer());
				foreach (Constraint constraint in Constraints)
				{
					string expression = constraint.Expression;
					string text = expression;
					if (newName != oldName)
					{
						text = ReplaceToken(expression, oldName, newName);
					}
					dictionary[constraint] = text;
					dictionary2[constraint] = omlParser.ProcessExpression(this, "<input>", text);
				}
				foreach (Goal goal in Goals)
				{
					string expression2 = goal.Expression;
					string text2 = expression2;
					if (newName != oldName)
					{
						text2 = ReplaceToken(expression2, oldName, newName);
					}
					dictionary3[goal] = text2;
					dictionary4[goal] = omlParser.ProcessExpression(this, "<input>", text2);
				}
				flag = true;
				foreach (Constraint constraint2 in Constraints)
				{
					constraint2._expression = dictionary[constraint2];
					constraint2._term = dictionary2[constraint2];
				}
				foreach (Goal goal2 in Goals)
				{
					goal2._expression = dictionary3[goal2];
					goal2._term = dictionary4[goal2];
				}
			}
			finally
			{
				if (!flag)
				{
					undo();
				}
			}
		}

		private static string ReplaceToken(string oldString, string oldName, string newName)
		{
			OmlLexer omlLexer = new OmlLexer();
			StaticStringText staticStringText = new StaticStringText("<input>", oldString);
			IEnumerable<Token> enumerable = omlLexer.LexSource(staticStringText);
			int num = 0;
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Token item in enumerable)
			{
				if (item.GetSpan(staticStringText.Version, out var span))
				{
					string text = oldString.Substring(span.Min, span.Lim - span.Min);
					if (text == oldName)
					{
						stringBuilder.Append(oldString.Substring(num, span.Min - num));
						stringBuilder.Append(newName);
						num = span.Lim;
					}
				}
			}
			stringBuilder.Append(oldString.Substring(num));
			return stringBuilder.ToString();
		}

		private static bool FindToken(string oldString, string oldName)
		{
			OmlLexer omlLexer = new OmlLexer();
			StaticStringText staticStringText = new StaticStringText("<input>", oldString);
			IEnumerable<Token> enumerable = omlLexer.LexSource(staticStringText);
			foreach (Token item in enumerable)
			{
				if (item.GetSpan(staticStringText.Version, out var span))
				{
					string text = oldString.Substring(span.Min, span.Lim - span.Min);
					if (text == oldName)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static void PrepareCollectionForRemoval<T>(ref ICollection<T> collection)
		{
			if (collection is List<T>)
			{
				collection = new HashSet<T>(collection);
			}
		}

		/// <summary>
		/// Remove an unused Decision from the model.
		/// </summary>
		/// <param name="decision">The decision to remove.</param>
		public void RemoveDecision(Decision decision)
		{
			PrepareCollectionForRemoval(ref _decisions);
			string name = decision.Name;
			ValidateUnusedToken(name);
			_decisions.Remove(decision);
			if (_validateNames)
			{
				_names.Remove(decision.Name);
			}
		}

		/// <summary>
		/// Remove an unused RecourseDecision from the model.
		/// </summary>
		/// <param name="decision">The decision to remove.</param>
		public void RemoveDecision(RecourseDecision decision)
		{
			PrepareCollectionForRemoval(ref _recourseDecisions);
			string name = decision.Name;
			ValidateUnusedToken(name);
			_recourseDecisions.Remove(decision);
			if (_validateNames)
			{
				_names.Remove(decision.Name);
			}
		}

		/// <summary>
		/// Add a constraint to the model.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Thrown if the model is invalid.</exception>
		/// <remarks>
		/// The term should evaluate to a boolean result.
		/// </remarks>
		public Constraint AddConstraint(string name, Term constraint)
		{
			VerifyModelNotFrozen();
			if ((object)constraint == null)
			{
				throw new ArgumentNullException("constraint");
			}
			VerifyOwningModelMatches(constraint, this);
			_hasValidSolution = false;
			if (name == null)
			{
				name = "constraint" + UniqueSuffix();
			}
			ValidateName(name);
			Constraint constraint2 = new Constraint(_context, name, constraint);
			_constraints.Add(constraint2);
			if (_validateNames)
			{
				_names.Add(name);
			}
			return constraint2;
		}

		/// <summary>
		/// Add a constraint by parsing an OML expression
		/// </summary>
		/// <param name="name"></param>
		/// <param name="expression"></param>
		/// <returns></returns>
		public Constraint AddConstraint(string name, string expression)
		{
			VerifyModelNotFrozen();
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			_hasValidSolution = false;
			if (name == null)
			{
				name = "constraint" + UniqueSuffix();
			}
			ValidateName(name);
			SolveRewriteSystem rs = new SolveRewriteSystem();
			OmlParser omlParser = new OmlParser(rs, _context, new OmlLexer());
			Term term = omlParser.ProcessExpression(this, "<input>", expression);
			Constraint constraint = new Constraint(_context, name, term, expression);
			_constraints.Add(constraint);
			if (_validateNames)
			{
				_names.Add(name);
			}
			return constraint;
		}

		/// <summary>
		/// Add a group of constraints to the model.
		/// </summary>
		public Constraint AddConstraints(string name, params Term[] constraints)
		{
			if (constraints == null)
			{
				throw new ArgumentNullException("constraints");
			}
			_hasValidSolution = false;
			return AddConstraint(name, And(constraints));
		}

		/// <summary>
		/// Remove a constraint from the model.
		/// </summary>
		/// <param name="constraint"></param>
		/// <returns></returns>
		public void RemoveConstraint(Constraint constraint)
		{
			VerifyModelNotFrozen();
			if (constraint == null)
			{
				throw new ArgumentNullException("constraint");
			}
			_hasValidSolution = false;
			_constraints.Remove(constraint);
			if (_validateNames)
			{
				_names.Remove(constraint.Name);
			}
		}

		internal Constraint UpdateConstraint(Constraint oldConstraint, string name, string expression)
		{
			VerifyModelNotFrozen();
			_hasValidSolution = false;
			if (name == null)
			{
				name = "constraint" + UniqueSuffix();
			}
			if (name != oldConstraint.Name)
			{
				ValidateName(name);
			}
			SolveRewriteSystem rs = new SolveRewriteSystem();
			OmlParser omlParser = new OmlParser(rs, _context, new OmlLexer());
			Term term = omlParser.ProcessExpression(this, "<input>", expression);
			if (_validateNames)
			{
				_names.Remove(oldConstraint.Name);
				_names.Add(name);
			}
			oldConstraint._term = term;
			oldConstraint._expression = expression;
			oldConstraint._name = name;
			return oldConstraint;
		}

		internal Goal UpdateGoal(Goal oldGoal, string name, GoalKind direction, string expression)
		{
			VerifyModelNotFrozen();
			_hasValidSolution = false;
			if (name == null)
			{
				name = "goal" + UniqueSuffix();
			}
			if (name != oldGoal.Name)
			{
				ValidateName(name);
			}
			SolveRewriteSystem rs = new SolveRewriteSystem();
			OmlParser omlParser = new OmlParser(rs, _context, new OmlLexer());
			Term term = omlParser.ProcessExpression(this, "<input>", expression);
			if (_validateNames)
			{
				_names.Remove(oldGoal.Name);
				_names.Add(name);
			}
			oldGoal._term = term;
			oldGoal._expression = expression;
			oldGoal._name = name;
			oldGoal._direction = direction;
			return oldGoal;
		}

		/// <summary>
		/// Add a goal by parsing an OML expression
		/// </summary>
		/// <param name="name"></param>
		/// <param name="direction"></param>
		/// <param name="expression"></param>
		/// <returns></returns>
		public Goal AddGoal(string name, GoalKind direction, string expression)
		{
			VerifyModelNotFrozen();
			SolveRewriteSystem rs = new SolveRewriteSystem();
			OmlParser omlParser = new OmlParser(rs, _context, new OmlLexer());
			Term term = omlParser.ProcessExpression(this, "<input>", expression);
			if (term.HasStructure(TermStructure.Multivalue))
			{
				throw new ModelException(Resources.ForeachCannotBeUsedAsAGoal);
			}
			_hasValidSolution = false;
			if (name == null)
			{
				name = "goal" + UniqueSuffix();
			}
			ValidateName(name);
			Goal goal = new Goal(_context, name, direction, term, expression);
			_goals.Add(goal);
			if (_validateNames)
			{
				_names.Add(name);
			}
			return goal;
		}

		/// <summary>
		/// Add a goal to the model.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Thrown if the model is invalid.</exception>
		public Goal AddGoal(string name, GoalKind direction, Term goal)
		{
			VerifyModelNotFrozen();
			if ((object)goal == null)
			{
				throw new ArgumentNullException("goal");
			}
			if (direction != 0 && direction != GoalKind.Minimize)
			{
				throw new ArgumentOutOfRangeException("direction");
			}
			if (goal.HasStructure(TermStructure.Multivalue))
			{
				throw new ModelException(Resources.ForeachCannotBeUsedAsAGoal);
			}
			VerifyOwningModelMatches(goal, this);
			_hasValidSolution = false;
			if (name == null)
			{
				name = "goal" + UniqueSuffix();
			}
			ValidateName(name);
			Goal goal2 = new Goal(_context, name, direction, goal);
			_goals.Add(goal2);
			if (_validateNames)
			{
				_names.Add(name);
			}
			return goal2;
		}

		/// <summary>
		/// Add a group of goals to the model.
		/// </summary>
		public Goal[] AddGoals(string name, GoalKind direction, params Term[] goals)
		{
			if (goals == null)
			{
				throw new ArgumentNullException("goals");
			}
			if (direction != 0 && direction != GoalKind.Minimize)
			{
				throw new ArgumentOutOfRangeException("direction");
			}
			_hasValidSolution = false;
			if (name == null)
			{
				name = "goal" + UniqueSuffix();
			}
			Goal[] array = new Goal[goals.Length];
			for (int i = 0; i < goals.Length; i++)
			{
				array[i] = AddGoal(name + i, direction, goals[i]);
			}
			return array;
		}

		/// <summary>
		/// Remove a goal from the model
		/// </summary>
		/// <param name="goal"></param>
		public void RemoveGoal(Goal goal)
		{
			VerifyModelNotFrozen();
			if (goal == null)
			{
				throw new ArgumentNullException("goal");
			}
			_hasValidSolution = false;
			_goals.Remove(goal);
			if (_validateNames)
			{
				_names.Remove(goal.Name);
			}
		}

		/// <summary>
		/// Remove a group of goals from the model
		/// </summary>
		/// <param name="goals"></param>
		public void RemoveGoals(params Goal[] goals)
		{
			if (goals == null)
			{
				throw new ArgumentNullException("goals");
			}
			_hasValidSolution = false;
			foreach (Goal goal in goals)
			{
				RemoveGoal(goal);
			}
		}

		internal void VerifyModelNotFrozen()
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.SfsSubmodelCannotModifyModels, new object[1] { Name }));
			}
		}

		internal static void VerifySaneInputs(params Term[] terms)
		{
			if (terms == null)
			{
				throw new ArgumentNullException("terms");
			}
			if (terms.Length == 0)
			{
				return;
			}
			Model model = null;
			int i;
			for (i = 0; i < terms.Length; i++)
			{
				if ((object)terms[i] == null)
				{
					throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture, "terms[{0}]", new object[1] { i }));
				}
				if (!terms[i].IsModelIndependentTerm)
				{
					model = terms[i]._owningModel;
					break;
				}
			}
			if (i < terms.Length && model == null)
			{
				throw new InvalidTermException(Resources.InvalidTermNotInModel, terms[0]);
			}
			for (; i < terms.Length; i++)
			{
				if ((object)terms[i] == null)
				{
					throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture, "terms[{0}]", new object[1] { i }));
				}
				if (!terms[i].IsModelIndependentTerm)
				{
					VerifyOwningModelMatches(terms[i], model);
				}
			}
		}

		private static void VerifyOwningModelMatches(Term term, Model model)
		{
			if (!term.IsModelIndependentTerm && term._owningModel != model)
			{
				throw new InvalidTermException(Resources.InvalidTermNotInModel, term);
			}
		}

		private static void VerifyOwningModelIsEmpty(Tuples tuples)
		{
			if (tuples._owningModel != null)
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.TermHasBeenAddedToAModel01, new object[2]
				{
					tuples.Name,
					tuples._owningModel.Name
				}));
			}
		}

		private static void VerifyOwningModelIsEmpty(Term term)
		{
			if (term._owningModel != null)
			{
				throw new ModelException(string.Format(CultureInfo.InvariantCulture, Resources.TermHasBeenAddedToAModel01, new object[2]
				{
					term.ToString(),
					term._owningModel.Name
				}));
			}
		}

		/// <summary>
		/// Verify that either all inputs are numeric, or all inputs are symbols with compatible domains.
		/// </summary>
		/// <param name="terms"></param>
		/// <param name="head"></param>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		internal static void VerifyCompatibleInputs(Operator head, params Term[] terms)
		{
			VerifySaneInputs(terms);
			if (terms.Length == 0)
			{
				throw new ArgumentException(Resources.EachOperationMustHaveAtLeastOneInput);
			}
			Term term = terms[0];
			if (FlattenOperation(head))
			{
				while (term is OperatorTerm operatorTerm && operatorTerm.Operation == head)
				{
					term = operatorTerm.Inputs[0];
				}
			}
			if (term.ValueClass == TermValueClass.Enumerated || term.ValueClass == TermValueClass.String)
			{
				VerifyEnumeratedInputs(head, terms, term);
			}
			else
			{
				VerifyNumericInputs(head, terms);
			}
		}

		internal static void VerifyEnumeratedInputs(Operator head, Term[] terms, Term firstTerm)
		{
			Domain domain = null;
			if (terms.Length == 0)
			{
				throw new ArgumentException(Resources.EachOperationMustHaveAtLeastOneInput);
			}
			List<StringConstantTerm> list = new List<StringConstantTerm>();
			foreach (Term term in terms)
			{
				if (term is OperatorTerm operatorTerm && operatorTerm.Operation == head)
				{
					continue;
				}
				if (term.ValueClass == TermValueClass.String)
				{
					if (term is StringConstantTerm item)
					{
						list.Add(item);
					}
					continue;
				}
				if (term.ValueClass != TermValueClass.Enumerated)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotSymbolic, new object[1] { term }));
				}
				if (domain == null)
				{
					domain = term.EnumeratedDomain;
				}
				if (term.EnumeratedDomain != domain)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.Inputs0And1HaveDifferentSymbolDomains, new object[2] { firstTerm, term }));
				}
			}
			if (domain == null)
			{
				return;
			}
			foreach (StringConstantTerm item2 in list)
			{
				if (!domain.EnumeratedNames.Contains(item2._value))
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.SymbolNotFoundInDomain, new object[2] { item2._value, firstTerm }));
				}
			}
		}

		/// <summary>
		/// Verify that all inputs are numeric (including boolean).
		/// </summary>
		/// <param name="terms"></param>
		/// <param name="head"></param>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		internal static void VerifyNumericInputs(Operator head, params Term[] terms)
		{
			VerifySaneInputs(terms);
			if (terms.Length == 0)
			{
				throw new ArgumentException(Resources.EachOperationMustHaveAtLeastOneInput);
			}
			foreach (Term term in terms)
			{
				OperatorTerm operatorTerm = term as OperatorTerm;
				if ((!FlattenOperation(head) || (object)operatorTerm == null || operatorTerm.Operation != head) && !term.IsNumeric)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotNumeric, new object[1] { term }));
				}
			}
		}

		/// <summary>
		/// Verify that the input is a single value (not a ForEach).
		/// </summary>
		/// <param name="term"></param>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		private static void VerifySingleValue(Term term)
		{
			if ((object)term == null)
			{
				throw new ArgumentNullException("term");
			}
			if (term is ForEachTerm)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.MustHaveASingleValue, new object[1] { term }));
			}
		}

		internal static bool FlattenOperation(Operator head)
		{
			switch (head)
			{
			case Operator.Equal:
			case Operator.Unequal:
			case Operator.Greater:
			case Operator.Less:
			case Operator.GreaterEqual:
			case Operator.LessEqual:
				return true;
			default:
				return false;
			}
		}

		internal static Term CreateInvocationTerm(Operator head, TermValueClass valueClass, params Term[] inputs)
		{
			if (inputs[0] is OperatorTerm)
			{
				OperatorTerm operatorTerm = inputs[0] as OperatorTerm;
				if (FlattenOperation(head) && operatorTerm.Operation == head)
				{
					Term[] array = new Term[operatorTerm.Inputs.Length + inputs.Length - 1];
					for (int i = 0; i < operatorTerm.Inputs.Length; i++)
					{
						array[i] = operatorTerm.Inputs[i];
					}
					for (int j = 1; j < inputs.Length; j++)
					{
						array[operatorTerm.Inputs.Length + j - 1] = inputs[j];
					}
					return CreateInvocationTermImpl(head, valueClass, array);
				}
			}
			return CreateInvocationTermImpl(head, valueClass, inputs);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		internal static Term CreateInvocationTermImpl(Operator head, TermValueClass valueClass, Term[] inputs)
		{
			switch (head)
			{
			case Operator.Equal:
				return new EqualTerm(inputs, valueClass);
			case Operator.Greater:
				return new GreaterTerm(inputs, valueClass);
			case Operator.GreaterEqual:
				return new GreaterEqualTerm(inputs, valueClass);
			case Operator.Less:
				return new LessTerm(inputs, valueClass);
			case Operator.LessEqual:
				return new LessEqualTerm(inputs, valueClass);
			case Operator.Plus:
				return new PlusTerm(inputs, valueClass);
			case Operator.Unequal:
				return new UnequalTerm(inputs, valueClass);
			case Operator.Abs:
				return new AbsTerm(inputs, valueClass);
			case Operator.And:
				return new AndTerm(inputs, valueClass);
			case Operator.Minus:
				return new MinusTerm(inputs, valueClass);
			case Operator.Not:
				return new NotTerm(inputs, valueClass);
			case Operator.Or:
				return new OrTerm(inputs, valueClass);
			case Operator.Quotient:
				return new QuotientTerm(inputs, valueClass);
			case Operator.Times:
				return new TimesTerm(inputs, valueClass);
			case Operator.Power:
				return new PowerTerm(inputs, valueClass);
			case Operator.Max:
				return new MaxTerm(inputs, valueClass);
			case Operator.Min:
				return new MinTerm(inputs, valueClass);
			case Operator.Cos:
				return new CosTerm(inputs, valueClass);
			case Operator.Sin:
				return new SinTerm(inputs, valueClass);
			case Operator.Tan:
				return new TanTerm(inputs, valueClass);
			case Operator.ArcCos:
				return new ArcCosTerm(inputs, valueClass);
			case Operator.ArcSin:
				return new ArcSinTerm(inputs, valueClass);
			case Operator.ArcTan:
				return new ArcTanTerm(inputs, valueClass);
			case Operator.Cosh:
				return new CoshTerm(inputs, valueClass);
			case Operator.Sinh:
				return new SinhTerm(inputs, valueClass);
			case Operator.Tanh:
				return new TanhTerm(inputs, valueClass);
			case Operator.Exp:
				return new ExpTerm(inputs, valueClass);
			case Operator.Log:
				return new LogTerm(inputs, valueClass);
			case Operator.Log10:
				return new Log10Term(inputs, valueClass);
			case Operator.Sqrt:
				return new SqrtTerm(inputs, valueClass);
			case Operator.If:
				return new IfTerm(inputs, valueClass);
			case Operator.Ceiling:
				return new CeilingTerm(inputs, valueClass);
			case Operator.Floor:
				return new FloorTerm(inputs, valueClass);
			default:
				throw new InvalidOperationException();
			}
		}

		internal void ClearDecisionValues()
		{
			foreach (Decision allDecision in AllDecisions)
			{
				allDecision.ClearData();
			}
		}

		internal void ExtractDecisionValues(SolutionMapping mapping)
		{
			mapping.ExtractDecisionValues(this, _context.WinningTask.Directive);
			_hasValidSolution = true;
		}

		internal static IEnumerable<T2> GetTopLevelItems<T1, T2>(IEnumerable<T1> source, Func<T1, bool> cond, Func<T1, T2> selectAction)
		{
			return from item in source
				where cond(item)
				select selectAction(item);
		}

		/// <summary>
		/// Read in all late-bound data and construct a substitution table that includes the data in the model.
		/// </summary>
		/// <returns>A substitution table which should be applied to the expressions in the model to hydrate them.</returns>
		internal void BindData(SolverContext context)
		{
			IEnumerable<Set> sets = Sets;
			context.TraceSource.TraceInformation("Data binding");
			foreach (Set item in sets)
			{
				((IDataBindable)item).DataBind(context);
			}
			foreach (Decision decision in _decisions)
			{
				((IDataBindable)decision).DataBind(context);
			}
			foreach (Parameter parameter in _parameters)
			{
				((IDataBindable)parameter).DataBind(context);
			}
			foreach (RandomParameter randomParameter in _randomParameters)
			{
				((IDataBindable)randomParameter).DataBind(context);
			}
			if (context.TraceSource.Listeners.Count > 0)
			{
				foreach (Set item2 in sets)
				{
					context.TraceSource.TraceInformation("Set {0} has {1} elements", item2.Name, item2.ValueSet._set.Count);
				}
			}
			_dataBound = true;
		}

		/// <summary>
		/// Addition
		/// </summary>
		/// <param name="terms"></param>
		/// <returns>The sum of the inputs.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Sum(params Term[] terms)
		{
			VerifyNumericInputs(Operator.Plus, terms);
			return CreateInvocationTerm(Operator.Plus, TermValueClass.Numeric, terms);
		}

		/// <summary>
		/// Addition
		/// </summary>
		/// <param name="term1"></param>
		/// <param name="term2"></param>
		/// <returns>The sum of the inputs.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Sum(Term term1, Term term2)
		{
			if (!term1.IsNumeric)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotNumeric, new object[1] { term1 }));
			}
			if (!term2.IsNumeric)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.IsNotNumeric, new object[1] { term2 }));
			}
			return new PlusTerm(new Term[2] { term1, term2 }, TermValueClass.Numeric);
		}

		/// <summary>
		/// Subtraction
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>The difference of the inputs.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Difference(Term left, Term right)
		{
			VerifyNumericInputs(Operator.Plus, left, right);
			VerifySingleValue(left);
			VerifySingleValue(right);
			return CreateInvocationTerm(Operator.Plus, TermValueClass.Numeric, left, CreateInvocationTerm(Operator.Minus, TermValueClass.Numeric, right));
		}

		/// <summary>
		/// Multiplication
		/// </summary>
		/// <param name="terms"></param>
		/// <returns>The product of the inputs.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Product(params Term[] terms)
		{
			VerifyNumericInputs(Operator.Times, terms);
			return CreateInvocationTerm(Operator.Times, TermValueClass.Numeric, terms);
		}

		/// <summary>
		/// Division
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>The quotient of the inputs.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Quotient(Term left, Term right)
		{
			VerifyNumericInputs(Operator.Quotient, left, right);
			VerifySingleValue(left);
			VerifySingleValue(right);
			return CreateInvocationTerm(Operator.Quotient, TermValueClass.Numeric, left, right);
		}

		/// <summary>
		/// Exponentiation
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>The first input raised to the power of the second input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Power(Term left, Term right)
		{
			VerifyNumericInputs(Operator.Power, left, right);
			VerifySingleValue(left);
			VerifySingleValue(right);
			return CreateInvocationTerm(Operator.Power, TermValueClass.Numeric, left, right);
		}

		/// <summary>
		/// Arithmetic negation.
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The negation of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Negate(Term term)
		{
			VerifyNumericInputs(Operator.Minus, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Minus, TermValueClass.Numeric, term);
		}

		/// <summary>Absolute value
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The absolute value of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Abs(Term term)
		{
			VerifyNumericInputs(Operator.Abs, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Abs, TermValueClass.Numeric, term);
		}

		/// <summary>The Min operation.
		/// </summary>
		/// <param name="terms">The arguments to the operation.</param>
		/// <returns>The minimum value of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Min(params Term[] terms)
		{
			VerifyNumericInputs(Operator.Min, terms);
			return CreateInvocationTerm(Operator.Min, TermValueClass.Numeric, terms);
		}

		/// <summary>The Max operation.
		/// </summary>
		/// <param name="terms">The arguments to the operation.</param>
		/// <returns>The minimum value of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Max(params Term[] terms)
		{
			VerifyNumericInputs(Operator.Max, terms);
			return CreateInvocationTerm(Operator.Max, TermValueClass.Numeric, terms);
		}

		/// <summary>
		/// Exponentiation
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>E raised to the power of the second input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Exp(Term term)
		{
			VerifyNumericInputs(Operator.Exp, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Exp, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Square root
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The (positive) square root of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Sqrt(Term term)
		{
			VerifyNumericInputs(Operator.Sqrt, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Sqrt, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Cosine.
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The cosine of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Cos(Term term)
		{
			VerifyNumericInputs(Operator.Cos, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Cos, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Sine.
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The sine of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Sin(Term term)
		{
			VerifyNumericInputs(Operator.Sin, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Sin, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Tangent.
		/// </summary>
		/// <param name="term">The input term.</param>
		/// <returns>The tangent of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Tan(Term term)
		{
			VerifyNumericInputs(Operator.Tan, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Tan, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Arc cosine.
		/// </summary>
		/// <param name="term">The input term.</param>
		/// <returns>The arc cosine of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term ArcCos(Term term)
		{
			VerifyNumericInputs(Operator.ArcCos, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.ArcCos, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Arc sine.
		/// </summary>
		/// <param name="term">The input term.</param>
		/// <returns>The arc sine of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term ArcSin(Term term)
		{
			VerifyNumericInputs(Operator.ArcSin, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.ArcSin, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Arc tangent.
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The arc tangent of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term ArcTan(Term term)
		{
			VerifyNumericInputs(Operator.ArcTan, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.ArcTan, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Hyperbolic cosine
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The hyperbolic cosine of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Cosh(Term term)
		{
			VerifyNumericInputs(Operator.Cosh, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Cosh, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Hyperbolic sine.
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The hyperbolic sine of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Sinh(Term term)
		{
			VerifyNumericInputs(Operator.Sinh, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Sinh, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Hyperbolic tangent.
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The hyperbolic tangent of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Tanh(Term term)
		{
			VerifyNumericInputs(Operator.Tanh, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Tanh, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Natural logarithm.
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The natural log of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Log(Term term)
		{
			VerifyNumericInputs(Operator.Log, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Log, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Base 10 logarithm.
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The base 10 log of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Log10(Term term)
		{
			VerifyNumericInputs(Operator.Log10, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Log10, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Ceiling.
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The ceiling of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Ceiling(Term term)
		{
			VerifyNumericInputs(Operator.Ceiling, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Ceiling, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Floor.
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>The floor of the input.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given input.</exception>
		public static Term Floor(Term term)
		{
			VerifyNumericInputs(Operator.Floor, term);
			VerifySingleValue(term);
			return CreateInvocationTerm(Operator.Floor, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Conditional term (trinary).
		/// </summary>
		/// <param name="condition">A term representing the condition for the operation.</param>
		/// <param name="resultTrue">The term representing the evaluation result when the condition is true.</param>
		/// <param name="resultFalse">The term representing the evaluation result when the condition is false.</param>
		/// <returns>If the first input is true the second input is returned, otherwise the third input is returned.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term If(Term condition, Term resultTrue, Term resultFalse)
		{
			VerifyNumericInputs(Operator.If, condition);
			VerifyNumericInputs(Operator.If, resultTrue, resultFalse);
			return CreateInvocationTerm(Operator.If, TermValueClass.Numeric, condition, resultTrue, resultFalse);
		}

		/// <summary>Equality.
		/// </summary>
		/// <param name="terms">The input terms for the operation.</param>
		/// <returns>1 if all the inputs are equal, 0 otherwise.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Equal(params Term[] terms)
		{
			VerifyCompatibleInputs(Operator.Equal, terms);
			return new IdentityTerm(CreateInvocationTerm(Operator.Equal, TermValueClass.Numeric, terms));
		}

		/// <summary>All-different.
		/// </summary>
		/// <param name="terms">The input terms for the operation.</param>
		/// <returns>1 if all the inputs are pairwise different, 0 otherwise.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term AllDifferent(params Term[] terms)
		{
			VerifyCompatibleInputs(Operator.Unequal, terms);
			return new IdentityTerm(CreateInvocationTerm(Operator.Unequal, TermValueClass.Numeric, terms));
		}

		/// <summary>
		/// Less-than.
		/// </summary>
		/// <param name="terms">The input terms for the operation.</param>
		/// <returns>1 if the inputs are strictly increasing, 0 otherwise.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Less(params Term[] terms)
		{
			VerifyCompatibleInputs(Operator.Less, terms);
			return new IdentityTerm(CreateInvocationTerm(Operator.Less, TermValueClass.Numeric, terms));
		}

		/// <summary>
		/// Less-than-or-equal.
		/// </summary>
		/// <param name="terms">The input terms for the operation.</param>
		/// <returns>1 if the inputs are increasing or equal, 0 otherwise.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term LessEqual(params Term[] terms)
		{
			VerifyCompatibleInputs(Operator.LessEqual, terms);
			return new IdentityTerm(CreateInvocationTerm(Operator.LessEqual, TermValueClass.Numeric, terms));
		}

		/// <summary>
		/// Greater-than
		/// </summary>
		/// <param name="terms"></param>
		/// <returns>1 if the inputs are strictly decreasing, 0 otherwise.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Greater(params Term[] terms)
		{
			VerifyCompatibleInputs(Operator.Greater, terms);
			return new IdentityTerm(CreateInvocationTerm(Operator.Greater, TermValueClass.Numeric, terms));
		}

		/// <summary>
		/// Greater-than-or-equal.
		/// </summary>
		/// <param name="terms">The input terms for the operation.</param>
		/// <returns>1 if the inputs are decreasing or equal, 0 otherwise.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term GreaterEqual(params Term[] terms)
		{
			VerifyCompatibleInputs(Operator.GreaterEqual, terms);
			return new IdentityTerm(CreateInvocationTerm(Operator.GreaterEqual, TermValueClass.Numeric, terms));
		}

		/// <summary>
		/// Boolean conjunction.
		/// </summary>
		/// <param name="terms">The input terms for the operation.</param>
		/// <returns>1 if all the inputs are true (nonzero), 0 otherwise.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term And(params Term[] terms)
		{
			VerifyNumericInputs(Operator.And, terms);
			return CreateInvocationTerm(Operator.And, TermValueClass.Numeric, terms);
		}

		/// <summary>
		/// Boolean disjunction.
		/// </summary>
		/// <param name="terms">The input terms for the operation.</param>
		/// <returns>1 if any of the inputs is true (nonzero), 0 otherwise.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Or(params Term[] terms)
		{
			VerifyNumericInputs(Operator.Or, terms);
			return CreateInvocationTerm(Operator.Or, TermValueClass.Numeric, terms);
		}

		/// <summary>
		/// Boolean negation.
		/// </summary>
		/// <param name="term">A term representing the input argument to the operation.</param>
		/// <returns>1 if the input is false (zero), otherwise 0.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Not(Term term)
		{
			VerifyNumericInputs(Operator.Not, term);
			return CreateInvocationTerm(Operator.Not, TermValueClass.Numeric, term);
		}

		/// <summary>
		/// Logical implication.
		/// </summary>
		/// <param name="antecedent">The antecedent term for the implication.</param>
		/// <param name="consequence">The consequence term for the implication.</param>
		/// <returns>1 if the antecedent is false (zero) or the consequence is true (nonzero), 0 otherwise.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term Implies(Term antecedent, Term consequence)
		{
			VerifyNumericInputs(Operator.None, antecedent, consequence);
			return Or(Not(antecedent), consequence);
		}

		/// <summary>
		/// At most m inputs are true.
		/// </summary>
		/// <param name="m">The number of number of Terms to be true</param>
		/// <param name="terms">a query</param>
		/// <returns>1 if at most m of the inputs are true (nonzero), 0 otherwise.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term AtMostMofN(int m, params Term[] terms)
		{
			VerifyNumericInputs(Operator.None, terms);
			return new IdentityTerm(GreaterEqual(m, Sum(terms)));
		}

		/// <summary>
		/// Exactly m inputs are true
		/// </summary>
		/// <param name="m">number of vars to be true</param>
		/// <param name="terms">a query</param>
		/// <returns>1 if exactly m of the inputs are true (nonzero), 0 otherwise.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if the operation is not valid for the given inputs.</exception>
		public static Term ExactlyMofN(int m, params Term[] terms)
		{
			VerifyNumericInputs(Operator.None, terms);
			return new IdentityTerm(Equal(m, Sum(terms)));
		}

		/// <summary>
		/// Creates a special ordered sets (type 1) constraint. The input must be of the form
		/// <code>c1 * d1 + c2 * d2 + ...</code>
		/// where cN are constants and dN are decisions. None of the constants may be zero, and
		/// the same constant may not appear twice.
		///
		/// The SOS1 constraint enforces the property that at most one of the given decisions can be nonzero.
		/// The constants should be associated with the problem in some natural way. The ordering of the constants
		/// is used by the solver to find a solution more quickly.
		/// </summary>
		/// <param name="referenceRow">The input term for the Sos1 operation.</param>
		/// <returns>A Term representing the Sos constraint.</returns>
		public static Term Sos1(Term referenceRow)
		{
			VerifyNumericInputs(Operator.None, referenceRow);
			VerifySingleValue(referenceRow);
			OperatorTerm operatorTerm = referenceRow as OperatorTerm;
			if ((object)operatorTerm == null)
			{
				if (!(referenceRow is IdentityTerm identityTerm))
				{
					throw new ArgumentException(Resources.InvalidFormForSos1Constraint, "referenceRow");
				}
				operatorTerm = identityTerm._input as OperatorTerm;
				if ((object)operatorTerm == null)
				{
					throw new ArgumentException(Resources.InvalidFormForSos1Constraint, "referenceRow");
				}
			}
			if (operatorTerm.Operation != 0)
			{
				throw new ArgumentException(Resources.InvalidFormForSos1Constraint, "referenceRow");
			}
			return new Sos1Term(operatorTerm);
		}

		/// <summary>
		/// Creates a special ordered sets reference row. The input must be a constraint of the form
		/// <code>x == c1 * d1 + c2 * d2 + ...</code>
		/// where cN are constants and dN are decisions. The order of the equality matters. None of the constants may be zero, and
		/// the same constant may not appear twice.
		///
		/// The result of this is the equality constraint given, plus additional requirements: the sum of the dN must be 1,
		/// at most two of the dN can be nonzero, and any nonzero dN must be adjacent in the list produced by sorting the dN by the
		/// corresponding cN.
		/// </summary>
		/// <param name="referenceRow">The input term for the Sos2 operation.</param>
		/// <returns>A Term representing the Sos constraint.</returns>
		public static Term Sos2(Term referenceRow)
		{
			VerifySingleValue(referenceRow);
			OperatorTerm operatorTerm = referenceRow as OperatorTerm;
			if ((object)operatorTerm == null)
			{
				if (!(referenceRow is IdentityTerm identityTerm))
				{
					throw new ArgumentException(Resources.InvalidFormForSos2Constraint, "referenceRow");
				}
				operatorTerm = identityTerm._input as OperatorTerm;
				if ((object)operatorTerm == null)
				{
					throw new ArgumentException(Resources.InvalidFormForSos2Constraint, "referenceRow");
				}
			}
			if (operatorTerm.Operation == Operator.Equal)
			{
				VerifyNumericInputs(Operator.None, referenceRow);
				if (operatorTerm.Inputs.Count() != 2)
				{
					throw new ArgumentException(Resources.InvalidFormForSos2Constraint, "referenceRow");
				}
				Term[] inputs = operatorTerm.Inputs;
				foreach (Term term in inputs)
				{
					if (term is ForEachTerm)
					{
						throw new ArgumentException(Resources.InvalidFormForSos2Constraint, "referenceRow");
					}
				}
				if (!(operatorTerm.Inputs.ElementAt(1) is OperatorTerm operatorTerm2))
				{
					throw new ArgumentException(Resources.InvalidFormForSos2Constraint, "referenceRow");
				}
				if (operatorTerm2.Operation != 0)
				{
					throw new ArgumentException(Resources.InvalidFormForSos2Constraint, "referenceRow");
				}
				return new Sos2Term(operatorTerm);
			}
			if (operatorTerm.Operation == Operator.Plus)
			{
				VerifyNumericInputs(Operator.None, referenceRow);
				return new Sos2Term(operatorTerm);
			}
			throw new ArgumentException(Resources.InvalidFormForSos2Constraint, "referenceRow");
		}

		/// <summary>
		/// Tests a tuple for membership in a Tuples.
		/// </summary>
		/// <param name="tuple">A tuple Term.</param>
		/// <param name="tupleList">The Tuples to be tested.</param>
		/// <returns>Returns a boolean term for the result.</returns>
		public static Term Equal(Term[] tuple, Tuples tupleList)
		{
			VerifySaneInputs(tuple);
			if (tupleList == null)
			{
				throw new ArgumentNullException("tupleList");
			}
			if (tupleList.OwningModel == null)
			{
				throw new InvalidTermException(Resources.TuplesMustBeAddedToTheModelBeforeBeingUsed);
			}
			return new ElementOfTerm(tuple, tupleList);
		}

		/// <summary>
		/// Expands to one element for each element of the range, as if the function was called for each element.
		/// The function is only actually called once.
		/// </summary>
		/// <param name="range">The Set over which to iterate.</param>
		/// <param name="values">A function transforming element terms in the Set to result Terms.</param>
		/// <returns>A Term representing the result.</returns>
		public static Term ForEach(Set range, Func<Term, Term> values)
		{
			if (range == null)
			{
				throw new ArgumentNullException("range");
			}
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}
			IterationTerm iterationTerm = new IterationTerm("iter`" + range.Name, range.ItemValueClass, range._domain);
			Term valueExpression = values(iterationTerm);
			return new ForEachTerm(iterationTerm, range, valueExpression);
		}

		/// <summary>
		/// Expands to either zero or one element for each element of the range, as if the function was called
		/// for each element on which the condition is true. The function is only actually called once.
		/// </summary>
		/// <param name="range">The Set over which to iterate.</param>
		/// <param name="values">A function transforming element terms in the Set to result Terms.</param>
		/// <param name="condition">A function transforming element terms in the Set to 
		/// boolean Terms. The values function will expand only for elements where the condition function
		/// is ture.</param>
		/// <returns>A Term representing the result.</returns>
		public static Term ForEachWhere(Set range, Func<Term, Term> values, Func<Term, Term> condition)
		{
			if (range == null)
			{
				throw new ArgumentNullException("range");
			}
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}
			if (condition == null)
			{
				throw new ArgumentNullException("condition");
			}
			IterationTerm iterationTerm = new IterationTerm("iter`" + range.Name, range.ItemValueClass, range._domain);
			Term valueExpression = values(iterationTerm);
			Term condExpression = condition(iterationTerm);
			return new ForEachWhereTerm(iterationTerm, range, valueExpression, condExpression);
		}

		internal static void VerifySaneInputs(Term left, Term right)
		{
			if (!left.IsModelIndependentTerm && left._owningModel == null)
			{
				throw new InvalidTermException(Resources.InvalidTermNotInModel, left);
			}
			if (!right.IsModelIndependentTerm && right._owningModel == null)
			{
				throw new InvalidTermException(Resources.InvalidTermNotInModel, right);
			}
			if (!left.IsModelIndependentTerm && !right.IsModelIndependentTerm && left._owningModel != right._owningModel)
			{
				throw new InvalidTermException(Resources.InvalidTermNotInModel, left);
			}
		}

		internal ITermModel DebugGetTermModel(bool useNamesAsKeys = false, bool returnSimplified = false)
		{
			SimplifiedTermModelWrapper simplifiedTermModelWrapper = new SimplifiedTermModelWrapper(new TermModel(EqualityComparer<object>.Default));
			Term.EvaluationContext evaluationContext = new Term.EvaluationContext();
			TermTask.Builder builder = new TermTask.Builder(this, simplifiedTermModelWrapper, evaluationContext);
			builder._useNamesAsKeys = useNamesAsKeys;
			int num = 0;
			foreach (Goal goal in _goals)
			{
				builder.AddGoal(goal, num++);
			}
			builder.AddConstraints(_constraints);
			if (!returnSimplified)
			{
				return simplifiedTermModelWrapper._model;
			}
			return simplifiedTermModelWrapper;
		}
	}
}
