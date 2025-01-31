using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term of the form X lessStrict Y where X and Y are numerical terms
	/// </summary>
	internal sealed class EvaluableBinaryLessStrict : EvaluableBooleanTerm
	{
		private readonly EvaluableNumericalTerm Input1;

		private readonly EvaluableNumericalTerm Input2;

		internal override TermModelOperation Operation => TermModelOperation.Less;

		internal EvaluableBinaryLessStrict(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
			: base(1 + Math.Max(input1.Depth, input2.Depth))
		{
			Input1 = input1;
			Input2 = input2;
		}

		public static double Apply(double x, double y)
		{
			double num = x - y;
			if (num >= 0.0)
			{
				num += 1.0;
			}
			return num;
		}

		internal override void Recompute(out bool change)
		{
			double num = Apply(Input1.Value, Input2.Value);
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
				return new EvaluableBinaryLessStrict(((EvaluableNumericalTerm)value) ?? Input1, ((EvaluableNumericalTerm)value2) ?? Input2);
			}
			return null;
		}
	}
}
