using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint on a list of variables, imposing that all
	///   variables be pairwise different. This version is extremely incremental,
	///   using watches. It scales very well and should be preferred for 
	///   large arrays
	/// </summary>
	/// <remarks>
	///   Version based on lazy-data-structures (watching).
	///   As incremental as it possibly gets, but space-consuming: 
	///   allocates arrays linear in the number of possible values.
	/// </remarks>
	internal class AllDifferentLazy : NaryConstraint<IntegerVariable>
	{
		/// <summary>
		///   Watches are dealt with as linked data-structures. This allows
		///   similar behaviour to having arrays of resizable lists but with
		///   guarantee that no memory whatsoever is allocated when watches
		///   move from one place to another.
		/// </summary>
		private class Watch
		{
			public readonly int VarIdx;

			public Watch Next;

			public Watch Prev;

			public Watch(int x)
			{
				VarIdx = x;
				Next = null;
				Prev = null;
			}
		}

		private List<int> _idxToUpdate = new List<int>();

		/// <summary>
		///   shift between values and arrays indexes (0-based)
		/// </summary>
		private long _minvalue;

		/// <summary>
		///   _forbidden[val-_minvalue] is non-null iff the value val is 
		///   forbidden and points to the cause, i.e. instantiated var that
		///   is instantiated to this value
		/// </summary>
		private IntegerVariable[] _forbidden;

		/// <summary>
		///   at position [val-_minvalue], keeps the list of variables whose
		///   lower bound watches the value val
		/// </summary>
		private Watch[] _lowerBoundsWatched;

		/// <summary>
		///   at position [val-_minvalue], keeps the list of variables whose
		///   upper bound watches the value val
		/// </summary>
		private Watch[] _upperBoundsWatched;

		/// <summary>
		///   at position i keeps the 
		///   value currently watched as lower bound of _args[i]
		/// </summary>
		private long[] _valueCurrentlyWatchedLower;

		/// <summary>
		///   at position i keeps the 
		///   value currently watched as upper bound of _args[i]
		/// </summary>
		private long[] _valueCurrentlyWatchedUpper;

		/// <summary>
		///   at position i keeps the 
		///   watch for the lower bound of _args[i]
		/// </summary>
		private readonly Watch[] _lbwatch;

		/// <summary>
		///   at position i keeps the 
		///   watch for the upper bound of _args[i]
		/// </summary>
		private readonly Watch[] _ubwatch;

		public AllDifferentLazy(Problem p, IntegerVariable[] tab)
			: base(p, new List<IntegerVariable>(tab).ToArray())
		{
			int num = tab.Length;
			Array.Copy(tab, _args, num);
			long num2 = long.MaxValue;
			long num3 = long.MinValue;
			foreach (IntegerVariable integerVariable in tab)
			{
				num2 = Math.Min(num2, integerVariable.LowerBound);
				num3 = Math.Max(num3, integerVariable.UpperBound);
			}
			_minvalue = num2;
			_forbidden = new IntegerVariable[num3 - num2 + 1];
			_lowerBoundsWatched = new Watch[num3 - num2 + 1];
			_upperBoundsWatched = new Watch[num3 - num2 + 1];
			_lbwatch = new Watch[num];
			_ubwatch = new Watch[num];
			_valueCurrentlyWatchedLower = new long[num];
			_valueCurrentlyWatchedUpper = new long[num];
			AnnotatedListener<int>.Listener l = WhenModified;
			for (int j = 0; j < num; j++)
			{
				IntegerVariable integerVariable2 = _args[j];
				_lbwatch[j] = new Watch(j);
				_ubwatch[j] = new Watch(j);
				integerVariable2.SubscribeToAnyModification(AnnotatedListener<int>.Generate(j, l));
				DelegateAdaptor @object = new DelegateAdaptor(j, WhenRestored);
				integerVariable2.SubscribeToRestored(@object.Run);
				_valueCurrentlyWatchedLower[j] = integerVariable2.LowerBound;
				_valueCurrentlyWatchedUpper[j] = integerVariable2.UpperBound;
				PlugLowerWatch(j);
				PlugUpperBound(j);
			}
		}

		private bool WhenModified(int varidx)
		{
			IntegerVariable integerVariable = _args[varidx];
			if (integerVariable.IsInstantiated())
			{
				return WhenInstantiated(varidx);
			}
			return WhenBoundsModified(varidx);
		}

		/// <summary>
		///   When the lower bound of _args[varidx] is modified we check in
		///   constant time if a variable is instantiated to this value
		///   and if so we find the first value not taken by an instantiated var
		/// </summary>
		private bool WhenBoundsModified(int varidx)
		{
			IntegerVariable integerVariable = _args[varidx];
			while (true)
			{
				long lowerBound = integerVariable.LowerBound;
				IntegerVariable integerVariable2 = _forbidden[lowerBound - _minvalue];
				if (integerVariable2 == null)
				{
					break;
				}
				if (!integerVariable.ImposeLowerBound(lowerBound + 1, new Cause(this, integerVariable2.AsSingleton)))
				{
					return false;
				}
			}
			while (true)
			{
				long upperBound = integerVariable.UpperBound;
				IntegerVariable integerVariable3 = _forbidden[upperBound - _minvalue];
				if (integerVariable3 == null)
				{
					break;
				}
				if (!integerVariable.ImposeUpperBound(upperBound - 1, new Cause(this, integerVariable3.AsSingleton)))
				{
					return false;
				}
			}
			UpdateLowerWatch(varidx);
			UpdateUpperWatch(varidx);
			return true;
		}

		/// <summary>
		///   Code called when the variable _args[varidx] is instantiated.
		///   We go through the variables that watch this value as their lower
		///   or upper bound.
		/// </summary>
		protected bool WhenInstantiated(int varidx)
		{
			IntegerVariable integerVariable = _args[varidx];
			long value = integerVariable.GetValue();
			long num = value - _minvalue;
			Cause c = new Cause(this, integerVariable.AsSingleton);
			if (_forbidden[num] != null)
			{
				if (_forbidden[num] == integerVariable)
				{
					return true;
				}
				return _forbidden[num].ImposeBoundsDifferentFrom(value, c);
			}
			_idxToUpdate.Clear();
			foreach (Watch item in LowerBoundWatches(value))
			{
				int varIdx = item.VarIdx;
				if (varIdx != varidx)
				{
					IntegerVariable integerVariable2 = _args[varIdx];
					if (!integerVariable2.ImposeLowerBound(value + 1, c))
					{
						return false;
					}
					_idxToUpdate.Add(varIdx);
				}
			}
			foreach (int item2 in _idxToUpdate)
			{
				UpdateLowerWatch(item2);
			}
			_idxToUpdate.Clear();
			foreach (Watch item3 in UpperBoundWatches(value))
			{
				int varIdx2 = item3.VarIdx;
				if (varIdx2 != varidx)
				{
					IntegerVariable integerVariable3 = _args[varIdx2];
					if (!integerVariable3.ImposeUpperBound(value - 1, c))
					{
						return false;
					}
					_idxToUpdate.Add(varIdx2);
				}
			}
			foreach (int item4 in _idxToUpdate)
			{
				UpdateUpperWatch(item4);
			}
			_forbidden[value - _minvalue] = integerVariable;
			UpdateLowerWatch(varidx);
			UpdateUpperWatch(varidx);
			return true;
		}

		/// <summary>
		///   Enumerates the lower-bound Watches attached to the value
		/// </summary>
		private IEnumerable<Watch> LowerBoundWatches(long val)
		{
			Watch start = _lowerBoundsWatched[val - _minvalue];
			return Iterate(start);
		}

		/// <summary>
		///   Enumerates the upper-bound Watches attached to the value
		/// </summary>
		private IEnumerable<Watch> UpperBoundWatches(long val)
		{
			Watch start = _upperBoundsWatched[val - _minvalue];
			return Iterate(start);
		}

		private IEnumerable<Watch> Iterate(Watch start)
		{
			for (Watch current = start; current != null; current = current.Next)
			{
				yield return current;
			}
		}

		/// <summary>
		///   Called when the problem is restored - we undo any change
		/// </summary>
		private void WhenRestored(int idx)
		{
			long num = _valueCurrentlyWatchedLower[idx];
			long num2 = _valueCurrentlyWatchedUpper[idx];
			UpdateLowerWatch(idx);
			UpdateUpperWatch(idx);
			if (num == num2 && _forbidden[num - _minvalue] == _args[idx])
			{
				_forbidden[num - _minvalue] = null;
			}
		}

		/// <summary>
		///   Removes the lower bound watch from the value it 
		///   is currently plugged to
		/// </summary>
		private void UpdateLowerWatch(int varidx)
		{
			IntegerVariable integerVariable = _args[varidx];
			long num = _valueCurrentlyWatchedLower[varidx];
			long lowerBound = integerVariable.LowerBound;
			if (num != lowerBound)
			{
				Watch watch = _lbwatch[varidx];
				Watch prev = watch.Prev;
				Watch next = watch.Next;
				if (prev == null)
				{
					_lowerBoundsWatched[num - _minvalue] = next;
				}
				else
				{
					prev.Next = next;
				}
				if (next != null)
				{
					next.Prev = prev;
				}
				watch.Prev = null;
				watch.Next = null;
				long num2 = lowerBound - _minvalue;
				Watch watch2 = _lowerBoundsWatched[num2];
				_lowerBoundsWatched[num2] = watch;
				_valueCurrentlyWatchedLower[varidx] = lowerBound;
				if (watch2 != null)
				{
					watch.Next = watch2;
					watch2.Prev = watch;
				}
			}
		}

		/// <summary>
		///   Removes the upper bound watch from the value it 
		///   is currently plugged to
		/// </summary>
		private void UpdateUpperWatch(int varidx)
		{
			IntegerVariable integerVariable = _args[varidx];
			long num = _valueCurrentlyWatchedUpper[varidx];
			long upperBound = integerVariable.UpperBound;
			if (num != upperBound)
			{
				Watch watch = _ubwatch[varidx];
				Watch prev = watch.Prev;
				Watch next = watch.Next;
				if (prev == null)
				{
					_upperBoundsWatched[num - _minvalue] = next;
				}
				else
				{
					prev.Next = next;
				}
				if (next != null)
				{
					next.Prev = prev;
				}
				watch.Prev = null;
				watch.Next = null;
				long num2 = upperBound - _minvalue;
				Watch watch2 = _upperBoundsWatched[num2];
				_upperBoundsWatched[num2] = watch;
				_valueCurrentlyWatchedUpper[varidx] = upperBound;
				if (watch2 != null)
				{
					watch.Next = watch2;
					watch2.Prev = watch;
				}
			}
		}

		/// <summary>
		///   Plugs the watch for the lower bound of the var of given 
		///   index to the slot corresponding to that var's current upper
		///   bound. The watch is assumed unplugged.
		/// </summary>
		private void PlugLowerWatch(int varidx)
		{
			IntegerVariable integerVariable = _args[varidx];
			long lowerBound = integerVariable.LowerBound;
			long num = lowerBound - _minvalue;
			Watch watch = _lbwatch[varidx];
			Watch watch2 = _lowerBoundsWatched[num];
			_lowerBoundsWatched[num] = watch;
			_valueCurrentlyWatchedLower[varidx] = lowerBound;
			if (watch2 != null)
			{
				watch.Next = watch2;
				watch2.Prev = watch;
			}
		}

		/// <summary>
		///   Plugs the watch for the upper bound of the var of given
		///   index to the slot corresponding to that var's current upper 
		///   bound. The watch is assumed initially unplugged.
		/// </summary>
		private void PlugUpperBound(int varidx)
		{
			IntegerVariable integerVariable = _args[varidx];
			long upperBound = integerVariable.UpperBound;
			long num = upperBound - _minvalue;
			Watch watch = _ubwatch[varidx];
			Watch watch2 = _upperBoundsWatched[num];
			_upperBoundsWatched[num] = watch;
			_valueCurrentlyWatchedUpper[varidx] = upperBound;
			if (watch2 != null)
			{
				watch.Next = watch2;
				watch2.Prev = watch;
			}
		}
	}
}
