using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Conversion Term: use when a numerical should be interpreted as a Boolean
	/// </summary>
	/// <remarks>
	/// Make sure to cache those terms: at most one should be constructed
	/// for any boolean term, and their construction should be rare
	/// </remarks>
	internal sealed class EvaluableConversionNumericalToBoolean : EvaluableBooleanTerm
	{
		private readonly EvaluableNumericalTerm _actualTerm;

		internal EvaluableConversionNumericalToBoolean(EvaluableNumericalTerm x)
			: base(x.Depth + 1)
		{
			_actualTerm = x;
		}

		internal override void Recompute(out bool change)
		{
			double num;
			if (_actualTerm.Value == 0.0)
			{
				num = 1.0;
			}
			else
			{
				if (_actualTerm.Value != 1.0)
				{
					throw new InvalidOperationException(Resources.NonBooleanInputs);
				}
				num = -1.0;
			}
			change = _violation != num;
			_violation = num;
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
				return new EvaluableConversionNumericalToBoolean((EvaluableNumericalTerm)value);
			}
			return null;
		}
	}
}
