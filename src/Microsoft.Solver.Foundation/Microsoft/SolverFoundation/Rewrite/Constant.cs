using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class Constant : Expression
	{
		public abstract object ObjectValue { get; }

		protected Constant(RewriteSystem rs)
			: base(rs)
		{
		}
	}
	internal abstract class Constant<T> : Constant where T : IEquatable<T>
	{
		private T _value;

		public override object ObjectValue => _value;

		public T Value => _value;

		protected Constant(RewriteSystem rs, T value)
			: base(rs)
		{
			_value = value;
		}

		public override string ToString()
		{
			return _value.ToString();
		}

		public override bool Equivalent(Expression expr)
		{
			if (expr == this)
			{
				return true;
			}
			if (expr is Constant<T> constant)
			{
				return _value.Equals(constant._value);
			}
			return false;
		}

		public override int GetEquivalenceHash()
		{
			return _value.GetHashCode();
		}
	}
}
