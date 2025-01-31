using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class MinusSymbol : Symbol
	{
		internal MinusSymbol(RewriteSystem rs)
			: base(rs, "Minus", ParseInfo.GetUnaryPrefix("-"))
		{
			AddAttributes(rs.Attributes.Listable);
		}

		public override Expression Invoke(bool fCanOwnArray, params Expression[] args)
		{
			if (args.Length == 1)
			{
				if (args[0].GetValue(out BigInteger val))
				{
					return new IntegerConstant(base.Rewrite, -val);
				}
				if (args[0].GetValue(out Rational val2))
				{
					return RationalConstant.Create(base.Rewrite, -val2);
				}
				if (args[0].GetValue(out double val3))
				{
					return new FloatConstant(base.Rewrite, 0.0 - val3);
				}
			}
			return base.Invoke(fCanOwnArray, args);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			if (ib[0].GetValue(out BigInteger val))
			{
				return new IntegerConstant(base.Rewrite, -val);
			}
			if (ib[0].GetValue(out Rational val2))
			{
				return RationalConstant.Create(base.Rewrite, -val2);
			}
			if (ib[0].GetValue(out double val3))
			{
				return new FloatConstant(base.Rewrite, 0.0 - val3);
			}
			return base.Rewrite.Builtin.Times.Invoke(base.Rewrite.Builtin.Integer.MinusOne, ib[0]);
		}
	}
}
