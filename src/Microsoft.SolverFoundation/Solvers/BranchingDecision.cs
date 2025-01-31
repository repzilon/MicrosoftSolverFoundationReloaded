namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class BranchingDecision
	{
		protected CspSolverTerm _var;

		protected ConstraintSystem _solver;

		protected TreeSearchSolver _decider;

		protected int _baselineChange;

		protected int _oldDepth;

		protected int _value;

		internal CspSolverTerm Term => _var;

		internal int OldDepth => _oldDepth;

		internal int BaselineChange => _baselineChange;

		internal virtual int Value
		{
			get
			{
				return _value;
			}
			set
			{
				_value = value;
				if (_var.Contains(value))
				{
					_var.Restrain(CspIntervalDomain.Create(_value, _value));
				}
			}
		}

		internal abstract CspSolverDomain Guesses { get; }

		internal BranchingDecision(TreeSearchSolver decider, CspSolverTerm var, int baselineChange, int oldDepth)
		{
			_var = var;
			_solver = var.InnerSolver;
			_decider = decider;
			_baselineChange = baselineChange;
			_oldDepth = oldDepth;
			_value = int.MinValue;
		}

		internal virtual void Backtrack()
		{
			_solver.Backtrack(_baselineChange);
		}

		internal abstract bool IsFinal();

		internal abstract bool TryNextValue();
	}
}
