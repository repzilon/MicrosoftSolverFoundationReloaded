using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ArcTanSymbol : UnaryMathSymbolBase
	{
		internal ArcTanSymbol(RewriteSystem rs)
			: base(rs, "ArcTan", Math.Atan)
		{
		}
	}
}
