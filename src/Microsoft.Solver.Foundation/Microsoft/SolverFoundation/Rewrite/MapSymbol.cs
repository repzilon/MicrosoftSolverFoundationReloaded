namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class MapSymbol : Symbol
	{
		internal MapSymbol(RewriteSystem rs)
			: base(rs, "Map")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2)
			{
				return null;
			}
			if (!(ib[1] is Invocation inv))
			{
				return ib[1];
			}
			Expression expression = ib[0];
			using (InvocationBuilder invocationBuilder = InvocationBuilder.GetBuilder(inv, fKeepAll: false))
			{
				while (invocationBuilder.StartNextArg())
				{
					invocationBuilder.AddNewArg(expression.Invoke(invocationBuilder.ArgCur));
				}
				return invocationBuilder.GetNew();
			}
		}
	}
}
