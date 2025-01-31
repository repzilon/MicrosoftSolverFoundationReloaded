using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A product of the form X * Y, where X and Y are numerical terms
	/// </summary>
	internal sealed class EvaluableBinaryProduct : EvaluableBinaryNumericalTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.Times;

		internal EvaluableBinaryProduct(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
			: base(input1, input2)
		{
		}

		internal override void Recompute(out bool change)
		{
			double num = Input1.Value * Input2.Value;
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm input1, EvaluableNumericalTerm input2) => new EvaluableBinaryProduct(input1, input2));
		}
	}
}
