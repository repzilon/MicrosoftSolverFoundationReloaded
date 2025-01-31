using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class GreaterTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Greater;

		internal GreaterTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Greater;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			for (int i = 0; i < inputs.Length - 1; i++)
			{
				if (inputs[i] <= inputs[i + 1])
				{
					return 0;
				}
			}
			return 1;
		}
	}
}
