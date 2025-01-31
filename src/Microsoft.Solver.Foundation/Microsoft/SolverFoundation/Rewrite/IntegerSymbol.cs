using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class IntegerSymbol : ConstantHeadSymbol
	{
		public readonly IntegerConstant Zero;

		public readonly IntegerConstant One;

		public readonly IntegerConstant Two;

		public readonly IntegerConstant MinusOne;

		internal IntegerSymbol(RewriteSystem rs)
			: base(rs, "Integer")
		{
			Zero = new IntegerConstant(rs, 0);
			One = new IntegerConstant(rs, 1);
			Two = new IntegerConstant(rs, 2);
			MinusOne = new IntegerConstant(rs, -1);
		}

		public override int CompareConstants(Expression expr0, Expression expr1)
		{
			IntegerConstant integerConstant = (IntegerConstant)expr0;
			IntegerConstant integerConstant2 = (IntegerConstant)expr1;
			return integerConstant.Value.CompareTo(integerConstant2.Value);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			if (ib[0] is IntegerConstant)
			{
				return ib[0];
			}
			if (ib[0].GetNumericValue(out var val))
			{
				return new IntegerConstant(base.Rewrite, (BigInteger)val);
			}
			return null;
		}
	}
}
