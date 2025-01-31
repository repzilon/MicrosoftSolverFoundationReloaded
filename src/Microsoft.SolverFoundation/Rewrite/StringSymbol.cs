using System;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class StringSymbol : ConstantHeadSymbol
	{
		public readonly StringConstant Empty;

		internal StringSymbol(RewriteSystem rs)
			: base(rs, "String")
		{
			Empty = new StringConstant(rs, "");
		}

		public override int CompareConstants(Expression expr0, Expression expr1)
		{
			StringConstant stringConstant = (StringConstant)expr0;
			StringConstant stringConstant2 = (StringConstant)expr1;
			return string.Compare(stringConstant.Value, stringConstant2.Value, StringComparison.Ordinal);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1)
			{
				return null;
			}
			if (ib[0] is StringConstant)
			{
				return ib[0];
			}
			return new StringConstant(base.Rewrite, ib[0].ToString());
		}
	}
}
