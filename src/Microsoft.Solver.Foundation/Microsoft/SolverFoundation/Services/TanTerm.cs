using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class TanTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Tan;

		internal TanTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Tan;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Tan(inputs[0].ToDouble());
		}
	}
}
