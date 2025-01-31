namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// This is the base type for all Symbols in the "BuiltinSolveSymbols".
	/// </summary>
	internal abstract class BaseSolveSymbol : Symbol
	{
		public new SolveRewriteSystem Rewrite => (SolveRewriteSystem)base.Rewrite;

		protected BaseSolveSymbol(SolveRewriteSystem rs, string strName)
			: base(rs, strName)
		{
		}

		protected BaseSolveSymbol(SolveRewriteSystem rs, string strName, ParseInfo pi)
			: base(rs, strName, pi)
		{
		}

		protected BaseSolveSymbol(SolveRewriteSystem rs, SymbolScope scope, string strName)
			: base(rs, scope, strName)
		{
		}

		protected BaseSolveSymbol(SolveRewriteSystem rs, SymbolScope scope, string strName, ParseInfo pi)
			: base(rs, scope, strName, pi)
		{
		}
	}
}
