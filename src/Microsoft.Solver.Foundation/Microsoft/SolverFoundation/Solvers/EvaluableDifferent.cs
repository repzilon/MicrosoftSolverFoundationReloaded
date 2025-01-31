using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term of the form X notEqualTo Y where X and Y are numerical terms
	/// </summary>
	internal sealed class EvaluableDifferent : EvaluableBooleanTerm
	{
		private readonly EvaluableNumericalTerm Input1;

		private readonly EvaluableNumericalTerm Input2;

		internal override TermModelOperation Operation => TermModelOperation.Unequal;

		internal EvaluableDifferent(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
			: base(1 + Math.Max(input1.Depth, input2.Depth))
		{
			Input1 = input1;
			Input2 = input2;
		}

		internal override void Recompute(out bool change)
		{
			double num = 0.0 - Math.Abs(Input1.Value - Input2.Value);
			if (num == 0.0)
			{
				num = 1.0;
			}
			change = _violation != num;
			_violation = num;
		}

		internal override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return EvaluationStatics.Enumerate((EvaluableTerm)Input1, (EvaluableTerm)Input2);
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			if (!map.TryGetValue(Input1, out var value))
			{
				value = Input1.Substitute(map);
			}
			if (!map.TryGetValue(Input2, out var value2))
			{
				value2 = Input2.Substitute(map);
			}
			if (value != null || value2 != null)
			{
				return new EvaluableDifferent(((EvaluableNumericalTerm)value) ?? Input1, ((EvaluableNumericalTerm)value2) ?? Input2);
			}
			return null;
		}
	}
}
