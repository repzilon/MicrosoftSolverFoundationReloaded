using System;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class ClrObjectWrapper : Constant
	{
		private readonly object _obj;

		private readonly Type _type;

		private int _hash;

		public override Expression Head => base.Rewrite.Builtin.ClrObject;

		public override object ObjectValue => _obj;

		public virtual object Value => _obj;

		public virtual Type Type => _type;

		public static Constant MakeConstant(RewriteSystem rs, object obj)
		{
			if (obj is string str)
			{
				return new StringConstant(rs, str);
			}
			if (obj is int)
			{
				return new IntegerConstant(rs, (int)obj);
			}
			if (obj is uint)
			{
				return new IntegerConstant(rs, (uint)obj);
			}
			if (obj is long)
			{
				return new IntegerConstant(rs, (long)obj);
			}
			if (obj is ulong)
			{
				return new IntegerConstant(rs, (ulong)obj);
			}
			if (obj is double)
			{
				return new FloatConstant(rs, (double)obj);
			}
			if (obj is float)
			{
				return new FloatConstant(rs, (float)obj);
			}
			if (obj is bool)
			{
				return new BooleanConstant(rs, (bool)obj);
			}
			Rational? rational = obj as Rational?;
			if (rational.HasValue)
			{
				return (Constant)RationalConstant.Create(rs, rational.Value);
			}
			return new ClrObjectWrapper(rs, obj);
		}

		private ClrObjectWrapper(RewriteSystem rs, object obj)
			: base(rs)
		{
			_obj = obj;
			_type = ((_obj == null) ? null : _obj.GetType());
		}

		public override string ToString()
		{
			try
			{
				return string.Format(CultureInfo.InvariantCulture, "Wrap({0})", new object[1] { (_obj == null) ? "<null>" : _obj.ToString() });
			}
			catch (Exception ex)
			{
				return string.Format(CultureInfo.InvariantCulture, Resources.ClrObjectWrapperException0, new object[1] { ex.Message });
			}
		}

		public override bool Equivalent(Expression expr)
		{
			return this == expr;
		}

		public override int GetEquivalenceHash()
		{
			if (_hash != 0)
			{
				return _hash;
			}
			if (_obj == null)
			{
				return _hash = 1;
			}
			int num = _obj.GetHashCode();
			if (num == 0)
			{
				num = 1;
			}
			return _hash = num;
		}
	}
}
