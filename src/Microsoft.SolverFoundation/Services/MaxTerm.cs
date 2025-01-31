using System.Diagnostics;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Rewrite;

namespace Microsoft.SolverFoundation.Services
{
	internal class MaxTerm : OperatorTerm
	{
		internal override Operator Operation => Operator.Max;

		internal override bool IsAssociativeAndCommutative
		{
			[DebuggerStepThrough]
			get
			{
				return true;
			}
		}

		internal MaxTerm(Term[] inputs, TermValueClass valueClass)
			: base(inputs, valueClass)
		{
		}

		protected override Symbol GetHeadSymbol(RewriteSystem rs)
		{
			return rs.Builtin.Max;
		}

		internal override Rational Evaluate(Rational[] inputs)
		{
			Rational rational = Rational.NegativeInfinity;
			foreach (Rational rational2 in inputs)
			{
				if (rational < rational2)
				{
					rational = rational2;
				}
			}
			return rational;
		}
	}
}
