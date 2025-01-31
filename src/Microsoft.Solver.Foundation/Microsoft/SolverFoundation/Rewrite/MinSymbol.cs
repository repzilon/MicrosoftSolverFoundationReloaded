namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class MinSymbol : Symbol
	{
		private NumberMin _nm;

		internal MinSymbol(RewriteSystem rs)
			: base(rs, "Min")
		{
			AddAttributes(rs.Attributes.Flat, rs.Attributes.Listable, rs.Attributes.UnaryIdentity, rs.Attributes.Orderless);
			_nm = new NumberMin(rs);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			return _nm.EvaluateInvocationArgs(ib);
		}
	}
}
