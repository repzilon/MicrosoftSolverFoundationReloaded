using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A Boolean term whose violation is always maintained equal to the violation
	/// of another term T, and which informs an observer of changes of violation of T.
	/// </summary>
	internal sealed class EvaluableBooleanObservable : EvaluableUnaryBooleanTerm
	{
		private readonly IEvaluationObserver _observer;

		public EvaluableBooleanObservable(EvaluableBooleanTerm input, IEvaluationObserver obs)
			: base(input)
		{
			_observer = obs;
		}

		internal override void Reinitialize(out bool change)
		{
			change = _violation != Input.Violation;
			_violation = Input.Violation;
		}

		internal override void Recompute(out bool change)
		{
			change = _violation != Input.Violation;
			if (change)
			{
				_observer.ValueChange(Input, _violation, Input.Violation);
				_violation = Input.Violation;
			}
		}

		public override EvaluableTerm Substitute(Dictionary<EvaluableTerm, EvaluableTerm> map)
		{
			return Substitute(map, (EvaluableBooleanTerm input) => new EvaluableBooleanObservable(input, _observer));
		}
	}
}
