using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term of the form F(X), where X is a numerical term and 
	/// F is a function from double to double
	/// </summary>
	/// <remarks>
	/// Intended for functions that require some amount of computation
	/// such as Exp, cosine. For those functions the overhead of an
	/// extra delegate call in addition to the virtual Recompute call
	/// is probably fine; and the gain in factoring all of them with
	/// one type of term is clear.
	/// </remarks>
	internal sealed class EvaluableUnaryNumericalFunction : EvaluableUnaryNumericalTerm
	{
		private readonly TermModelOperation _op;

		private readonly Func<double, double> _fun;

		internal override TermModelOperation Operation => _op;

		internal EvaluableUnaryNumericalFunction(Func<double, double> fun, EvaluableNumericalTerm arg, TermModelOperation op)
			: base(arg)
		{
			_fun = fun;
			_op = op;
		}

		internal override void Recompute(out bool change)
		{
			double num = _fun(Input.Value);
			change = _value != num;
			_value = num;
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm input) => new EvaluableUnaryNumericalFunction(_fun, input, _op));
		}
	}
}
