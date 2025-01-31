using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class SinSymbol : UnaryMathSymbolBase
	{
		internal SinSymbol(RewriteSystem rs)
			: base(rs, "Sin", Math.Sin)
		{
		}
	}
}
