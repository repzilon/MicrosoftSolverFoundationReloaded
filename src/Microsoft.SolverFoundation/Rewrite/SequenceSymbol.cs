namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class SequenceSymbol : Symbol
	{
		internal SequenceSymbol(RewriteSystem rs)
			: base(rs, "Sequence")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			return new ExprSequenceArray(base.Rewrite, ib.ArgsArray);
		}
	}
}
