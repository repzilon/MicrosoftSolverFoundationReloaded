using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class FilteredForeachSymbol : Symbol
	{
		internal FilteredForeachSymbol(RewriteSystem rs)
			: base(rs, "FilteredForeach", new ParseInfo(ParseInfoOptions.IteratorScope))
		{
			AddAttributes(rs.Attributes.HoldAll);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count <= 2)
			{
				if (ib.Count < 2)
				{
					return null;
				}
				Expression expression = ib[0].Evaluate();
				if (!(expression is BooleanConstant booleanConstant))
				{
					return null;
				}
				if (booleanConstant.Value)
				{
					return ib[1];
				}
				return base.Rewrite.Builtin.ArgumentSplice.Invoke();
			}
			IterInfo[] rgii = new IterInfo[ib.Count - 2];
			List<Expression> list = new List<Expression>();
			Substitution sub = new Substitution();
			if (!Generate(ib, rgii, list, 0, sub))
			{
				return null;
			}
			return base.Rewrite.Builtin.ArgumentSplice.Invoke(list.ToArray());
		}

		/// <summary>
		/// Generate each element of the foreach.
		/// </summary>
		/// <param name="ib">The InvocationBuilder containing the Foreach arguments</param>
		/// <param name="rgii">A list of IterInfos which the iterator expressions will be parsed into</param>
		/// <param name="rgexpr">A List to put the results into</param>
		/// <param name="iii">The index of the IterInfo being iterated over at this level</param>
		/// <param name="sub">A substitution containing the values of the iterators so far</param>
		/// <returns></returns>
		private bool Generate(InvocationBuilder ib, IterInfo[] rgii, List<Expression> rgexpr, int iii, Substitution sub)
		{
			base.Rewrite.CheckAbort();
			if (iii >= rgii.Length)
			{
				Expression expression = sub.Apply(ib[iii]).Evaluate();
				if (!(expression is BooleanConstant booleanConstant))
				{
					return false;
				}
				if (booleanConstant.Value)
				{
					rgexpr.Add(sub.Apply(ib[iii + 1]).Evaluate());
				}
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
				bool flag = Generate(ib, rgii, rgexpr, iii + 1, sub);
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
