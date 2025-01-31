namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class FreeOfSymbol : Symbol
	{
		internal FreeOfSymbol(RewriteSystem rs)
			: base(rs, "FreeOf")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2)
			{
				return null;
			}
			return base.Rewrite.Builtin.Boolean.Get(!ExpressionVisitor.ContainsMatch(ib[0], ib[1]));
		}
	}
}
