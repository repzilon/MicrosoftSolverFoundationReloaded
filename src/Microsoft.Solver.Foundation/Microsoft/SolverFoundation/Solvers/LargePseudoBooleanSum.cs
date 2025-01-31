using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between a list of Boolean variables and an Integer
	///   variable S, imposing that S represent the number of variables true in 
	///   the list.
	/// </summary>
	/// <remarks>
	///   An improved implementation would probably use watching
	/// </remarks>
	internal class LargePseudoBooleanSum : DisolverConstraint
	{
		private BooleanVariable[] _vars;

		private IntegerVariable _sum;

		private BacktrackableIntSet _idxUnassignedVars;

		private Backtrackable<int> _nbTrue;

		private Backtrackable<int> _nbFalse;

		private int MinSum => _nbTrue.Value;

		private int MaxSum => _vars.Length - _nbFalse.Value;

		public LargePseudoBooleanSum(Problem p, BooleanVariable[] tab, IntegerVariable s)
			: base(p, GlobalConstraintUtilities.Join<BooleanVariable, IntegerVariable>(tab, s))
		{
			int num = tab.Length;
			_sum = s;
			_nbTrue = new Backtrackable<int>(p.IntTrail, 0);
			_nbFalse = new Backtrackable<int>(p.IntTrail, 0);
			_vars = new BooleanVariable[num];
			Array.Copy(tab, _vars, num);
			_idxUnassignedVars = new BacktrackableIntSet(p.IntSetTrail, 0, num - 1);
			_idxUnassignedVars.Fill();
			AnnotatedListener<int>.Listener l = WhenTrue;
			AnnotatedListener<int>.Listener l2 = WhenFalse;
			for (int i = 0; i < num; i++)
			{
				_vars[i].SubscribeToTrue(AnnotatedListener<int>.Generate(i, l));
				_vars[i].SubscribeToFalse(AnnotatedListener<int>.Generate(i, l2));
			}
			_sum.SubscribeToAnyModification(WhenSumModified);
		}

		private bool WhenTrue(int idx)
		{
			_nbTrue.Value += 1;
			_idxUnassignedVars.Remove(idx);
			if (_sum.ImposeLowerBound(MinSum, base.Cause))
			{
				return WhenSumSupDecreasedOrNbTrueIncreased();
			}
			return false;
		}

		private bool WhenFalse(int idx)
		{
			_nbFalse.Value += 1;
			_idxUnassignedVars.Remove(idx);
			if (_sum.ImposeUpperBound(MaxSum, base.Cause))
			{
				return WhenSumInfIncreasedOrNbFalseIncreased();
			}
			return false;
		}

		private bool WhenSumModified()
		{
			if (WhenSumInfIncreasedOrNbFalseIncreased())
			{
				return WhenSumSupDecreasedOrNbTrueIncreased();
			}
			return false;
		}

		private bool WhenSumInfIncreasedOrNbFalseIncreased()
		{
			if (MaxSum <= _sum.LowerBound)
			{
				return SetAllUnassignedVars(val: true);
			}
			return true;
		}

		private bool WhenSumSupDecreasedOrNbTrueIncreased()
		{
			if (MinSum >= _sum.UpperBound)
			{
				return SetAllUnassignedVars(val: false);
			}
			return true;
		}

		private bool SetAllUnassignedVars(bool val)
		{
			FiniteIntSet.Enumerator enumerator = _idxUnassignedVars.GetEnumerator();
			while (enumerator.MoveNext())
			{
				int current = enumerator.Current;
				BooleanVariable booleanVariable = _vars[current];
				if (booleanVariable.Status == BooleanVariableState.Unassigned && !booleanVariable.ImposeValue(val, base.Cause))
				{
					return false;
				}
			}
			return true;
		}
	}
}
