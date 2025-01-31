using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	internal sealed class ValueTable0<TValue> : ValueTable<TValue>
	{
		private TValue _value;

		private bool _valueExists;

		public override IEnumerable<object[]> Keys
		{
			get
			{
				if (_valueExists)
				{
					yield return new object[0];
				}
			}
		}

		public override IEnumerable<TValue> Values
		{
			get
			{
				if (_valueExists)
				{
					yield return _value;
				}
			}
		}

		internal ValueTable0(Domain domain, ValueSet[] indexSets)
			: base(domain, indexSets)
		{
		}

		protected override bool TryGetValueImpl(out TValue value, params object[] keys)
		{
			value = _value;
			return _valueExists;
		}

		/// <summary>Adds a value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="keys">The keys.</param>
		/// <exception cref="T:System.ArgumentException">Thrown if there is already a value.</exception>
		protected override void AddImpl(TValue value, params object[] keys)
		{
			if (_valueExists)
			{
				throw new ArgumentException();
			}
			SetImpl(value, keys);
		}

		protected override void SetImpl(TValue value, params object[] indexes)
		{
			_value = value;
			_valueExists = true;
		}
	}
}
