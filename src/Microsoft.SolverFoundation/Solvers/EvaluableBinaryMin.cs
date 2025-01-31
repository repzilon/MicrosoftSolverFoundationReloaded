using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A sum of the form Min(X, Y), where X and Y are numerical terms
	/// </summary>
	internal sealed class EvaluableBinaryMin : EvaluableBinaryNumericalTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.Min;

		internal EvaluableBinaryMin(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
			: base(input1, input2)
		{
		}

		internal override void Recompute(out bool change)
		{
			double num = Math.Min(Input1.Value, Input2.Value);
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm newInput1, EvaluableNumericalTerm newInput2) => new EvaluableBinaryMin(newInput1, newInput2));
		}
	}
}
