namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ApplySymbol : Symbol
	{
		internal ApplySymbol(RewriteSystem rs)
			: base(rs, "Apply")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2)
			{
				return null;
			}
			if (!(ib[1] is Invocation invocation))
			{
				return null;
			}
			return invocation.Apply(ib[0]);
		}
	}
}
