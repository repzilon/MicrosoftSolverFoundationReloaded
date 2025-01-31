using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class Sos2RowTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Sos2Row;

		internal Sos2RowTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Linear);
			if ((termStructure & TermStructure.Linear) != 0)
			{
				_structure |= TermStructure.Sos2;
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
