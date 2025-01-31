using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A table of values indexed by some number of keys, which can be any type (strings, doubles, etc).
	/// This implementation supports a maximum of five keys.
	/// </summary>
	internal abstract class ValueTable<TValue>
	{
		internal Domain _domain;

		internal ValueSet[] _indexSets;

		public int IndexCount => _indexSets.Length;

		public abstract IEnumerable<object[]> Keys { get; }

		public abstract IEnumerable<TValue> Values { get; }

		protected ValueTable(Domain domain, ValueSet[] indexSets)
		{
			_indexSets = indexSets;
			_domain = domain;
		}

		/// <summary>
		/// Factory method to construct a value table with the specified types.
		/// </summary>
		///
		/// <param name="domain"></param>
		/// <param name="indexSets"></param>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.ModelException">Thrown if there are too many index sets.</exception>
		/// <exception cref="T:System.NotSupportedException">Thrown if the domain is not numeric.</exception>
		public static ValueTable<TValue> Create(Domain domain, params ValueSet[] indexSets)
		{
			if (indexSets.Length > 0 && domain != null && !domain.IsNumeric && domain != Domain.DistributedValue)
			{
				throw new NotSupportedException(Resources.ParametersWithNonNumericDataAreNotSupported);
			}
			switch (indexSets.Length)
			{
			case 0:
				return new ValueTable0<TValue>(domain, indexSets);
			case 1:
				return new ValueTable1<TValue>(domain, indexSets);
			case 2:
				return new ValueTable2<TValue>(domain, indexSets);
			case 3:
				return new ValueTable3<TValue>(domain, indexSets);
			case 4:
				return new ValueTable4<TValue>(domain, indexSets);
			case 5:
				return new ValueTable5<TValue>(domain, indexSets);
			default:
				return new ValueTableN<TValue>(domain, indexSets);
			}
		}

		private static object CanonicalizeObject(object index)
		{
			if (Domain.TryCastToDouble(index, out var dblValue))
			{
				return dblValue;
			}
			return index;
		}

		/// <summary>
		/// Add a value to the value table.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="indexes"></param>
		/// <exception cref="T:System.ArgumentException">Thrown if there is already a value for the specified indexes.</exception>
		/// <exception cref="T:Microsoft.SolverFoundation.Common.InvalidModelDataException">Thrown if the data or the indexes are out-of-range.</exception>
		public void Add(TValue value, params object[] indexes)
		{
			object[] canonicalIndexes = GetCanonicalIndexes(value, indexes);
			AddImpl(value, canonicalIndexes);
		}

		public void Set(TValue value, params object[] indexes)
		{
			object[] canonicalIndexes = GetCanonicalIndexes(value, indexes);
			SetImpl(value, canonicalIndexes);
		}

		private object[] GetCanonicalIndexes(TValue value, object[] indexes)
		{
			if (indexes.Length == 0)
			{
				return indexes;
			}
			object[] array = new object[indexes.Length];
			for (int i = 0; i < indexes.Length; i++)
			{
				array[i] = CanonicalizeObject(indexes[i]);
			}
			if (_domain != null && !_domain.IsValidValue(value))
			{
				throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.TheValue0IsNotAnAllowable1Value, new object[2] { value, _domain }));
			}
			for (int j = 0; j < indexes.Length; j++)
			{
				if (_domain != null && _indexSets[j] != null && !_indexSets[j].Domain.IsValidValue(array[j]))
				{
					throw new InvalidModelDataException(string.Format(CultureInfo.InvariantCulture, Resources.TheValue0IsNotAnAllowable1Value, new object[2]
					{
						array[j],
						_indexSets[j].Domain
					}));
				}
			}
			for (int k = 0; k < indexes.Length; k++)
			{
				if (_indexSets[k] != null && !_indexSets[k].Add(array[k]))
				{
					InvalidOperationException ex = new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidIndexForSet0, new object[1] { array[k] }));
					ex.Data["index"] = k;
					throw ex;
				}
			}
			return array;
		}

		protected abstract void AddImpl(TValue value, params object[] indexes);

		protected abstract void SetImpl(TValue value, params object[] indexes);

		/// <summary>
		/// Get a value from the value table. Retsurns false if the key is not found.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="indexes"></param>
		/// <returns></returns>
		public bool TryGetValue(out TValue value, params object[] indexes)
		{
			object[] array;
			if (indexes.Length > 0)
			{
				array = new object[indexes.Length];
				for (int i = 0; i < indexes.Length; i++)
				{
					array[i] = CanonicalizeObject(indexes[i]);
				}
			}
			else
			{
				array = indexes;
			}
			return TryGetValueImpl(out value, array);
		}

		protected abstract bool TryGetValueImpl(out TValue value, params object[] indexes);

		internal static object GetExprValue(Expression expr)
		{
			if (expr.GetValue(out string val))
			{
				return val;
			}
			if (expr.GetValue(out int val2))
			{
				return val2;
			}
			if (expr.GetValue(out Rational val3))
			{
				return val3;
			}
			if (expr.GetValue(out double val4))
			{
				return val4;
			}
			return null;
		}
	}
}
