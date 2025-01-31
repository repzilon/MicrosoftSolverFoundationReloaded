using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Basic class of sets of a homogeneous type
	/// </summary>
	/// <remarks>
	///   Implementation is a bit stupid, in that we store useless (null) value
	///   - only the key is needed. Some things, e.g. Enumeration, won't be
	///   particularly speedy.
	/// </remarks>
	internal class Set<T> : IEnumerable<T>, IEnumerable
	{
		internal struct SetEnumerator : IEnumerator<T>, IDisposable, IEnumerator
		{
			private Dictionary<T, object>.Enumerator _enumerator;

			private readonly Set<T> _set;

			object IEnumerator.Current => _enumerator.Current.Key;

			T IEnumerator<T>.Current => _enumerator.Current.Key;

			public SetEnumerator(Set<T> set)
			{
				_enumerator = set._dict.GetEnumerator();
				_set = set;
			}

			public bool MoveNext()
			{
				return _enumerator.MoveNext();
			}

			void IDisposable.Dispose()
			{
				_enumerator.Dispose();
			}

			public void Reset()
			{
				_enumerator = _set._dict.GetEnumerator();
			}
		}

		private Dictionary<T, object> _dict;

		/// <summary>
		///   returns the number of elements currently stored in the set
		/// </summary>
		public int Cardinality => _dict.Count;

		/// <summary>
		///   Construction of an empty set
		/// </summary>
		public Set()
		{
			_dict = new Dictionary<T, object>();
		}

		/// <summary>
		///   Adds an element to the set; allowed even
		///   if the element is already present (will be stored only once)
		/// </summary>
		public void Add(T elt)
		{
			_dict[elt] = null;
		}

		/// <summary>
		///   Removes an element from the set;
		///   Precondition: the element should be included
		/// </summary>
		public void Remove(T elt)
		{
			_dict.Remove(elt);
		}

		/// <summary>
		///   Removes all elements from the set
		/// </summary>
		public void Clear()
		{
			_dict.Clear();
		}

		/// <summary>
		///   Does the set contain the element?
		/// </summary>
		public bool Contains(T elt)
		{
			return _dict.ContainsKey(elt);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return new SetEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new SetEnumerator(this);
		}
	}
}
