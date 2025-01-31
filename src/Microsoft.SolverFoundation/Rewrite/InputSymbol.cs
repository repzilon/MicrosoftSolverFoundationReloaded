namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class InputSymbol : Symbol
	{
		internal InputSymbol(RewriteSystem rs)
			: base(rs, "BindIn", new ParseInfo("<==", Precedence.Atom, Precedence.Invocation, ParseInfoOptions.CreateScope))
		{
			AddAttributes(rs.Attributes.HoldAll, rs.Attributes.HoldSplice);
		}
	}
}
