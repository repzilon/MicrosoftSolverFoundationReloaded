using System.Diagnostics;

namespace Microsoft.SolverFoundation.Solvers
{
	[DebuggerDisplay("ForwardDecision{_var.ToString()} : {_var._key.ToString()}")]
	internal class ForwardDecision : BranchingDecision
	{
		internal override CspSolverDomain Guesses
		{
			get
			{
				CspSolverDomain finiteValue = _var.FiniteValue;
				finiteValue.Intersect(finiteValue.First, _value, out var newD);
				return newD;
			}
		}

		internal ForwardDecision(TreeSearchSolver decider, CspSolverTerm var, int baselineChange, int oldDepth)
			: base(decider, var, baselineChange, oldDepth)
		{
		}

		internal override bool IsFinal()
		{
			Backtrack();
			return _var.FiniteValue.Last <= _value;
		}

		internal override bool TryNextValue()
		{
			Backtrack();
			CspSolverDomain finiteValue = _var.FiniteValue;
			if (_value < finiteValue.Last)
			{
				_value = finiteValue.Succ(_value);
				_var.Restrain(CspIntervalDomain.Create(_value, _value));
				return true;
			}
			return false;
		}
	}
}
