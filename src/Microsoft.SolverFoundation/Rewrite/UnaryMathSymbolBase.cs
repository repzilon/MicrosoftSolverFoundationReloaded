using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class UnaryMathSymbolBase : Symbol
	{
		private Func<double, double> _f;

		internal UnaryMathSymbolBase(RewriteSystem rs, string name, Func<double, double> f)
			: base(rs, name)
		{
			AddAttributes(rs.Attributes.Listable);
			_f = f;
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			Expression expression = ib[0];
			if (expression.GetValue(out Rational val))
			{
				Rational rational = _f(val.ToDouble());
				if (rational == val)
				{
					return expression;
				}
				return RationalConstant.Create(base.Rewrite, rational);
			}
			return base.EvaluateInvocationArgs(ib);
		}
	}
}
