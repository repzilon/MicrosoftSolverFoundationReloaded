namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class IdenticalSymbol : Symbol
	{
		internal IdenticalSymbol(RewriteSystem rs)
			: base(rs, "Identical", new ParseInfo("===", Precedence.Compare, Precedence.Plus, ParseInfoOptions.VaryadicInfix))
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count > 1)
			{
				Expression expression = ib[0];
				int num = ib.Count;
				while (--num > 0)
				{
					if (!expression.Equivalent(ib[num]))
					{
						return base.Rewrite.Builtin.Boolean.False;
					}
				}
			}
			return base.Rewrite.Builtin.Boolean.True;
		}
	}
}
