namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class LengthSymbol : Symbol
	{
		internal LengthSymbol(RewriteSystem rs)
			: base(rs, "Length")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			return new IntegerConstant(base.Rewrite, ib[0].Arity);
		}
	}
}
