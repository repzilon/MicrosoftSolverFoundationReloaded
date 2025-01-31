namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ReplaceAllVisitor : RewriteVisitor
	{
		private RewriteSystem _rs;

		private Invocation _rules;

		public ReplaceAllVisitor(RewriteSystem rs, Invocation rules)
		{
			_rs = rs;
			_rules = rules;
		}

		public override Expression Visit(Expression expr)
		{
			int irule;
			Expression result = _rs.ApplyRuleSet(expr, _rules, out irule);
			if (irule >= 0)
			{
				return result;
			}
			return base.Visit(expr);
		}
	}
}
