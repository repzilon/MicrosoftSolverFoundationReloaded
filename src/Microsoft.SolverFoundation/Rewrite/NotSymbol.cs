namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class NotSymbol : Symbol
	{
		internal NotSymbol(RewriteSystem rs)
			: base(rs, "Not", ParseInfo.GetUnaryPrefix("!"))
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1 || !ib[0].GetValue(out bool val))
			{
				return null;
			}
			return base.Rewrite.Builtin.Boolean.Get(!val);
		}
	}
}
