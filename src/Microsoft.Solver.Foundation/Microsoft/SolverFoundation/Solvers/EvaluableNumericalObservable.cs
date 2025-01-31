using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A term whose value is always maintained equal to the value
	/// of another term T, and which informs an observer of the changes of value of T.
	/// </summary>
	internal sealed class EvaluableNumericalObservable : EvaluableUnaryNumericalTerm
	{
		private readonly IEvaluationObserver _observer;

		public EvaluableNumericalObservable(EvaluableNumericalTerm input, IEvaluationObserver obs)
			: base(input)
		{
			_observer = obs;
		}

		internal override void Reinitialize(out bool change)
		{
			change = _value != Input.Value;
			_value = Input.Value;
		}

		internal override void Recompute(out bool change)
		{
			change = _value != Input.Value;
			if (change)
			{
				_observer.ValueChange(Input, _value, Input.Value);
				_value = Input.Value;
			}
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableNumericalTerm input) => new EvaluableNumericalObservable(input, _observer));
		}
	}
}
