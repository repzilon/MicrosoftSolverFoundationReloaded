using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A list of constant value tuples.
	/// </summary>
	public sealed class Tuples
	{
		private IEnumerable _binding;

		internal Domain[] _domains;

		private Func<object, Rational[]> _extractValues;

		private string _name;

		internal Model _owningModel;

		private Rational[][] _data;

		/// <summary>
		/// The name of the tuples
		/// </summary>
		public string Name => _name;

		/// <summary>
		/// A description.
		/// </summary>
		public string Description { get; set; }

		internal bool IsBound => Binding != null;

		internal Rational[][] Data => _data;

		internal IEnumerable Binding => _binding;

		internal Model OwningModel => _owningModel;

		internal bool NeedsBind => !IsConstant;

		internal Domain[] Domains => _domains;

		internal bool IsConstant => _data != null;

		/// <summary>
		/// This creates Tuples which can be used as a Random Parameter for Stochastic Programming or for 
		/// Table Constraints
		/// </summary>
		/// <param name="name">The name of the new instance.</param>
		/// <param name="domains">Domains of the Tuples.</param>
		public Tuples(string name, IEnumerable<Domain> domains)
			: this(name, domains, null, new Set[0])
		{
		}

		/// <summary>
		/// This creates Tuples which can be used as a Random Parameter for Stochastic Programming or for 
		/// Table Constraints
		/// </summary>
		/// <param name="name">Tuples' name</param>
		/// <param name="domains">Domains of the Tuples</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar decision</param>
		public Tuples(string name, IEnumerable<Domain> domains, params Set[] indexSets)
			: this(name, domains, null, indexSets)
		{
		}

		/// <summary>
		/// This creates Tuples which can be used as a Random Parameter for Stochastic Programming or for 
		/// Table Constraints
		/// </summary>
		/// <param name="name">Tuples' name</param>
		/// <param name="domains">Domains of the Tuples</param>
		/// <param name="data">The tuple data.</param>
		public Tuples(string name, IEnumerable<Domain> domains, IEnumerable<Rational[]> data)
			: this(name, domains, data, new Set[0])
		{
		}

		/// <summary>
		/// This creates Tuples which can be used as a Random Parameter for Stochastic Programming or for 
		/// Table Constraints
		/// </summary>
		/// <param name="name">Tuples' name</param>
		/// <param name="domains">Domains of the Tuples</param>
		/// <param name="data">The data.</param>
		/// <param name="indexSets">The index sets to use. Omit to create a scalar decision</param>
		private Tuples(string name, IEnumerable<Domain> domains, IEnumerable<Rational[]> data, params Set[] indexSets)
		{
			if (indexSets == null)
			{
				throw new ArgumentNullException("indexSets");
			}
			if (domains == null)
			{
				throw new ArgumentNullException("domains");
			}
			foreach (Domain domain in domains)
			{
				if (domain == null)
				{
					throw new ArgumentNullException("domains");
				}
			}
			if (domains.Count() == 0)
			{
				throw new ArgumentNullException("domains");
			}
			if (indexSets.Length != 0)
			{
				throw new NotSupportedException(Resources.CurrentlyOnlyTuplesWhichIsUsedAsARandomParameterSupportsIndexing);
			}
			if (data != null)
			{
				_data = data.ToArray();
			}
			if (name == null)
			{
				_name = "tuple" + Model.UniqueSuffix();
			}
			else
			{
				_name = name;
			}
			_domains = domains.ToArray();
		}

		/// <summary>Binds Tuples to a data source.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each data element.</param>
		/// <param name="fieldNames">The names of the properties of each input object which contain the values. One for each domain</param>
		/// <param name="indexFields">The names of the properties of each input object which contain the indexes of the data elements, one for
		/// each index set which was provided when the Tuples was created.</param>
		public void SetBinding<T>(IEnumerable<T> binding, string[] fieldNames, params string[] indexFields)
		{
			if (binding == null)
			{
				throw new ArgumentNullException("binding");
			}
			if (fieldNames == null)
			{
				throw new ArgumentNullException("fieldNames");
			}
			if (indexFields.Length != 0)
			{
				throw new NotSupportedException(Resources.CurrentlyOnlyTuplesWhichIsUsedAsARandomParameterSupportsIndexing);
			}
			if (IsConstant)
			{
				return;
			}
			if (_owningModel != null)
			{
				_owningModel.VerifyModelNotFrozen();
			}
			if (fieldNames.Length != _domains.Length)
			{
				throw new ArgumentOutOfRangeException("fieldNames", Resources.TheNumberOfValuePropertiesMustMatchTheNumberOfElementsInTheTuple);
			}
			_binding = binding;
			Func<T, Rational>[] fieldGetters = new Func<T, Rational>[_domains.Length];
			for (int i = 0; i < _domains.Length; i++)
			{
				fieldGetters[i] = DataBindingSupport.MakeAccessorDelegate<T, Rational>(fieldNames[i], _domains[i]);
			}
			_extractValues = delegate(object value)
			{
				Rational[] array = new Rational[_domains.Length];
				for (int j = 0; j < _domains.Length; j++)
				{
					ref Rational reference = ref array[j];
					reference = fieldGetters[j]((T)value);
					if (!_domains[j].IsValidValue(array[j]))
					{
						throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.TupleDataDoesNotBelongToDomainSpecifiedIn01, new object[2]
						{
							array[j],
							_domains[j]
						}));
					}
				}
				return array;
			};
		}

		/// <summary>
		/// Binds a Tuples to data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="binding"></param>
		/// <param name="fieldNames"></param>
		public void SetBinding<T>(IEnumerable<T> binding, string[] fieldNames)
		{
			SetBinding(binding, fieldNames, new string[0]);
		}

		internal IEnumerable<int[]> ExpandValuesInt()
		{
			IEnumerable<Rational[]> valuesFromData = ((!IsConstant) ? (from object value in _binding
				select _extractValues(value)) : _data.Select((Rational[] valuesRational) => valuesRational));
			foreach (Rational[] valuesRational2 in valuesFromData)
			{
				int[] result = new int[valuesRational2.Length];
				for (int i = 0; i < valuesRational2.Length; i++)
				{
					if (!valuesRational2[i].IsInteger())
					{
						throw new InvalidTermException(Resources.ValueInElementOfIsNotAnInteger);
					}
					result[i] = (int)valuesRational2[i];
				}
				yield return result;
			}
		}
	}
}
