using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term of the form F(X), where X is a numerical term and 
	/// F is a binary (two-argument) real-valued function
	/// </summary>
	/// <remarks>
	/// Intended for functions that require some amount of computation
	/// such as Pow
	/// </remarks>
	internal sealed class EvaluableBinaryNumericalFunction : EvaluableBinaryNumericalTerm
	{
		private readonly TermModelOperation _op;

		private readonly Func<double, double, double> _fun;

		internal override TermModelOperation Operation => _op;

		internal EvaluableBinaryNumericalFunction(Func<double, double, double> fun, EvaluableNumericalTerm input1, EvaluableNumericalTerm input2, TermModelOperation op)
			: base(input1, input2)
		{
			_fun = fun;
			_op = op;
		}

		internal override void Recompute(out bool change)
		{
			double num = _fun(Input1.Value, Input2.Value);
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm newInput1, EvaluableNumericalTerm newInput2) => new EvaluableBinaryNumericalFunction(_fun, newInput1, newInput2, _op));
		}
	}
}
