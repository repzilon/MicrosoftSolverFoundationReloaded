using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A sum of the form sum_i X[i]
	/// where X is a vector of numerical terms.
	/// </summary>
	internal sealed class EvaluableSimpleSum : EvaluableSum
	{
		internal override TermModelOperation Operation => TermModelOperation.Plus;

		internal EvaluableSimpleSum(EvaluableNumericalTerm[] inputs)
			: base(inputs)
		{
		}

		internal override void Recompute(out bool change)
		{
			double num = 0.0;
			for (int num2 = _inputs.Length - 1; num2 >= 0; num2--)
			{
				num += _inputs[num2].Value;
			}
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm[] newInputs) => new EvaluableSimpleSum(newInputs));
		}
	}
}
