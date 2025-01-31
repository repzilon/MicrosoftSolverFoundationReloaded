using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class ClearBaseSymbol : Symbol
	{
		internal ClearBaseSymbol(RewriteSystem rs, string strName)
			: base(rs, strName)
		{
			AddAttributes(rs.Attributes.HoldAll);
		}

		protected abstract Expression CheckForLocked(Symbol sym);

		protected abstract void ClearSymbol(Symbol sym);

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			List<Expression> list = null;
			for (int i = 0; i < ib.Count; i++)
			{
				Symbol sym;
				if ((sym = ib[i] as Symbol) == null)
				{
					if (!ib[i].GetValue(out string val))
					{
						if (list == null)
						{
							list = new List<Expression>();
						}
						list.Add(base.Rewrite.Fail(Resources.IsNotASymbol, ib[i]));
						continue;
					}
					if (!base.Rewrite.Scope.GetSymbolThis(val, out sym))
					{
						continue;
					}
				}
				Expression expression = CheckForLocked(sym);
				if (expression == null)
				{
					ClearSymbol(sym);
					continue;
				}
				if (list == null)
				{
					list = new List<Expression>();
				}
				list.Add(expression);
			}
			if (list == null)
			{
				return base.Rewrite.Builtin.Null;
			}
			return base.Rewrite.Builtin.List.Invoke(list.ToArray());
		}
	}
}
