using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Base class for unconstrained nonlinear models.
	/// </summary>
	public abstract class UnconstrainedNonlinearModel : INonlinearModel, IRowVariableModel, IGoalModel
	{
		internal sealed class Goal : IGoal, IComparable<Goal>
		{
			private bool _fEnabled;

			private bool _fMinimize;

			private IRowVariableModel _mod;

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

			public Goal(IRowVariableModel mod, int vid, int pri, bool fMinimize)
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

		private const int VidForRow = 0;

		private object[] _variableKeys;

		private Dictionary<object, int> _keyToVidMapping;

		private Rational[] _variableValues;

		private int _vidCount;

		private int _rowVid;

		private object _rowKey;

		private Rational _rowValue;

		private Goal _goal;

		private int _modelReadCount;

		private bool RowExists => _rowVid == 0;

		private bool GoalExists => _goal != null;

		/// <summary>
		/// Sets the value of the only row of the model 
		/// </summary>
		protected Rational RowValue
		{
			set
			{
				_rowValue = value;
			}
		}

		/// <summary>
		/// return count for keys (not including null) for variables 
		/// </summary>
		protected int VariableKeyCount
		{
			get
			{
				DebugContracts.NonNull(_keyToVidMapping);
				return _keyToVidMapping.Count;
			}
		}

		/// <summary>
		/// Used for row or variable key comparison 
		/// </summary>
		public IEqualityComparer<object> KeyComparer => _keyToVidMapping.Comparer;

		/// <summary> return the variable index collection, inclusive of rows
		/// </summary>
		public IEnumerable<int> Indices
		{
			get
			{
				try
				{
					_modelReadCount++;
					if (RowExists)
					{
						yield return _rowVid;
					}
					for (int vid = 1; vid <= VariableCount; vid++)
					{
						yield return vid;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary> Return the variable and row key collection.
		/// Indices are guaranteed to &gt;= 0 and &lt; KeyCount.
		/// </summary>
		public IEnumerable<object> Keys
		{
			get
			{
				try
				{
					_modelReadCount++;
					if (RowExists)
					{
						yield return _rowKey;
					}
					foreach (object variableKey in GetVariableKeys())
					{
						yield return variableKey;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary> the number of keys, inclusive of rows and variables.
		/// </summary>
		public int KeyCount => VariableCount + RowCount;

		/// <summary> return the row index collection 
		/// </summary>
		public IEnumerable<int> RowIndices
		{
			get
			{
				try
				{
					_modelReadCount++;
					if (RowExists)
					{
						yield return _rowVid;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary> return the row key collection 
		/// </summary>
		public IEnumerable<object> RowKeys
		{
			get
			{
				try
				{
					_modelReadCount++;
					if (RowExists)
					{
						yield return _rowKey;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary> the number of rows in the model 
		/// </summary>
		public int RowCount
		{
			get
			{
				if (!RowExists)
				{
					return 0;
				}
				return 1;
			}
		}

		/// <summary>
		/// Gets an IEnumerable containing the variable indexes.
		/// </summary>
		/// <remarks>
		/// The result is guaranteed to be the integer values from 1 to VariableCount.
		/// </remarks>
		public IEnumerable<int> VariableIndices
		{
			get
			{
				try
				{
					_modelReadCount++;
					int variableCount = VariableCount;
					for (int vid = 1; vid <= variableCount; vid++)
					{
						yield return vid;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary>
		/// Gets an IEnumerable containing the variable keys.
		/// </summary>
		public IEnumerable<object> VariableKeys
		{
			get
			{
				try
				{
					_modelReadCount++;
					foreach (object variableKey in GetVariableKeys())
					{
						yield return variableKey;
					}
				}
				finally
				{
					_modelReadCount--;
				}
			}
		}

		/// <summary>
		/// Gets the number of variables in the model.
		/// </summary>
		public int VariableCount => _vidCount;

		/// <summary> return the number of integer variables 
		/// </summary>
		int IRowVariableModel.IntegerIndexCount => 0;

		/// <summary>
		/// Function value callback.
		/// * INonlinearModel: the model.
		/// * int: the row (goal or constraint) index.
		/// * ValuesByIndex: the variable values.
		/// * bool: is first evaluator call with those variable values.
		/// * double: the row value (returned by the callback).
		/// </summary>
		/// <remarks>This callback must be set before solving the model</remarks>
		public Func<INonlinearModel, int, ValuesByIndex, bool, double> FunctionEvaluator { get; set; }

		/// <summary>
		/// Gradient callback.
		/// * INonlinearModel: the model.
		/// * int: the row (goal or constraint) index.
		/// * ValuesByIndex: the variable values.
		/// * bool: is first evaluator call with those variable values.
		/// * ValuesByIndex: the gradient values (set by the user).
		/// </summary>
		/// <remarks>All entries which related to variables declared as an active by SetActiveVariables method, 
		/// needs to be filled in ValuesByIndex of gradients</remarks>
		public Action<INonlinearModel, int, ValuesByIndex, bool, ValuesByIndex> GradientEvaluator { get; set; }

		/// <summary>
		/// Return the goal collection of this model. 
		/// </summary>
		public IEnumerable<IGoal> Goals
		{
			get
			{
				if (GoalExists)
				{
					yield return _goal;
				}
			}
		}

		/// <summary>
		/// The number of goals in this model
		/// </summary>
		public int GoalCount
		{
			get
			{
				if (!GoalExists)
				{
					return 0;
				}
				return 1;
			}
		}

		/// <summary>
		/// The only goal of the model
		/// </summary>
		/// <returns>The goal if exists, if not return null</returns>
		public IGoal TheGoal => _goal;

		/// <summary>Invoke this function before modfying the model.
		/// </summary>
		protected void PreChange()
		{
			if (_modelReadCount > 0)
			{
				throw new InvalidOperationException(Resources.ModelShouldNotBeEditedWhileEnumeratingRowsOrVariables);
			}
		}

		private void ValidateRowVid(int vid)
		{
			if (!RowExists || vid != 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownRowVariableIndex, new object[1] { vid }));
			}
		}

		/// <summary>
		/// Validate a vid, throwing ArgumentException if not valid.
		/// </summary>
		/// <param name="vid">A vid.</param>
		protected void ValidateVid(int vid)
		{
			if (vid < 0 || vid > VariableCount || (!RowExists && vid == 0))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownVariableId, new object[1] { vid }));
			}
		}

		/// <summary>
		/// This mapped between variable vid to internal model and solver variable index
		/// </summary>
		internal static int GetVariableListIndex(int vid)
		{
			return vid - 1;
		}

		private IEnumerable<object> GetVariableKeys()
		{
			int variableCount = VariableCount;
			int i = 0;
			try
			{
				object[] variableKeys = _variableKeys;
				foreach (object key in variableKeys)
				{
					i++;
					yield return key;
					if (i == variableCount)
					{
						break;
					}
				}
			}
			finally
			{
			}
			int lastNullKeyCount = variableCount - _variableKeys.Length;
			for (i = 0; i < lastNullKeyCount; i++)
			{
				yield return null;
			}
		}

		/// <summary>Creates a new instance.
		/// </summary>
		/// <param name="comparer">Key comparer</param>
		protected UnconstrainedNonlinearModel(IEqualityComparer<object> comparer)
		{
			_variableKeys = new object[0];
			_keyToVidMapping = new Dictionary<object, int>(comparer);
			_variableValues = new Rational[0];
			_vidCount = 0;
			_rowVid = -1;
		}

		/// <summary>
		/// Maps the variable index from the key. If not found, KeyNotFoundException will be thrown 
		/// </summary>
		/// <param name="key"></param>
		/// <returns>variable index </returns>
		public int GetIndexFromKey(object key)
		{
			return _keyToVidMapping[key];
		}

		/// <summary>
		/// Try to get the variable index based on the key
		/// </summary>
		/// <param name="key">the key value </param>
		/// <param name="vid">the variable index </param>
		/// <returns>true if the variable exists, otherwise false</returns>
		public bool TryGetIndexFromKey(object key, out int vid)
		{
			if (_keyToVidMapping.TryGetValue(key, out vid))
			{
				return true;
			}
			vid = -1;
			return false;
		}

		/// <summary>
		/// Map from the variable index to the key. If not found, ArgumentException will be thrown
		/// </summary>
		/// <param name="vid">the variable index</param>
		/// <returns>the variable key</returns>
		/// <remarks>key might be null</remarks>
		public object GetKeyFromIndex(int vid)
		{
			ValidateVid(vid);
			if (IsRow(vid))
			{
				return _rowKey;
			}
			int variableListIndex = GetVariableListIndex(vid);
			if (variableListIndex >= _variableKeys.Length)
			{
				return null;
			}
			return _variableKeys[variableListIndex];
		}

		/// <summary> If the model already includes a row referenced by key, this sets vid to the row’s index and returns false. 
		/// Otherwise, if the model already includes a user variable referenced by key, this sets vid to -1 and returns false. 
		/// Otherwise, this adds a new row associated with key to the model, assigns the next available index to the new row, sets vid to this index, 
		/// and returns true.
		/// CompactQuasiNewtonSolver can have just one row. By convention this row will always have 0 as an index.
		/// </summary>
		/// <param name="key">a key for the row</param>
		/// <param name="vid">a row variable index </param>
		/// <returns>true if added successfully, otherwise false</returns>
		public bool AddRow(object key, out int vid)
		{
			PreChange();
			if (key != null)
			{
				if (_keyToVidMapping.ContainsKey(key))
				{
					vid = -1;
					return false;
				}
				if (KeyComparer.Equals(key, _rowKey))
				{
					vid = _rowVid;
					return false;
				}
			}
			if (RowExists)
			{
				throw new NotSupportedException(Resources.OnlyOneRowIsSupportedForCQNModel);
			}
			vid = 0;
			_rowKey = key;
			_rowVid = vid;
			_rowValue = Rational.Indeterminate;
			return true;
		}

		/// <summary>
		/// validate if it is a row index 
		/// </summary>
		/// <param name="vid">row index</param>
		/// <returns>true if a row otherwise false</returns>
		public bool IsRow(int vid)
		{
			ValidateVid(vid);
			return vid == 0;
		}

		/// <summary>
		/// Adjusts whether the bounds of a vid should be respected or ignored during solving. 
		/// By default, bounds are respected.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <param name="fIgnore">whether to ignore the bounds</param>
		/// <remarks>Not supported by unconstrained solvers</remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported by unconstrained solvers</exception>
		void IRowVariableModel.SetIgnoreBounds(int vid, bool fIgnore)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Get the flag whether is bound is ignored
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if bounds are ignored, otherwise false</returns>
		/// <remarks>Not supported by unconstrained solvers</remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported by unconstrained solvers</exception>
		bool IRowVariableModel.GetIgnoreBounds(int vid)
		{
			throw new NotSupportedException();
		}

		/// <summary>Set or adjust upper and lower bounds for a vid.
		/// </summary>
		/// <param name="vid">The variable index.</param>
		/// <param name="numLo">The lower bound.</param>
		/// <param name="numHi">The upper bound.</param>
		/// <remarks>Not supported by unconstrained solvers.  Logically, a vid may have an upper bound of Infinity and/or a lower 
		/// bound of -Infinity. Specifying other non-finite values should be avoided. 
		/// If a vid has a lower bound that is greater than its upper bound, the model is automatically infeasible, 
		/// and ArgumentException is thrown.  
		/// </remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported by unconstrained solvers.</exception>
		/// <exception cref="T:System.ArgumentException">Thrown if upper and lower bounds are incompatible.</exception>
		public virtual void SetBounds(int vid, Rational numLo, Rational numHi)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Set or adjust the lower bound for a vid.
		/// </summary>
		/// <param name="vid">The variable index.</param>
		/// <param name="numLo">The lower bound.</param>
		/// <remarks>Not supported by unconstrained solvers.</remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported by unconstrained solvers.</exception>
		/// <exception cref="T:System.ArgumentException">Thrown if upper and lower bounds are incompatible.</exception>
		public virtual void SetLowerBound(int vid, Rational numLo)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Set or adjust the upper bound for a vid. 
		/// </summary>
		/// <param name="vid">The variable index.</param>
		/// <param name="numHi">The upper bound.</param>
		/// <remarks>Not supported by unconstrained solvers.</remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported by unconstrained solvers.</exception>
		/// <exception cref="T:System.ArgumentException">Thrown if upper and lower bounds are incompatible.</exception>
		public virtual void SetUpperBound(int vid, Rational numHi)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Return the bounds for a vid.
		/// </summary>
		/// <param name="vid">The variable index.</param>
		/// <param name="numLo">The current lower bound.</param>
		/// <param name="numHi">The current upper bound.</param>
		/// <remarks>Not supported by unconstrained solvers.</remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported by unconstrained solvers.</exception>
		public virtual void GetBounds(int vid, out Rational numLo, out Rational numHi)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// The AddVariable method ensures that a user variable with the given key is in the model.
		/// </summary>
		/// <remarks>
		/// If the model already includes a user variable referenced by key, this sets vid to the variable’s index 
		/// and returns false. Otherwise, if the model already includes a row referenced by key, this sets vid to -1 and returns false. 
		/// Otherwise, this adds a new user variable associated with key to the model, assigns the next available index to the new variable, 
		/// sets vid to this index, and returns true.
		/// By convention variables get indexes from 1 ... VariableCount in the order they were added.
		/// </remarks>
		/// <param name="key">Variable key.</param>
		/// <param name="vid">Variable index.</param>
		/// <returns>True if added successfully, otherwise false.</returns>
		public virtual bool AddVariable(object key, out int vid)
		{
			PreChange();
			if (key != null)
			{
				if (_keyToVidMapping.TryGetValue(key, out vid))
				{
					return false;
				}
				if (KeyComparer.Equals(key, _rowKey))
				{
					vid = -1;
					return false;
				}
			}
			vid = VariableCount + 1;
			if (key != null)
			{
				_keyToVidMapping[key] = vid;
				Statics.EnsureArraySize(ref _variableKeys, vid);
				_variableKeys[vid - 1] = key;
			}
			Statics.EnsureArraySize(ref _variableValues, vid);
			ref Rational reference = ref _variableValues[vid - 1];
			reference = Rational.Indeterminate;
			_vidCount = vid;
			return true;
		}

		/// <summary>
		/// Get the value associated with the variable index. This is typically used when retrieving results.
		/// </summary>
		/// <param name="vid">A variable index.</param>
		/// <returns>The variable value.</returns>
		public Rational GetValue(int vid)
		{
			ValidateVid(vid);
			if (IsRow(vid))
			{
				return _rowValue;
			}
			return _variableValues[GetVariableListIndex(vid)];
		}

		/// <summary>Copy variable values to an array.
		/// </summary>
		/// <param name="x">An array of Double.</param>
		/// <param name="defaultValue">The default value to be substituted for non-finite values.</param>
		protected void CopyVariableValuesTo(double[] x, double defaultValue)
		{
			for (int i = 1; i <= VariableCount; i++)
			{
				int variableListIndex = GetVariableListIndex(i);
				if (_variableValues[variableListIndex].IsFinite)
				{
					x[variableListIndex] = _variableValues[variableListIndex].ToDouble();
				}
				else
				{
					x[variableListIndex] = defaultValue;
				}
			}
		}

		/// <summary>Copy variable values from an array.
		/// </summary>
		/// <param name="x">An array of Double.</param>
		protected void CopyVariableValuesFrom(double[] x)
		{
			for (int i = 1; i <= VariableCount; i++)
			{
				SetValue(i, x[GetVariableListIndex(i)]);
			}
		}

		/// <summary>
		/// The default value for a vid is Indeterminate. 
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <param name="value">The value for the variable.</param> 
		/// <remarks>This class can be used to represent not just a model, 
		/// but also a current state for the model’s variables. 
		/// The state associates with each vid a current value represented as a Rational. 
		/// This state will be used as a starting point when solving. Setting a value for a row is ignored by the solver.</remarks>
		public void SetValue(int vid, Rational value)
		{
			PreChange();
			ValidateVid(vid);
			if (!IsRow(vid))
			{
				_variableValues[GetVariableListIndex(vid)] = value;
			}
		}

		/// <summary>Set the goal value.
		/// </summary>
		/// <param name="value">The value for the goal.</param> 
		protected void SetGoalValue(Rational value)
		{
			_rowValue = value;
		}

		/// <summary>
		/// Mark a variable as an integer variable 
		/// </summary>
		/// <param name="vid">a variable index </param>
		/// <param name="fInteger">whether to be an integer variable</param>
		/// <remarks>Not supported by unconstrained solvers</remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported by unconstrained solvers</exception>
		void IRowVariableModel.SetIntegrality(int vid, bool fInteger)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Check if a variable is an integer variable
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if this variable is an integer variable. Otherwise false.</returns>
		bool IRowVariableModel.GetIntegrality(int vid)
		{
			ValidateVid(vid);
			return false;
		}

		/// <summary>Specify variables that participate in the row. 
		/// </summary>
		/// <param name="rowVid">the row index</param>
		/// <returns>Enumeration of active varibles on rowVid</returns>
		/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index</exception>
		/// <remarks>In the case of a model which has explicit linear terms (implements ILinearModel),
		/// there is no need to specify the linear terms with this call.</remarks>
		/// <remarks>Not supported by unconstrained solvers</remarks>
		/// <exception cref="T:System.NotSupportedException">Not supported by unconstrained solvers</exception>
		IEnumerable<int> INonlinearModel.GetActiveVariables(int rowVid)
		{
			throw new NotSupportedException();
		}

		/// <summary>Is a specific variable active in a specific row
		/// </summary>
		/// <param name="rowVid">the row index</param>
		/// <param name="varVid">the variable index</param>
		/// <returns>true if variable is active, otherwise false</returns>
		/// <remarks>Not supported by unconstrained solvers</remarks>
		/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index, 
		/// or varVid is not a legal variable index</exception>
		/// <exception cref="T:System.NotSupportedException">Not supported by unconstrained solvers</exception>
		bool INonlinearModel.IsActiveVariable(int rowVid, int varVid)
		{
			throw new NotSupportedException();
		}

		/// <summary>Set all variables in a row to be active/inactive
		/// </summary>
		/// <param name="rowVid">the row index</param>
		/// <param name="active">if true, all variables become active, 
		/// if false all variables become inactive</param>
		/// <remarks>Not supported by unconstrained solvers</remarks>
		/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index</exception>
		/// <exception cref="T:System.NotSupportedException">All variables should be active</exception>
		void INonlinearModel.SetActiveVariables(int rowVid, bool active)
		{
			ValidateRowVid(rowVid);
			if (!active)
			{
				throw new NotSupportedException(Resources.OnlyOneRowIsSupportedForCQNModel + Resources.AllVariablesShouldBeActiveForTheRow);
			}
		}

		/// <summary>Set a specific variable in a row to be active/inactive
		/// </summary>
		/// <param name="rowVid">the row index</param>
		/// <param name="varVid">the variable index</param>
		/// <param name="active">if true, the variable becomes active, 
		/// if false it becomes inactive</param>
		/// <remarks>Not supported by unconstrained solvers</remarks>
		/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index, 
		/// or varVid is not a legal variable index</exception>
		/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index</exception>
		/// <exception cref="T:System.NotSupportedException">Not supported by unconstrained solvers</exception>
		void INonlinearModel.SetActiveVariable(int rowVid, int varVid, bool active)
		{
			ValidateRowVid(rowVid);
			if (!active)
			{
				throw new NotSupportedException(Resources.OnlyOneRowIsSupportedForCQNModel + Resources.AllVariablesShouldBeActiveForTheRow);
			}
		}

		/// <summary>Mark a row as a goal.
		/// </summary>
		/// <param name="vid">A row id.</param>
		/// <param name="pri">The priority of a goal.</param>
		/// <param name="minimize">Whether to minimize the goal row.</param>
		/// <returns>An IGoal representing the goal.</returns>
		/// <remarks>This solver supports only one goal.</remarks>
		public IGoal AddGoal(int vid, int pri, bool minimize)
		{
			PreChange();
			ValidateVid(vid);
			if (GoalExists)
			{
				throw new NotSupportedException(Resources.OnlyOneGoalIsSupportedWithCQNSolver);
			}
			_goal = new Goal(this, vid, pri, minimize);
			return _goal;
		}

		/// <summary>
		/// Check if a row id is a goal row 
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <returns>true if this a goal row. otherwise false</returns>
		public bool IsGoal(int vid)
		{
			ValidateVid(vid);
			if (GoalExists && _goal.Index == vid)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Check if a row id is a goal. If true, return the goal entry 
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <param name="goal">return the goal entry</param>
		/// <returns>true if a goal row. otherwise false</returns>
		public bool IsGoal(int vid, out IGoal goal)
		{
			if (IsGoal(vid))
			{
				goal = _goal;
				return true;
			}
			goal = null;
			return false;
		}

		/// <summary>
		/// Remove a goal row
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <returns>true if the goal is removed. otherwise false</returns>
		public bool RemoveGoal(int vid)
		{
			PreChange();
			if (IsGoal(vid))
			{
				_goal = null;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Clear all the goals 
		/// </summary>
		/// <remarks>Not needed for unconstrained solvers</remarks>
		public void ClearGoals()
		{
			PreChange();
			if (GoalExists)
			{
				RemoveGoal(0);
			}
		}

		/// <summary>
		/// Return a goal entry if the row id is a goal
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>A goal entry. Null if not a goal row</returns>
		public IGoal GetGoalFromIndex(int vid)
		{
			ValidateVid(vid);
			if (GoalExists && _goal.Index == vid)
			{
				return _goal;
			}
			return null;
		}

		/// <summary>
		/// Adds a row as a goal.
		/// Unconstrained models can have just one row. By convention this row will always have 0 as an index.
		/// </summary>
		/// <param name="key">a key for the row</param>
		/// <param name="pri">the priority of a goal</param>
		/// <param name="minimize">whether to minimize the goal row</param>
		/// <param name="vid">a row variable index of the goal row if successful, or -1 if not</param>
		/// <returns>the new goal if added successfully, otherwise null</returns>
		/// <remarks>If the model already includes a row referenced by key, and the row is not a goal, this make the row a goal. </remarks>
		public virtual IGoal AddRowAsGoal(object key, int pri, bool minimize, out int vid)
		{
			AddRow(key, out vid);
			if (vid != -1)
			{
				return AddGoal(vid, pri, minimize);
			}
			return null;
		}
	}
}
