using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term Abs(X), where X is a numerical term
	/// </summary>
	internal sealed class EvaluableAbs : EvaluableUnaryNumericalTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.Abs;

		internal EvaluableAbs(EvaluableNumericalTerm input)
			: base(input)
		{
		}

		internal override void Recompute(out bool change)
		{
			double num = Math.Abs(Input.Value);
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm input) => new EvaluableAbs(input));
		}
	}
}
