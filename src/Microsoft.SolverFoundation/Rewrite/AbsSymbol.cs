using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class AbsSymbol : Symbol
	{
		internal AbsSymbol(RewriteSystem rs)
			: base(rs, "Abs")
		{
			AddAttributes(rs.Attributes.Listable);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			Expression expression = ib[0];
			if (expression.GetValue(out BigInteger val))
			{
				BigInteger absoluteValue = val.AbsoluteValue;
				if (absoluteValue == val)
				{
					return expression;
				}
				return new IntegerConstant(base.Rewrite, absoluteValue);
			}
			if (expression.GetValue(out Rational val2))
			{
				Rational absoluteValue2 = val2.AbsoluteValue;
				if (absoluteValue2 == val2)
				{
					return expression;
				}
				return RationalConstant.Create(base.Rewrite, absoluteValue2);
			}
			if (expression.GetValue(out double val3))
			{
				double num = Math.Abs(val3);
				if (num == val3 || double.IsNaN(val3))
				{
					return expression;
				}
				return new FloatConstant(base.Rewrite, num);
			}
			return null;
		}
	}
}
