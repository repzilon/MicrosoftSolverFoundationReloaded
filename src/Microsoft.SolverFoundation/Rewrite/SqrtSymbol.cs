using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal class SqrtSymbol : PowerSymbolBase
	{
		private static Expression _half;

		internal SqrtSymbol(RewriteSystem rs)
			: base(rs, "Sqrt")
		{
			AddAttributes(rs.Attributes.Listable);
			if (_half == null)
			{
				_half = RationalConstant.Create(rs, Rational.Get(1, 2));
			}
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			ib.AddNewArg(_half);
			return base.EvaluateInvocationArgs(ib);
		}
	}
}
