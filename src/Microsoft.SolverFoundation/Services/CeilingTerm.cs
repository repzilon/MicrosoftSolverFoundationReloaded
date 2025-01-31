using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class CeilingTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Ceiling;

		internal CeilingTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Ceiling;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return inputs[0].GetCeiling();
		}
	}
}
