namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class XorSymbol : Symbol
	{
		private BooleanXor _bx;

		internal XorSymbol(RewriteSystem rs)
			: base(rs, "Xor", new ParseInfo("^|", Precedence.Xor, Precedence.AndAlso, ParseInfoOptions.VaryadicInfix))
		{
			AddAttributes(rs.Attributes.Flat, rs.Attributes.Listable, rs.Attributes.UnaryIdentity, rs.Attributes.Orderless);
			_bx = new BooleanXor(rs);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			return _bx.EvaluateInvocationArgs(ib);
		}
	}
}
