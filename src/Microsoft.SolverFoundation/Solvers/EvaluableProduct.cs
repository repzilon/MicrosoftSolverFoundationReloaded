using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A product of the form X1*X2*..., where X is a vector of numerical terms
	/// </summary>
	internal sealed class EvaluableProduct : EvaluableNaryNumericalTerm
	{
		internal override TermModelOperation Operation => TermModelOperation.Times;

		internal EvaluableProduct(EvaluableNumericalTerm[] args)
			: base(args)
		{
		}

		internal override void Recompute(out bool change)
		{
			double num = 1.0;
			for (int num2 = Inputs.Length - 1; num2 >= 0; num2--)
			{
				num *= Inputs[num2].Value;
			}
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm[] newInputs) => new EvaluableProduct(newInputs));
		}
	}
}
