namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class OrSymbol : Symbol
	{
		private BooleanOr _bor;

		internal OrSymbol(RewriteSystem rs)
			: base(rs, "Or", new ParseInfo("|", Precedence.Or, Precedence.Xor, ParseInfoOptions.VaryadicInfix))
		{
			AddAttributes(rs.Attributes.Flat, rs.Attributes.Listable, rs.Attributes.UnaryIdentity, rs.Attributes.Orderless);
			_bor = new BooleanOr(rs);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			return _bor.EvaluateInvocationArgs(ib);
		}
	}
}
