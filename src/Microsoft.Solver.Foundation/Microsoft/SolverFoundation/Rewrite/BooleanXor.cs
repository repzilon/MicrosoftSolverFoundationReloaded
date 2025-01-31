namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class BooleanXor : ConstantCombiner<bool>
	{
		private RewriteSystem _rs;

		protected override bool Identity => false;

		protected override Expression IdentityExpr => _rs.Builtin.Boolean.False;

		public BooleanXor(RewriteSystem rs)
		{
			_rs = rs;
		}

		protected override bool IsIdentity(bool val)
		{
			return !val;
		}

		protected override bool IsSink(bool val)
		{
			return false;
		}

		protected override bool IsFinalSink(bool val)
		{
			return false;
		}

		protected override bool CombineConsts(ref bool valTot, Expression expr)
		{
			if (expr.GetValue(out bool val))
			{
				valTot ^= val;
				return true;
			}
			return false;
		}

		protected override Expression ExprFromConst(bool val)
		{
			return _rs.Builtin.Boolean.Get(val);
		}
	}
}
