using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>  A constraint imposing that the start times of a number of tasks be 
	///            scheduled so that at no time instant their total consumption of
	///            a shared resource is larger than the availability of this resource
	/// </summary>
	internal sealed class DisjointPacking : BooleanFunction
	{
		/// <summary>
		/// A time window during which some energy is consumed
		/// </summary>
		internal struct TimeWindow
		{
			public int Start;

			public int End;

			public long EnergyLeft;
		}

		/// <summary>
		/// Result of one iteration of propagator
		/// </summary>
		private enum PackingState
		{
			Stable,
			Change,
			Failure
		}

		/// <summary>
		/// A Task groups together a start time (typically a variable), 
		/// a duration and a consumption
		/// </summary>
		internal struct Task
		{
			public CspSolverTerm Start;

			public int Duration;

			public int Consumption;

			public int EarliestRelease => Start.First;

			public int LatestRelease => Start.Last;

			public bool IsFixed => Start.Count == 1;

			public int LatestCompletion => Start.Last + Duration - 1;

			/// <summary>
			/// Resource * time consumed
			/// </summary>
			public long Energy => (long)Duration * (long)Consumption;

			/// <summary>
			/// Lower bound on energy consumed by the task
			/// during the time interval [start, end]
			/// </summary>
			public long Intersection(int start, int end)
			{
				return Math.Min(IntersectionEarly(start, end), IntersectionLate(start, end));
			}

			/// <summary>
			/// Energy consumed by the task duing the time interval [start, end]
			/// ASSUMING the tast is scheduled as early as possible
			/// </summary>
			public long IntersectionEarly(int start, int end)
			{
				int earliestRelease = EarliestRelease;
				return Consumption * TimeOverlap(earliestRelease, earliestRelease + Duration - 1, start, end);
			}

			/// <summary>
			/// Energy consumed by the task duing the time interval [start, end]
			/// ASSUMING the tast is scheduled as late as possible
			/// </summary>
			public long IntersectionLate(int start, int end)
			{
				int latestRelease = LatestRelease;
				return Consumption * TimeOverlap(latestRelease, latestRelease + Duration - 1, start, end);
			}

			/// <summary>
			/// Would it be ok to schedule the task at the given 
			/// candidate start time considering the available energy in the
			/// time window?
			/// </summary>
			public bool StartTimeIsPossible(int candidateStartTime, TimeWindow window)
			{
				long num = Consumption * TimeOverlap(candidateStartTime, candidateStartTime + Duration - 1, window.Start, window.End);
				long num2 = window.EnergyLeft + Intersection(window.Start, window.End);
				return num <= num2;
			}

			private static long TimeOverlap(int left, int right, int start, int end)
			{
				if (start > left)
				{
					left = start;
				}
				if (end < right)
				{
					right = end;
				}
				if (left > right)
				{
					return 0L;
				}
				return right - left + 1;
			}
		}

		private readonly Task[] _taskList;

		private readonly long _capacity;

		private readonly int _mintime;

		private readonly int _maxtime;

		internal override string Name => "DisjointPack";

		/// <summary>  A constraint imposing that the start times of a number of tasks be 
		///            scheduled so that at no time instant their total consumption of
		///            a shared resource is larger than the availability of this resource
		/// </summary>
		internal DisjointPacking(ConstraintSystem store, CspSolverTerm[] starts, int[] durations, int[] consumptions, int limit)
			: base(store, starts)
		{
			int num = starts.Length;
			_capacity = limit;
			_taskList = new Task[num];
			_mintime = int.MaxValue;
			_maxtime = int.MinValue;
			for (int i = 0; i < num; i++)
			{
				ref Task reference = ref _taskList[i];
				reference = new Task
				{
					Start = starts[i],
					Consumption = consumptions[i],
					Duration = durations[i]
				};
				_mintime = Math.Min(_mintime, starts[i].First);
				_maxtime = Math.Max(_maxtime, starts[i].Last + durations[i]);
			}
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			IEnumerable<int> source = _taskList.Select((Task x) => x.Duration);
			IEnumerable<int> source2 = _taskList.Select((Task x) => x.Consumption);
			return newModel.Packing(_inputs, source.ToArray(), source2.ToArray(), (int)_capacity);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			throw new NotImplementedException();
		}

		public override void Accept(IVisitor visitor)
		{
			throw new NotImplementedException();
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			if (base.Count == 1 && IsTrue)
			{
				bool flag = PropagateUntilStable();
				conflict = (flag ? null : this);
				return false;
			}
			return PropagateUnknown(out conflict);
		}

		/// <summary>
		/// Propagation algorithm when the constraint is
		/// negated or unknown
		/// </summary>
		internal bool PropagateUnknown(out CspSolverTerm conflict)
		{
			foreach (TimeWindow item in EnumerateTimeWindows())
			{
				if (item.EnergyLeft < 0)
				{
					return Intersect(0, 0, out conflict);
				}
			}
			if (!Array.Exists(_taskList, (Task t) => !t.IsFixed))
			{
				return Intersect(1, 1, out conflict);
			}
			conflict = null;
			return false;
		}

		/// <summary>
		/// Main propagation method for the usual case
		/// where the constraint IsTrue
		/// </summary>   
		/// <returns>
		/// False if inconsistency found;
		/// note that this convention is different from the one in Propagate()
		/// </returns>
		/// <remarks>
		/// Use if we want to reach stable bounds...
		/// Or is it best to leave this to the propagator queue 
		/// (constraint will be re-activated anyway)
		/// This version is better for unit testing - easier to
		/// know what it's doing
		/// </remarks>
		internal bool PropagateUntilStable()
		{
			while (true)
			{
				switch (PropagateOneStep())
				{
				case PackingState.Failure:
					return false;
				case PackingState.Stable:
					return true;
				}
			}
		}

		/// <summary>
		/// Main propagation method for the usual case
		/// where the constraint IsTrue
		/// </summary>
		/// <remarks>
		/// Use if we rely on propagation queue to re-activate
		/// this constraint until the bounds are stable
		/// </remarks>
		private PackingState PropagateOneStep()
		{
			PackingState result = PackingState.Stable;
			Pair<int, int>[] array = new Pair<int, int>[_taskList.Length];
			for (int i = 0; i < _taskList.Length; i++)
			{
				CspSolverTerm start = _taskList[i].Start;
				ref Pair<int, int> reference = ref array[i];
				reference = new Pair<int, int>(start.First, start.Last);
			}
			foreach (TimeWindow item in EnumerateTimeWindows())
			{
				if (Model.CheckAbort())
				{
					return PackingState.Stable;
				}
				for (int j = 0; j < _taskList.Length; j++)
				{
					Task t = _taskList[j];
					PackingState packingState = ReviseBounds(item, t, ref array[j]);
					switch (packingState)
					{
					case PackingState.Change:
						result = packingState;
						break;
					case PackingState.Failure:
						return packingState;
					}
				}
			}
			for (int k = 0; k < _taskList.Length; k++)
			{
				if (!UpdateVarBounds(_taskList[k].Start, array[k]))
				{
					return PackingState.Failure;
				}
			}
			return result;
		}

		/// <returns>
		/// false if inconsistency found;
		/// note that this convention is different from the one in Propagate()
		/// </returns>
		private static bool UpdateVarBounds(CspSolverTerm x, Pair<int, int> bounds)
		{
			if (bounds.First > bounds.Second)
			{
				return false;
			}
			if (bounds.First == x.First && bounds.Second == x.Last)
			{
				return true;
			}
			x.Intersect(bounds.First, bounds.Second, out var conflict);
			return conflict == null;
		}

		/// <summary>
		/// Compute a new pair [lower bound, upper bound]
		/// for the start time of the task
		/// considering its intersection with a particular time window
		/// </summary>
		private static PackingState ReviseBounds(TimeWindow window, Task t, ref Pair<int, int> currentBounds)
		{
			int i = currentBounds.First;
			int num;
			for (num = currentBounds.Second; i <= num && !t.StartTimeIsPossible(i, window); i++)
			{
			}
			while (num > i && !t.StartTimeIsPossible(num, window))
			{
				num--;
			}
			if (i > num)
			{
				return PackingState.Failure;
			}
			if (currentBounds.First == i && currentBounds.Second == num)
			{
				return PackingState.Stable;
			}
			currentBounds.First = i;
			currentBounds.Second = num;
			return PackingState.Change;
		}

		/// <summary>
		/// Enumerates all time intervals of interest in which some
		/// energy is consumed. These windows are the basis of reasoning
		/// </summary>
		internal IEnumerable<TimeWindow> EnumerateTimeWindows()
		{
			List<int> keyTimeSteps = GetKeyTimeSteps();
			for (int i = 0; i < keyTimeSteps.Count; i++)
			{
				int windowStart = keyTimeSteps[i];
				for (int j = i; j < keyTimeSteps.Count; j++)
				{
					int windowEnd = keyTimeSteps[j];
					long occupiedEnergy = 0L;
					Task[] taskList = _taskList;
					foreach (Task task in taskList)
					{
						occupiedEnergy += task.Intersection(windowStart, windowEnd);
					}
					if (occupiedEnergy > 0)
					{
						yield return new TimeWindow
						{
							Start = keyTimeSteps[i],
							End = keyTimeSteps[j],
							EnergyLeft = _capacity * (windowEnd - windowStart + 1) - occupiedEnergy
						};
					}
				}
			}
		}

		/// <summary>
		/// Gets the ordered non-redundant sequence of 
		/// time steps corresponding to a task's
		/// earliest/latest release or earliest/latest completions
		/// </summary>
		internal List<int> GetKeyTimeSteps()
		{
			List<int> list = new List<int>(4 * _taskList.Length);
			Task[] taskList = _taskList;
			for (int i = 0; i < taskList.Length; i++)
			{
				Task task = taskList[i];
				int earliestRelease = task.EarliestRelease;
				int latestRelease = task.LatestRelease;
				int num = task.Duration - 1;
				list.Add(earliestRelease);
				list.Add(latestRelease);
				list.Add(earliestRelease + num);
				list.Add(latestRelease + num);
			}
			InPlaceSortUnique(list);
			return list;
		}

		/// <summary>
		/// modifies the array in-place so that its elements 
		/// are sorted non-redundant
		/// </summary>
		internal static void InPlaceSortUnique(List<int> list)
		{
			list.Sort();
			int count = list.Count;
			int num = 0;
			int num2 = 0;
			while (num2 < count)
			{
				int num4 = (list[num] = list[num2]);
				do
				{
					num2++;
				}
				while (num2 < count && list[num2] == num4);
				num++;
			}
			list.RemoveRange(num, count - num);
			list.TrimExcess();
		}

		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			int num = 0;
			for (int i = _mintime; i <= _maxtime; i++)
			{
				int num2 = 0;
				Task[] taskList = _taskList;
				for (int j = 0; j < taskList.Length; j++)
				{
					Task task = taskList[j];
					int integerValue = ls.GetIntegerValue(task.Start);
					if (integerValue <= i && i < integerValue + task.Duration)
					{
						num2 += task.Consumption;
					}
				}
				if (num2 > _capacity)
				{
					num += (int)(num2 - _capacity);
				}
			}
			ls[this] = ((num > 0) ? num : (-1));
		}

		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			int num = 0;
			CspVariable cspVariable = null;
			for (int i = _mintime; i <= _maxtime; i++)
			{
				int num2 = 0;
				CspVariable cspVariable2 = null;
				foreach (int item in CspFunction.RandomlyEnumerateValuesInRange(0, _taskList.Length, ls.RandomSource))
				{
					Task task = _taskList[item];
					CspSolverTerm start = task.Start;
					int integerValue = ls.GetIntegerValue(start);
					if (integerValue > i || i >= integerValue + task.Duration)
					{
						continue;
					}
					num2 += task.Consumption;
					if (cspVariable2 == null)
					{
						ValueWithGradients integerGradients = ls.GetIntegerGradients(start);
						if (integerGradients.DecGradient < 0)
						{
							cspVariable2 = integerGradients.DecVariable;
						}
						else if (integerGradients.IncGradient > 0)
						{
							cspVariable2 = integerGradients.IncVariable;
						}
					}
				}
				if (num2 > _capacity)
				{
					int num3 = (int)(num2 - _capacity);
					if (num3 > num && cspVariable2 != null)
					{
						num = num3;
						cspVariable = cspVariable2;
					}
				}
			}
			ls.SetGradients(this, (cspVariable != null) ? (-1) : 0, cspVariable, 0, null);
		}

		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			if (target == 0)
			{
				return base.SelectSubtermToFlip(ls, target);
			}
			foreach (int item in CspFunction.RandomlyEnumerateValuesInRange(_mintime, _maxtime + 1, ls.RandomSource))
			{
				int num = 0;
				CspSolverTerm cspSolverTerm = null;
				foreach (int item2 in CspFunction.RandomlyEnumerateValuesInRange(0, _taskList.Length, ls.RandomSource))
				{
					Task task = _taskList[item2];
					int integerValue = ls.GetIntegerValue(task.Start);
					if (integerValue <= item && item < integerValue + task.Duration)
					{
						num += task.Consumption;
						cspSolverTerm = task.Start;
					}
				}
				if (num > _capacity)
				{
					int value = cspSolverTerm.BaseValueSet.Pick(ls.RandomSource);
					return new KeyValuePair<CspSolverTerm, int>(cspSolverTerm, value);
				}
			}
			return base.SelectSubtermToFlip(ls, target);
		}
	}
}
