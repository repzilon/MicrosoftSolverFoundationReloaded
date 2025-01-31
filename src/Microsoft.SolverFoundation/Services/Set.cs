using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>A Set represents a set of values within a Domain.
	/// </summary>
	/// <remarks>
	/// A Set is an unordered collection of items that is used to create indexed Parameter
	/// or Decision objects. Sets are also used in Foreach and FilteredForeach expressions.
	/// The items in a Set are established by data binding the Parameters and Decisions in which
	/// the Set appears.
	/// </remarks>
	public sealed class Set : IDataBindable
	{
		/// <summary>
		/// A ValueSet representing the actual values. Constructed during data binding, and thrown out after the solver has been hydrated.
		/// </summary>
		private ValueSet _valueSet;

		/// <summary>
		/// Adds a data element to the internal ValueTable.
		/// </summary>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">Thrown if duplicate or out-of-range data is detected in the model.</exception>
		private Action<object, ValueSet> _addValueTableElement;

		/// <summary>
		/// The domain which all the values of this set belong to.
		/// </summary>
		internal readonly Domain _domain;

		/// <summary>
		/// The name of the Set. Provided by the user. Not necessarily unique.
		/// </summary>
		private readonly string _name;

		private Term[] _fixedRange;

		internal Term[] _fixedValues;

		/// <summary>The binding for this Set (optional).
		/// </summary>
		internal IEnumerable Binding { get; private set; }

		/// <summary>The limiting element in a fixed range Set (if any).
		/// </summary>
		/// <remarks>
		/// This property is null if this Set is not defined by a range.
		/// This element does not actually belong to the set.</remarks>
		internal Term FixedLimit
		{
			get
			{
				if (_fixedRange == null)
				{
					return null;
				}
				return _fixedRange[1];
			}
		}

		/// <summary>The first element in a fixed range Set (if any).
		/// </summary>
		/// <remarks>
		/// This property is null if this Set is not defined by a range.
		/// </remarks>
		internal Term FixedStart
		{
			get
			{
				if (_fixedRange == null)
				{
					return null;
				}
				return _fixedRange[0];
			}
		}

		/// <summary>The step in a fixed range Set (if any).
		/// </summary>
		/// <remarks>
		/// This property is null if this Set is not defined by a range.
		/// </remarks>
		internal Term FixedStep
		{
			get
			{
				if (_fixedRange == null)
				{
					return null;
				}
				return _fixedRange[2];
			}
		}

		/// <summary>Indicates whether the values for the Set were established upon creation.
		/// </summary>
		public bool IsConstant
		{
			get
			{
				if (_fixedValues == null)
				{
					return _fixedRange != null;
				}
				return true;
			}
		}

		/// <summary>Indicates whether additional values may be added to a Set, for example by data binding operations on Parameters.
		/// </summary>
		public bool IsLocked
		{
			get
			{
				if (_valueSet == null || !_valueSet.IsLocked)
				{
					return IsConstant;
				}
				return true;
			}
		}

		/// <summary>
		/// The name of the set.
		/// </summary>
		public string Name => _name;

		/// <summary>
		/// A ValueSet representing the actual values. Constructed during data binding, and thrown out after the solver has been hydrated.
		/// </summary>
		internal ValueSet ValueSet => _valueSet;

		/// <summary>
		/// The type of each element of the set.
		/// </summary>
		internal TermValueClass ItemValueClass => _domain.ValueClass;

		/// <summary>
		/// Create a new set
		/// </summary>
		/// <param name="domain">The Domain for values of this set.</param>
		/// <param name="name">The name of the set.</param>
		public Set(Domain domain, string name)
		{
			if (domain == null)
			{
				throw new ArgumentNullException("domain");
			}
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			_domain = domain;
			_name = name;
		}

		/// <summary>
		/// Create a set of numbers with a given start, limit, and step.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="limit"></param>
		/// <param name="step"></param>
		public Set(Term start, Term limit, Term step)
		{
			if ((object)start == null)
			{
				throw new ArgumentNullException("start");
			}
			if ((object)limit == null)
			{
				throw new ArgumentNullException("limit");
			}
			if ((object)step == null)
			{
				throw new ArgumentNullException("step");
			}
			_domain = Domain.Real;
			_name = string.Format(CultureInfo.InvariantCulture, "{{{0},{1},{2}}}", new object[3]
			{
				start.ToString(),
				limit.ToString(),
				step.ToString()
			});
			_fixedRange = new Term[3] { start, limit, step };
		}

		/// <summary>
		/// Create a set with fixed values.
		/// </summary>
		public Set(Term[] values, Domain domain, string name)
		{
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}
			_domain = domain;
			_name = name;
			_fixedValues = values;
		}

		/// <summary>
		/// Create a set with fixed values.
		/// </summary>
		public Set(Term[] values)
			: this(values, Domain.Real, "{...}")
		{
		}

		/// <summary>
		/// Create the ValueSet object at the beginning of data binding. The actual data is added as a side effect
		/// of Parameter.DataBind.
		/// </summary>
		void IDataBindable.DataBind(SolverContext context)
		{
			DataBindImpl();
		}

		void IDataBindable.PropagateValues(SolverContext context)
		{
		}

		internal void CreateFixedSet(Term.EvaluationContext evalContext)
		{
			if (IsConstant && Binding == null)
			{
				_valueSet = ValueSet.Create(_domain);
				if (_fixedValues != null)
				{
					Term[] fixedValues = _fixedValues;
					foreach (Term term in fixedValues)
					{
						if (!term.TryEvaluateConstantValue(out Rational value, evalContext))
						{
							throw new NotSupportedException(Resources.OnlyConstantValuesAreAllowedForTheIterationSetInForeachFilteredForeach);
						}
						_valueSet.Add((double)value);
					}
				}
				else
				{
					if (!FixedStart.TryEvaluateConstantValue(out Rational value2, evalContext))
					{
						throw new NotSupportedException(Resources.OnlyConstantValuesAreAllowedForTheIterationSetInForeachFilteredForeach);
					}
					if (!FixedStep.TryEvaluateConstantValue(out Rational value3, evalContext))
					{
						throw new NotSupportedException(Resources.OnlyConstantValuesAreAllowedForTheIterationSetInForeachFilteredForeach);
					}
					if (!FixedLimit.TryEvaluateConstantValue(out Rational value4, evalContext))
					{
						throw new NotSupportedException(Resources.OnlyConstantValuesAreAllowedForTheIterationSetInForeachFilteredForeach);
					}
					Rational rational = (value4 - value2) / value3;
					for (Rational rational2 = 0; rational2 < rational; rational2 += (Rational)1)
					{
						_valueSet.Add((double)(value2 + rational2 * value3));
					}
				}
				_valueSet.LockValues();
			}
			else
			{
				if (_valueSet != null && _valueSet._set != null && _valueSet._set.Count != 0)
				{
					return;
				}
				if (_valueSet == null || _valueSet._set == null)
				{
					_valueSet = ValueSet.Create(_domain);
				}
				if (!_domain.IntRestricted || _fixedValues != null)
				{
					return;
				}
				if (_domain.ValidValues != null)
				{
					Rational[] validValues = _domain.ValidValues;
					foreach (Rational rational3 in validValues)
					{
						_valueSet.Add((double)rational3);
					}
				}
				else if (_domain.MinValue.IsFinite && _domain.MaxValue.IsFinite)
				{
					for (Rational ceiling = _domain.MinValue.GetCeiling(); ceiling <= _domain.MaxValue.GetFloor(); ceiling += (Rational)1)
					{
						_valueSet.Add((double)ceiling);
					}
				}
			}
		}

		/// <summary>
		/// Binds the set to data. The data must be in the form of a sequence of
		/// objects, where each object has a property for the value for a set element.
		///
		/// The data is read each time Context.Solve() is called.
		/// </summary>
		/// <param name="binding">A sequence of objects, one for each set element.</param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">A property or field isn't found.</exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
		public void SetBinding<T>(IEnumerable<T> binding)
		{
			if (binding == null)
			{
				throw new ArgumentNullException("binding");
			}
			if (IsLocked)
			{
				throw new InvalidOperationException(Resources.SetBindingMayNotBeCalledOnASetWithFixedValues);
			}
			SetBindingCore(binding);
		}

		private void SetBindingCore<T>(IEnumerable<T> binding)
		{
			_addValueTableElement = delegate(object obj, ValueSet valueSet)
			{
				if (!_domain.IsValidValue(obj))
				{
					throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.TheValue0IsNotAnAllowable1Value, new object[2] { obj, _domain }));
				}
				try
				{
					if (Domain.TryCastToDouble(obj, out var dblValue))
					{
						valueSet.Add(dblValue);
					}
					else
					{
						valueSet.Add(obj);
					}
				}
				catch (ArgumentException innerException)
				{
					throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.DuplicateEntriesDetectedInDataBoundTo0, new object[1] { _name }), innerException);
				}
			};
			Binding = binding;
		}

		private void DataBindImpl()
		{
			_valueSet = ValueSet.Create(_domain);
			if (Binding == null)
			{
				return;
			}
			int num = 0;
			foreach (object item in Binding)
			{
				_addValueTableElement(item, _valueSet);
				num++;
			}
			if (num <= 0)
			{
				throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.MustHaveAtLeastOneValue, new object[1] { _name }));
			}
			_valueSet.LockValues();
		}

		/// <summary>
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return _name;
		}
	}
}
