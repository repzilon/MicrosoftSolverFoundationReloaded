using System;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary>
	/// Not for the faint of heart.
	/// A hash table that doesn't actually store the key. This requires callers to provide
	/// a comparison delegate and hash value. GetHashCode is never called.
	/// </summary>
	internal class HashTable<T>
	{
		private struct Entry
		{
			public T t;

			public uint hash;

			public int ientNext;
		}

		private int[] _rgjent;

		private Entry[] _rgent;

		private int _cent;

		public int Count => _cent;

		public HashTable()
		{
			_rgjent = new int[32];
		}

		/// <summary>
		/// Look for the item. Calls back on the comparison function to determine
		/// a match.
		/// </summary>
		public bool Get<U>(U u, uint hash, Func<U, T, bool> fn, out T t)
		{
			for (int num = _rgjent[hash % _rgjent.Length] - 1; num >= 0; num = _rgent[num].ientNext)
			{
				if (_rgent[num].hash == hash && fn(u, _rgent[num].t))
				{
					t = _rgent[num].t;
					return true;
				}
			}
			t = default(T);
			return false;
		}

		/// <summary>
		/// Adds the item. Does NOT check for whether the item is already present.
		/// </summary>
		public void Add(uint hash, T t)
		{
			int num = (int)(hash % _rgjent.Length);
			if (_rgent == null)
			{
				_rgent = new Entry[10];
			}
			else if (_cent >= _rgent.Length)
			{
				Array.Resize(ref _rgent, _rgent.Length * 3 / 2);
			}
			_rgent[_cent].t = t;
			_rgent[_cent].hash = hash;
			_rgent[_cent].ientNext = _rgjent[num] - 1;
			_rgjent[num] = ++_cent;
			if (_cent >= 2 * _rgjent.Length)
			{
				GrowTable();
			}
		}

		/// <summary>
		/// This is called when the average depth is 2. It then quadruples the number
		/// of buckets, reducing the average depth to 1/2.
		/// </summary>
		private void GrowTable()
		{
			int num = _rgjent.Length * 4;
			_rgjent = new int[num];
			for (int i = 0; i < _cent; i++)
			{
				int num2 = (int)(_rgent[i].hash % _rgjent.Length);
				_rgent[i].ientNext = _rgjent[num2] - 1;
				_rgjent[num2] = i + 1;
			}
		}
	}
}
