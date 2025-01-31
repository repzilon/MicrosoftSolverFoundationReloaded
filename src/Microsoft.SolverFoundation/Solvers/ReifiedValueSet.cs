using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint that connects an array of Boolean variables to some
	///   values of a discrete variable. Allows to "watch" a number of values
	///   of X and register delegates called when it is known whether the
	///   variable takes this value.
	/// </summary>
	/// <remarks>
	///   This is essentially a shorthand for a sequence of N refied
	///   equalities: b[0] iff (x = value[0]), ... , b[N-1] iff (x = value[N-1]).
	///   But the cost is considerably amortized: Decomposing would require to
	///   wake-up N propagators; here we need can react to most changes in
	///   constant time
	/// </remarks>
	internal class ReifiedValueSet : NaryConstraint<DiscreteVariable>
	{
		private IntegerVariable _x;

		private long[] _values;

		private BooleanVariable[] _b;

		/// <summary>
		///   Position p of the left-most Boolean variable such that:
		///   _values[p] sup equal to _x.LowerBound. Under-approximated
		/// </summary>
		private Backtrackable<int> _firstNonDiscardedBool;

		/// <summary>
		///   Position p of the right-most Boolean Variable such that:
		///   _values[p] lessEqual to _x.UpperBound. Over-approximated.
		/// </summary>
		private Backtrackable<int> _lastNonDiscardedBool;

		/// <summary>
		///   Creates a constraint imposing that for each i in [0, N[ 
		///   b[i] is true iff x == values[i]
		///   (where values and b are of the same size N).
		///   The values should be sorted in strictly increasing order.
		/// </summary>
		public ReifiedValueSet(Problem p, IntegerVariable x, long[] values, BooleanVariable[] b)
			: base(p, GlobalConstraintUtilities.Join<BooleanVariable, IntegerVariable>(b, x))
		{
			int num = b.Length;
			_x = x;
			_values = values;
			_b = b;
			x.SubscribeToAnyModification(WhenXmodified);
			AnnotatedListener<int>.Listener l = WhenBfalse;
			AnnotatedListener<int>.Listener l2 = WhenBtrue;
			for (int i = 0; i < num; i++)
			{
				_b[i].SubscribeToFalse(AnnotatedListener<int>.Generate(i, l));
				_b[i].SubscribeToTrue(AnnotatedListener<int>.Generate(i, l2));
			}
			_firstNonDiscardedBool = new Backtrackable<int>(_problem.IntTrail, 0);
			_lastNonDiscardedBool = new Backtrackable<int>(_problem.IntTrail, num - 1);
		}

		private bool WhenXmodified()
		{
			bool flag = true;
			if (_x.IsInstantiated())
			{
				flag = WhenXinstantiated();
			}
			if (flag && NarrowLowerBound())
			{
				return NarrowUpperBound();
			}
			return false;
		}

		protected bool WhenXinstantiated()
		{
			int num = FindPosition(_x.GetValue());
			if (num >= 0)
			{
				Cause c = new Cause(this, _x.AsSingleton);
				return _b[num].ImposeValueTrue(c);
			}
			return true;
		}

		protected bool NarrowLowerBound()
		{
			int num = _values.Length;
			long lowerBound = _x.LowerBound;
			int value = _firstNonDiscardedBool.Value;
			int num2 = value;
			while (true)
			{
				if (num2 >= num)
				{
					SaveFirstNonDiscardedBool(value, num2);
					return true;
				}
				if (_values[num2] >= lowerBound)
				{
					break;
				}
				Cause c = new Cause(this, _x.AsSingleton);
				if (!_b[num2].ImposeValueFalse(c))
				{
					return false;
				}
				num2++;
			}
			long upperBound = _x.UpperBound;
			long num3 = lowerBound;
			while (num2 < num && _values[num2] == num3 && _b[num2].Status == BooleanVariableState.False)
			{
				num2++;
				num3++;
				if (num3 > upperBound)
				{
					return _x.ImposeEmptyDomain(base.Cause);
				}
			}
			if (lowerBound != num3 && !_x.ImposeLowerBound(num3, base.Cause))
			{
				return false;
			}
			SaveFirstNonDiscardedBool(value, num2);
			return true;
		}

		protected bool NarrowUpperBound()
		{
			long upperBound = _x.UpperBound;
			int value = _lastNonDiscardedBool.Value;
			int num = value;
			while (true)
			{
				if (num < 0)
				{
					SaveLastNonDiscardedBool(value, num);
					return true;
				}
				if (_values[num] <= upperBound)
				{
					break;
				}
				Cause c = new Cause(this, _x.AsSingleton);
				if (!_b[num].ImposeValueFalse(c))
				{
					return false;
				}
				num--;
			}
			long lowerBound = _x.LowerBound;
			long num2 = upperBound;
			while (num >= 0 && _values[num] == num2 && _b[num].Status == BooleanVariableState.False)
			{
				num--;
				num2--;
				if (num2 < lowerBound)
				{
					return _x.ImposeEmptyDomain(base.Cause);
				}
			}
			if (num2 != upperBound && !_x.ImposeUpperBound(num2, base.Cause))
			{
				return _x.ImposeEmptyDomain(base.Cause);
			}
			SaveLastNonDiscardedBool(value, num);
			return true;
		}

		protected bool WhenBfalse(int idx)
		{
			long removedValue = _values[idx];
			Cause c = new Cause(this, _b[idx].AsSingleton);
			return _x.ImposeBoundsDifferentFrom(removedValue, c);
		}

		protected bool WhenBtrue(int idx)
		{
			long newval = _values[idx];
			Cause c = new Cause(this, _b[idx].AsSingleton);
			return _x.ImposeValue(newval, c);
		}

		/// <summary>
		///   Finds the position of a certain element in _values.
		///   Returns something negative if the element is not found.
		/// </summary>
		private int FindPosition(long v)
		{
			int num = _values.Length;
			int num2 = num - 1;
			long num3 = _values[0];
			long num4 = _values[num2];
			if (num4 - num3 == num2)
			{
				long num5 = v - num3;
				int num6 = (int)num5;
				return (0 <= num6 && num6 < num) ? num6 : (-1);
			}
			return Array.BinarySearch(_values, v);
		}

		private void SaveFirstNonDiscardedBool(int oldValue, int newValue)
		{
			if (newValue != oldValue)
			{
				_firstNonDiscardedBool.Value = newValue;
			}
		}

		private void SaveLastNonDiscardedBool(int oldValue, int newValue)
		{
			if (newValue != oldValue)
			{
				_lastNonDiscardedBool.Value = newValue;
			}
		}
	}
}
