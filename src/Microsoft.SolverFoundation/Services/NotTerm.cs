using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class NotTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Not;

		internal NotTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Not;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return (inputs[0] == 0) ? 1 : 0;
		}
	}
}
