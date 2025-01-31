using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term of the form X or Y, where X and Y are Boolean terms
	/// </summary>
	internal sealed class EvaluableBinaryOr : EvaluableBinaryBooleanTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.Or;

		internal EvaluableBinaryOr(EvaluableBooleanTerm input1, EvaluableBooleanTerm input2)
			: base(input1, input2)
		{
		}

		public static double Apply(double l, double r)
		{
			return (l < 0.0 && r < 0.0) ? (l + r) : Math.Min(l, r);
		}

		internal override void Recompute(out bool change)
		{
			double num = Apply(Input1.Violation, Input2.Violation);
			change = _violation != num;
			_violation = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableBooleanTerm newInput1, EvaluableBooleanTerm newInput2) => new EvaluableBinaryOr(newInput1, newInput2));
		}
	}
}
