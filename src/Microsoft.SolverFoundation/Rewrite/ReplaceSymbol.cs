namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ReplaceSymbol : Symbol
	{
		internal ReplaceSymbol(RewriteSystem rs)
			: base(rs, "Replace")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2 || !base.Rewrite.IsValidRuleSet(ib[1]))
			{
				return null;
			}
			int irule;
			return base.Rewrite.ApplyRuleSet(ib[0], (Invocation)ib[1], out irule);
		}
	}
}
