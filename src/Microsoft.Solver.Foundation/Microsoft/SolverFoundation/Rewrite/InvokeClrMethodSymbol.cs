using System;
using System.Globalization;
using System.Reflection;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class InvokeClrMethodSymbol : Symbol
	{
		internal InvokeClrMethodSymbol(RewriteSystem rs)
			: base(rs, "InvokeClrMethod")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count < 2 || !(ib[0] is ClrObjectWrapper clrObjectWrapper) || !ib[1].GetValue(out string val))
			{
				return null;
			}
			if (clrObjectWrapper.Value == null)
			{
				return base.Rewrite.Fail(Resources.CalledOnNull, Name);
			}
			object[] array = new object[ib.Count - 2];
			for (int i = 0; i < array.Length; i++)
			{
				if (!(ib[i + 2] is Constant constant))
				{
					return null;
				}
				array[i] = constant.ObjectValue;
			}
			object obj;
			try
			{
				Type type;
				obj = ((!((type = clrObjectWrapper.Value as Type) != null)) ? clrObjectWrapper.Type.InvokeMember(val, BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, ReflectionBinder.Instance, clrObjectWrapper.Value, array, CultureInfo.InvariantCulture) : type.InvokeMember(val, BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, ReflectionBinder.Instance, null, array, CultureInfo.InvariantCulture));
			}
			catch (Exception ex)
			{
				return base.Rewrite.Fail(Resources.Exception1, Name, ex.Message);
			}
			return ClrObjectWrapper.MakeConstant(base.Rewrite, obj);
		}
	}
}
