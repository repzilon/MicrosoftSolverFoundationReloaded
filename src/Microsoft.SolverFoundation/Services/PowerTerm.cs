using System;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class PowerTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Power;

		internal PowerTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			if (inputs[1] is ConstantTerm && inputs[1].TryEvaluateConstantValue(out Rational value, new EvaluationContext()))
			{
				if (value == 0)
				{
					_structure |= TermStructure.Constant | TermStructure.Linear | TermStructure.Quadratic;
				}
				else if (value == 1)
				{
					_structure |= inputs[0]._structure;
				}
				else if (value == 2 && inputs[0].HasStructure(TermStructure.Linear))
				{
					_structure |= TermStructure.Quadratic;
				}
			}
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Power;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			if (Rational.Power(inputs[0], inputs[1], out var ratRes))
			{
				return ratRes;
			}
			return Math.Pow((double)inputs[0], (double)inputs[1]);
		}
	}
}
