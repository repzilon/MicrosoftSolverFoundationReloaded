using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class FractionalPartSymbol : Symbol
	{
		internal FractionalPartSymbol(RewriteSystem rs)
			: base(rs, "FractionalPart")
		{
			AddAttributes(rs.Attributes.Listable);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			Expression expression = ib[0];
			if (expression.GetValue(out BigInteger _))
			{
				return base.Rewrite.Builtin.Integer.Zero;
			}
			if (expression.GetValue(out Rational val2))
			{
				if (!val2.IsFinite)
				{
					return base.Rewrite.Builtin.Integer.Zero;
				}
				return RationalConstant.Create(base.Rewrite, val2.GetFractionalPart());
			}
			if (expression.GetValue(out double val3))
			{
				if (!NumberUtils.IsFinite(val3))
				{
					return base.Rewrite.Builtin.Integer.Zero;
				}
				return new FloatConstant(base.Rewrite, val3 - Math.Truncate(val3));
			}
			return null;
		}
	}
}
