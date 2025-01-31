#define TRACE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A group of terms which will take concrete values only when data is bound.
	/// </summary>
	public sealed class Parameter : Term, IDataBindable, IIndexable
	{
		internal readonly Set[] _indexSets;

		/// <summary>
		/// Adds a data element to the internal ValueTable.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">Thrown if duplicate or out-of-range data is detected in the model.</exception>
		private Action<object, ValueTable<double>> _addValueTableElement;

		internal int _count;

		internal Domain _domain;

		private ReadOnlyCollection<Set> _indexSetsCache;

		internal string _name;

		internal Parameter _refKey;

		private ValueTable<double> _valueTable;

		private Rational _defaultValue;

		internal override bool IsModelIndependentTerm => false;

		internal override TermType TermType => TermType.Parameter;

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

		internal IEnumerable Binding { get; private set; }

		internal ValueTable<double> ValueTable => _valueTable;

		/// <summary>
		/// REVIEW shahark: currently not added to the random types, but in future releases
		/// if we support stochastic CSP we may need that 
		/// </summary>
		internal override Domain EnumeratedDomain => _domain;

		/// <summary>
		/// Indexes by one or more values
		/// </summary>
		/// <param name="indexes"></param>
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

		Set[] IIndexable.IndexSets => _indexSets;

		TermValueClass IIndexable.ValueClass => ValueClass;

		TermValueClass IIndexable.DomainValueClass => _domain.ValueClass;

		/// <summary>
		/// The name of the parameter.
		/// </summary>
		public string Name => _name;

		/// <summary>
		/// A description.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Create a new parameter.
		/// </summary>
		/// <param name="domain">The set of values each element of the parameter can take, such as Model.Real</param>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		public Parameter(Domain domain, string name)
			: this(domain, name, new Set[0])
		{
		}

		/// <summary>
		/// Create a new parameter.
		/// </summary>
		/// <param name="domain">The set of values each element of the parameter can take, such as Model.Real</param>
		/// <param name="name">A name for the parameter. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets"></param>
		public Parameter(Domain domain, string name, params Set[] indexSets)
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
				throw new ModelException(Resources.OnlyNumericParametersCanBeIndexed);
			}
			_domain = domain;
			_name = name;
			_indexSets = indexSets;
			if (_name == null)
			{
				_name = "parameter_" + Model.UniqueSuffix();
			}
			_structure = TermStructure.Constant | TermStructure.Linear | TermStructure.Quadratic | TermStructure.Differentiable;
			if (domain.IntRestricted)
			{
				_structure |= TermStructure.Integer;
			}
			if (domain is BooleanDomain)
			{
				_structure |= TermStructure.LinearConstraint | TermStructure.DifferentiableConstraint;
			}
			_defaultValue = Rational.Indeterminate;
		}

		/// <summary>
		/// Shallow copy constructor
		/// </summary>
		private Parameter(string baseName, Parameter source)
		{
			_owningModel = null;
			_addValueTableElement = source._addValueTableElement;
			_count = source._count;
			_domain = source._domain;
			_indexSets = source._indexSets;
			_name = Term.BuildFullName(baseName, source._name);
			_refKey = source;
			_valueTable = source._valueTable;
			Binding = source.Binding;
			_structure = source._structure;
			_defaultValue = source._defaultValue;
		}

		/// <summary>Re-sets the ith index set (refreshing the cache).
		/// </summary>
		public void SetIndexSet(int i, Set set)
		{
			_indexSets[i] = set;
			_indexSetsCache = null;
		}

		/// <summary>Establish values for the Parameter by binding to its data source.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">Thrown if duplicate or out-of-range data is detected in the model.</exception>
		void IDataBindable.DataBind(SolverContext context)
		{
			if ((object)_refKey != null && _refKey._valueTable == null && _refKey.Binding != null)
			{
				((IDataBindable)_refKey).DataBind(context);
			}
			if ((object)_refKey == null || _refKey.Binding != Binding)
			{
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
				ValueTable<double> valueTable = ValueTable<double>.Create(_domain, array);
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
				_valueTable = valueTable;
				context.TraceSource.TraceInformation("{0} values read", num);
			}
			else
			{
				_addValueTableElement = _refKey._addValueTableElement;
				_valueTable = _refKey._valueTable;
			}
		}

		void IDataBindable.PropagateValues(SolverContext context)
		{
		}

		bool IIndexable.TryEvaluateConstantValue(object[] inputValues, out Rational value, EvaluationContext context)
		{
			if (_owningModel._level != 0)
			{
				return TryEvaluateSubmodelInstanceConstantValue(inputValues, out value, context);
			}
			if (!ValueTable.TryGetValue(out var value2, inputValues))
			{
				if (_defaultValue.IsIndeterminate)
				{
					throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.MissingParameterValue, new object[2]
					{
						_name,
						Statics.DebugCommaDelimitedList(inputValues)
					}));
				}
				value = _defaultValue;
			}
			else
			{
				value = value2;
			}
			return true;
		}

		internal override Term Clone(string baseName)
		{
			return new Parameter(baseName, this);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueField">The name of the property of each input object which contains the value of the data element.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public void SetBinding<T>(IEnumerable<T> binding, string valueField)
		{
			SetBinding(binding, valueField, new string[0]);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueField">The name of the property of each input object which contains the value of the data element.</param>
		/// <param name="defaultValue">Used as a default value for indexes that are not provided by the binding.</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data elements, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public void SetBinding<T>(IEnumerable<T> binding, string valueField, Rational defaultValue, params string[] indexFields)
		{
			if (defaultValue.IsIndeterminate)
			{
				throw new ArgumentOutOfRangeException("defaultValue", Resources.DefaultValueShouldBeDefiniteNumber);
			}
			SetBindingCore(binding, valueField, defaultValue, indexFields);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueField">The name of the property of each input object which contains the value of the data element.</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data elements, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public void SetBinding<T>(IEnumerable<T> binding, string valueField, params string[] indexFields)
		{
			SetBindingCore(binding, valueField, Rational.Indeterminate, indexFields);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueField">The name of the property of each input object which contains the value of the data element.</param>
		/// <param name="defaultValue">Used as a default value for data which can't found in binding sequence.</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data elements, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		private void SetBindingCore<T>(IEnumerable<T> binding, string valueField, Rational defaultValue, params string[] indexFields)
		{
			if (binding == null)
			{
				throw new ArgumentNullException("binding");
			}
			if (valueField == null)
			{
				throw new ArgumentNullException("valueField");
			}
			if (indexFields == null)
			{
				throw new ArgumentNullException("indexFields");
			}
			if (_owningModel != null)
			{
				_owningModel.VerifyModelNotFrozen();
			}
			if (indexFields.Length != _indexSets.Length)
			{
				throw new ArgumentOutOfRangeException("indexFields", Resources.TheNumberOfIndexPropertiesMustMatchTheNumberOfIndexSets);
			}
			_defaultValue = defaultValue;
			Type intermediateValueFieldType = DataBindingSupport.GetIntermediateValueFieldType(binding, valueField, _domain);
			Func<T, Rational> valueFieldGetter = DataBindingSupport.MakeAccessorDelegate<T, Rational>(valueField, _domain, intermediateValueFieldType);
			Func<T, object>[] array = new Func<T, object>[indexFields.Length];
			for (int i = 0; i < indexFields.Length; i++)
			{
				array[i] = DataBindingSupport.MakeAccessorDelegate<T, object>(indexFields[i], _indexSets[i]._domain);
			}
			SetBindingCore2(binding, valueFieldGetter, array);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueGetter">A function to select the value from each data element.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public void SetBinding<T>(IEnumerable<T> binding, Func<T, Rational> valueGetter)
		{
			SetBinding(binding, valueGetter, new Func<T, object>[0]);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueGetter">A function to select the value from each data element.</param>
		/// <param name="defaultValue">Used as a default value for indexes that are not provided by the binding.</param>
		/// <param name="indexGetter">Functions to select the indexes for each data element, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public void SetBinding<T>(IEnumerable<T> binding, Func<T, Rational> valueGetter, Rational defaultValue, params Func<T, object>[] indexGetter)
		{
			if (defaultValue.IsIndeterminate)
			{
				throw new ArgumentOutOfRangeException("defaultValue", Resources.DefaultValueShouldBeDefiniteNumber);
			}
			SetBindingCore(binding, valueGetter, defaultValue, indexGetter);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueGetter">A function to select the value from each data element.</param>
		/// <param name="indexGetter">Functions to select the indexes for each data element, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public void SetBinding<T>(IEnumerable<T> binding, Func<T, Rational> valueGetter, params Func<T, object>[] indexGetter)
		{
			SetBindingCore(binding, valueGetter, Rational.Indeterminate, indexGetter);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueGetter">A function to select the value from each data element.</param>
		/// <param name="defaultValue">Used as a default value for data which can't found in binding sequence.</param>
		/// <param name="indexGetter">Functions to select the indexes for each data element, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		private void SetBindingCore<T>(IEnumerable<T> binding, Func<T, Rational> valueGetter, Rational defaultValue, params Func<T, object>[] indexGetter)
		{
			if (binding == null)
			{
				throw new ArgumentNullException("binding");
			}
			if (valueGetter == null)
			{
				throw new ArgumentNullException("valueField");
			}
			if (indexGetter == null)
			{
				throw new ArgumentNullException("indexFields");
			}
			if (_owningModel != null)
			{
				_owningModel.VerifyModelNotFrozen();
			}
			if (indexGetter.Length != _indexSets.Length)
			{
				throw new ArgumentOutOfRangeException("indexFields", Resources.TheNumberOfIndexPropertiesMustMatchTheNumberOfIndexSets);
			}
			_defaultValue = defaultValue;
			Func<T, object>[] indexFieldGetters = WrapEnumGetters(indexGetter);
			SetBindingCore2(binding, valueGetter, indexFieldGetters);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueGetter">A function to select the value from each data element.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public void SetBinding<T>(IEnumerable<T> binding, Func<T, string> valueGetter)
		{
			SetBinding(binding, valueGetter, new Func<T, object>[0]);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueGetter">A function to select the value from each data element.</param>
		/// <param name="defaultValue">Used as a default value for indexes that are not provided by the binding.</param>
		/// <param name="indexGetter">Functions to select the indexes for each data element, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public void SetBinding<T>(IEnumerable<T> binding, Func<T, string> valueGetter, string defaultValue, params Func<T, object>[] indexGetter)
		{
			if (defaultValue == null)
			{
				throw new ArgumentNullException("defaultValue");
			}
			SetBindingCore(binding, valueGetter, defaultValue, indexGetter);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueGetter">A function to select the value from each data element.</param>
		/// <param name="indexGetter">Functions to select the indexes for each data element, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public void SetBinding<T>(IEnumerable<T> binding, Func<T, string> valueGetter, params Func<T, object>[] indexGetter)
		{
			SetBindingCore(binding, valueGetter, null, indexGetter);
		}

		/// <summary>
		/// Binds the parameter to data. Each parameter must be bound before solving. The data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. 
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueGetter">A function to select the value from each data element.</param>
		/// <param name="defaultValue">Used as a default value for data which can't found in binding sequence.</param>
		/// <param name="indexGetter">Functions to select the indexes for each data element, one for
		/// each index set which was provided when the Parameter was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		private void SetBindingCore<T>(IEnumerable<T> binding, Func<T, string> valueGetter, string defaultValue, params Func<T, object>[] indexGetter)
		{
			if (binding == null)
			{
				throw new ArgumentNullException("binding");
			}
			if (valueGetter == null)
			{
				throw new ArgumentNullException("valueField");
			}
			if (indexGetter == null)
			{
				throw new ArgumentNullException("indexFields");
			}
			if (_owningModel != null)
			{
				_owningModel.VerifyModelNotFrozen();
			}
			if (indexGetter.Length != _indexSets.Length)
			{
				throw new ArgumentOutOfRangeException("indexFields", Resources.TheNumberOfIndexPropertiesMustMatchTheNumberOfIndexSets);
			}
			if (_domain.ValueClass != TermValueClass.Enumerated)
			{
				throw new NotSupportedException(Resources.OnlyEnumeratedDomainsSupportStringBinding);
			}
			if (defaultValue != null)
			{
				_defaultValue = _domain.GetOrdinal(defaultValue);
			}
			else
			{
				_defaultValue = Rational.Indeterminate;
			}
			Func<T, Rational> valueFieldGetter = (T t) => _domain.GetOrdinal(valueGetter(t));
			Func<T, object>[] indexFieldGetters = WrapEnumGetters(indexGetter);
			SetBindingCore2(binding, valueFieldGetter, indexFieldGetters);
		}

		private Func<T, object>[] WrapEnumGetters<T>(Func<T, object>[] indexGetter)
		{
			Func<T, object>[] array = new Func<T, object>[indexGetter.Length];
			Array.Copy(indexGetter, array, indexGetter.Length);
			for (int i = 0; i < _indexSets.Length; i++)
			{
				if (_indexSets[i]._domain.ValueClass == TermValueClass.Enumerated)
				{
					Domain capturedDomain = _indexSets[i]._domain;
					Func<T, object> getter = indexGetter[i];
					array[i] = (T o) => capturedDomain.GetOrdinal((string)getter(o));
				}
			}
			return array;
		}

		private void SetBindingCore2<T>(IEnumerable<T> binding, Func<T, Rational> valueFieldGetter, Func<T, object>[] indexFieldGetters)
		{
			_addValueTableElement = delegate(object obj, ValueTable<double> table)
			{
				object[] array = new object[indexFieldGetters.Length];
				for (int i = 0; i < indexFieldGetters.Length; i++)
				{
					array[i] = indexFieldGetters[i]((T)obj);
				}
				Rational rational = valueFieldGetter((T)obj);
				if (!_domain.IsValidValue(rational))
				{
					throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.TheValue0IsNotAnAllowable1Value, new object[2] { rational, _domain }));
				}
				try
				{
					table.Add((double)rational, array);
				}
				catch (ArgumentException innerException)
				{
					throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.DuplicateEntriesDetectedInDataBoundTo0, new object[1] { _name }), innerException);
				}
				catch (InvalidOperationException ex)
				{
					if (ex.Data["index"] is int num)
					{
						throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.DataBindingFailedForParameter012, new object[3]
						{
							Name,
							_indexSets[num].Name,
							array[num]
						}), ex);
					}
					throw;
				}
			};
			Binding = binding;
		}

		private bool TryEvaluateSubmodelInstanceConstantValue(object[] inputValues, out Rational value, EvaluationContext context)
		{
			SubmodelInstance submodelInstance = SubmodelInstance.FollowPath(context);
			if (submodelInstance != null && submodelInstance.TryGetParameter(this, out var val))
			{
				if (inputValues == null)
				{
					return val.TryEvaluateConstantValue(out value, context);
				}
				return ((IIndexable)val).TryEvaluateConstantValue(inputValues, out value, context);
			}
			throw new InvalidTermException(Resources.InternalError);
		}

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
			if (_valueTable == null)
			{
				value = 0;
				return false;
			}
			double value2;
			bool result = _valueTable.TryGetValue(out value2);
			value = value2;
			return result;
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return _name;
		}
	}
}
