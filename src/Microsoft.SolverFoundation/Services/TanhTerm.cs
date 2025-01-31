using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class TanhTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Tanh;

		internal TanhTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Tanh;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Tanh(inputs[0].ToDouble());
		}
	}
}
