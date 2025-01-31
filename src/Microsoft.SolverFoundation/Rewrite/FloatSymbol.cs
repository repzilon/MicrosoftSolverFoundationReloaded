using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class FloatSymbol : ConstantHeadSymbol
	{
		internal FloatSymbol(RewriteSystem rs)
			: base(rs, "Float")
		{
		}

		public override int CompareConstants(Expression expr0, Expression expr1)
		{
			FloatConstant floatConstant = (FloatConstant)expr0;
			FloatConstant floatConstant2 = (FloatConstant)expr1;
			return floatConstant.Value.CompareTo(floatConstant2.Value);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			if (ib[0] is FloatConstant)
			{
				return ib[0];
			}
			if (ib[0].GetValue(out Rational val))
			{
				return new FloatConstant(base.Rewrite, (double)val);
			}
			return null;
		}
	}
}
