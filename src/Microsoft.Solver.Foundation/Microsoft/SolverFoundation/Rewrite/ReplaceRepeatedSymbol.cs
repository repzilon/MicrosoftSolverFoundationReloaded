using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ReplaceRepeatedSymbol : Symbol
	{
		internal ReplaceRepeatedSymbol(RewriteSystem rs)
			: base(rs, "ReplaceRepeated", new ParseInfo("/..", Precedence.Replace))
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			int val = 1000;
			if (ib.Count == 2)
			{
				val = 1000;
			}
			else if (ib.Count != 3 || !ib[2].GetValue(out val) || val < 0)
			{
				return null;
			}
			if (!base.Rewrite.IsValidRuleSet(ib[1]))
			{
				return null;
			}
			ReplaceAllVisitor replaceAllVisitor = new ReplaceAllVisitor(base.Rewrite, (Invocation)ib[1]);
			Expression expression = ib[0];
			int num = val;
			while (true)
			{
				Expression expression2 = replaceAllVisitor.Visit(expression).Evaluate();
				if (expression.Equivalent(expression2))
				{
					break;
				}
				if (--num <= 0)
				{
					return base.Rewrite.Fail(Resources.IterationLimitExceededInReplaceRepeated, ib[0], ib[1], val);
				}
				expression = expression2;
			}
			return expression;
		}
	}
}
