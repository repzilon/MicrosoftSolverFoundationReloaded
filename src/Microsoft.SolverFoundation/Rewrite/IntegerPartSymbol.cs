using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class IntegerPartSymbol : Symbol
	{
		internal IntegerPartSymbol(RewriteSystem rs)
			: base(rs, "IntegerPart")
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
			if (expression.GetValue(out BigInteger _))
			{
				return expression;
			}
			if (expression.GetNumericValue(out var val2))
			{
				return RationalConstant.Create(base.Rewrite, val2.GetIntegerPart());
			}
			return null;
		}
	}
}
