namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class UnIdenticalSymbol : Symbol
	{
		internal UnIdenticalSymbol(RewriteSystem rs)
			: base(rs, "UnIdentical", new ParseInfo("!==", Precedence.Compare, Precedence.Plus, ParseInfoOptions.VaryadicInfix))
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count > 1)
			{
				int num = ib.Count - 1;
				while (--num >= 0)
				{
					Expression expression = ib[num];
					int num2 = ib.Count;
					while (--num2 > num)
					{
						if (expression.Equivalent(ib[num2]))
						{
							return base.Rewrite.Builtin.Boolean.False;
						}
					}
				}
			}
			return base.Rewrite.Builtin.Boolean.True;
		}
	}
}
