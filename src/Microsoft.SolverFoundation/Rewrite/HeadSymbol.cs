namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class HeadSymbol : Symbol
	{
		internal HeadSymbol(RewriteSystem rs)
			: base(rs, "Head")
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			return ib[0].Head;
		}
	}
}
