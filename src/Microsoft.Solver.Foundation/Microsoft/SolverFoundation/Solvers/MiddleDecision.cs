using System;
using System.Diagnostics;

namespace Microsoft.SolverFoundation.Solvers
{
	[DebuggerDisplay("ForwardDecision{_var.ToString()} : {_var._key.ToString()}")]
	internal class MiddleDecision : BranchingDecision
	{
		private bool _evenDomain;

		private int _step;

		private int _direction;

		private int _valId;

		private int _middleValId;

		internal override CspSolverDomain Guesses
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		internal MiddleDecision(TreeSearchSolver decider, CspSolverTerm var, int baselineChange, int oldDepth)
			: base(decider, var, baselineChange, oldDepth)
		{
			_evenDomain = var.FiniteValue.Count % 2 == 0;
			_step = 0;
			_direction = 1;
			_valId = (_middleValId = var.FiniteValue.Count / 2);
		}

		internal override bool IsFinal()
		{
			Backtrack();
			if (_evenDomain)
			{
				return _valId == 0;
			}
			return _valId == _var.FiniteValue.Count - 1;
		}

		internal override bool TryNextValue()
		{
			Backtrack();
			CspSolverDomain finiteValue = _var.FiniteValue;
			if ((_evenDomain && _valId != 0) || (!_evenDomain && _valId != finiteValue.Count - 1))
			{
				_valId = _middleValId + _step * _direction;
				_value = finiteValue[_valId];
				_var.Restrain(CspIntervalDomain.Create(_value, _value));
				if (_direction == -1)
				{
					_direction = 1;
				}
				else
				{
					_direction = -1;
					_step++;
				}
				return true;
			}
			return false;
		}
	}
}
