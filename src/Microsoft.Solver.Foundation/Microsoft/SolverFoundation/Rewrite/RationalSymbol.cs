using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class RationalSymbol : ConstantHeadSymbol
	{
		public readonly Expression Infinity;

		public readonly Expression UnsignedInfinity;

		public readonly Expression Indeterminate;

		internal RationalSymbol(RewriteSystem rs)
			: base(rs, "Rational")
		{
			Infinity = RationalConstant.Create(rs, Rational.PositiveInfinity);
			UnsignedInfinity = RationalConstant.Create(rs, Rational.UnsignedInfinity);
			Indeterminate = RationalConstant.Create(rs, Rational.Indeterminate);
		}

		public override int CompareConstants(Expression expr0, Expression expr1)
		{
			RationalConstant rationalConstant = (RationalConstant)expr0;
			RationalConstant rationalConstant2 = (RationalConstant)expr1;
			return rationalConstant.Value.CompareTo(rationalConstant2.Value);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			if (ib[0] is RationalConstant || ib[0] is IntegerConstant)
			{
				return ib[0];
			}
			if (ib[0].GetValue(out double val))
			{
				return RationalConstant.Create(base.Rewrite, val);
			}
			return null;
		}
	}
}
