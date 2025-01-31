using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// <summary>
	/// RowVariableGoalModel is implementation of an optimization model 
	/// consisting of decision variables, rows and goals
	/// </summary>
	/// </summary>
	public abstract class RowVariableGoalModel : RowVariableModel, IGoalModel
	{
		internal sealed class Goal : IGoal, IComparable<Goal>
		{
			private bool _fEnabled;

			private bool _fMinimize;

			private RowVariableGoalModel _mod;

			private int _oid;

			private int _pri;

			private int _vid;

			public int OrderIndex
			{
				get
				{
					return _oid;
				}
				set
				{
					_oid = value;
					_mod._rggoalSorted = null;
				}
			}

			public object Key => _mod.GetKeyFromIndex(_vid);

			public int Index => _vid;

			public int Priority
			{
				get
				{
					return _pri;
				}
				set
				{
					_pri = value;
					_mod._rggoalSorted = null;
				}
			}

			public bool Minimize
			{
				get
				{
					return _fMinimize;
				}
				set
				{
					_fMinimize = value;
				}
			}

			public bool Enabled
			{
				get
				{
					return _fEnabled;
				}
				set
				{
					_fEnabled = value;
				}
			}

			public Goal(RowVariableGoalModel mod, int vid, int pri, bool fMinimize)
			{
				_mod = mod;
				_vid = vid;
				_pri = pri;
				_fMinimize = fMinimize;
				_fEnabled = true;
			}

			public int CompareTo(Goal goal)
			{
				if (_pri < goal._pri)
				{
					return -1;
				}
				if (_pri > goal._pri)
				{
					return 1;
				}
				if (_oid < goal._oid)
				{
					return -1;
				}
				if (_oid > goal._oid)
				{
					return 1;
				}
				return 0;
			}
		}

		internal Dictionary<int, Goal> _mpvidgoal;

		internal int _oidNext;

		internal Goal[] _rggoalSorted;

		/// <summary>
		/// The number of goals in this linear model.
		/// </summary>
		public virtual int GoalCount => _mpvidgoal.Count;

		/// <summary>
		/// Return the goal collection of this linear model. 
		/// </summary>
		public virtual IEnumerable<IGoal> Goals
		{
			get
			{
				if (_rggoalSorted == null)
				{
					_rggoalSorted = new Goal[_mpvidgoal.Count];
					int num = 0;
					foreach (Goal value in _mpvidgoal.Values)
					{
						_rggoalSorted[num++] = value;
					}
					Array.Sort(_rggoalSorted);
				}
				return Statics.ImplicitCastIter<Goal, IGoal>(_rggoalSorted);
			}
		}

		/// <summary>
		/// The IEqualityComparer's are for the internal dictionaries from key to row/variable.
		/// </summary>
		protected RowVariableGoalModel(IEqualityComparer<object> comparer)
			: base(comparer)
		{
			_mpvidgoal = new Dictionary<int, Goal>();
			_rggoalSorted = null;
			_oidNext = 0;
		}

		internal virtual bool IsActiveGoal(int vid)
		{
			if (_mpvidgoal.TryGetValue(vid, out var value))
			{
				return value.Enabled;
			}
			return false;
		}

		/// <summary>Mark a row as a goal.
		/// </summary>
		/// <param name="vid">A row id.</param>
		/// <param name="pri">The priority of a goal.</param>
		/// <param name="minimize">Whether to minimize the goal row.</param>
		/// <returns>An IGoal representing the goal.</returns>
		public virtual IGoal AddGoal(int vid, int pri, bool minimize)
		{
			PreChange();
			ValidateVid(vid);
			_mpvidgoal.Remove(vid);
			Goal goal = new Goal(this, vid, pri, minimize);
			_mpvidgoal.Add(vid, goal);
			goal.OrderIndex = _oidNext++;
			_rggoalSorted = null;
			return goal;
		}

		/// <summary>
		/// Clear all the goals. 
		/// </summary>
		public virtual void ClearGoals()
		{
			PreChange();
			_mpvidgoal.Clear();
			_rggoalSorted = null;
		}

		/// <summary>
		/// Remove a goal row.
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <returns>true if the goal is removed. otherwise false</returns>
		public virtual bool RemoveGoal(int vid)
		{
			PreChange();
			if (!_mpvidgoal.Remove(vid))
			{
				return false;
			}
			_rggoalSorted = null;
			return true;
		}

		/// <summary>
		/// Check if a row id is a goal row. 
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <returns>true if this a goal row. otherwise false</returns>
		public virtual bool IsGoal(int vid)
		{
			ValidateVid(vid);
			return _mpvidgoal.ContainsKey(vid);
		}

		/// <summary>
		/// Check if a row id is a goal. If true, return the goal entry.
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <param name="goal">return the goal entry</param>
		/// <returns>true if a goal row. otherwise false</returns>
		public virtual bool IsGoal(int vid, out IGoal goal)
		{
			ValidateVid(vid);
			if (_mpvidgoal.TryGetValue(vid, out var value))
			{
				goal = value;
				return true;
			}
			goal = null;
			return false;
		}

		/// <summary>
		/// Return a goal entry if the row id is a goal.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>A goal entry. Null if not a goal row</returns>
		public virtual IGoal GetGoalFromIndex(int vid)
		{
			ValidateVid(vid);
			if (_mpvidgoal.TryGetValue(vid, out var value))
			{
				return value;
			}
			return null;
		}
	}
}
