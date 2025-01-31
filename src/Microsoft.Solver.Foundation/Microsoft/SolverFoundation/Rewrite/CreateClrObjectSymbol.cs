using System;
using System.Reflection;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class CreateClrObjectSymbol : Symbol
	{
		internal CreateClrObjectSymbol(RewriteSystem rs)
			: base(rs, "CreateClrObject")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count < 1 || !(ib[0] is ClrObjectWrapper clrObjectWrapper))
			{
				return null;
			}
			Type type;
			if (clrObjectWrapper.Value == null || (type = clrObjectWrapper.Value as Type) == null)
			{
				return base.Rewrite.Fail(Resources.CalledOnNonType, Name);
			}
			object[] array = new object[ib.Count - 1];
			for (int i = 0; i < array.Length; i++)
			{
				if (!(ib[i + 1] is Constant constant))
				{
					return null;
				}
				array[i] = constant.ObjectValue;
			}
			object obj;
			try
			{
				obj = Activator.CreateInstance(type, BindingFlags.Default, ReflectionBinder.Instance, array, null);
			}
			catch (Exception ex)
			{
				return base.Rewrite.Fail(Resources.Exception1, Name, ex.Message);
			}
			return ClrObjectWrapper.MakeConstant(base.Rewrite, obj);
		}
	}
}
