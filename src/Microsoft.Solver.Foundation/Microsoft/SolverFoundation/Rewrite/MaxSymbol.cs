namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class MaxSymbol : Symbol
	{
		private NumberMax _nm;

		internal MaxSymbol(RewriteSystem rs)
			: base(rs, "Max")
		{
			AddAttributes(rs.Attributes.Flat, rs.Attributes.Listable, rs.Attributes.UnaryIdentity, rs.Attributes.Orderless);
			_nm = new NumberMax(rs);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			return _nm.EvaluateInvocationArgs(ib);
		}
	}
}
