namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class OuterSymbol : Symbol
	{
		internal OuterSymbol(RewriteSystem rs)
			: base(rs, "Outer")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count <= 1)
			{
				return null;
			}
			Invocation[] array = new Invocation[ib.Count - 1];
			for (int i = 1; i < ib.Count; i++)
			{
				if (!(ib[i] is Invocation invocation) || invocation.Head != base.Rewrite.Builtin.List)
				{
					return null;
				}
				array[i - 1] = invocation;
			}
			Expression[] rgexprArgs = new Expression[array.Length];
			return Generate(ib[0], array, 0, rgexprArgs);
		}

		private Expression Generate(Expression exprHead, Invocation[] rglist, int ilist, Expression[] rgexprArgs)
		{
			base.Rewrite.CheckAbort();
			if (ilist >= rglist.Length)
			{
				return exprHead.Invoke(fCanOwnArray: false, rgexprArgs).Evaluate();
			}
			int arity = rglist[ilist].Arity;
			if (arity == 0)
			{
				return base.Rewrite.Builtin.List.Invoke();
			}
			Expression[] array = new Expression[arity];
			for (int i = 0; i < arity; i++)
			{
				rgexprArgs[ilist] = rglist[ilist][i];
				array[i] = Generate(exprHead, rglist, ilist + 1, rgexprArgs);
			}
			return base.Rewrite.Builtin.List.Invoke(array);
		}
	}
}
