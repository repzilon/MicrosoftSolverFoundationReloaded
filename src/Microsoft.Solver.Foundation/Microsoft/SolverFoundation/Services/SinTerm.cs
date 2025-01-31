using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class SinTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Sin;

		internal SinTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Sin;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Sin(inputs[0].ToDouble());
		}
	}
}
