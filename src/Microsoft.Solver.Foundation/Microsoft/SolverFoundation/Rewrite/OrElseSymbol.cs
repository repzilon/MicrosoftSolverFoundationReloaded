namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class OrElseSymbol : BoolLogSymbol
	{
		protected override BooleanConstant Identity => base.Rewrite.Builtin.Boolean.False;

		internal OrElseSymbol(RewriteSystem rs)
			: base(rs, "OrElse", new ParseInfo("||", Precedence.OrElse, Precedence.Implies, ParseInfoOptions.VaryadicInfix))
		{
		}
	}
}
