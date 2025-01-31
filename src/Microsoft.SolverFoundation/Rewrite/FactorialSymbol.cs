using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class FactorialSymbol : Symbol
	{
		internal FactorialSymbol(RewriteSystem rs)
			: base(rs, "Factorial")
		{
			AddAttributes(rs.Attributes.Listable);
		}

		public override Expression EvaluateInvocationArgs(InvocationBuilder ib)
		{
			if (ib.Count != 1 || !ib[0].GetValue(out BigInteger val) || !BigInteger.TryFactorial(val, out val))
			{
				return null;
			}
			return new IntegerConstant(base.Rewrite, val);
		}
	}
}
