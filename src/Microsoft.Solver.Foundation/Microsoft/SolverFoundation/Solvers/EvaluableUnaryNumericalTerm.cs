using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Root class for numerical terms that have a single numerical input
	/// </summary>
	internal abstract class EvaluableUnaryNumericalTerm : EvaluableNumericalTerm
	{
		public readonly EvaluableNumericalTerm Input;

		internal EvaluableUnaryNumericalTerm(EvaluableNumericalTerm input)
			: base(1 + input.Depth)
		{
			Input = input;
		}

		internal sealed override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return EvaluationStatics.Enumerate((EvaluableTerm)Input);
		}

		protected EvaluableUnaryNumericalTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map, Func<EvaluableNumericalTerm, EvaluableUnaryNumericalTerm> constructor)
		{
			if (!map.TryGetValue(Input, out var value))
			{
				value = Input.Substitute(map);
			}
			if (value != null)
			{
				return constructor((EvaluableNumericalTerm)value);
			}
			return null;
		}
	}
}
