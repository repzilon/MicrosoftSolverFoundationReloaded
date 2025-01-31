using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Root class for numerical terms that have two numerical inputs
	/// </summary>
	internal abstract class EvaluableBinaryNumericalTerm : EvaluableNumericalTerm
	{
		public readonly EvaluableNumericalTerm Input1;

		public readonly EvaluableNumericalTerm Input2;

		internal EvaluableBinaryNumericalTerm(EvaluableNumericalTerm input1, EvaluableNumericalTerm input2)
			: base(1 + Math.Max(input1.Depth, input2.Depth))
		{
			Input1 = input1;
			Input2 = input2;
		}

		internal sealed override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return EvaluationStatics.Enumerate((EvaluableTerm)Input1, (EvaluableTerm)Input2);
		}

		protected EvaluableBinaryNumericalTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map, Func<EvaluableNumericalTerm, EvaluableNumericalTerm, EvaluableBinaryNumericalTerm> constructor)
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
				return constructor(((EvaluableNumericalTerm)value) ?? Input1, ((EvaluableNumericalTerm)value2) ?? Input2);
			}
			return null;
		}
	}
}
