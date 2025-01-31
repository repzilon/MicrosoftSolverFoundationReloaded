using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term -X where X is a numerical term
	/// </summary>
	internal sealed class EvaluableUnaryMinus : EvaluableUnaryNumericalTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.Minus;

		internal EvaluableUnaryMinus(EvaluableNumericalTerm input)
			: base(input)
		{
		}

		internal override void Recompute(out bool change)
		{
			double num = 0.0 - Input.Value;
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm input) => new EvaluableUnaryMinus(input));
		}
	}
}
