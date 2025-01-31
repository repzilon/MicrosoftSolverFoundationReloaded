using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class SelectSymbol : Symbol
	{
		internal SelectSymbol(RewriteSystem rs)
			: base(rs, "Select")
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
			if (base.Rewrite.IsValidRuleSet(expression2))
			{
				Invocation rules = (Invocation)expression2;
				for (int i = 0; i < expression.Arity; i++)
				{
					int irule;
					Expression item = base.Rewrite.ApplyRuleSet(expression[i], rules, out irule);
					if (irule >= 0)
					{
						list.Add(item);
					}
				}
			}
			else
			{
				for (int j = 0; j < expression.Arity; j++)
				{
					Expression expression3 = expression[j];
					if (base.Rewrite.Match(expression2, expression3))
					{
						list.Add(expression3);
					}
				}
			}
			return base.Rewrite.Builtin.List.Invoke(list.ToArray());
		}
	}
}
