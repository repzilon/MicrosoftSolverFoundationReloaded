using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class AbsTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Abs;

		internal AbsTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Abs;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			if (!(inputs[0] < 0))
			{
				return inputs[0];
			}
			return -inputs[0];
		}
	}
}
