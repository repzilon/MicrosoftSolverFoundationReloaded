namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class AndSymbol : Symbol
	{
		private BooleanAnd _band;

		internal AndSymbol(RewriteSystem rs)
			: base(rs, "And", new ParseInfo("&", Precedence.And, Precedence.Compare, ParseInfoOptions.VaryadicInfix))
		{
			AddAttributes(rs.Attributes.Flat, rs.Attributes.Listable, rs.Attributes.UnaryIdentity, rs.Attributes.Orderless);
			_band = new BooleanAnd(rs);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			return _band.EvaluateInvocationArgs(ib);
		}
	}
}
