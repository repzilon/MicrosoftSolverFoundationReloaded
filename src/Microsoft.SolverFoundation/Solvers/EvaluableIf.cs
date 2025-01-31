using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A product of the form If(Cond, caseTrue, caseFalse) 
	/// where Cond is a Boolean term, and caseTrue and caseFalse are numerical terms
	/// </summary>
	internal sealed class EvaluableIf : EvaluableNumericalTerm
	{
		private readonly EvaluableBooleanTerm _condition;

		private readonly EvaluableNumericalTerm _caseTrue;

		private readonly EvaluableNumericalTerm _caseFalse;

		internal override TermModelOperation Operation => TermModelOperation.If;

		internal EvaluableIf(EvaluableBooleanTerm condition, EvaluableNumericalTerm caseTrue, EvaluableNumericalTerm caseFalse)
			: base(1 + EvaluableTerm.MaxDepth(condition, caseTrue, caseFalse))
		{
			_condition = condition;
			_caseTrue = caseTrue;
			_caseFalse = caseFalse;
		}

		internal override void Recompute(out bool change)
		{
			double num = (_condition.Value ? _caseTrue.Value : _caseFalse.Value);
			change = _value != num;
			_value = num;
		}

		internal override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return EvaluationStatics.Enumerate<EvaluableTerm>(_condition, _caseTrue, _caseFalse);
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			if (!map.TryGetValue(_condition, out var value))
			{
				value = _condition.Substitute(map);
			}
			if (!map.TryGetValue(_caseTrue, out var value2))
			{
				value2 = _caseTrue.Substitute(map);
			}
			if (!map.TryGetValue(_caseFalse, out var value3))
			{
				value3 = _caseFalse.Substitute(map);
			}
			if (value != null || value2 != null || value3 != null)
			{
				return new EvaluableIf(((EvaluableBooleanTerm)value) ?? _condition, ((EvaluableNumericalTerm)value2) ?? _caseTrue, ((EvaluableNumericalTerm)value3) ?? _caseFalse);
			}
			return null;
		}
	}
}
