using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class FloorSymbol : Symbol
	{
		internal FloorSymbol(RewriteSystem rs)
			: base(rs, "Floor")
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
				return RationalConstant.Create(base.Rewrite, val2.GetFloor());
			}
			return null;
		}
	}
}
