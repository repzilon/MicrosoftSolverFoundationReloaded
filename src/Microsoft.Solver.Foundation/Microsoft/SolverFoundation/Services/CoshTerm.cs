using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class CoshTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Cosh;

		internal CoshTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Cosh;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Cosh(inputs[0].ToDouble());
		}
	}
}
