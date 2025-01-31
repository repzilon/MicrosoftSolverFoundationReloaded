using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class ExpTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Exp;

		internal ExpTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Exp;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Exp(inputs[0].ToDouble());
		}
	}
}
