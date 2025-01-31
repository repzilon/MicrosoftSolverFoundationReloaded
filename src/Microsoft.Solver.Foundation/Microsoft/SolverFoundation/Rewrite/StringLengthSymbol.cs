namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class StringLengthSymbol : Symbol
	{
		internal StringLengthSymbol(RewriteSystem rs)
			: base(rs, "StringLength")
		{
			AddAttributes(rs.Attributes.Listable);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1 || !(ib[0] is StringConstant stringConstant))
			{
				return null;
			}
			return new IntegerConstant(base.Rewrite, stringConstant.Value.Length);
		}
	}
}
