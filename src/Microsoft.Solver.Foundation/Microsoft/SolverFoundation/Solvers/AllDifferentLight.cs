using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Constraint on a list of variables, imposing that all
	///   variables be pairwise different.
	/// </summary>
	internal class AllDifferentLight : NaryConstraint<IntegerVariable>
	{
		/// <summary>
		///   sorted-increasing-unique, list of forbidden values
		/// </summary>
		private List<long> _forbiddenValues;

		/// <summary>
		///   For each index i we record the variable
		///   that is instantiated to _forbiddenValues[i]
		/// </summary>
		private List<IntegerVariable> _reasons;

		/// <summary>
		///   When a variable is identified by the algorithm as instantiated
		///   we record its value here at the corresponding index.
		///   For non-instantiated vars we store -infinity
		/// </summary>
		private long[] _instantiated;

		public AllDifferentLight(Problem p, IntegerVariable[] tab)
			: base(p, new List<IntegerVariable>(tab).ToArray())
		{
			int num = tab.Length;
			Array.Copy(tab, _args, num);
			_forbiddenValues = new List<long>();
			_reasons = new List<IntegerVariable>();
			_instantiated = new long[num];
			AnnotatedListener<int>.Listener l = WhenModified;
			for (int i = 0; i < num; i++)
			{
				_instantiated[i] = long.MinValue;
				IntegerVariable integerVariable = _args[i];
				integerVariable.SubscribeToAnyModification(AnnotatedListener<int>.Generate(i, l));
				DelegateAdaptor @object = new DelegateAdaptor(i, WhenRestored);
				integerVariable.SubscribeToRestored(@object.Run);
			}
		}

		private bool WhenModified(int varidx)
		{
			IntegerVariable integerVariable = _args[varidx];
			if (integerVariable.IsInstantiated())
			{
				return WhenInstantiated(varidx);
			}
			if (CheckLowerBound(integerVariable))
			{
				return CheckUpperBound(integerVariable);
			}
			return false;
		}

		/// <summary>
		///   Check whether the lower bound of the variable is forbidden;
		///   logarithmic time (called very often)
		/// </summary>
		private bool CheckLowerBound(IntegerVariable x)
		{
			int count = _forbiddenValues.Count;
			long lowerBound = x.LowerBound;
			int num = _forbiddenValues.BinarySearch(lowerBound);
			if (num >= 0)
			{
				for (int i = num; i < count && _forbiddenValues[i] == x.LowerBound; i++)
				{
					IntegerVariable integerVariable = _reasons[i];
					if (!x.ImposeLowerBound(x.LowerBound + 1, new Cause(this, integerVariable.AsSingleton)))
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		///   Check whether the upper bound of the variable is forbidden;
		///   logarithmic time (called very often)
		/// </summary>
		private bool CheckUpperBound(IntegerVariable x)
		{
			long upperBound = x.UpperBound;
			int num = _forbiddenValues.BinarySearch(upperBound);
			_ = _forbiddenValues.Count;
			if (num >= 0)
			{
				int num2 = num;
				while (num2 >= 0 && _forbiddenValues[num2] == x.UpperBound)
				{
					IntegerVariable integerVariable = _reasons[num2];
					if (!x.ImposeUpperBound(x.UpperBound - 1, new Cause(this, integerVariable.AsSingleton)))
					{
						return false;
					}
					num2--;
				}
			}
			return true;
		}

		/// <summary>
		///   Propagation and work to do when a variable is instantiated.
		///   Costly (linear time iteration + linear time list insertions)
		///   but called relatively rarely - only when instantiations.
		/// </summary>
		private bool WhenInstantiated(int varidx)
		{
			IntegerVariable integerVariable = _args[varidx];
			long value = integerVariable.GetValue();
			Cause c = new Cause(this, integerVariable.AsSingleton);
			int num = Utils.PositionFirstGreaterEqual(_forbiddenValues, value);
			if (num < _forbiddenValues.Count && _forbiddenValues[num] == value)
			{
				IntegerVariable integerVariable2 = _reasons[num];
				if (integerVariable == integerVariable2)
				{
					return true;
				}
				return integerVariable2.ImposeBoundsDifferentFrom(value, c);
			}
			for (int num2 = _args.Length - 1; num2 >= 0; num2--)
			{
				if (num2 != varidx)
				{
					IntegerVariable integerVariable3 = _args[num2];
					if (!integerVariable3.ImposeBoundsDifferentFrom(value, c))
					{
						return false;
					}
				}
			}
			_forbiddenValues.Insert(num, value);
			_reasons.Insert(num, integerVariable);
			_instantiated[varidx] = value;
			return true;
		}

		/// <summary>
		///   When an instantiated variable has forced us to forbid a value
		///   and is uninstantiated we undo the effects
		/// </summary>
		private void WhenRestored(int varidx)
		{
			long num = _instantiated[varidx];
			if (num != long.MinValue)
			{
				int index = Utils.PositionFirstGreaterEqual(_forbiddenValues, num);
				_forbiddenValues.RemoveAt(index);
				_reasons.RemoveAt(index);
				_instantiated[varidx] = long.MinValue;
			}
		}
	}
}
