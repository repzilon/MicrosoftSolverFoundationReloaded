using System.Diagnostics;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class MinTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Min;

		internal override bool IsAssociativeAndCommutative
		{
			[DebuggerStepThrough]
			get
			{
				return true;
			}
		}

		internal MinTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Min;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			Rational rational = Rational.PositiveInfinity;
			foreach (Rational rational2 in inputs)
			{
				if (rational > rational2)
				{
					rational = rational2;
				}
			}
			return rational;
		}
	}
}
