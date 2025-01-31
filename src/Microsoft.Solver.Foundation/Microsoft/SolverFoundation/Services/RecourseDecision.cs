using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> RecourseDecisions are Decisions that are made in response to the realization of a RandomParameter.
	/// </summary>
	/// <remarks>
	/// Recourse decisions are sometimes called "second-stage" because such decisions can be made 
	/// only after the randomness is resolved.  Each RecourseDecision has an underlying Decision 
	/// for each second stage problem.  Example: the weather may be modeled using a ScenariosParameter. 
	/// If the weather turns out to be dry, we may not be able to produce enough of a certain crop 
	/// and may need to purchase it from someone else.  In this case, the purchase amount is a recourse decision.  
	/// </remarks>
	[DebuggerDisplay("{_name}")]
	public sealed class RecourseDecision : Term, IIndexable, IDataBindable
	{
		internal readonly Set[] _indexSets;

		private SolverContext _context;

		private Decision _currentDecision;

		internal Domain _domain;

		private ReadOnlyCollection<Set> _indexSetsCache;

		internal string _name;

		internal int _id = -1;

		internal RecourseDecision _refKey;

		internal List<Decision> _secondStageDecisions;

		internal double[] _secondStageProbabilities;

		internal ValueTable<double[]> _secondStageResults;

		internal override bool IsModelIndependentTerm => false;

		internal override TermType TermType => TermType.RecourseDecision;

		/// <summary>
		/// The index sets passed in when this object was created.
		/// </summary>
		public ReadOnlyCollection<Set> IndexSets
		{
			get
			{
				if (_indexSetsCache == null)
				{
					_indexSetsCache = new ReadOnlyCollection<Set>(new List<Set>(_indexSets));
				}
				return _indexSetsCache;
			}
		}

		/// <summary>
		/// When setting the CurrentSecondStageDecision this decision is kept
		/// </summary>
		internal Decision CurrentSecondStageDecision
		{
			get
			{
				return _currentDecision;
			}
			set
			{
				_currentDecision = value;
				if ((object)value != null)
				{
					_secondStageDecisions.Add(value);
				}
			}
		}

		/// <summary>
		/// Term indexed by one or more indexes 
		/// </summary>
		/// <param name="indexes">The indexes for the particular term</param>
		/// <returns>A Term that represents the indexed recourse decision.</returns>
		public Term this[params Term[] indexes]
		{
			get
			{
				if (_owningModel == null)
				{
					throw new InvalidTermException(Resources.InvalidTermDecisionNotInModel, this);
				}
				DataBindingSupport.ValidateIndexArguments(_name, _indexSets, indexes);
				return new IndexTerm(this, _owningModel, indexes, _domain.ValueClass);
			}
		}

		internal override TermValueClass ValueClass
		{
			get
			{
				if (_indexSets.Length > 0)
				{
					return TermValueClass.Table;
				}
				return _domain.ValueClass;
			}
		}

		internal override Domain EnumeratedDomain => _domain;

		Set[] IIndexable.IndexSets => _indexSets;

		/// <summary>
		/// The name of the decision.
		/// </summary>
		public string Name => _name;

		/// <summary>
		/// A description.
		/// </summary>
		public string Description { get; set; }

		TermValueClass IIndexable.ValueClass => ValueClass;

		TermValueClass IIndexable.DomainValueClass => _domain.ValueClass;

		/// <summary>Create a new non-indexed recourse decision. 
		/// </summary>
		/// <param name="domain">The set of values each element of the decision can take, such as Model.Real</param>
		/// <param name="name">A name for the decision. The name must be unique. If the value is null, a unique name will be generated.</param>
		public RecourseDecision(Domain domain, string name)
			: this(domain, name, new Set[0])
		{
		}

		/// <summary>
		/// Create a new indexed recourse decision. The recourse decision may be a single-valued (scalar), or multi-valued (a table).
		/// To create a single-valued decision, pass in a zero-length indexSets array.
		/// If indexSets has nonzero length, each element of it represents a set of values which this decision
		/// is indexed by. For example, if there are two index sets, then this decision takes two indexes, one from the
		/// first set and one from the second set.
		/// The total number of decisions is the product of the sizes of all the index sets.
		/// </summary>
		/// <param name="domain">The set of values each element of the decision can take, such as Model.Real</param>
		/// <param name="name">A name for the decision. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar decision.</param>
		public RecourseDecision(Domain domain, string name, params Set[] indexSets)
		{
			if (domain == null)
			{
				throw new ArgumentNullException("domain");
			}
			if (indexSets == null)
			{
				throw new ArgumentNullException("indexSets");
			}
			if (indexSets.Length > 0 && !domain.IsNumeric)
			{
				throw new ModelException(Resources.OnlyNumericDecisionsCanBeIndexed);
			}
			_domain = domain;
			_name = name;
			_indexSets = indexSets;
			_secondStageDecisions = new List<Decision>();
			if (_name == null)
			{
				_name = "recourseDecision_" + Model.UniqueSuffix();
			}
			_structure = TermStructure.Linear | TermStructure.Quadratic | TermStructure.Differentiable;
			if (domain.IntRestricted)
			{
				_structure |= TermStructure.Integer;
			}
		}

		private RecourseDecision(string baseName, RecourseDecision source)
		{
			_name = Term.BuildFullName(baseName, source._name);
			_refKey = source;
			_domain = source._domain;
			_indexSets = source._indexSets;
			_context = source._context;
			_currentDecision = source._currentDecision;
			_secondStageDecisions = new List<Decision>(source._secondStageDecisions);
			_secondStageProbabilities = source._secondStageProbabilities;
			_structure = source._structure;
		}

		/// <summary>Re-sets the ith index set (refreshing the cache).
		/// </summary>
		public void SetIndexSet(int i, Set set)
		{
			_indexSets[i] = set;
			_indexSetsCache = null;
		}

		/// <summary>Reset second-stage decision information.
		/// </summary>
		internal void Reset()
		{
			_currentDecision = null;
			_secondStageDecisions.Clear();
		}

		void IDataBindable.DataBind(SolverContext context)
		{
			_context = context;
		}

		void IDataBindable.PropagateValues(SolverContext context)
		{
			throw new NotImplementedException();
		}

		internal override Term Clone(string baseName)
		{
			return new RecourseDecision(baseName, this);
		}

		/// <summary>
		/// Convert the value to a string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (string.IsNullOrEmpty(Name))
			{
				return base.ToString();
			}
			return Name;
		}

		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			value = 0;
			return false;
		}

		/// <summary>
		/// This is called by the hydrator after all values of the inner decisions was set
		/// </summary>
		internal void CalculateSecondStageResults()
		{
			if (_secondStageResults != null || _domain.ValueClass != 0)
			{
				return;
			}
			throw new NotImplementedException();
		}

		internal void ClearSecondStageResults()
		{
			ValueSet[] array = new ValueSet[_indexSets.Length];
			for (int i = 0; i < _indexSets.Length; i++)
			{
				array[i] = IndexSets[i].ValueSet;
			}
			_secondStageResults = ValueTable<double[]>.Create(null, array);
		}

		/// <summary>Set the second stage result summary (avg, min, max)
		/// </summary>
		/// <remarks>This is called during solving when using decomposition, or after solving finished
		/// when using DE</remarks>
		/// <param name="results">avg, min, max</param>
		/// <param name="keys"></param>
		internal void SetSecondStageResult(double[] results, params object[] keys)
		{
			_secondStageResults.Set(results, keys);
		}

		/// <summary>Gets the up to date second stage results (avg, min, max)
		/// </summary>
		/// <param name="keys"></param>
		/// <returns>resluts if exist, null otherwise</returns>
		internal double[] GetSecondStageResults(params object[] keys)
		{
			double[] value = new double[3];
			if (_secondStageResults.TryGetValue(out value, keys))
			{
				return value;
			}
			return null;
		}

		/// <summary>
		/// Gets the underlying decision ready, so it can have the value when solving.  
		/// </summary>
		internal void InitCurrentDecision()
		{
			((IDataBindable)CurrentSecondStageDecision).DataBind(_context);
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}

		bool IIndexable.TryEvaluateConstantValue(object[] inputValues, out Rational value, EvaluationContext context)
		{
			value = 0;
			return false;
		}
	}
}
