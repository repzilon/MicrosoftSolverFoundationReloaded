namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class SetDelayedSymbol : SetBaseSymbol
	{
		internal SetDelayedSymbol(RewriteSystem rs)
			: base(rs, "SetDelayed", new ParseInfo(":=", Precedence.Function, Precedence.Assign, ParseInfoOptions.CreateScope))
		{
			AddAttributes(rs.Attributes.HoldAll, rs.Attributes.HoldSplice);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2)
			{
				return null;
			}
			if (!GetAssignmentParts(ib, out var sym, out var exprLeft, out var exprRight, out var exprCond, out var exprFail))
			{
				return exprFail;
			}
			sym.AddRule(base.Rewrite.Builtin.RuleDelayed, exprLeft, exprRight, exprCond);
			return base.Rewrite.Builtin.Null;
		}
	}
}
