namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ThreadSymbol : Symbol
	{
		internal ThreadSymbol(RewriteSystem rs)
			: base(rs, "Thread")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			if (ib[0] is Invocation inv)
			{
				using (InvocationBuilder ib2 = InvocationBuilder.GetBuilder(inv, fKeepAll: true))
				{
					if (base.Rewrite.ThreadOverLists(ib2, out var exprRes))
					{
						return exprRes;
					}
				}
			}
			return ib[0];
		}
	}
}
