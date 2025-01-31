using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class TanSymbol : UnaryMathSymbolBase
	{
		internal TanSymbol(RewriteSystem rs)
			: base(rs, "Tan", Math.Tan)
		{
		}
	}
}
