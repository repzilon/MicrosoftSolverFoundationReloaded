using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class CosTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Cos;

		internal CosTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Cos;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Cos(inputs[0].ToDouble());
		}
	}
}
