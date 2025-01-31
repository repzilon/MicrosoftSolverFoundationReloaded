namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class CacheSequenceSymbol : Symbol
	{
		internal CacheSequenceSymbol(RewriteSystem rs)
			: base(rs, "CacheSequence")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			if (!(ib[0] is ExprSequence exprSequence))
			{
				return null;
			}
			if (exprSequence.IsCached)
			{
				return exprSequence;
			}
			return new CachedExprSequence(exprSequence);
		}
	}
}
