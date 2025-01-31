using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class TimesTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Times;

		internal TimesTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			int num = 0;
			for (int i = 0; i < inputs.Length; i++)
			{
				if (inputs[i].HasStructure(TermStructure.Multivalue))
				{
					num += 100;
				}
				num = (inputs[i].HasStructure(TermStructure.Constant) ? num : (inputs[i].HasStructure(TermStructure.Linear) ? (num + 1) : ((!inputs[i].HasStructure(TermStructure.Quadratic)) ? (num + 100) : (num + 2))));
				if (num > 2)
				{
					break;
				}
			}
			if (num <= 1)
			{
				_structure |= TermStructure.Linear;
			}
			if (num <= 2)
			{
				_structure |= TermStructure.Quadratic;
			}
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Times;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			Rational result = 1;
			foreach (Rational rational in inputs)
			{
				result *= rational;
			}
			return result;
		}
	}
}
