using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between an Integer Variable Y and a list of 
	///   Integer variables [X1 .. Xn] imposing that Y be equal to (at least)
	///   one of the variables in the list. 
	///
	///   Additionally, the constraint maintains a list of Booleans [B1 .. Bn];
	///   each Bi is true iff Y is equal to Xi.
	///   In some cases these Booleans can just be ignored, in some other
	///   cases they can be used to discard some equalities.
	/// </summary>
	/// <remarks>
	///   We are using watching to locate the support of the minimum 
	///   and the maximum values of Y
	/// </remarks>
	internal class SetMembership : DisolverConstraint
	{
		private IntegerVariable _x;

		private IntegerVariable[] _list;

		private BooleanVariable[] _conditions;

		private List<DisolverConstraint> _internalConstraints;

		/// <summary>
		///   Watches the position of a variable with minimal lower bound
		/// </summary>
		private Backtrackable<int> _watchMinBound;

		/// <summary>
		///   Watches the position of a variable with maximal upper bound
		/// </summary>
		private Backtrackable<int> _watchMaxBound;

		public SetMembership(Problem p, IntegerVariable x, IntegerVariable[] l, BooleanVariable[] b)
			: base(p, GlobalConstraintUtilities.Join(l, GlobalConstraintUtilities.Join<BooleanVariable, IntegerVariable>(b, x)))
		{
			int num = l.Length;
			_internalConstraints = new List<DisolverConstraint>();
			_list = l;
			_conditions = b;
			_x = x;
			AnnotatedListener<int>.Listener l2 = WhenModified;
			for (int i = 0; i < num; i++)
			{
				BasicEvent.Listener l3 = AnnotatedListener<int>.Generate(i, l2);
				l[i].SubscribeToAnyModification(l3);
				b[i].SubscribeToFalse(l3);
			}
			for (int j = 0; j < num; j++)
			{
				_internalConstraints.Add(new ReifiedEquality(p, x, l[j], b[j]));
			}
			int second = MinimumLowerBound().Second;
			int second2 = MaximumUpperBound().Second;
			_watchMinBound = new Backtrackable<int>(_problem.IntTrail, second);
			_watchMaxBound = new Backtrackable<int>(_problem.IntTrail, second2);
			_internalConstraints.Add(new LargeClause(p, b));
		}

		public SetMembership(Problem p, IntegerVariable x, IntegerVariable[] l)
			: this(p, x, l, p.CreateInternalBooleanVariableArray(l.Length))
		{
		}

		protected bool WhenModified(int idx)
		{
			if (_watchMinBound.Value == idx)
			{
				Pair<long, int> pair = MinimumLowerBound();
				_watchMinBound.Value = pair.Second;
				if (!_x.ImposeLowerBound(pair.First, base.Cause))
				{
					return false;
				}
			}
			if (_watchMaxBound.Value == idx)
			{
				Pair<long, int> pair2 = MaximumUpperBound();
				_watchMaxBound.Value = pair2.Second;
				if (!_x.ImposeUpperBound(pair2.First, base.Cause))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		///   Returns the value of the minimal lower bound, 
		///   together with the position of one of the vars that
		///   has this low bound
		/// </summary>
		private Pair<long, int> MinimumLowerBound()
		{
			int num = _list.Length;
			int b = -1;
			long num2 = long.MaxValue;
			for (int i = 0; i < num; i++)
			{
				if (_conditions[i].Status != BooleanVariableState.False)
				{
					long lowerBound = _list[i].LowerBound;
					if (lowerBound < num2)
					{
						b = i;
						num2 = lowerBound;
					}
				}
			}
			return new Pair<long, int>(num2, b);
		}

		/// <summary>
		///   Returns the value of the maximal upper bound, 
		///   together with the position of one of the vars that
		///   has this upper bound
		/// </summary>
		private Pair<long, int> MaximumUpperBound()
		{
			int num = _list.Length;
			int b = -1;
			long num2 = long.MinValue;
			for (int i = 0; i < num; i++)
			{
				if (_conditions[i].Status != BooleanVariableState.False)
				{
					long upperBound = _list[i].UpperBound;
					if (upperBound > num2)
					{
						b = i;
						num2 = upperBound;
					}
				}
			}
			return new Pair<long, int>(num2, b);
		}
	}
}
