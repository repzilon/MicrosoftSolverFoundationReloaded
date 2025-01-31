using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	internal sealed class ValueTable1<TValue> : ValueTable<TValue>
	{
		private struct Key
		{
			public object key0;
		}

		private readonly Dictionary<Key, TValue> _dictionary;

		public override IEnumerable<object[]> Keys
		{
			get
			{
				foreach (Key k in _dictionary.Keys)
				{
					yield return new object[1] { k.key0 };
				}
			}
		}

		public override IEnumerable<TValue> Values => _dictionary.Values;

		internal ValueTable1(Domain domain, ValueSet[] indexSets)
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
			return result;
		}
	}
}
