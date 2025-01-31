using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A difference of the form X - Y, where X and Y are numerical terms
	/// </summary>
	internal sealed class EvaluableMinus : EvaluableBinaryNumericalTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.Minus;

		internal EvaluableMinus(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
			: base(input1, input2)
		{
		}

		internal override void Recompute(out bool change)
		{
			double num = Input1.Value - Input2.Value;
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm newInput1, EvaluableNumericalTerm newInput2) => new EvaluableMinus(newInput1, newInput2));
		}
	}
}
