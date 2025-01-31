using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ModSymbol : Symbol
	{
		internal ModSymbol(RewriteSystem rs)
			: base(rs, "Mod")
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
			Rational floorResidual = (val / val2).GetFloorResidual();
			return RationalConstant.Create(base.Rewrite, floorResidual * val2);
		}
	}
}
