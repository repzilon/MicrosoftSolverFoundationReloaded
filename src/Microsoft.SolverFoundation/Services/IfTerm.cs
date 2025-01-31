using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class IfTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.If;

		internal IfTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.If;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			if (!(inputs[0] >= 0.5))
			{
				return inputs[2];
			}
			return inputs[1];
		}
	}
}
