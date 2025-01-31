using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Root class for Boolean terms that have a Boolean numerical input
	/// </summary>
	internal abstract class EvaluableUnaryBooleanTerm : EvaluableBooleanTerm
	{
		public readonly EvaluableBooleanTerm Input;

		internal EvaluableUnaryBooleanTerm(EvaluableBooleanTerm input)
			: base(1 + input.Depth)
		{
			Input = input;
		}

		internal sealed override IEnumerable<EvaluableTerm> EnumerateInputs()
		{
			return EvaluationStatics.Enumerate((EvaluableTerm)Input);
		}

		protected EvaluableUnaryBooleanTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map, Func<EvaluableBooleanTerm, EvaluableUnaryBooleanTerm> constructor)
		{
			if (!map.TryGetValue(Input, out var value))
			{
				value = Input.Substitute(map);
			}
			if (value != null)
			{
				return constructor((EvaluableBooleanTerm)value);
			}
			return null;
		}
	}
}
