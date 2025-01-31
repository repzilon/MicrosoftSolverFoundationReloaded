using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class LogTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Log;

		internal LogTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Log;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			return Math.Log(inputs[0].ToDouble());
		}
	}
}
