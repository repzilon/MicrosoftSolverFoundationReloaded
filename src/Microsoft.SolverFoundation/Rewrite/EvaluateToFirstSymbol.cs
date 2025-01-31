namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class EvaluateToFirstSymbol : Symbol
	{
		internal EvaluateToFirstSymbol(RewriteSystem rs)
			: base(rs, "EvaluateToFirst")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count == 0)
			{
				return base.Rewrite.Builtin.Null;
			}
			return ib[0];
		}
	}
}
