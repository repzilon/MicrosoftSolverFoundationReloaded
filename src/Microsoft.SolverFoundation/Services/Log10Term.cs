using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class Log10Term : OperatorTerm
	{
		internal override Operator Operation => Operator.Log10;

		internal Log10Term(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Log10;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Log10(inputs[0].ToDouble());
		}
	}
}
