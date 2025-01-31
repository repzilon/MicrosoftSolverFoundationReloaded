namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class QuotientTruncSymbol : Symbol
	{
		internal QuotientTruncSymbol(RewriteSystem rs)
			: base(rs, "QuotientTrunc")
		{
			AddAttributes(rs.Attributes.Listable);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2)
			{
				return null;
			}
			if (!ib[0].GetNumericValue(out var val) || !ib[1].GetNumericValue(out var val2) || val2.IsZero)
			{
				return null;
			}
			return RationalConstant.Create(base.Rewrite, (val / val2).GetIntegerPart());
		}
	}
}
