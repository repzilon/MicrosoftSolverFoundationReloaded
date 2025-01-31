using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class IntegerConstant : Constant<BigInteger>
	{
		public override Expression Head => base.Rewrite.Builtin.Integer;

		public override bool IsNumericValue => true;

		public IntegerConstant(RewriteSystem rs, BigInteger bn)
			: base(rs, bn)
		{
		}

		public override bool GetValue(out BigInteger val)
		{
			val = base.Value;
			return true;
		}

		public override bool GetValue(out Rational val)
		{
			val = base.Value;
			return true;
		}

		public override bool GetValue(out int val)
		{
			val = (int)base.Value;
			return val == base.Value;
		}

		public override bool GetNumericValue(out Rational val)
		{
			val = base.Value;
			return true;
		}
	}
}
