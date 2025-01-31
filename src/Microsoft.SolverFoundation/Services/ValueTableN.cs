using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal sealed class ValueTableN<TValue> : ValueTable<TValue>
	{
		private struct Key : IEquatable<Key>
		{
			public object[] keys;

			public override bool Equals(object obj)
			{
				if (!(obj is Key other))
				{
					return false;
				}
				return Equals(other);
			}

			public static bool operator ==(Key x, Key y)
			{
				return x.Equals(y);
			}

			public static bool operator !=(Key x, Key y)
			{
				return !x.Equals(y);
			}

			public override int GetHashCode()
			{
				int num = keys[0].GetHashCode();
				for (int i = 1; i < keys.Length; i++)
				{
					num = Statics.CombineHash(num, keys[i].GetHashCode());
				}
				return num;
			}

			public bool Equals(Key other)
			{
				if (keys.Length != other.keys.Length)
				{
					return false;
				}
				for (int i = 0; i < keys.Length; i++)
				{
					if (!keys[i].Equals(other.keys[i]))
					{
						return false;
					}
				}
				return true;
			}
		}

		private readonly Dictionary<Key, TValue> _dictionary;

		public override IEnumerable<object[]> Keys
		{
			get
			{
				foreach (Key key in _dictionary.Keys)
				{
					yield return key.keys;
				}
			}
		}

		public override IEnumerable<TValue> Values => _dictionary.Values;

		internal ValueTableN(Domain domain, ValueSet[] indexSets)
			: base(domain, indexSets)
		{
			_dictionary = new Dictionary<Key, TValue>();
		}

		protected override bool TryGetValueImpl(out TValue value, params object[] keys)
		{
			Key key = MakeKey(keys);
			return _dictionary.TryGetValue(key, out value);
		}

		protected override void AddImpl(TValue value, params object[] keys)
		{
			Key key = MakeKey(keys);
			_dictionary.Add(key, value);
		}

		protected override void SetImpl(TValue value, params object[] indexes)
		{
			Key key = MakeKey(indexes);
			_dictionary[key] = value;
		}

		private static Key MakeKey(object[] keys)
		{
			Key result = default(Key);
			result.keys = keys;
			return result;
		}
	}
}
