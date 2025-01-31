namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class InSymbol : SetOpSymbol
	{
		internal InSymbol(RewriteSystem rs)
			: base(rs, "In")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2)
			{
				return null;
			}
			if (ib[0].Head == base.Rewrite.Builtin.Tuple)
			{
				return GetExpr(IsTupleInSet(ib[0], ib[1]));
			}
			return GetExpr(IsValueInSet(ib[0], ib[1]));
		}
	}
}
