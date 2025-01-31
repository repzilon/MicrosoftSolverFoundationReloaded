using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class RationalConstant : Constant<Rational>
	{
		public override Expression Head => base.Rewrite.Builtin.Rational;

		public override bool IsNumericValue => true;

		private RationalConstant(RewriteSystem rs, Rational rat)
			: base(rs, rat)
		{
		}

		public static Expression Create(RewriteSystem rs, Rational rat)
		{
			if (rat.IsInteger(out var bn))
			{
				return new IntegerConstant(rs, bn);
			}
			return new RationalConstant(rs, rat);
		}

		public override bool GetValue(out Rational val)
		{
			val = base.Value;
			return true;
		}

		public override bool GetNumericValue(out Rational val)
		{
			val = base.Value;
			return true;
		}
	}
}
