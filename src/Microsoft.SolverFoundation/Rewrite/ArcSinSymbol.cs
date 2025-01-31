using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ArcSinSymbol : UnaryMathSymbolBase
	{
		internal ArcSinSymbol(RewriteSystem rs)
			: base(rs, "ArcSin", Math.Asin)
		{
		}
	}
}
