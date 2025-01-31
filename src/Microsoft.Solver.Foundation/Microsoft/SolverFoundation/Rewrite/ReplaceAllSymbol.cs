namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ReplaceAllSymbol : Symbol
	{
		internal ReplaceAllSymbol(RewriteSystem rs)
			: base(rs, "ReplaceAll", new ParseInfo("/.", Precedence.Replace))
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2 || !base.Rewrite.IsValidRuleSet(ib[1]))
			{
				return null;
			}
			ReplaceAllVisitor replaceAllVisitor = new ReplaceAllVisitor(base.Rewrite, (Invocation)ib[1]);
			return replaceAllVisitor.Visit(ib[0]);
		}
	}
}
