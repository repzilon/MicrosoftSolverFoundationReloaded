using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// A set of values, which can be any type (strings, doubles, etc).
	/// </summary>
	internal class ValueSet
	{
		internal Domain _domain;

		internal HashSet<object> _set;

		private int _lockCount;

		public Domain Domain => _domain;

		/// <summary>Gets whether new values can be added to the ValueSet.
		/// </summary>
		public bool IsLocked => _lockCount > 0;

		internal IEnumerable<object> Values
		{
			get
			{
				foreach (object item in _set)
				{
					yield return item;
				}
			}
		}

		private ValueSet(Domain domain)
		{
			_domain = domain;
			_set = new HashSet<object>();
		}

		/// <summary>
		/// Creates a new ValueSet.
		/// </summary>
		/// <param name="domain">The domain of values the ValueSet can accept.</param>
		/// <returns>A new ValueSet.</returns>
		/// <exception cref="T:System.NotSupportedException">Thrown if the domain is an enumerated domain.</exception>
		public static ValueSet Create(Domain domain)
		{
			return new ValueSet(domain);
		}

		/// <summary>Try to add an item to the ValueSet. Returns true if the set contains the item after the call is completed.
		/// </summary>
		/// <param name="value">The value to add.</param>
		/// <returns>Returns true if the set contains the item. If the ValueSet is locked then new items cannot be added.
		/// This may still result in Add returning true if the item was added before the ValueSet was locked.
		/// </returns>
		internal bool Add(object value)
		{
			if (_lockCount <= 0)
			{
				_set.Add(value);
				return true;
			}
			return _set.Contains(value);
		}

		internal void LockValues()
		{
			_lockCount++;
		}

		internal void UnlockValues()
		{
			_lockCount--;
		}
	}
}
