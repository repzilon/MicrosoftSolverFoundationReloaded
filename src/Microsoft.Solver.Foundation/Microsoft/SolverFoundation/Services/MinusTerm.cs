using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class MinusTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Minus;

		internal MinusTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Linear | TermStructure.Quadratic | TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Minus;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return -inputs[0];
		}
	}
}
