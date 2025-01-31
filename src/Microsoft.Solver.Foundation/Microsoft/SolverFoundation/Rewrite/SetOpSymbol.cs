using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class SetOpSymbol : Symbol
	{
		internal SetOpSymbol(RewriteSystem rs, string name)
			: base(rs, name)
		{
		}

		protected Expression GetExpr(bool? res)
		{
			if (!res.HasValue)
			{
				return null;
			}
			return base.Rewrite.Builtin.Boolean.Get(res.Value);
		}

		protected bool? IsTupleInSet(Expression val, Expression set)
		{
			if (set.Head == base.Rewrite.Builtin.Tuple)
			{
				return IsTupleInTupleSet(val, set);
			}
			if (set.Head == base.Rewrite.Builtin.List)
			{
				return IsTupleInListSet(val, set);
			}
			return null;
		}

		protected bool? IsValueInSet(Expression val, Expression set)
		{
			if (!val.GetNumericValue(out var val2))
			{
				return null;
			}
			return IsValueInSet(val2, set);
		}

		protected bool? IsValueInSet(Rational num, Expression set)
		{
			if (set == base.Rewrite.Builtin.Integers)
			{
				return num.IsInteger();
			}
			if (set == base.Rewrite.Builtin.Reals)
			{
				return num.IsFinite;
			}
			if (set.Head == base.Rewrite.Builtin.List)
			{
				int num2 = 0;
				for (int i = 0; i < set.Arity; i++)
				{
					if (set[i].GetNumericValue(out var val))
					{
						if (val == num)
						{
							return true;
						}
						num2++;
					}
				}
				if (num2 == set.Arity)
				{
					return false;
				}
			}
			else if ((set.Head == base.Rewrite.Builtin.Integers || set.Head == base.Rewrite.Builtin.Reals) && set.Arity == 2)
			{
				if (!num.IsFinite || (!num.IsInteger() && set.Head == base.Rewrite.Builtin.Integers))
				{
					return false;
				}
				if ((set[0].GetNumericValue(out var val2) && val2 > num) || (set[1].GetNumericValue(out var val3) && val3 < num))
				{
					return false;
				}
				if (val2.HasSign && val3.HasSign)
				{
					return true;
				}
			}
			return null;
		}

		protected bool? IsTupleInTupleSet(Expression val, Expression set)
		{
			if (val.Arity != set.Arity)
			{
				return null;
			}
			bool? flag = true;
			for (int i = 0; i < val.Arity; i++)
			{
				flag = ((val[i].Head != base.Rewrite.Builtin.Tuple) ? (flag & IsValueInSet(val[i], set[i])) : (flag & IsTupleInSet(val[i], set[i])));
				if (flag == false)
				{
					return false;
				}
			}
			return flag;
		}

		protected bool? IsTupleInListSet(Expression val, Expression set)
		{
			if (!IsPrimitiveTuple(val))
			{
				return null;
			}
			bool? flag = false;
			for (int i = 0; i < set.Arity; i++)
			{
				flag |= MatchPrimitiveTuples(val, set[i]);
				if (flag == true)
				{
					return true;
				}
			}
			return flag;
		}

		protected bool IsPrimitiveTuple(Expression expr)
		{
			if (expr.GetNumericValue(out var _))
			{
				return true;
			}
			if (expr.Head != base.Rewrite.Builtin.Tuple)
			{
				return false;
			}
			for (int i = 0; i < expr.Arity; i++)
			{
				if (!IsPrimitiveTuple(expr[i]))
				{
					return false;
				}
			}
			return true;
		}

		protected bool? MatchPrimitiveTuples(Expression exprLeft, Expression exprRight)
		{
			Rational val2;
			if (exprLeft.GetNumericValue(out var val))
			{
				if (exprRight.GetNumericValue(out val2))
				{
					return val == val2;
				}
				if (exprRight.Head != base.Rewrite.Builtin.Tuple)
				{
					return null;
				}
				return false;
			}
			if (exprLeft.Head != base.Rewrite.Builtin.Tuple)
			{
				return null;
			}
			if (exprRight.GetNumericValue(out val2))
			{
				return false;
			}
			if (exprRight.Head != base.Rewrite.Builtin.Tuple)
			{
				return null;
			}
			if (exprLeft.Arity != exprRight.Arity)
			{
				return false;
			}
			bool? flag = true;
			for (int i = 0; i < exprLeft.Arity; i++)
			{
				flag &= MatchPrimitiveTuples(exprLeft[i], exprRight[i]);
				if (flag == false)
				{
					return flag;
				}
			}
			return flag;
		}
	}
}
