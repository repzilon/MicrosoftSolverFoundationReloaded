using System;
using System.Globalization;
using System.Reflection;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class GetClrValueSymbol : Symbol
	{
		private BindingFlags _flags;

		internal GetClrValueSymbol(RewriteSystem rs, BindingFlags flags, string strName)
			: base(rs, strName)
		{
			_flags = flags;
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2 || !(ib[0] is ClrObjectWrapper clrObjectWrapper) || !ib[1].GetValue(out string val))
			{
				return null;
			}
			if (clrObjectWrapper.Value == null)
			{
				return base.Rewrite.Fail(Resources.CalledOnNull, Name);
			}
			object obj;
			try
			{
				Type type;
				obj = ((!((type = clrObjectWrapper.Value as Type) != null)) ? clrObjectWrapper.Type.InvokeMember(val, BindingFlags.Instance | _flags, ReflectionBinder.Instance, clrObjectWrapper.Value, new object[0], CultureInfo.InvariantCulture) : type.InvokeMember(val, BindingFlags.Static | _flags, ReflectionBinder.Instance, null, new object[0], CultureInfo.InvariantCulture));
			}
			catch (Exception ex)
			{
				return base.Rewrite.Fail(Resources.Exception1, Name, ex.Message);
			}
			return ClrObjectWrapper.MakeConstant(base.Rewrite, obj);
		}
	}
}
