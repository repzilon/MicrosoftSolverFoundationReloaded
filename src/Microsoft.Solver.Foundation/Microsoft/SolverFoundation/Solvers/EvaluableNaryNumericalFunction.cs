using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term of the form F(X), where X is a numerical term and 
	/// F is an n-ary real-valued function
	/// </summary>
	internal sealed class EvaluableNaryNumericalFunction : EvaluableNaryNumericalTerm
	{
		private Func<double[], double> _fun;

		private double[] _x;

		internal override TermModelOperation Operation => TermModelOperation.Function;

		internal EvaluableNaryNumericalFunction(Func<double[], double> fun, EvaluableNumericalTerm[] args)
			: base(args)
		{
			_fun = fun;
			_x = Inputs.Select((EvaluableNumericalTerm x) => x.Value).ToArray();
		}

		internal override void Recompute(out bool change)
		{
			for (int i = 0; i < Inputs.Length; i++)
			{
				_x[i] = Inputs[i].Value;
			}
			double num = _fun(_x);
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm[] newInputs) => new EvaluableNaryNumericalFunction(_fun, newInputs));
		}
	}
}
