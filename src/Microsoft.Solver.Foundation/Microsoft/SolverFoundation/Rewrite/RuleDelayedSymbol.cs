namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class RuleDelayedSymbol : Symbol
	{
		internal RuleDelayedSymbol(RewriteSystem rs)
			: base(rs, "RuleDelayed", new ParseInfo(":>", Precedence.Condition, Precedence.Rule, ParseInfoOptions.CreateScope))
		{
			AddAttributes(rs.Attributes.HoldRest, rs.Attributes.HoldSplice);
		}
	}
}
