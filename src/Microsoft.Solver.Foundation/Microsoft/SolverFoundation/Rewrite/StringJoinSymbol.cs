using System.Text;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class StringJoinSymbol : Symbol
	{
		internal StringJoinSymbol(RewriteSystem rs)
			: base(rs, "StringJoin")
		{
			AddAttributes(rs.Attributes.Flat, rs.Attributes.UnaryIdentity);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count == 0)
			{
				return base.Rewrite.Builtin.String.Empty;
			}
			if (ib.Count == 1)
			{
				if (ib[0] is StringConstant)
				{
					return ib[0];
				}
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < ib.Count; i++)
			{
				if (!ib[i].GetValue(out string val))
				{
					return null;
				}
				stringBuilder.Append(val);
			}
			return new StringConstant(base.Rewrite, stringBuilder.ToString());
		}
	}
}
