using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint on a list of Boolean variables, imposing that at
	///   least one of the Booleans be true
	/// </summary>
	/// <remarks>
	///   This is a copy-cut of the Pseudo-Boolean, simplified and specialized 
	///   for a minimum sum of one. Not yet a good specialized (SAT-like)
	///   implementation using watching, needless to say.
	/// </remarks>
	internal class LargeClause : NaryConstraint<BooleanVariable>
	{
		private Backtrackable<int> _nbFalse;

		public LargeClause(Problem p, BooleanVariable[] tab)
			: base(p, tab)
		{
			int num = tab.Length;
			Array.Copy(tab, _args, num);
			_nbFalse = new Backtrackable<int>(p.IntTrail, 0);
			for (int i = 0; i < num; i++)
			{
				_args[i].SubscribeToFalse(WhenFalse);
			}
		}

		protected bool WhenFalse()
		{
			int num = _nbFalse.Value + 1;
			if (num >= _args.Length - 1 && !UnitPropagate())
			{
				return false;
			}
			_nbFalse.Value = num;
			return true;
		}

		protected bool UnitPropagate()
		{
			int num = _args.Length;
			for (int i = 0; i < num; i++)
			{
				BooleanVariable booleanVariable = _args[i];
				switch (booleanVariable.Status)
				{
				case BooleanVariableState.Unassigned:
					return booleanVariable.ImposeValue(b: true, base.Cause);
				case BooleanVariableState.True:
					return true;
				}
			}
			return _args[0].ImposeEmptyDomain(base.Cause);
		}
	}
}
