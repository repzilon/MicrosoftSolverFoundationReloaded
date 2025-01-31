namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ConditionSymbol : Symbol
	{
		internal ConditionSymbol(RewriteSystem rs)
			: base(rs, "Condition", new ParseInfo("/;", Precedence.Condition, Precedence.OrElse, ParseInfoOptions.BorrowScope))
		{
			AddAttributes(rs.Attributes.HoldAll);
		}
	}
}
