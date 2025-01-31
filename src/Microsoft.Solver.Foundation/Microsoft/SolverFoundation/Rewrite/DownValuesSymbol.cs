namespace Microsoft.SolverFoundation.Rewrite
{
	internal class DownValuesSymbol : Symbol
	{
		internal DownValuesSymbol(RewriteSystem rs)
			: base(rs, "DownValues")
		{
			AddAttributes(rs.Attributes.HoldAll);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1 || !(ib[0] is Symbol symbol))
			{
				return null;
			}
			return symbol.DownValues;
		}
	}
}
