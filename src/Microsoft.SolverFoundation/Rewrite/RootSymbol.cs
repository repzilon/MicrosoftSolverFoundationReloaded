namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class RootSymbol : Symbol
	{
		internal RootSymbol(RewriteSystem rs)
			: base(rs, "Symbol")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count < 1 || ib.Count > 2)
			{
				return null;
			}
			if (!ib[0].GetValue(out string val))
			{
				return null;
			}
			bool val2 = false;
			if (ib.Count == 2 && !ib[1].GetValue(out val2))
			{
				return null;
			}
			if (!base.Rewrite.Scope.GetSymbolThis(val, out var sym))
			{
				if (!val2 || val == null || val.Length == 0)
				{
					return null;
				}
				return new Symbol(base.Rewrite, val);
			}
			return sym;
		}
	}
}
