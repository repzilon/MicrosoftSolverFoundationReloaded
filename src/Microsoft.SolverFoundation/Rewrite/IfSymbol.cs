namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class IfSymbol : Symbol
	{
		internal IfSymbol(RewriteSystem rs)
			: base(rs, "If")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count == 3 && ib[0].GetValue(out bool val))
			{
				if (!val)
				{
					return ib[2];
				}
				return ib[1];
			}
			return null;
		}
	}
}
