using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class ArcSinTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.ArcSin;

		internal ArcSinTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.ArcSin;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Asin(inputs[0].ToDouble());
		}
	}
}
