using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Root class for Boolean terms that have two Boolean inputs
	/// </summary>
	internal abstract class EvaluableBinaryBooleanTerm : EvaluableBooleanTerm
	{
		public readonly EvaluableBooleanTerm Input1;

		public readonly EvaluableBooleanTerm Input2;

		internal EvaluableBinaryBooleanTerm(EvaluableBooleanTerm input1, EvaluableBooleanTerm input2)
			: base(1 + Math.Max(input1.Depth, input2.Depth))
		{
			Input1 = input1;
			Input2 = input2;
		}

		internal sealed override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return EvaluationStatics.Enumerate((EvaluableTerm)Input1, (EvaluableTerm)Input2);
		}

		internal override IEnumerable<EvaluableTerm> EnumerateMoveCandidates()
		{
			if (Input1.Value == base.Value)
			{
				yield return Input1;
			}
			if (Input2.Value == base.Value)
			{
				yield return Input2;
			}
		}

		protected EvaluableBinaryBooleanTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map, Func<EvaluableBooleanTerm, EvaluableBooleanTerm, EvaluableBinaryBooleanTerm> constructor)
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
				return constructor(((EvaluableBooleanTerm)value) ?? Input1, ((EvaluableBooleanTerm)value2) ?? Input2);
			}
			return null;
		}
	}
}
