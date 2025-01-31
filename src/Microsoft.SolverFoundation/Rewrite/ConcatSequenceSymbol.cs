namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ConcatSequenceSymbol : Symbol
	{
		internal ConcatSequenceSymbol(RewriteSystem rs)
			: base(rs, "ConcatSequence")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count == 0)
			{
				return new ExprSequenceArray(base.Rewrite, Invocation.EmptyArgs);
			}
			int num = ib.Count;
			while (--num >= 0)
			{
				if (!(ib[num] is ExprSequence))
				{
					return null;
				}
			}
			if (ib.Count == 1)
			{
				return ib[0];
			}
			ExprSequence[] array = new ExprSequence[ib.Count];
			int num2 = ib.Count;
			while (--num2 >= 0)
			{
				array[num2] = ib[num2] as ExprSequence;
			}
			return new ExprSequenceConcat(base.Rewrite, array);
		}
	}
}
