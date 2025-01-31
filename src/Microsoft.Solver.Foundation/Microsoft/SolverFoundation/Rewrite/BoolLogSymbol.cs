namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class BoolLogSymbol : Symbol
	{
		protected abstract BooleanConstant Identity { get; }

		internal BoolLogSymbol(RewriteSystem rs, string name, ParseInfo pi)
			: base(rs, name, pi)
		{
			AddAttributes(rs.Attributes.Flat, rs.Attributes.HoldAll);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			int i;
			for (i = 0; i < ib.Count; i++)
			{
				ib[i] = ib[i].Evaluate();
				if (!(ib[i] is BooleanConstant booleanConstant))
				{
					break;
				}
				if (booleanConstant.Value != Identity.Value)
				{
					return booleanConstant;
				}
			}
			if (i == ib.Count)
			{
				return Identity;
			}
			if (i > 0)
			{
				ib.RemoveRange(0, i);
			}
			return null;
		}
	}
}
