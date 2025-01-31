using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class QuotientTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Quotient;

		internal QuotientTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			if (inputs[1].HasStructure(TermStructure.Constant))
			{
				_structure |= inputs[0].Structure & (TermStructure.Constant | TermStructure.Linear | TermStructure.Quadratic | TermStructure.Differentiable);
			}
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Quotient;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			if (inputs[1] == 0)
			{
				return Rational.Indeterminate;
			}
			return inputs[0] / inputs[1];
		}
	}
}
