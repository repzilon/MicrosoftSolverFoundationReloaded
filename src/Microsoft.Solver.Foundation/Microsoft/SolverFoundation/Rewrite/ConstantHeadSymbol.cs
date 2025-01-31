namespace Microsoft.SolverFoundation.Rewrite
{
	internal abstract class ConstantHeadSymbol : Symbol
	{
		protected ConstantHeadSymbol(RewriteSystem rs, string strName)
			: base(rs, strName)
		{
		}

		public abstract int CompareConstants(Expression expr0, Expression expr1);
	}
}
