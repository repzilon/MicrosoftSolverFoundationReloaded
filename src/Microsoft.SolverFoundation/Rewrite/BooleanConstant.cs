namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class BooleanConstant : Constant<bool>
	{
		public override Expression Head => base.Rewrite.Builtin.Boolean;

		internal BooleanConstant(RewriteSystem rs, bool f)
			: base(rs, f)
		{
		}

		public override bool GetValue(out bool val)
		{
			val = base.Value;
			return true;
		}
	}
}
