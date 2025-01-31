using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class Sos1RowTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Sos1Row;

		internal Sos1RowTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Linear);
			if ((termStructure & TermStructure.Linear) != 0)
			{
				_structure |= TermStructure.Sos1;
			}
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			throw new NotImplementedException();
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			throw new NotImplementedException();
		}

		internal override bool TryEvaluateConstantValue(out Rational value, EvaluationContext context)
		{
			value = 0;
			return false;
		}
	}
}
