using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal sealed class ValueTable3<TValue> : ValueTable<TValue>
	{
		private struct Key : IEquatable<Key>
		{
			public object key0;

			public object key1;

			public object key2;

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
				return Statics.CombineHash(key0.GetHashCode(), Statics.CombineHash(key1.GetHashCode(), key2.GetHashCode()));
			}

			public bool Equals(Key other)
			{
				if (key0.Equals(other.key0) && key1.Equals(other.key1))
				{
					return key2.Equals(other.key2);
				}
				return false;
			}
		}

		private readonly Dictionary<Key, TValue> _dictionary;

		public override IEnumerable<object[]> Keys
		{
			get
			{
				foreach (Key k in _dictionary.Keys)
				{
					yield return new object[3] { k.key0, k.key1, k.key2 };
				}
			}
		}

		public override IEnumerable<TValue> Values => _dictionary.Values;

		internal ValueTable3(Domain domain, ValueSet[] indexSets)
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
			result.key0 = keys[0];
			result.key1 = keys[1];
			result.key2 = keys[2];
			return result;
		}
	}
}
