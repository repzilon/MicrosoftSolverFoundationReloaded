namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class AsIntSymbol : Symbol
	{
		internal AsIntSymbol(RewriteSystem rs)
			: base(rs, "AsInt")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count == 1 && ib[0].GetValue(out bool val))
			{
				if (!val)
				{
					return base.Rewrite.Builtin.Integer.Zero;
				}
				return base.Rewrite.Builtin.Integer.One;
			}
			return null;
		}
	}
}
