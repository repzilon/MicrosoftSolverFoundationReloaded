using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A product of the form Max(X1, X2...) where X is a vector of numerical terms
	/// </summary>
	internal sealed class EvaluableMax : EvaluableNaryNumericalTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.Max;

		internal EvaluableMax(EvaluableNumericalTerm[] inputs)
			: base(inputs)
		{
		}

		internal override void Recompute(out bool change)
		{
			double num = double.MinValue;
			for (int num2 = Inputs.Length - 1; num2 >= 0; num2--)
			{
				num = Math.Max(num, Inputs[num2].Value);
			}
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm[] newInputs) => new EvaluableMax(newInputs));
		}
	}
}
