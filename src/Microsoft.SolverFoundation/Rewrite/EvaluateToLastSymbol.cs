namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class EvaluateToLastSymbol : Symbol
	{
		internal EvaluateToLastSymbol(RewriteSystem rs)
			: base(rs, "EvaluateToLast", new ParseInfo("\\", Precedence.Then))
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count == 0)
			{
				return base.Rewrite.Builtin.Null;
			}
			return ib[ib.Count - 1];
		}
	}
}
