using System.Diagnostics;

namespace Microsoft.SolverFoundation.Solvers
{
	[DebuggerDisplay("GoalDecisionLinear{_var.ToString()} : {_var._key.ToString()}")]
	internal class GoalDecisionLinear : BranchingDecision
	{
		internal override CspSolverDomain Guesses
		{
			get
			{
				CspSolverDomain finiteValue = _var.FiniteValue;
				finiteValue.Intersect(_value, finiteValue.Last, out var newD);
				return newD;
			}
		}

		internal GoalDecisionLinear(TreeSearchSolver decider, CspSolverTerm var, int baselineChange, int oldDepth)
			: base(decider, var, baselineChange, oldDepth)
		{
		}

		internal override bool IsFinal()
		{
			Backtrack();
			return _var.FiniteValue.First >= _value;
		}

		internal override bool TryNextValue()
		{
			_value = _var.FiniteValue.Last;
			Backtrack();
			CspSolverDomain finiteValue = _var.FiniteValue;
			if (_value > finiteValue.First)
			{
				_value = finiteValue.Pred(_value);
				_var.Restrain(CspIntervalDomain.Create(_var.First, _value));
				return true;
			}
			return false;
		}
	}
}
