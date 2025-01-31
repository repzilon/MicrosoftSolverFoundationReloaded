using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal sealed class Sos1Term : OperatorTerm
	{
		internal override Operator Operation => Operator.Sos1;

		internal Sos1Term(Term input)
			: base(new Term[1] { input }, TermValueClass.Numeric)
		{
			if (input.HasStructure(TermStructure.Linear))
			{
				_structure |= TermStructure.Sos1;
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
