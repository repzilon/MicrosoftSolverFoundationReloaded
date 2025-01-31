namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class UnequalSymbol : CompareSymbol
	{
		internal override Direction Dir => Direction.Unequal;

		internal UnequalSymbol(RewriteSystem rs)
			: base(rs, "Unequal", new ParseInfo("!=", Precedence.Compare, Precedence.Plus, ParseInfoOptions.VaryadicInfix | ParseInfoOptions.Comparison))
		{
			AddAttributes(rs.Attributes.Orderless);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count < 2)
			{
				return base.Rewrite.Builtin.Boolean.True;
			}
			bool flag = ib[0] is Constant;
			int num = ib.Count;
			while (--num > 0)
			{
				flag = flag && ib[num] is Constant;
				int num2 = num;
				while (--num2 >= 0)
				{
					if (ib[num].Equivalent(ib[num2]) || (CompareSymbol.CanCompare(ib[num], ib[num2], Direction.Equal, out var fRes) && fRes))
					{
						return base.Rewrite.Builtin.Boolean.False;
					}
				}
			}
			if (!flag)
			{
				return null;
			}
			return base.Rewrite.Builtin.Boolean.True;
		}
	}
}
