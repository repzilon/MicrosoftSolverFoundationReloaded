using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term of the form not(X), where X is a Boolean term
	/// </summary>
	internal sealed class EvaluableNot : EvaluableUnaryBooleanTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.Not;

		internal EvaluableNot(EvaluableBooleanTerm input)
			: base(input)
		{
		}

		internal override void Recompute(out bool change)
		{
			change = _violation != 0.0 - Input.Violation;
			_violation = 0.0 - Input.Violation;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableBooleanTerm input) => new EvaluableNot(input));
		}
	}
}
