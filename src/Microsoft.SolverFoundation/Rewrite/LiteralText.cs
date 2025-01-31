namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class LiteralText : Expression
	{
		private readonly string _strName;

		public override Expression Head => base.Rewrite.Builtin.Root;

		public LiteralText(RewriteSystem rs, string strName)
			: base(rs)
		{
			_strName = strName;
		}

		public override string ToString()
		{
			return _strName;
		}

		public override bool Equivalent(Expression expr)
		{
			return this == expr;
		}

		public override int GetEquivalenceHash()
		{
			return GetHashCode();
		}
	}
}
