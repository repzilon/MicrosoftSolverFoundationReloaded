using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint between an array of integer variables tab[0] ... tab[n-1],
	///   an Integer variable Idx and an integer variable Res, imposing that
	///   tab[idx] = res.
	/// </summary>
	/// <remarks>
	///   Weaker pruning than using Boolean for var/value pairs, but
	///   better space scalability.
	/// </remarks>
	internal class Index : DisolverConstraint
	{
		private IntegerVariable[] _tab;

		private IntegerVariable _idx;

		private IntegerVariable _res;

		/// <summary>
		///   Watches the position of a variable with minimal lower bound
		/// </summary>
		private Backtrackable<int> _watchMinBound;

		/// <summary>
		///   Watches the position of a variable with maximal upper bound
		/// </summary>
		private Backtrackable<int> _watchMaxBound;

		public Index(Problem p, IntegerVariable[] tab, IntegerVariable idx, IntegerVariable res)
			: base(p, GlobalConstraintUtilities.Join<DiscreteVariable, IntegerVariable>(GlobalConstraintUtilities.Join<IntegerVariable, IntegerVariable>(tab, idx), res))
		{
			int num = tab.Length;
			_idx = idx;
			_res = res;
			_tab = tab;
			AnnotatedListener<int>.Listener l = WhenArrayModified;
			for (int i = 0; i < num; i++)
			{
				BasicEvent.Listener l2 = AnnotatedListener<int>.Generate(i, l);
				tab[i].SubscribeToAnyModification(l2);
			}
			_idx.SubscribeToAnyModification(WhenIndexModified);
			_res.SubscribeToAnyModification(WhenResultModified);
			_problem.SubscribeToInitialPropagation(Initialize);
			int second = MinimumLowerBound().Second;
			int second2 = MaximumUpperBound().Second;
			_watchMinBound = new Backtrackable<int>(_problem.IntTrail, second);
			_watchMaxBound = new Backtrackable<int>(_problem.IntTrail, second2);
		}

		/// <summary>
		///   Called at beginning of first propagation loop
		/// </summary>
		private bool Initialize()
		{
			return _idx.ImposeRange(0L, _tab.Length - 1, base.Cause);
		}

		/// <summary>
		///   Called when array modified at position i
		/// </summary>
		protected bool WhenArrayModified(int i)
		{
			if (_idx.IsInstantiated())
			{
				if (_idx.GetValue() != i)
				{
					return true;
				}
				return WhenIndexInstantiated();
			}
			if (IsDisjointFromResult(i) && !_idx.ImposeBoundsDifferentFrom(i, base.Cause))
			{
				return false;
			}
			if (_watchMinBound.Value == i)
			{
				Pair<long, int> pair = MinimumLowerBound();
				if (!_res.ImposeLowerBound(pair.First, base.Cause))
				{
					return false;
				}
				_watchMinBound.Value = pair.Second;
			}
			if (_watchMaxBound.Value == i)
			{
				Pair<long, int> pair2 = MaximumUpperBound();
				if (!_res.ImposeUpperBound(pair2.First, base.Cause))
				{
					return false;
				}
				_watchMaxBound.Value = pair2.Second;
			}
			return true;
		}

		/// <summary>
		///   Code called when the result variable is modified
		/// </summary>
		protected bool WhenResultModified()
		{
			if (_idx.IsInstantiated())
			{
				return WhenIndexInstantiated();
			}
			return CheckIndexBounds();
		}

		/// <summary>
		///   Code called when the index is modified
		/// </summary>
		protected bool WhenIndexModified()
		{
			if (_idx.IsInstantiated())
			{
				return WhenIndexInstantiated();
			}
			if (!CheckIndexBounds())
			{
				return false;
			}
			if (!IsWithinIndexRange(_watchMinBound.Value))
			{
				Pair<long, int> pair = MinimumLowerBound();
				if (!_res.ImposeLowerBound(pair.First, base.Cause))
				{
					return false;
				}
				_watchMinBound.Value = pair.Second;
			}
			if (!IsWithinIndexRange(_watchMaxBound.Value))
			{
				Pair<long, int> pair2 = MaximumUpperBound();
				if (!_res.ImposeUpperBound(pair2.First, base.Cause))
				{
					return false;
				}
				_watchMaxBound.Value = pair2.Second;
			}
			return true;
		}

		/// <summary>
		///   Code called when the index is instantiated to a value I. 
		///   In this case the constraint behaves like an equality between 
		///   _tab[I] and Res, nothing more
		/// </summary>
		private bool WhenIndexInstantiated()
		{
			IntegerVariable integerVariable = _tab[_idx.GetValue()];
			if (_res.ImposeRange(integerVariable.LowerBound, integerVariable.UpperBound, base.Cause))
			{
				return integerVariable.ImposeRange(_res.LowerBound, _res.UpperBound, base.Cause);
			}
			return false;
		}

		/// <summary>
		///   makes sure the bounds of the Index variable
		///   are not disjoint from the result
		/// </summary>
		private bool CheckIndexBounds()
		{
			while (IsDisjointFromResult(_idx.LowerBound))
			{
				if (!_idx.ImposeLowerBound(_idx.LowerBound + 1, base.Cause))
				{
					return false;
				}
			}
			while (IsDisjointFromResult(_idx.UpperBound))
			{
				if (!_idx.ImposeUpperBound(_idx.UpperBound - 1, base.Cause))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		///   Computes min {lb(_tab[i]) : i  is acceptable};
		///   also returns one of the acceptable i's with the 
		///   corresponding lower bound
		/// </summary>
		private Pair<long, int> MinimumLowerBound()
		{
			int b = -1;
			long num = long.MaxValue;
			int num2 = (int)Math.Max(0L, _idx.LowerBound);
			int num3 = (int)Math.Min(_tab.Length - 1, _idx.UpperBound);
			for (int i = num2; i <= num3; i++)
			{
				if (!IsDisjointFromResult(i))
				{
					long lowerBound = _tab[i].LowerBound;
					if (lowerBound < num)
					{
						b = i;
						num = lowerBound;
					}
				}
			}
			return new Pair<long, int>(num, b);
		}

		/// <summary>
		///   Computes max {ub(_tab[i]) : i  is acceptable};
		///   also returns one of the acceptable i's with the 
		///   corresponding upper bound
		/// </summary>
		private Pair<long, int> MaximumUpperBound()
		{
			int b = -1;
			long num = long.MinValue;
			int num2 = (int)Math.Max(0L, _idx.LowerBound);
			int num3 = (int)Math.Min(_tab.Length - 1, _idx.UpperBound);
			for (int i = num2; i <= num3; i++)
			{
				if (!IsDisjointFromResult(i))
				{
					long upperBound = _tab[i].UpperBound;
					if (upperBound > num)
					{
						b = i;
						num = upperBound;
					}
				}
			}
			return new Pair<long, int>(num, b);
		}

		/// <summary>
		///   true if the position i is within the current range of
		///   the Index variable
		/// </summary>
		private bool IsWithinIndexRange(int i)
		{
			if (_idx.LowerBound <= i)
			{
				return i <= _idx.UpperBound;
			}
			return false;
		}

		/// <summary>
		///   False if _tab[i] can potentially be equal to Res
		/// </summary>
		private bool IsDisjointFromResult(long i)
		{
			if (_tab[i].LowerBound <= _res.UpperBound)
			{
				return _res.LowerBound > _tab[i].UpperBound;
			}
			return true;
		}
	}
}
