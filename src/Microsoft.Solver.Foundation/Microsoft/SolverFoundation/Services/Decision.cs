#define TRACE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary> A group of decision variables for which a solver finds values.
	/// </summary>
	/// <remarks>
	/// Decisions are output variables determined by a solver. All decisions have a Domain
	/// which determine the range of acceptable values.
	/// A decision may be single-valued (scalar), or multi-valued (a table). To create a 
	/// single-valued decision, pass in a zero-length indexSets array. If indexSets has nonzero 
	/// length, each element represents a set of values over which the decision is indexed. For 
	/// example, if there are two index sets, then this decision takes two indexes, one from the 
	/// first set and one from the second set. The total number of decisions is the product of the
	/// sizes of all the index sets.
	/// </remarks>
	[DebuggerDisplay("{_name}")]
	public sealed class Decision : Term, IVariable, IDataBindable, IIndexable
	{
		private class IndexValueComparer : IComparer<object>
		{
			public int Compare(object x, object y)
			{
				string text = x as string;
				string text2 = y as string;
				if (text != null && text2 == null)
				{
					return 1;
				}
				if (text2 != null && text == null)
				{
					return -1;
				}
				return Comparer.Default.Compare(x, y);
			}
		}

		internal readonly Set[] _indexSets;

		internal Domain _domain;

		internal ValueTable<Rational> _hintValues;

		private ReadOnlyCollection<Set> _indexSetsCache;

		internal string _name;

		internal Decision _refKey;

		private Action<object, ValueTable<double>> _updateObjectValue;

		internal ValueTable<double> _valueTable;

		internal int _id = -1;

		private IEnumerable _binding;

		private string _description;

		private static readonly ReadOnlyCollection<Set> EmptyIndexSets = new ReadOnlyCollection<Set>(new List<Set>());

		private static readonly Set[] EmptySets = new Set[0];

		internal override bool IsModelIndependentTerm => false;

		internal override TermType TermType => TermType.Decision;

		/// <summary>
		/// The index sets passed in when this object was created.
		/// </summary>
		public ReadOnlyCollection<Set> IndexSets
		{
			get
			{
				if (_indexSets.Length == 0)
				{
					return EmptyIndexSets;
				}
				if (_indexSetsCache == null)
				{
					_indexSetsCache = new ReadOnlyCollection<Set>(new List<Set>(_indexSets));
				}
				return _indexSetsCache;
			}
		}

		/// <summary>
		/// A LINQ binding to a database column to write the values to
		/// </summary>
		public IEnumerable Binding
		{
			get
			{
				return _binding;
			}
			private set
			{
				_binding = value;
			}
		}

		/// <summary>
		/// Term indexed by one or more indexes 
		/// </summary>
		/// <param name="indexes">The indexes for the particular term</param>
		/// <returns>A Term that represents the indexed Decision.</returns>
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
		public string Description
		{
			get
			{
				return _description;
			}
			set
			{
				_description = value;
			}
		}

		TermValueClass IIndexable.ValueClass => ValueClass;

		TermValueClass IIndexable.DomainValueClass => _domain.ValueClass;

		/// <summary>
		/// Create a new decision. The decision may be a single value (scalar), or multiple values (a table).
		/// To create a single-value decision, pass in a zero-length indexSets array.
		/// If indexSets has nonzero length, each element of it represents a set of values which this decision
		/// is indexed by. For example, if there are two index sets, then this decision takes two indexes, one from the
		/// first set and one from the second set.
		/// The total number of decisions is the product of the sizes of all the index sets.
		/// </summary>
		/// <param name="domain">The set of values each element of the decision can take, such as Model.Real</param>
		/// <param name="name">A name for the decision. The name must be unique. If the value is null, a unique name will be generated.</param>
		public Decision(Domain domain, string name)
			: this(domain, name, EmptySets)
		{
		}

		/// <summary>
		/// Create a new decision. The decision may be a single value (scalar), or multiple values (a table).
		/// To create a single-value decision, pass in a zero-length indexSets array.
		/// If indexSets has nonzero length, each element of it represents a set of values which this decision
		/// is indexed by. For example, if there are two index sets, then this decision takes two indexes, one from the
		/// first set and one from the second set.
		/// The total number of decisions is the product of the sizes of all the index sets.
		/// </summary>
		/// <param name="domain">The set of values each element of the decision can take, such as Model.Real</param>
		/// <param name="name">A name for the decision. The name must be unique. If the value is null, a unique name will be generated.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar decision.</param>
		public Decision(Domain domain, string name, params Set[] indexSets)
		{
			if (domain == null)
			{
				throw new ArgumentNullException("domain");
			}
			if (indexSets == null)
			{
				throw new ArgumentNullException("indexSets");
			}
			_domain = domain;
			_name = name;
			_indexSets = indexSets;
			if (_name == null)
			{
				_name = "decision_" + Model.UniqueSuffix();
			}
			_structure = TermStructure.Linear | TermStructure.Quadratic | TermStructure.Differentiable;
			if (domain.IntRestricted)
			{
				_structure |= TermStructure.Integer;
			}
		}

		/// <summary>
		/// A shallow copy constructor
		/// </summary>
		private Decision(string baseName, Decision source)
		{
			_owningModel = null;
			_domain = source._domain;
			_indexSets = source._indexSets;
			_name = Term.BuildFullName(baseName, source._name);
			_refKey = source;
			_valueTable = source._valueTable;
			_updateObjectValue = source._updateObjectValue;
			Binding = source.Binding;
			_structure = source.Structure;
		}

		/// <summary>Re-sets the ith index set (refreshing the cache).
		/// </summary>
		public void SetIndexSet(int i, Set set)
		{
			_indexSets[i] = set;
			_indexSetsCache = null;
		}

		void IDataBindable.DataBind(SolverContext context)
		{
			ClearData();
		}

		void IDataBindable.PropagateValues(SolverContext context)
		{
			context.TraceSource.TraceInformation("Propagating data for {0}", Name);
			int num = 0;
			if (Binding != null)
			{
				foreach (object item in Binding)
				{
					_updateObjectValue(item, _valueTable);
					num++;
				}
			}
			context.TraceSource.TraceInformation("{0} values written", num);
		}

		bool IIndexable.TryEvaluateConstantValue(object[] inputValues, out Rational value, EvaluationContext context)
		{
			value = 0;
			return false;
		}

		void IVariable.SetValue(Rational value, object[] indexes)
		{
			_valueTable.Add((double)value, indexes);
		}

		internal override Term Clone(string baseName)
		{
			return new Decision(baseName, this);
		}

		/// <summary>
		/// Binds the decision to data. A decision does not need to be bound to data. If it is, the data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. The sequence must contain one object
		/// for each decision.
		///
		/// The data is written when Context.PropagateDecisions is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueField">The name of the property of each input object which will be assigned the value of the data decisions.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">The property or field is not writable.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string valueField)
		{
			SetBinding(binding, valueField, new string[0]);
		}

		/// <summary>
		/// Binds the decision to data. A decision does not need to be bound to data. If it is, the data must be in the form of a sequence of
		/// objects, where each object has properties for the value and index(s) of the data element. The sequence must contain one object
		/// for each decision.
		///
		/// The data is written when Context.PropagateDecisions is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="valueField">The name of the property of each input object which will be assigned the value of the data decisions.</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data decisions, one for
		/// each index set which was provided when the Decision was created.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">The property or field is not writable.</exception>
		public void SetBinding<T>(IEnumerable<T> binding, string valueField, params string[] indexFields)
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
			if (binding is ITable table2 && table2.IsReadOnly)
			{
				throw new InvalidModelDataException(Resources.CannotBindReadOnlyDataTableForOutput);
			}
			Type typeFromHandle = typeof(T);
			PropertyInfo valueFieldProperty = typeFromHandle.GetProperty(valueField);
			FieldInfo valueFieldField = typeFromHandle.GetField(valueField);
			PropertyInfo defaultMemberProperty = DataBindingSupport.GetDefaultMemberProperty(typeFromHandle);
			if (valueFieldProperty == null && valueFieldField == null)
			{
				throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.ThePropertyOrField0WasNotFound, new object[1] { valueField }));
			}
			Func<T, object>[] indexFieldGetters = new Func<T, object>[indexFields.Length];
			for (int i = 0; i < indexFields.Length; i++)
			{
				indexFieldGetters[i] = DataBindingSupport.MakeAccessorDelegate<T, object>(indexFields[i], _indexSets[i]._domain);
			}
			Func<double, object> valueConverter = DataBindingSupport.MakeConversionDelegate<double, object>(_domain);
			_updateObjectValue = delegate(object obj, ValueTable<double> table)
			{
				object[] array = new object[indexFieldGetters.Length];
				for (int j = 0; j < indexFieldGetters.Length; j++)
				{
					array[j] = indexFieldGetters[j]((T)obj);
				}
				if (!table.TryGetValue(out var value, array))
				{
					throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, "The object '{0}' could not be assigned a value from {1} because an index was out of range.", new object[2] { obj, _name }));
				}
				object obj2 = valueConverter(value);
				if (valueFieldProperty != null)
				{
					valueFieldProperty.SetValue(obj, obj2, new object[0]);
				}
				else if (valueFieldField != null)
				{
					valueFieldField.SetValue(obj, obj2);
				}
				else
				{
					defaultMemberProperty.GetSetMethod().Invoke(obj, new object[2] { valueField, obj2 });
				}
			};
			Binding = binding;
		}

		/// <summary>Formats the value of the current instance using the specified format.
		/// </summary>
		/// <param name="format">The format to use (or null).</param>
		/// <param name="formatProvider">The provider to use to format the value (or null).</param>
		/// <returns>The value of the current instance in the specified format.</returns>
		/// <remarks>
		/// The "n" format causes the decision names to be printed rather than values.
		/// </remarks>
		public override string ToString(string format, IFormatProvider formatProvider)
		{
			if (_valueTable != null && _valueTable.IndexCount == 0)
			{
				Rational value = GetValue();
				if (!(format == "n"))
				{
					return StringValue(value);
				}
				return _name;
			}
			if (_valueTable != null)
			{
				if (format == "n")
				{
					string text;
					if (_indexSets == null)
					{
						text = "";
					}
					else
					{
						string[] value2 = _indexSets.Select((Set s) => s.Name).ToArray();
						text = string.Join(", ", value2);
					}
					return _name + "[" + text + "]";
				}
				StringBuilder stringBuilder = new StringBuilder();
				bool flag = true;
				foreach (object[] key in _valueTable.Keys)
				{
					if (!flag)
					{
						stringBuilder.Append(", ");
					}
					flag = false;
					stringBuilder.Append("(");
					bool flag2 = true;
					object[] array = key;
					foreach (object value3 in array)
					{
						if (!flag2)
						{
							stringBuilder.Append(",");
						}
						flag2 = false;
						stringBuilder.Append(value3);
					}
					stringBuilder.Append("): ");
					_valueTable.TryGetValue(out var value4, key);
					stringBuilder.Append(value4);
				}
				return stringBuilder.ToString();
			}
			if (Name != null)
			{
				return Name;
			}
			return base.ToString();
		}

		/// <summary> Returns a string that represents the current Decision.
		/// </summary>
		/// <param name="formatProvider">The provider to use to format the value (or null).</param>
		/// <returns>The value of the current instance in the specified format.</returns>
		public string ToString(IFormatProvider formatProvider)
		{
			return ToString(null, formatProvider);
		}

		/// <summary>Returns a string that represents the current Decision.
		/// </summary>
		/// <returns>The value of the current instance in the specified format.</returns>
		public override string ToString()
		{
			return ToString(null, CultureInfo.CurrentCulture);
		}

		/// <summary>Unlike the public ToString() this one uses the decision name and not its value
		/// </summary>
		internal string ToString(object[] indexes)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(Name);
			stringBuilder.Append(Statics.JoinArrayToString(indexes));
			return stringBuilder.ToString();
		}

		/// <summary>
		/// Convert the (single) value to a double-precision floating-point number
		/// </summary>
		/// <returns>The value of the decision as double-precision floating-point</returns>
		/// <exception cref="T:System.InvalidCastException">Thrown if the decision has no value or multiple values.</exception>
		/// <exception cref="T:System.InvalidCastException">Thrown if the value is not numeric.</exception>
		public double ToDouble()
		{
			PreConvCheck();
			try
			{
				Rational value = GetValue();
				return DoubleValue(value);
			}
			catch (KeyNotFoundException innerException)
			{
				throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture, Resources.HasMultipleValues, new object[1] { _name }), innerException);
			}
		}

		private void PreConvCheck()
		{
			if (_valueTable == null)
			{
				throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture, Resources.DoesNotYetHaveAValue, new object[1] { _name }));
			}
		}

		/// <summary>
		/// Convert the (indexed) value to a string
		/// </summary>
		/// <param name="indexes">The indexes of the value to get.</param>
		/// <returns>The value of the decision as string</returns>
		/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">Thrown if there is no value for the given indexes.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">Thrown if the number of indexes given does not match the number expected.</exception>
		public string GetString(params object[] indexes)
		{
			Rational value = GetValue(indexes);
			return StringValue(value);
		}

		/// <summary>
		/// Convert the (indexed) value to a double-precision floating-point number
		/// </summary>
		/// <param name="indexes">The indexes of the value to get.</param>
		/// <returns>The value of the decision as double-precision floating-point</returns>
		/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">Thrown if there is no value for the given indexes.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">Thrown if the number of indexes given does not match the number expected.</exception>
		/// <exception cref="T:System.InvalidCastException">Thrown if the value is not numeric.</exception>
		public double GetDouble(params object[] indexes)
		{
			Rational value = GetValue(indexes);
			return DoubleValue(value);
		}

		/// <summary>
		/// Gets a value from the underlying ValueTable
		/// </summary>
		/// <param name="indexes">The indexes of the value to get. They do not need to be translated to canonical form.</param>
		/// <returns>The Rational value.</returns>
		/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">Thrown if there is no value for the given indexes.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">Thrown if the number of indexes given does not match the number expected.</exception>
		private Rational GetValue(params object[] indexes)
		{
			if (_valueTable == null)
			{
				throw new KeyNotFoundException();
			}
			if (indexes.Length != _valueTable.IndexCount)
			{
				throw new ArgumentOutOfRangeException("indexes");
			}
			if (!_valueTable.TryGetValue(out var value, indexes))
			{
				return _domain.MinValue;
			}
			return value;
		}

		/// <summary>
		/// Provides a hint value which is close to the optimal value of the decision. Some solvers may
		/// be able to use the hint to find solutions faster.
		/// </summary>
		/// <param name="value">The initial value</param>
		/// <param name="indexes">The indexes of the value to set</param>
		public void SetInitialValue(Rational value, params object[] indexes)
		{
			if (indexes.Length != _indexSets.Length)
			{
				throw new ArgumentOutOfRangeException("indexes");
			}
			if (_hintValues == null)
			{
				ValueSet[] array = new ValueSet[_indexSets.Length];
				for (int i = 0; i < _indexSets.Length; i++)
				{
					array[i] = _indexSets[i].ValueSet;
				}
				_hintValues = ValueTable<Rational>.Create(null, array);
			}
			_hintValues.Set(value, indexes);
		}

		/// <summary>
		/// Returns a sequence of (value, indexes) elements for this decision. Each element is an array
		/// where the first element is the result value, and the remaining elements are the index values.
		/// </summary>
		/// <returns>A sequence of (value, indexes) elements for this decision</returns>
		public IEnumerable<object[]> GetValues()
		{
			int[] curIndexes = new int[_indexSets.Length];
			object[][] indexSetValues = new object[_indexSets.Length][];
			IComparer<object> objectComparer = new IndexValueComparer();
			for (int i = 0; i < _indexSets.Length; i++)
			{
				indexSetValues[i] = _indexSets[i].ValueSet._set.OrderBy((object x) => x, objectComparer).ToArray();
			}
			object[] indexes = new object[_indexSets.Length];
			while (true)
			{
				object[] result = new object[_indexSets.Length + 1];
				for (int j = 0; j < _indexSets.Length; j++)
				{
					if (indexSetValues[j].Length != 0)
					{
						result[j + 1] = (indexes[j] = indexSetValues[j][curIndexes[j]]);
						continue;
					}
					yield break;
				}
				if (_valueTable.TryGetValue(out var value, indexes))
				{
					if (_domain.ValueClass == TermValueClass.Enumerated)
					{
						result[0] = _domain.EnumeratedNames[(int)value];
					}
					else if (_domain.IsBoolean)
					{
						result[0] = ((value >= 0.5) ? true : false);
					}
					else
					{
						result[0] = value;
					}
					yield return result;
				}
				if (_indexSets.Length == 0)
				{
					break;
				}
				int num = _indexSets.Length - 1;
				while (num >= 0)
				{
					curIndexes[num]++;
					if (curIndexes[num] < indexSetValues[num].Length)
					{
						break;
					}
					curIndexes[num] = 0;
					if (num > 0)
					{
						num--;
						continue;
					}
					yield break;
				}
			}
		}

		/// <summary>
		/// Converts the internal value to a boolean, as appropriate for the domain.
		/// </summary>
		/// <param name="value"></param>
		/// <returns>The boolean result.</returns>
		/// <exception cref="T:System.InvalidCastException">Thrown if the value is not boolean.</exception>
		private bool BoolValue(Rational value)
		{
			if (_domain.ValueClass == TermValueClass.Numeric)
			{
				return value != 0.0;
			}
			throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture, Resources.DoesNotHaveANumericValue, new object[1] { _name }));
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="T:System.InvalidCastException">Thrown if the value is not numeric.</exception>
		private double DoubleValue(Rational value)
		{
			if (_domain.IsNumeric)
			{
				return (double)value;
			}
			throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture, Resources.DoesNotHaveANumericValue, new object[1] { _name }));
		}

		private string StringValue(Rational value)
		{
			if (_domain.ValueClass == TermValueClass.Numeric)
			{
				return value.ToString();
			}
			if (_domain.ValueClass == TermValueClass.Enumerated)
			{
				return _domain.EnumeratedNames[(int)value];
			}
			return value.ToString();
		}

		internal void ClearData()
		{
			ValueSet[] array = new ValueSet[_indexSets.Length];
			for (int i = 0; i < _indexSets.Length; i++)
			{
				array[i] = _indexSets[i].ValueSet;
			}
			_valueTable = ValueTable<double>.Create(null, array);
		}

		/// <summary>
		/// Creates a new DecisionBinding wrapping this. Only integer and enumerated decisions that are not indexed may be bound.
		/// </summary>
		/// <returns></returns>
		public DecisionBinding CreateBinding()
		{
			if (_owningModel == null)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.CannotCreateDecisionBindingOnDecisionNotAddedToAModel, new object[1] { Name }));
			}
			if (!_owningModel.IsRootModel)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.CannotCreateDecisionBindingOnDecisionNotAddedToRootModel, new object[1] { Name }));
			}
			return new DecisionBinding(this);
		}

		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			value = 0;
			return false;
		}

		internal override Result Visit<Result, Arg>(ITermVisitor<Result, Arg> visitor, Arg arg)
		{
			return visitor.Visit(this, arg);
		}
	}
}
