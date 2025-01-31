using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class FloorTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Floor;

		internal FloorTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Ceiling;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return inputs[0].GetFloor();
		}
	}
}
