using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Conversion Term: use when a Boolean term should be interpreted as an integer
	/// </summary>
	/// <remarks>
	/// Make sure to cache those terms: at most one should be constructed
	/// for any boolean term, and their construction should be rare
	/// </remarks>
	internal sealed class EvaluableConversionBooleanToNumerical : EvaluableNumericalTerm
	{
		private readonly EvaluableBooleanTerm _actualTerm;

		internal EvaluableConversionBooleanToNumerical(EvaluableBooleanTerm x)
			: base(x.Depth + 1)
		{
			_actualTerm = x;
		}

		internal override void Recompute(out bool change)
		{
			int num = (_actualTerm.Value ? 1 : 0);
			change = _value != (double)num;
			_value = num;
		}

		internal override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return EvaluationStatics.Enumerate((EvaluableTerm)_actualTerm);
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			if (!map.TryGetValue(_actualTerm, out var value))
			{
				value = _actualTerm.Substitute(map);
			}
			if (value != null)
			{
				return new EvaluableConversionBooleanToNumerical((EvaluableBooleanTerm)value);
			}
			return null;
		}
	}
}
