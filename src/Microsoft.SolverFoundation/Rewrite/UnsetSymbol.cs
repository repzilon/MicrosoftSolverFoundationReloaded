namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class UnsetSymbol : SetBaseSymbol
	{
		internal UnsetSymbol(RewriteSystem rs)
			: base(rs, "Unset", new ParseInfo("=.", Precedence.Function, Precedence.None, ParseInfoOptions.CreateScope))
		{
			AddAttributes(rs.Attributes.HoldAll);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			if (!GetAssignmentParts(ib, out var sym, out var exprLeft, out var _, out var exprCond, out var exprFail))
			{
				return exprFail;
			}
			sym.RemoveRule(exprLeft, exprCond);
			return base.Rewrite.Builtin.Null;
		}
	}
}
