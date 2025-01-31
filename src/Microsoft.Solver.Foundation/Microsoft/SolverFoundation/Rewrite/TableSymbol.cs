using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class TableSymbol : Symbol
	{
		internal TableSymbol(RewriteSystem rs)
			: base(rs, "Table", new ParseInfo(ParseInfoOptions.IteratorScope))
		{
			AddAttributes(rs.Attributes.HoldAll);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count <= 1)
			{
				if (ib.Count != 1)
				{
					return null;
				}
				return ib[0];
			}
			IterInfo[] rgii = new IterInfo[ib.Count - 1];
			List<Expression>[] rgrgexpr = new List<Expression>[ib.Count - 1];
			Substitution sub = new Substitution();
			return Generate(ib, rgii, rgrgexpr, 0, sub);
		}

		private Expression Generate(InvocationBuilder ib, IterInfo[] rgii, List<Expression>[] rgrgexpr, int iii, Substitution sub)
		{
			base.Rewrite.CheckAbort();
			if (iii >= rgii.Length)
			{
				return sub.Apply(ib[iii]).Evaluate();
			}
			IterInfo iterInfo = rgii[iii];
			if (iterInfo == null)
			{
				iterInfo = (rgii[iii] = new IterInfo());
			}
			if (!iterInfo.Parse(sub.Apply(ib[iii]).Evaluate(), fFinite: true))
			{
				return null;
			}
			List<Expression> list = rgrgexpr[iii];
			if (list == null)
			{
				list = (rgrgexpr[iii] = new List<Expression>());
			}
			else
			{
				list.Clear();
			}
			int mark = sub.PushMark();
			Expression next;
			while ((next = iterInfo.GetNext(base.Rewrite)) != null)
			{
				if (iterInfo.Sym != null)
				{
					sub.Add(iterInfo.Sym, next);
				}
				Expression expression = Generate(ib, rgii, rgrgexpr, iii + 1, sub);
				sub.PopToMark(mark);
				if (expression == null)
				{
					return null;
				}
				list.Add(expression);
			}
			return base.Rewrite.Builtin.List.Invoke(list.ToArray());
		}
	}
}
