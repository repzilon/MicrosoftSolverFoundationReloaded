using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class CompareSymbol : Symbol
	{
		internal enum Direction
		{
			Equal,
			Unequal,
			LessEqual,
			Less,
			GreaterEqual,
			Greater,
			Bad
		}

		private static readonly Func<Rational, Rational, bool>[] _mpdirfnRatRat = new Func<Rational, Rational, bool>[7]
		{
			(Rational rat1, Rational rat2) => rat1 == rat2,
			(Rational rat1, Rational rat2) => rat1 != rat2,
			(Rational rat1, Rational rat2) => rat1 <= rat2,
			(Rational rat1, Rational rat2) => rat1 < rat2,
			(Rational rat1, Rational rat2) => rat1 >= rat2,
			(Rational rat1, Rational rat2) => rat1 > rat2,
			null
		};

		private static readonly Func<Rational, double, bool>[] _mpdirfnRatDbl = new Func<Rational, double, bool>[7]
		{
			(Rational rat, double dbl) => rat == dbl,
			(Rational rat, double dbl) => rat != dbl,
			(Rational rat, double dbl) => rat <= dbl,
			(Rational rat, double dbl) => rat < dbl,
			(Rational rat, double dbl) => rat >= dbl,
			(Rational rat, double dbl) => rat > dbl,
			null
		};

		private static readonly Func<double, Rational, bool>[] _mpdirfnDblRat = new Func<double, Rational, bool>[7]
		{
			(double dbl, Rational rat) => dbl == rat,
			(double dbl, Rational rat) => dbl != rat,
			(double dbl, Rational rat) => dbl <= rat,
			(double dbl, Rational rat) => dbl < rat,
			(double dbl, Rational rat) => dbl >= rat,
			(double dbl, Rational rat) => dbl > rat,
			null
		};

		private static readonly Func<double, double, bool>[] _mpdirfnDblDbl = new Func<double, double, bool>[7]
		{
			(double dbl1, double dbl2) => dbl1.Equals(dbl2),
			(double dbl1, double dbl2) => !dbl1.Equals(dbl2),
			(double dbl1, double dbl2) => dbl1 <= dbl2,
			(double dbl1, double dbl2) => dbl1 < dbl2,
			(double dbl1, double dbl2) => dbl1 >= dbl2,
			(double dbl1, double dbl2) => dbl1 > dbl2,
			null
		};

		internal abstract Direction Dir { get; }

		protected static bool IsNumeric(Expression expr)
		{
			if (!(expr is IntegerConstant) && !(expr is RationalConstant))
			{
				return expr is FloatConstant;
			}
			return true;
		}

		internal static bool CanCompare(Expression expr1, Expression expr2, Direction dir, out bool fRes)
		{
			Rational val2;
			double val3;
			double val4;
			if (expr1.GetValue(out Rational val))
			{
				if (expr2.GetValue(out val2))
				{
					fRes = _mpdirfnRatRat[(int)dir](val, val2);
					return true;
				}
				if (expr2.GetValue(out val3))
				{
					fRes = _mpdirfnRatDbl[(int)dir](val, val3);
					return true;
				}
			}
			else if (expr1.GetValue(out val4))
			{
				if (expr2.GetValue(out val2))
				{
					fRes = _mpdirfnDblRat[(int)dir](val4, val2);
					return true;
				}
				if (expr2.GetValue(out val3))
				{
					fRes = _mpdirfnDblDbl[(int)dir](val4, val3);
					return true;
				}
			}
			fRes = false;
			return false;
		}

		internal static bool CompareNumbers(Expression expr1, Expression expr2, Direction dir)
		{
			FloatConstant floatConstant = null;
			FloatConstant floatConstant2 = expr2 as FloatConstant;
			Rational val2;
			if (expr1.GetValue(out Rational val))
			{
				if (expr2.GetValue(out val2))
				{
					return _mpdirfnRatRat[(int)dir](val, val2);
				}
				if (floatConstant2 != null)
				{
					return _mpdirfnRatDbl[(int)dir](val, floatConstant2.Value);
				}
			}
			if (expr1 is FloatConstant floatConstant3)
			{
				double value = floatConstant3.Value;
				if (expr2.GetValue(out val2))
				{
					return _mpdirfnDblRat[(int)dir](value, val2);
				}
				if (floatConstant2 != null)
				{
					return _mpdirfnDblDbl[(int)dir](value, floatConstant2.Value);
				}
			}
			return false;
		}

		internal CompareSymbol(RewriteSystem rs, string strName, ParseInfo pi)
			: base(rs, strName, pi)
		{
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count < 2)
			{
				return base.Rewrite.Builtin.Boolean.True;
			}
			if (ib.Count == 2)
			{
				if (!CanCompare(ib[0], ib[1], Dir, out var fRes))
				{
					return null;
				}
				return base.Rewrite.Builtin.Boolean.Get(fRes);
			}
			int num = 1;
			int num2 = 0;
			Expression expression = null;
			for (int i = 0; i < ib.Count; i++)
			{
				Expression expression2 = ib[i];
				if (!IsNumeric(expression2))
				{
					num = 0;
				}
				else
				{
					if (expression != null && !CompareNumbers(expression, expression2, Dir))
					{
						return base.Rewrite.Builtin.Boolean.False;
					}
					num++;
					expression = expression2;
				}
				if (num < 3)
				{
					num2++;
				}
				ib[num2 - 1] = ib[i];
			}
			if (num >= 2)
			{
				num2--;
			}
			if (num2 == 0)
			{
				return base.Rewrite.Builtin.Boolean.True;
			}
			if (num2 < ib.Count)
			{
				ib.RemoveRange(num2, ib.Count);
			}
			return null;
		}
	}
}
