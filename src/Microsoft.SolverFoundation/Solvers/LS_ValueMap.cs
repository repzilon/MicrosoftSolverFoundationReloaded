using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A specialized Mapping from keys of type LS_Value to an arbitrary
	///           IEquatable type. Will have best efficiency when the keys 
	///           are taken from a contiguous range, otherwise is equivalent to a
	///           dictionary.
	/// </summary>
	/// <remarks> The representation depends on whether the keys are contiguous. 
	///           If such is the case we use an array representation. If on
	///           the contrary the keys have vastly different values then indexing is
	///           not a reasonable option anymore and then we use a dictionary.
	///           In the current version the choice is made dynamically and we switch
	///           at some point from contiguous to general (dictionary) representation 
	///           if this ever needed, i.e. if some very different values appear.
	///           In the current version we do not switch back to a contiguous
	///           representation: once we use a dictionary this choice is not reversible. 
	///           An estimate of number of values to be stored, given by the user
	///           at construction time, is currently used to determine how large a
	///           different we allow between the values in a contiguous representation.
	/// </remarks>
	/// <remarks> Invariant: at most one of the arrays is not-null
	/// </remarks>
	internal class LS_ValueMap<ValueType> where ValueType : IEquatable<ValueType>
	{
		/// <summary> An array used when the representation is dense
		///           (null otherwise)
		/// </summary>
		private ValueType[] _array;

		/// <summary> A dictionary used when the representation is sparse
		///           (null otherwise)
		/// </summary>
		private Dictionary<int, ValueType> _dictionary;

		/// <summary> Lowest acceptable value</summary>
		private int _lowerBound;

		/// <summary> Highest acceptable value</summary>
		private int _upperBound;

		/// <summary> Value associated to any key by default</summary>
		private readonly ValueType _defaultValue;

		/// <summary> Expected number of entries </summary>
		private readonly int _estimatedSize;

		/// <summary> Gets/sets the value associated to a key
		/// </summary>
		public ValueType this[int key]
		{
			get
			{
				if (_array != null)
				{
					if (_lowerBound > key || key > _upperBound)
					{
						return _defaultValue;
					}
					return _array[key - _lowerBound];
				}
				ValueType value = _defaultValue;
				if (_dictionary != null)
				{
					_dictionary.TryGetValue(key, out value);
				}
				return value;
			}
			set
			{
				if (_array != null)
				{
					if (_lowerBound <= key && key <= _upperBound)
					{
						_array[key - _lowerBound] = value;
					}
					else
					{
						SwitchToGeneralRepresentation();
					}
				}
				else if (_dictionary == null)
				{
					CreateContiguousRepresentation(key, value);
				}
				if (_dictionary != null)
				{
					if (value.Equals(_defaultValue))
					{
						_dictionary.Remove(key);
					}
					else
					{
						_dictionary[key] = value;
					}
				}
			}
		}

		/// <summary> A specialized Dictionary whose keys are Integer values
		///           from a restricted range
		/// </summary>
		/// <param name="estimatedSize">An estimate of how many entries
		///           will be entered in the dictionary
		/// </param>
		/// <param name="defaultValue">The value associated to any entry 
		///           by default, i.e. if not explicitly overwritten
		/// </param>
		public LS_ValueMap(int estimatedSize, ValueType defaultValue)
		{
			_defaultValue = defaultValue;
			_estimatedSize = Math.Min(estimatedSize, 1000000);
			_lowerBound = int.MaxValue;
			_upperBound = int.MinValue;
		}

		/// <summary> Resets the value associated to all keys to the default
		/// </summary>
		/// <remarks> We don't change representation, we just reset - 
		///           the allocated collection remains allocated
		/// </remarks>
		public void Clear()
		{
			if (_array != null)
			{
				for (int i = 0; i < _array.Length; i++)
				{
					_array[i] = _defaultValue;
				}
			}
			if (_dictionary != null)
			{
				_dictionary.Clear();
			}
		}

		/// <summary> Enumerates all value pairs where the value is not the default
		/// </summary>
		public IEnumerable<KeyValuePair<int, ValueType>> EnumerateModifiedEntries()
		{
			if (_array != null)
			{
				for (int key = _lowerBound; key <= _upperBound; key++)
				{
					ValueType val = _array[key - _lowerBound];
					if (!val.Equals(_defaultValue))
					{
						yield return new KeyValuePair<int, ValueType>(key, _array[key - _lowerBound]);
					}
					if (key == int.MaxValue)
					{
						break;
					}
				}
			}
			else
			{
				if (_dictionary == null)
				{
					yield break;
				}
				foreach (KeyValuePair<int, ValueType> item in _dictionary)
				{
					yield return item;
				}
			}
		}

		/// <summary> Create a contiguous representation 
		/// </summary>
		private void CreateContiguousRepresentation(int initialKey, ValueType initialValue)
		{
			int num = _estimatedSize * 2;
			_lowerBound = ((initialKey <= int.MinValue + num) ? int.MinValue : (initialKey - num));
			_upperBound = ((initialKey >= int.MinValue - num) ? int.MaxValue : (initialKey + num));
			_array = new ValueType[_upperBound - _lowerBound + 1];
			Clear();
			_array[initialKey - _lowerBound] = initialValue;
		}

		/// <summary> Construct a non-contiguous representation
		/// </summary>
		private void SwitchToGeneralRepresentation()
		{
			_dictionary = new Dictionary<int, ValueType>();
			for (int i = _lowerBound; i <= _upperBound; i++)
			{
				ValueType value = _array[i - _lowerBound];
				if (!value.Equals(_defaultValue))
				{
					_dictionary.Add(i, value);
				}
				if (i == int.MaxValue)
				{
					break;
				}
			}
			_array = null;
		}
	}
}
