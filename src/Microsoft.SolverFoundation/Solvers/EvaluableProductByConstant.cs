using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A product of the form X * Y, where X and Y are numerical terms
	/// Specialized for the case where Y is Constant
	/// </summary>
	internal sealed class EvaluableProductByConstant : EvaluableUnaryNumericalTerm
	{
		private readonly double _coef;

		internal override TermModelOperation Operation => TermModelOperation.Times;

		internal EvaluableProductByConstant(double input1, EvaluableNumericalTerm input2)
			: base(input2)
		{
			_coef = input1;
		}

		internal override void Recompute(out bool change)
		{
			double num = _coef * Input.Value;
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm input) => new EvaluableProductByConstant(_coef, input));
		}
	}
}
