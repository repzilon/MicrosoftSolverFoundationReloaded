using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term Identity of X where X is a Boolean term
	/// </summary>
	internal sealed class EvaluableBooleanIdentity : EvaluableUnaryBooleanTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.Identity;

		internal EvaluableBooleanIdentity(EvaluableBooleanTerm input)
			: base(input)
		{
		}

		internal override void Recompute(out bool change)
		{
			change = _violation != Input.Violation;
			_violation = Input.Violation;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableBooleanTerm input) => new EvaluableBooleanIdentity(input));
		}
	}
}
