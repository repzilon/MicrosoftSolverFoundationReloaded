using System.Diagnostics;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class PlusTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Plus;

		internal override bool IsAssociativeAndCommutative
		{
			[DebuggerStepThrough]
			get
			{
				return true;
			}
		}

		internal PlusTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
			TermStructure termStructure = OperatorTerm.InputStructure(inputs, TermStructure.Linear | TermStructure.Quadratic | TermStructure.Differentiable);
			_structure |= termStructure;
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Plus;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			Rational result = 0;
			foreach (Rational rational in inputs)
			{
				result += rational;
			}
			return result;
		}
	}
}
