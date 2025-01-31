using System.Diagnostics;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class OrTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Or;

		internal override bool IsAssociativeAndCommutative
		{
			[DebuggerStepThrough]
			get
			{
				return true;
			}
		}

		internal OrTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = TermStructure.Constant | TermStructure.Linear | TermStructure.LinearConstraint | TermStructure.Differentiable | TermStructure.DifferentiableConstraint;
			int num = 0;
			for (int i = 0; i < inputs.Length; i++)
			{
				if (!inputs[i].HasStructure(TermStructure.Constant))
				{
					num++;
					TermStructure termStructure2 = inputs[i].Structure;
					if ((termStructure2 & TermStructure.Linear) != 0)
					{
						termStructure2 |= TermStructure.LinearConstraint;
					}
					if ((termStructure2 & TermStructure.Differentiable) != 0)
					{
						termStructure2 |= TermStructure.DifferentiableConstraint;
					}
					termStructure &= termStructure2;
				}
			}
			if (num <= 1)
			{
				_structure |= termStructure;
			}
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Or;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			foreach (Rational rational in inputs)
			{
				if (rational != 0)
				{
					return 1;
				}
			}
			return 0;
		}
	}
}
