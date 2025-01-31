using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class SqrtTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Sqrt;

		internal SqrtTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Sqrt;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Sqrt(inputs[0].ToDouble());
		}
	}
}
