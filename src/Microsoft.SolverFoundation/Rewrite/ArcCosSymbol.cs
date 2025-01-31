using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ArcCosSymbol : UnaryMathSymbolBase
	{
		internal ArcCosSymbol(RewriteSystem rs)
			: base(rs, "ArcCos", Math.Acos)
		{
		}
	}
}
