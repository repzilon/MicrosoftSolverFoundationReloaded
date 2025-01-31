namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class DoSymbol : Symbol
	{
		internal DoSymbol(RewriteSystem rs)
			: base(rs, "Do", new ParseInfo(ParseInfoOptions.IteratorScope))
		{
			AddAttributes(rs.Attributes.HoldAll);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count <= 1)
			{
				return null;
			}
			IterInfo[] rgii = new IterInfo[ib.Count - 1];
			Substitution sub = new Substitution();
			if (!Generate(ib, rgii, 0, sub))
			{
				return null;
			}
			return base.Rewrite.Builtin.Null;
		}

		private bool Generate(InvocationBuilder ib, IterInfo[] rgii, int iii, Substitution sub)
		{
			base.Rewrite.CheckAbort();
			if (iii >= rgii.Length)
			{
				sub.Apply(ib[iii]).Evaluate();
				return true;
			}
			IterInfo iterInfo = rgii[iii];
			if (iterInfo == null)
			{
				iterInfo = (rgii[iii] = new IterInfo());
			}
			if (!iterInfo.Parse(sub.Apply(ib[iii]).Evaluate(), fFinite: true))
			{
				return false;
			}
			int mark = sub.PushMark();
			Expression next;
			while ((next = iterInfo.GetNext(base.Rewrite)) != null)
			{
				if (iterInfo.Sym != null)
				{
					sub.Add(iterInfo.Sym, next);
				}
				bool flag = Generate(ib, rgii, iii + 1, sub);
				sub.PopToMark(mark);
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}
	}
}
