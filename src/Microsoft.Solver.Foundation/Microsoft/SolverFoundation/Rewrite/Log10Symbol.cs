using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class Log10Symbol : UnaryMathSymbolBase
	{
		internal Log10Symbol(RewriteSystem rs)
			: base(rs, "Log10", Math.Log10)
		{
		}
	}
}
