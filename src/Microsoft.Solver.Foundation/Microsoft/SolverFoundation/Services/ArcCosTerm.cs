using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class ArcCosTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.ArcCos;

		internal ArcCosTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.ArcCos;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Acos(inputs[0].ToDouble());
		}
	}
}
