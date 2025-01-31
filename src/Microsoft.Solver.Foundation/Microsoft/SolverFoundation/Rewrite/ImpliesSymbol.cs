namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ImpliesSymbol : Symbol
	{
		internal ImpliesSymbol(RewriteSystem rs)
			: base(rs, "Implies", new ParseInfo("-:", Precedence.Or, Precedence.Implies))
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2)
			{
				return null;
			}
			if (!ib[0].GetValue(out bool val))
			{
				return null;
			}
			if (!ib[1].GetValue(out bool val2))
			{
				return null;
			}
			return base.Rewrite.Builtin.Boolean.Get(!val || val2);
		}
	}
}
