using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class FilterSymbol : Symbol
	{
		internal FilterSymbol(RewriteSystem rs)
			: base(rs, "Filter")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2)
			{
				return null;
			}
			List<Expression> list = new List<Expression>();
			Expression expression = ib[0];
			Expression expression2 = ib[1];
			for (int i = 0; i < expression.Arity; i++)
			{
				Expression expression3 = expression[i];
				Expression expression4 = expression2.Invoke(expression3).Evaluate();
				if (expression4.GetValue(out bool val) && val)
				{
					list.Add(expression3);
				}
			}
			return base.Rewrite.Builtin.List.Invoke(list.ToArray());
		}
	}
}
