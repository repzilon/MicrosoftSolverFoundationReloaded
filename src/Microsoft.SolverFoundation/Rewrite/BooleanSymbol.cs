namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class BooleanSymbol : ConstantHeadSymbol
	{
		public readonly BooleanConstant False;

		public readonly BooleanConstant True;

		internal BooleanSymbol(RewriteSystem rs)
			: base(rs, "Boolean")
		{
			False = new BooleanConstant(rs, f: false);
			True = new BooleanConstant(rs, f: true);
		}

		public BooleanConstant Get(bool fVal)
		{
			if (!fVal)
			{
				return False;
			}
			return True;
		}

		public override int CompareConstants(Expression expr0, Expression expr1)
		{
			BooleanConstant booleanConstant = (BooleanConstant)expr0;
			BooleanConstant booleanConstant2 = (BooleanConstant)expr1;
			return booleanConstant.Value.CompareTo(booleanConstant2.Value);
		}
	}
}
