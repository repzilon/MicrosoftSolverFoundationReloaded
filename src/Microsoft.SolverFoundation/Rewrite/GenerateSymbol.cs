using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class GenerateSymbol : Symbol
	{
		internal GenerateSymbol(RewriteSystem rs)
			: base(rs, "Generate", new ParseInfo(ParseInfoOptions.IteratorScope))
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
			Invocation newRaw = ib.GetNewRaw();
			return new ExprSequenceEnumerable(base.Rewrite, Generate(newRaw));
		}

		private IEnumerable<Expression> Generate(Invocation inv)
		{
			IterInfo[] rgii = new IterInfo[inv.Arity - 1];
			int[] rgmark = new int[inv.Arity - 1];
			Substitution sub = new Substitution();
			int iii = 0;
			bool fDown = true;
			while (true)
			{
				bool fNext = true;
				if (fDown)
				{
					if (iii >= rgii.Length)
					{
						yield return sub.Apply(inv[iii]).Evaluate();
						fNext = false;
					}
					else
					{
						IterInfo ii = rgii[iii];
						if (ii == null)
						{
							int num = iii;
							IterInfo iterInfo;
							ii = (iterInfo = new IterInfo());
							rgii[num] = iterInfo;
						}
						if (!ii.Parse(sub.Apply(inv[iii]).Evaluate(), fFinite: false))
						{
							yield return base.Rewrite.Fail(Resources.BadIterator0, inv[iii]);
							break;
						}
						rgmark[iii] = sub.PushMark();
					}
				}
				if (fNext)
				{
					IterInfo iterInfo2 = rgii[iii];
					Expression next = iterInfo2.GetNext(base.Rewrite);
					if (next == null)
					{
						fNext = false;
					}
					else if (iterInfo2.Sym != null)
					{
						sub.Add(iterInfo2.Sym, next);
					}
				}
				if (fNext)
				{
					iii++;
					fDown = true;
					continue;
				}
				int num2;
				iii = (num2 = iii - 1);
				if (num2 >= 0)
				{
					sub.PopToMark(rgmark[iii]);
					fDown = false;
					continue;
				}
				break;
			}
		}
	}
}
