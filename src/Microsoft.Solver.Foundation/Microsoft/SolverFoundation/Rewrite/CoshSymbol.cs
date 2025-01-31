using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class CoshSymbol : UnaryMathSymbolBase
	{
		internal CoshSymbol(RewriteSystem rs)
			: base(rs, "Cosh", Math.Cosh)
		{
		}
	}
}
