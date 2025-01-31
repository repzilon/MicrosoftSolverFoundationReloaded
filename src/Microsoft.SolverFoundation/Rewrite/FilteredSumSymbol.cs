using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class FilteredSumSymbol : Symbol
	{
		internal FilteredSumSymbol(RewriteSystem rs)
			: base(rs, "FilteredSum", new ParseInfo(ParseInfoOptions.IteratorScope))
		{
			AddAttributes(rs.Attributes.HoldAll);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count <= 1)
			{
				return null;
			}
			if (ib.Count == 2)
			{
				Expression expression = ib[0].Evaluate();
				if (!(expression is BooleanConstant booleanConstant))
				{
					return null;
				}
				if (booleanConstant.Value)
				{
					return ib[0];
				}
				return base.Rewrite.Builtin.Integer.Zero;
			}
			IterInfo[] rgii = new IterInfo[ib.Count - 2];
			Substitution sub = new Substitution();
			List<Expression> rgexpr = null;
			Number numTot = default(Number);
			if (!Generate(ib, rgii, 0, sub, ref rgexpr, ref numTot))
			{
				return null;
			}
			if (rgexpr == null)
			{
				if (numTot._fFloat)
				{
					return new FloatConstant(base.Rewrite, numTot._dbl + (double)numTot._rat);
				}
				return RationalConstant.Create(base.Rewrite, numTot._rat);
			}
			if (numTot._fFloat)
			{
				rgexpr.Insert(0, new FloatConstant(base.Rewrite, numTot._dbl + (double)numTot._rat));
			}
			else if (!numTot._rat.IsZero)
			{
				rgexpr.Insert(0, RationalConstant.Create(base.Rewrite, numTot._rat));
			}
			return base.Rewrite.Builtin.Plus.Invoke(rgexpr.ToArray());
		}

		private bool Generate(InvocationBuilder ib, IterInfo[] rgii, int iii, Substitution sub, ref List<Expression> rgexpr, ref Number numTot)
		{
			base.Rewrite.CheckAbort();
			if (iii >= rgii.Length)
			{
				Expression expression = sub.Apply(ib[iii]).Evaluate();
				if (!(expression is BooleanConstant booleanConstant))
				{
					return false;
				}
				if (!booleanConstant.Value)
				{
					return true;
				}
				expression = sub.Apply(ib[iii + 1]).Evaluate();
				double val2;
				if (expression.GetValue(out Rational val))
				{
					numTot._rat += val;
				}
				else if (expression.GetValue(out val2))
				{
					numTot._fFloat = true;
					numTot._dbl += val2;
				}
				else
				{
					if (rgexpr == null)
					{
						rgexpr = new List<Expression>();
					}
					rgexpr.Add(expression);
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
				if (!Generate(ib, rgii, iii + 1, sub, ref rgexpr, ref numTot))
				{
					return false;
				}
				sub.PopToMark(mark);
			}
			return true;
		}
	}
}
