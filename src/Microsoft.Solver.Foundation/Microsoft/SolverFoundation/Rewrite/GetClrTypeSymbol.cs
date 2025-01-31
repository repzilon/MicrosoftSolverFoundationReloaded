using System;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class GetClrTypeSymbol : Symbol
	{
		internal GetClrTypeSymbol(RewriteSystem rs)
			: base(rs, "GetClrType")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			Type type;
			try
			{
				if (ib[0].GetValue(out string val))
				{
					type = Type.GetType(val, throwOnError: false);
				}
				else
				{
					if (!(ib[0] is ClrObjectWrapper clrObjectWrapper))
					{
						return null;
					}
					type = clrObjectWrapper.Type;
				}
			}
			catch (Exception ex)
			{
				return base.Rewrite.Fail(Resources.Exception1, Name, ex.Message);
			}
			return ClrObjectWrapper.MakeConstant(base.Rewrite, type);
		}
	}
}
