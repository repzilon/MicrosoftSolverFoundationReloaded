using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term Identity of X where X is a numerical term
	/// </summary>
	internal sealed class EvaluableUnaryIdentity : EvaluableUnaryNumericalTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.Identity;

		internal EvaluableUnaryIdentity(EvaluableNumericalTerm input)
			: base(input)
		{
		}

		internal override void Recompute(out bool change)
		{
			double value = Input.Value;
			change = _value != value;
			_value = value;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm input) => new EvaluableUnaryIdentity(input));
		}
	}
}
