using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A sum of the form sum_i COEF[i]*X[i] 
	/// where X is a vector of numerical terms, and the coefficients are integers
	/// </summary>
	internal sealed class EvaluableWeightedIntSum : EvaluableSum
	{
		private readonly int[] _coefficients;

		internal override TermModelOperation Operation => TermModelOperation.Plus;

		/// <summary>
		/// A sum of the form sum_i coef[i]*x[i]
		/// </summary>
		internal EvaluableWeightedIntSum(int[] coefs, EvaluableNumericalTerm[] inputs)
			: base(inputs)
		{
			_coefficients = coefs;
		}

		internal override void Recompute(out bool change)
		{
			double num = 0.0;
			for (int num2 = _inputs.Length - 1; num2 >= 0; num2--)
			{
				num += (double)_coefficients[num2] * _inputs[num2].Value;
			}
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm[] newInputs) => new EvaluableWeightedIntSum(_coefficients, newInputs));
		}
	}
}
