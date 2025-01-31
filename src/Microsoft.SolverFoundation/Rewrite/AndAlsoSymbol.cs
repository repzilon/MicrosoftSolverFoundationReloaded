namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class AndAlsoSymbol : BoolLogSymbol
	{
		protected override BooleanConstant Identity => base.Rewrite.Builtin.Boolean.True;

		internal AndAlsoSymbol(RewriteSystem rs)
			: base(rs, "AndAlso", new ParseInfo("&&", Precedence.AndAlso, Precedence.And, ParseInfoOptions.VaryadicInfix))
		{
		}
	}
}
