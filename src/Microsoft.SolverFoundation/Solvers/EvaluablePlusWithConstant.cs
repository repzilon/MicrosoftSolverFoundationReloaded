using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A sum of the form X + Y, where X and Y are numerical terms
	/// Specialized for the case where Y is Constant
	/// </summary>
	internal sealed class EvaluablePlusWithConstant : EvaluableUnaryNumericalTerm
	{
		private readonly double _constant;

		internal override TermModelOperation Operation => TermModelOperation.Plus;

		internal EvaluablePlusWithConstant(EvaluableNumericalTerm input, double constant)
			: base(input)
		{
			_constant = constant;
		}

		internal override void Recompute(out bool change)
		{
			double num = Input.Value + _constant;
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm input) => new EvaluablePlusWithConstant(input, _constant));
		}
	}
}
