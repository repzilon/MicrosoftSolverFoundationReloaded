namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class SortSymbol : Symbol
	{
		internal SortSymbol(RewriteSystem rs)
			: base(rs, "Sort")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1 || !(ib[0] is Invocation inv))
			{
				return null;
			}
			using (InvocationBuilder invocationBuilder = InvocationBuilder.GetBuilder(inv, fKeepAll: true))
			{
				invocationBuilder.SortArgs();
				return invocationBuilder.GetNew();
			}
		}
	}
}
