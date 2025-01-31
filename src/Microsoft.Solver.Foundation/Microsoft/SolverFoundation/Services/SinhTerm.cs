using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class SinhTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Sinh;

		internal SinhTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Sinh;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Sinh(inputs[0].ToDouble());
		}
	}
}
