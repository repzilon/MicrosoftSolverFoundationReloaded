namespace Microsoft.SolverFoundation.Rewrite
{
	internal class AttributesSymbol : Symbol
	{
		internal AttributesSymbol(RewriteSystem rs)
			: base(rs, "Attributes")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1 || !(ib[0] is Symbol symbol))
			{
				return null;
			}
			return symbol.Attributes;
		}
	}
}
