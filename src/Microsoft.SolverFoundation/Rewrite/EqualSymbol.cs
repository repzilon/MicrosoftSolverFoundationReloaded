namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class EqualSymbol : CompareSymbol
	{
		internal override Direction Dir => Direction.Equal;

		internal EqualSymbol(RewriteSystem rs)
			: base(rs, "Equal", new ParseInfo("==", Precedence.Compare, Precedence.Plus, ParseInfoOptions.VaryadicInfix | ParseInfoOptions.Comparison))
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count < 2)
			{
				return base.Rewrite.Builtin.Boolean.True;
			}
			bool fRes;
			if (ib.Count == 2)
			{
				if (CompareSymbol.CanCompare(ib[0], ib[1], Dir, out fRes))
				{
					return base.Rewrite.Builtin.Boolean.Get(fRes);
				}
				if (ib[0].Equivalent(ib[1]))
				{
					return base.Rewrite.Builtin.Boolean.True;
				}
				if (ib[0] is Constant && ib[1] is Constant)
				{
					return base.Rewrite.Builtin.Boolean.False;
				}
				return null;
			}
			int num = ib.Count;
			while (--num > 0)
			{
				bool flag = ib[num] is Constant;
				int num2 = num;
				while (--num2 >= 0)
				{
					if (ib[num].Equivalent(ib[num2]) || (CompareSymbol.CanCompare(ib[num], ib[num2], Direction.Equal, out fRes) && fRes))
					{
						ib.RemoveRange(num, num + 1);
						break;
					}
					if (flag && ib[num2] is Constant)
					{
						return base.Rewrite.Builtin.Boolean.False;
					}
				}
			}
			if (ib.Count >= 2)
			{
				return null;
			}
			return base.Rewrite.Builtin.Boolean.True;
		}
	}
}
