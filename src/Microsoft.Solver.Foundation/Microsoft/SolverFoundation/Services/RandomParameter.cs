#define TRACE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A group of terms that take stochastic values.
	/// </summary>
	/// <remarks>
	/// Random parameters are used to model randomness in input data.  Random parameters 
	/// can be discrete or continuous, and can be used where (non-random) Parameters or 
	/// constants would normally be used in Constraints.  RandomParameter is the base class 
	/// for a number of commonly used distributions including uniform, normal, and log normal.  
	/// A special type of random parameter is a ScenariosParameter, where each scenario 
	/// contains a value and a probability. 
	/// </remarks>
	public abstract class RandomParameter : Term, IDataBindable, IIndexable
	{
		internal readonly Domain _domain = Domain.DistributedValue;

		internal readonly Set[] _indexSets;

		/// <summary>
		/// Adds a data element to the internal ValueTable.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">Thrown if duplicate or out-of-range data is detected in the model.</exception>
		internal Action<object, ValueTable<DistributedValue>> _addValueTableElement;

		internal int _count;

		private ReadOnlyCollection<Set> _indexSetsCache;

		internal string _name;

		internal RandomParameter _refKey;

		private ValueTable<DistributedValue> _valueTable;

		/// <summary>If true, evaluating the parameter will return the expected value instead of sampling.
		/// </summary>
		internal bool EvaluateExpectedValue { get; set; }

		/// <summary>The index sets for this parameter.
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

		internal bool NeedsBind => ValueTable == null;

		internal IEnumerable Binding { get; set; }

		internal ValueTable<DistributedValue> ValueTable
		{
			get
			{
				return _valueTable;
			}
			set
			{
				_valueTable = value;
			}
		}

		/// <summary>Indexes by one or more values.
		/// </summary>
		/// <param name="indexes">The index terms.</param>
		/// <returns></returns>
		public Term this[params Term[] indexes]
		{
			get
			{
				if (_owningModel == null)
				{
					throw new InvalidTermException(Resources.InvalidTermParameterNotInModel, this);
				}
				DataBindingSupport.ValidateIndexArguments(_name, _indexSets, indexes);
				return new IndexTerm(this, _owningModel, indexes, _domain.ValueClass);
			}
		}

		internal override bool IsModelIndependentTerm => false;

		internal override TermType TermType => TermType.RandomParameter;

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

		/// <summary>
		/// The name of the parameter.
		/// </summary>
		public string Name => _name;

		/// <summary>
		/// A description.
		/// </summary>
		public string Description { get; set; }

		Set[] IIndexable.IndexSets => _indexSets;

		TermValueClass IIndexable.ValueClass => ValueClass;

		/// <summary>
		/// Random parameter for now is just numeric
		/// </summary>
		TermValueClass IIndexable.DomainValueClass => _domain.ValueClass;

		/// <summary>
		/// Create a new RandomParameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		protected RandomParameter(string name)
			: this(name, new Set[0])
		{
		}

		/// <summary>Create a new RandomParameter.
		/// </summary>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar parameter.</param>
		protected RandomParameter(string name, params Set[] indexSets)
		{
			if (indexSets == null)
			{
				throw new ArgumentNullException("indexSets");
			}
			_name = name;
			_indexSets = indexSets;
			if (_name == null)
			{
				_name = "randomparameter_" + Model.UniqueSuffix();
			}
			_structure = TermStructure.Constant | TermStructure.Linear | TermStructure.Quadratic | TermStructure.Differentiable;
		}

		/// <summary>Create a new RandomParameter.
		/// </summary>
		internal RandomParameter(string baseName, RandomParameter source)
		{
			_name = Term.BuildFullName(baseName, source._name);
			_refKey = source;
			_indexSets = source._indexSets;
			_count = source._count;
			_valueTable = source._valueTable;
			_addValueTableElement = source._addValueTableElement;
			_domain = source._domain;
			_structure = source._structure;
		}

		/// <summary>Re-sets the ith index set (refreshing the cache).
		/// </summary>
		public void SetIndexSet(int i, Set set)
		{
			_indexSets[i] = set;
			_indexSetsCache = null;
		}

		/// <summary>
		///
		/// </summary>
		///
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">Thrown if duplicate or out-of-range data is detected in the model.</exception>
		void IDataBindable.DataBind(SolverContext context)
		{
			if ((object)_refKey != null && _refKey._valueTable == null && _refKey.Binding != null)
			{
				((IDataBindable)_refKey).DataBind(context);
			}
			if ((object)_refKey == null || _refKey.Binding != Binding)
			{
				DataBind(context);
				return;
			}
			_addValueTableElement = _refKey._addValueTableElement;
			_valueTable = _refKey._valueTable;
		}

		void IDataBindable.PropagateValues(SolverContext context)
		{
		}

		/// <summary>Gets the current value of a non-indexed random parameter.
		/// </summary>
		/// <param name="inputValues">The input values.</param>
		/// <param name="value">An output parameter which stores the value (if successful).</param>
		/// <param name="context">The EvaluationContext.</param>
		/// <returns>Returns true if the random parameter could be evaluated as a constant.</returns>
		bool IIndexable.TryEvaluateConstantValue(object[] inputValues, out Rational value, EvaluationContext context)
		{
			if (_owningModel._level != 0)
			{
				return TryEvaluateSubmodelInstanceConstantValue(inputValues, out value, context);
			}
			if (!ValueTable.TryGetValue(out var value2, inputValues))
			{
				throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.MissingParameterValue, new object[2]
				{
					_name,
					string.Join(",", inputValues.Select((object inputValue) => inputValue.ToString()).ToArray())
				}));
			}
			value = GetConstantValue(value2);
			return true;
		}

		/// <summary>Returns the value of the current sample or the mean value
		/// </summary>
		private Rational GetConstantValue(DistributedValue distributedValue)
		{
			if (EvaluateExpectedValue)
			{
				return distributedValue.Distribution.Mean;
			}
			return distributedValue.CurrentSample;
		}

		/// <summary>
		/// Used in SetBinding methods to make sure the 
		/// owning model is not frozen 
		/// </summary>
		protected void VerifyModelNotFrozen()
		{
			if (_owningModel != null)
			{
				_owningModel.VerifyModelNotFrozen();
			}
		}

		/// <summary>
		/// Bind the data. Some minor implementation can depends on the random parameter type
		/// </summary>
		/// <param name="context"></param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">Thrown if duplicate or out-of-range data is detected in the model.</exception>
		protected virtual void DataBind(SolverContext context)
		{
			if (!NeedsBind)
			{
				return;
			}
			context.TraceSource.TraceInformation("Reading data for {0}", Name);
			if (Binding == null)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.NoDataBindingSetOn0, new object[1] { _name }));
			}
			ValueSet[] array = new ValueSet[_indexSets.Length];
			for (int i = 0; i < _indexSets.Length; i++)
			{
				array[i] = _indexSets[i].ValueSet;
			}
			ValueTable<DistributedValue> valueTable = ValueTable<DistributedValue>.Create(_domain, array);
			int num = 0;
			foreach (object item in Binding)
			{
				_addValueTableElement(item, valueTable);
				num++;
			}
			if (num <= 0)
			{
				throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.MustHaveAtLeastOneValue, new object[1] { _name }));
			}
			ValueTable = valueTable;
			context.TraceSource.TraceInformation("{0} values read", num);
		}

		/// <summary>This method creates getters
		/// for the indexes and also does all the checks common to all random parameters
		/// </summary>
		/// <returns>getters for each index</returns>
		protected Func<T, object>[] GetIndexFieldGetters<T>(IEnumerable<T> binding, params string[] indexFields)
		{
			if (binding == null)
			{
				throw new ArgumentNullException("binding");
			}
			if (indexFields == null)
			{
				throw new ArgumentNullException("indexFields");
			}
			VerifyModelNotFrozen();
			if (indexFields.Length != _indexSets.Length)
			{
				throw new ArgumentOutOfRangeException("indexFields", Resources.TheNumberOfIndexPropertiesMustMatchTheNumberOfIndexSets);
			}
			if (!NeedsBind)
			{
				throw new ModelException(Resources.RandomParameterHasAllreadyFilledWithDistributionDetails);
			}
			Binding = binding;
			Func<T, object>[] array = new Func<T, object>[indexFields.Length];
			for (int i = 0; i < indexFields.Length; i++)
			{
				array[i] = DataBindingSupport.MakeAccessorDelegate<T, object>(indexFields[i], _indexSets[i]._domain);
			}
			return array;
		}

		/// <summary>Use the index field accessor delegates to get the indexes from the object.
		/// </summary>
		protected static object[] GetIndexes<T>(object value, Func<T, object>[] indexFieldGetters)
		{
			object[] array = new object[indexFieldGetters.Length];
			for (int i = 0; i < indexFieldGetters.Length; i++)
			{
				array[i] = indexFieldGetters[i]((T)value);
			}
			return array;
		}

		internal void AddToTable(ValueTable<DistributedValue> table, DistributedValue value, object[] indexes)
		{
			try
			{
				table.Add(value, indexes);
			}
			catch (ArgumentException innerException)
			{
				throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.DuplicateEntriesDetectedInDataBoundTo0, new object[1] { _name }), innerException);
			}
		}

		private bool TryEvaluateSubmodelInstanceConstantValue(object[] inputValues, out Rational value, EvaluationContext context)
		{
			SubmodelInstance submodelInstance = SubmodelInstance.FollowPath(context);
			if (submodelInstance != null && submodelInstance.TryGetRandomParameter(this, out var val))
			{
				if (inputValues == null)
				{
					return val.TryEvaluateConstantValue(out value, context);
				}
				return ((IIndexable)val).TryEvaluateConstantValue(inputValues, out value, context);
			}
			throw new InvalidTermException(Resources.InternalError);
		}

		/// <summary>Gets the current value of a non-indexed random parameter.
		/// </summary>
		/// <param name="value">An output parameter which stores the value (if successful).</param>
		/// <param name="context">The EvaluationContext.</param>
		/// <returns>Returns true if the random parameter could be evaluated as a constant.</returns>
		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			if (_owningModel._level != 0)
			{
				return TryEvaluateSubmodelInstanceConstantValue(null, out value, context);
			}
			if (_indexSets.Length != 0)
			{
				throw new NotImplementedException();
			}
			value = 0;
			if (_valueTable == null)
			{
				return false;
			}
			DistributedValue value2;
			bool result = _valueTable.TryGetValue(out value2);
			value = GetConstantValue(value2);
			return result;
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}

		/// <summary>Returns a string representation for the RandomParameter.
		/// </summary>
		/// <returns>A string representation for the RandomParameter.</returns>
		public override string ToString()
		{
			return _name;
		}
	}
}
