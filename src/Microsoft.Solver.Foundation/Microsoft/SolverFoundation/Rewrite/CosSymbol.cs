using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class CosSymbol : UnaryMathSymbolBase
	{
		internal CosSymbol(RewriteSystem rs)
			: base(rs, "Cos", Math.Cos)
		{
		}
	}
}
