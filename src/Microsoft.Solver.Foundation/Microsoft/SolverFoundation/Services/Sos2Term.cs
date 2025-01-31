using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal sealed class Sos2Term : OperatorTerm
	{
		internal override Operator Operation => Operator.Sos2;

		internal Sos2Term(Term input)
			: base(new Term[1] { input }, TermValueClass.Numeric)
		{
			if (input.HasStructure(TermStructure.LinearInequality))
			{
				_structure |= TermStructure.Sos2;
			}
			if (input.HasStructure(TermStructure.Linear))
			{
				_structure |= TermStructure.Sos2;
			}
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

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			throw new NotImplementedException();
		}
	}
}
