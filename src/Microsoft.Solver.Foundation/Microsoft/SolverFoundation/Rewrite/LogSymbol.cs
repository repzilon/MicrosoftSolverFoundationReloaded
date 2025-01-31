using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class LogSymbol : UnaryMathSymbolBase
	{
		internal LogSymbol(RewriteSystem rs)
			: base(rs, "Log", Math.Log)
		{
		}
	}
}
