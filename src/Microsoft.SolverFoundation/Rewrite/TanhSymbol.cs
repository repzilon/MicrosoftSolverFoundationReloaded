using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class TanhSymbol : UnaryMathSymbolBase
	{
		internal TanhSymbol(RewriteSystem rs)
			: base(rs, "Tanh", Math.Tanh)
		{
		}
	}
}
