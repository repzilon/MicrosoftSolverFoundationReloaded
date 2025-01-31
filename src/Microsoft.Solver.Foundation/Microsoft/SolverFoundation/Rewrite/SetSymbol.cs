namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// This and SetDelayed are almost identical. SetDelayed has HoldAll while Set has HoldFirst.
	/// SetDelayed returns Null when successful and this returns the rhs.
	/// </summary>
	internal sealed class SetSymbol : SetBaseSymbol
	{
		internal SetSymbol(RewriteSystem rs)
			: base(rs, "Set", new ParseInfo("=", Precedence.Function, Precedence.Assign, ParseInfoOptions.CreateScope))
		{
			AddAttributes(rs.Attributes.HoldFirst, rs.Attributes.HoldSplice);
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
			sym.AddRule(base.Rewrite.Builtin.Rule, exprLeft, exprRight, exprCond);
			return exprRight;
		}
	}
}
