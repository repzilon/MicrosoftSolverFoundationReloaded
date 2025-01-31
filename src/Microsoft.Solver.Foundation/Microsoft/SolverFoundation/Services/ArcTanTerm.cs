using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class ArcTanTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.ArcTan;

		internal ArcTanTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.ArcTan;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Atan(inputs[0].ToDouble());
		}
	}
}
