using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class LessEqualTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.LessEqual;

		internal LessEqualTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Linear | TermStructure.Differentiable);
			if ((termStructure & TermStructure.Linear) != 0)
			{
				_structure |= TermStructure.LinearInequality | TermStructure.LinearConstraint;
			}
			if ((termStructure & TermStructure.Differentiable) != 0)
			{
				_structure |= TermStructure.DifferentiableConstraint;
			}
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.LessEqual;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			for (int i = 0; i < inputs.Length - 1; i++)
			{
				if (inputs[i] > inputs[i + 1])
				{
					return 0;
				}
			}
			return 1;
		}
	}
}
