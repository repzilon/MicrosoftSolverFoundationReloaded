using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ModTruncSymbol : Symbol
	{
		internal ModTruncSymbol(RewriteSystem rs)
			: base(rs, "ModTrunc")
		{
			AddAttributes(rs.Attributes.Listable);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 2)
			{
				return null;
			}
			if (!ib[0].GetNumericValue(out var val) || !ib[1].GetNumericValue(out var val2) || val2.IsZero)
			{
				return null;
			}
			if (val2.IsInfinite && val.IsFinite)
			{
				return ib[0];
			}
			Rational fractionalPart = (val / val2).GetFractionalPart();
			return RationalConstant.Create(base.Rewrite, fractionalPart * val2);
		}
	}
}
