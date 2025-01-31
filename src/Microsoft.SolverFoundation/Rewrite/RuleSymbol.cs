namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class RuleSymbol : Symbol
	{
		internal RuleSymbol(RewriteSystem rs)
			: base(rs, "Rule", new ParseInfo("->", Precedence.Condition, Precedence.Rule, ParseInfoOptions.CreateScope))
		{
			AddAttributes(rs.Attributes.HoldSplice);
		}
	}
}
