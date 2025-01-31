using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class SinhSymbol : UnaryMathSymbolBase
	{
		internal SinhSymbol(RewriteSystem rs)
			: base(rs, "Sinh", Math.Sinh)
		{
		}
	}
}
