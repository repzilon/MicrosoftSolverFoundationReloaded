using System;
using System.Diagnostics;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> The quality associated to a solution. 
	///           This quality is the aggregation of a number of 
	///           numerical values, ordered by decreasing importance.
	/// </summary>
	/// <remarks> First value: 1 if the evaluation of the solution cannot
	///           even be evaluated correctly (e.g. overflow); 0 otherwise.
	///           Second value: 0 if the solution satisfies the hard 
	///           constraints; a positive value otherwise.
	///           Other values: the value of the minimization goals, 
	///           ordered by decreasing importance.
	/// </remarks>
	[DebuggerDisplay("{Overflow ? \"overflow\" : \"Violation = \" Violation}")]
	internal struct AggregateQuality
	{
		/// <summary> Quality criteria, aggregated by decreasing order
		///           of importance
		/// </summary>
		private readonly int[] _values;

		/// <summary> Get/set a flag that is true for a solution whose
		///           quality cannot be correctly evaluated because it
		///           causes a numerical overflow. 
		/// </summary>
		internal bool Overflow
		{
			get
			{
				return _values[0] == 1;
			}
			set
			{
				_values[0] = (value ? 1 : 0);
			}
		}

		/// <summary> Get/set the violation of the solution, i.e. a
		///           penalty term that is 0 if the hard constraints
		///           are satisfied, and positive otherwise
		/// </summary>
		internal int Violation
		{
			get
			{
				return _values[1];
			}
			set
			{
				_values[1] = value;
			}
		}

		/// <summary> Get/set the value of the minimization goal 
		///           of the given index
		/// </summary>
		/// <param name="idx">The index of a minimization goal</param>
		internal int this[int idx]
		{
			get
			{
				return _values[idx + 2];
			}
			set
			{
				_values[idx + 2] = value;
			}
		}

		/// <summary> Construction of an initial quality dimensioned for the
		///           solver. By default the quality is set to the worst
		///           (highest possible) value.
		/// </summary>
		internal AggregateQuality(ConstraintSystem s)
		{
			_values = new int[s._minimizationGoals.Count + 2];
			Reset();
		}

		internal AggregateQuality(int[] values)
		{
			_values = values;
		}

		/// <summary> Sets a quality to the worst (highest) possible value
		/// </summary>
		internal void Reset()
		{
			_values[0] = 1;
			for (int i = 1; i < _values.Length; i++)
			{
				_values[i] = int.MaxValue;
			}
		}

		/// <summary> Sets this to another aggregate quality 
		///           of the same dimension
		/// </summary>
		internal void CopyFrom(AggregateQuality source)
		{
			Array.Copy(source._values, _values, _values.Length);
		}

		/// <summary> Difference with another quality: returns 0 if the two qualities 
		///           are equal, otherwise finds the first objective where there is
		///           a difference and returns this difference
		/// </summary>
		/// <remarks> Can serve as a CompareTo function:
		///           Negative if this is lexicographically lower than other; 
		///           Zero if this is equal to lower; 
		///           Positive otherwise (i.e. other is lower).
		///           If the change is in the numerical overflow flag we return
		///           a huge positive or negative value rather than -1 or +1, to indicate
		///           that the difference is major
		/// </remarks>
		internal int Difference(AggregateQuality other)
		{
			int[] values = other._values;
			if (_values[0] != 0 || values[0] != 0)
			{
				int num = _values[0] - values[0];
				if (num == 0)
				{
					return 0;
				}
				if (num < 0)
				{
					return int.MinValue;
				}
				return int.MaxValue;
			}
			int num2 = _values.Length;
			for (int i = 1; i < num2; i++)
			{
				int num = _values[i] - values[i];
				if (num != 0)
				{
					return num;
				}
			}
			return 0;
		}

		/// <summary> Is this quality strictly lower (meaning better)
		///           than the other one?
		/// </summary>
		internal bool LessStrict(AggregateQuality other)
		{
			return Difference(other) < 0;
		}

		/// <summary> Is this quality lower (meaning better) than 
		///           or equal to the other one?
		/// </summary>
		internal bool LessEqual(AggregateQuality other)
		{
			return Difference(other) <= 0;
		}
	}
}
