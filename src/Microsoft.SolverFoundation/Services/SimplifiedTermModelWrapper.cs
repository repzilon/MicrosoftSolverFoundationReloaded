using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// This class wraps an ITermModel and applies some simplifications as the model is built.
	/// </summary>
	internal class SimplifiedTermModelWrapper : ITermModel, IRowVariableModel, IGoalModel
	{
		internal ITermModel _model;

		private Dictionary<Rational, int> _constantVidCache;

		private Dictionary<Tuple<TermModelOperation, int>, int> _unaryOperationCache;

		private Dictionary<Tuple<TermModelOperation, int, int>, int> _binaryOperationCache;

		/// <summary>
		/// Used for row or variable key comparison 
		/// </summary>
		public IEqualityComparer<object> KeyComparer => _model.KeyComparer;

		/// <summary> return the variable index collection, inclusive of rows
		/// </summary>
		public IEnumerable<int> Indices => _model.Indices;

		/// <summary> Return the variable and row key collection.
		/// Indices are guaranteed to &gt;= 0 and &lt; KeyCount.
		/// </summary>
		public IEnumerable<object> Keys => _model.Keys;

		/// <summary> the number of keys, inclusive of rows and variables.
		/// </summary>
		public int KeyCount => _model.KeyCount;

		/// <summary> Return the row index collection. 
		/// </summary>
		public IEnumerable<int> RowIndices => _model.RowIndices;

		/// <summary> Return the row key collection. 
		/// </summary>
		public IEnumerable<object> RowKeys => _model.RowKeys;

		/// <summary> The number of rows in the model. 
		/// </summary>
		public int RowCount => _model.RowCount;

		/// <summary>
		/// return the variable index collection
		/// </summary>
		public IEnumerable<int> VariableIndices => _model.VariableIndices;

		/// <summary>
		/// return the variable key collection 
		/// </summary>
		public IEnumerable<object> VariableKeys => _model.VariableKeys;

		/// <summary>
		/// return the variable count 
		/// </summary>
		public int VariableCount => _model.VariableCount;

		/// <summary> return the number of integer variables 
		/// </summary>
		public int IntegerIndexCount => _model.IntegerIndexCount;

		/// <summary>
		/// Return the goal collection of this model. 
		/// </summary>
		public IEnumerable<IGoal> Goals => _model.Goals;

		/// <summary>
		/// The number of goals in this model
		/// </summary>
		public int GoalCount => _model.GoalCount;

		internal int MaxVid
		{
			get
			{
				if (_model is TermModel termModel)
				{
					return termModel.MaxVid;
				}
				return _model.Indices.Max();
			}
		}

		internal SimplifiedTermModelWrapper(ITermModel model)
		{
			_model = model;
			_constantVidCache = new Dictionary<Rational, int>();
			_unaryOperationCache = new Dictionary<Tuple<TermModelOperation, int>, int>();
			_binaryOperationCache = new Dictionary<Tuple<TermModelOperation, int, int>, int>();
		}

		/// <summary>
		/// Adds an operation row to the model. This row can be turned into a constraint by setting the bounds.
		/// Examples of 1-operand operations: identity, negation, not, sin, cos, tan, exp, log, abs
		/// Examples of 2-operand operations: plus, minus, times, quotient, pow, max, min, and, or, equality/inequalities
		/// Examples of 3-operand operations: if
		/// </summary>
		/// <param name="op">The operation.</param>
		/// <param name="vidNew">The vid of the new row, or the vid of an existing row that would have the same value as the new row.</param>
		/// <param name="vid1">Vid of the input argument.</param>
		/// <returns>True if a new row was added. False if an existing row was reused.</returns>
		public bool AddOperation(TermModelOperation op, out int vidNew, int vid1)
		{
			if (op == TermModelOperation.Identity)
			{
				return _model.AddOperation(op, out vidNew, vid1);
			}
			if (_model.IsConstant(vid1))
			{
				Rational value = OperationHelpers.EvaluateUnaryOp(op, _model.GetValue(vid1));
				return AddConstant(value, out vidNew);
			}
			Tuple<TermModelOperation, int> key = Tuple.Create(op, vid1);
			if (_unaryOperationCache.TryGetValue(key, out vidNew))
			{
				return false;
			}
			bool result = _model.AddOperation(op, out vidNew, vid1);
			_unaryOperationCache.Add(key, vidNew);
			return result;
		}

		/// <summary>
		/// Adds an operation row to the model.
		/// </summary>
		/// <param name="op">The operation.</param>
		/// <param name="vidNew">The vid of the new row, or the vid of an existing row with the same value as the new row.</param>
		/// <param name="vid1">Vid of the first input argument.</param>
		/// <param name="vid2">Vid of the second input argument.</param>
		/// <returns>True if a new row was added. False if an existing row was reused.</returns>
		public bool AddOperation(TermModelOperation op, out int vidNew, int vid1, int vid2)
		{
			if (_model.IsConstant(vid1) && _model.IsConstant(vid2))
			{
				Rational value = OperationHelpers.EvaluateBinaryOp(op, _model.GetValue(vid1), _model.GetValue(vid2));
				return AddConstant(value, out vidNew);
			}
			switch (op)
			{
			case TermModelOperation.Plus:
				return AddSimplifiedPlus(op, vid1, vid2, out vidNew);
			case TermModelOperation.Times:
				return AddSimplifiedTimes(op, vid1, vid2, out vidNew);
			case TermModelOperation.Max:
			case TermModelOperation.Min:
			case TermModelOperation.And:
			case TermModelOperation.Or:
			case TermModelOperation.Equal:
				return AddCommutativeOp(op, vid1, vid2, out vidNew);
			case TermModelOperation.Quotient:
				return AddSimplifiedQuotient(op, vid1, vid2, out vidNew);
			case TermModelOperation.Power:
				return AddSimplifiedPower(op, vid1, vid2, out vidNew);
			default:
				return AddNoncommutativeOp(op, vid1, vid2, out vidNew);
			}
		}

		private bool AddSimplifiedPlus(TermModelOperation op, int vid1, int vid2, out int vidNew)
		{
			if (_model.IsConstant(vid1) && _model.GetValue(vid1).IsZero)
			{
				vidNew = vid2;
				return false;
			}
			if (_model.IsConstant(vid2) && _model.GetValue(vid2).IsZero)
			{
				vidNew = vid1;
				return false;
			}
			if (_model.IsOperation(vid1) && _model.GetOperation(vid1) == TermModelOperation.Minus && _model.GetOperand(vid1, 0) == vid2)
			{
				return AddConstant(0, out vidNew);
			}
			if (_model.IsOperation(vid2) && _model.GetOperation(vid2) == TermModelOperation.Minus && _model.GetOperand(vid2, 0) == vid1)
			{
				return AddConstant(0, out vidNew);
			}
			return AddCommutativeOp(op, vid1, vid2, out vidNew);
		}

		private bool AddSimplifiedTimes(TermModelOperation op, int vid1, int vid2, out int vidNew)
		{
			if (_model.IsConstant(vid1))
			{
				Rational value = _model.GetValue(vid1);
				if (value.IsOne)
				{
					vidNew = vid2;
					return false;
				}
				if (value == -1)
				{
					return AddOperation(TermModelOperation.Minus, out vidNew, vid2);
				}
			}
			if (_model.IsConstant(vid2))
			{
				Rational value2 = _model.GetValue(vid2);
				if (value2.IsOne)
				{
					vidNew = vid1;
					return false;
				}
				if (value2 == -1)
				{
					return AddOperation(TermModelOperation.Minus, out vidNew, vid1);
				}
			}
			return AddCommutativeOp(op, vid1, vid2, out vidNew);
		}

		private bool AddSimplifiedQuotient(TermModelOperation op, int vid1, int vid2, out int vidNew)
		{
			if (vid1 == vid2)
			{
				return AddConstant(1, out vidNew);
			}
			if (_model.IsConstant(vid2))
			{
				Rational value = _model.GetValue(vid2);
				if (value.IsOne)
				{
					vidNew = vid1;
					return false;
				}
				if (value == -1)
				{
					return AddOperation(TermModelOperation.Minus, out vidNew, vid1);
				}
			}
			return AddNoncommutativeOp(op, vid1, vid2, out vidNew);
		}

		private bool AddSimplifiedPower(TermModelOperation op, int vid1, int vid2, out int vidNew)
		{
			if (_model.IsConstant(vid2))
			{
				Rational value = _model.GetValue(vid2);
				if (value.IsOne)
				{
					vidNew = vid1;
					return false;
				}
				if (value == 2)
				{
					return AddOperation(TermModelOperation.Times, out vidNew, vid1, vid1);
				}
			}
			return AddNoncommutativeOp(op, vid1, vid2, out vidNew);
		}

		private bool AddNoncommutativeOp(TermModelOperation op, int vid1, int vid2, out int vidNew)
		{
			Tuple<TermModelOperation, int, int> cacheKey = Tuple.Create(op, vid1, vid2);
			return AddCachedOp(op, vid1, vid2, cacheKey, out vidNew);
		}

		private bool AddCommutativeOp(TermModelOperation op, int vid1, int vid2, out int vidNew)
		{
			Tuple<TermModelOperation, int, int> cacheKey = ((vid1 > vid2) ? Tuple.Create(op, vid2, vid1) : Tuple.Create(op, vid1, vid2));
			return AddCachedOp(op, vid1, vid2, cacheKey, out vidNew);
		}

		private bool AddCachedOp(TermModelOperation op, int vid1, int vid2, Tuple<TermModelOperation, int, int> cacheKey, out int vidNew)
		{
			if (_binaryOperationCache.TryGetValue(cacheKey, out vidNew))
			{
				return false;
			}
			bool result = _model.AddOperation(op, out vidNew, vid1, vid2);
			_binaryOperationCache.Add(cacheKey, vidNew);
			return result;
		}

		/// <summary>
		/// Adds an operation row to the model.
		/// </summary>
		/// <param name="op">The operation.</param>
		/// <param name="vidNew">The vid of the new row, or the vid of an existing row with the same value as the new row.</param>
		/// <param name="vid1">Vid of the first input argument.</param>
		/// <param name="vid2">Vid of the second input argument.</param>
		/// <param name="vid3">Vid of the third input argument.</param>
		/// <returns>True if a new row was added. False if an existing row was reused.</returns>
		public bool AddOperation(TermModelOperation op, out int vidNew, int vid1, int vid2, int vid3)
		{
			return _model.AddOperation(op, out vidNew, vid1, vid2, vid3);
		}

		/// <summary>
		/// Adds an operation row to the model.
		/// </summary>
		/// <param name="op">The operation.</param>
		/// <param name="vidNew">The vid of the new row, or the vid of an existing row with the same value as the new row.</param>
		/// <param name="vids">The vids of the input arguments.</param>
		/// <returns>True if a new row was added. False if an existing row was reused.</returns>
		public bool AddOperation(TermModelOperation op, out int vidNew, params int[] vids)
		{
			if (vids.Length == 1)
			{
				return AddOperation(op, out vidNew, vids[0]);
			}
			if (vids.Length == 2)
			{
				return AddOperation(op, out vidNew, vids[0], vids[1]);
			}
			return _model.AddOperation(op, out vidNew, vids);
		}

		/// <summary>
		/// Adds a variable to the model, with bounds and integrality given at creation time.
		/// </summary>
		/// <param name="key">The optional key of the new variable, or null.</param>
		/// <param name="vid">The vid of the new variable, or of an existing variable with the same key.</param>
		/// <param name="lower">The lower bound.</param>
		/// <param name="upper">The upper bound.</param>
		/// <param name="isInteger">True if the new variable should be restricted to only integer values.</param>
		/// <returns>True if a new variable was added. False if an existing variable had the same key.</returns>
		public bool AddVariable(object key, out int vid, Rational lower, Rational upper, bool isInteger)
		{
			return _model.AddVariable(key, out vid, lower, upper, isInteger);
		}

		/// <summary>
		/// Adds a variable to the model, with a fixed set of possible values.
		/// </summary>
		/// <param name="key">The optional key of the new variable, or null.</param>
		/// <param name="vid">The vid of the new variable, or of an existing variable with the same key.</param>
		/// <param name="possibleValues">An array of possible values of the new variable. The caller must not modify the array after passing it to this function.</param>
		/// <returns>True if a new variable was added. False if an existing variable had the same key.</returns>
		public bool AddVariable(object key, out int vid, IEnumerable<Rational> possibleValues)
		{
			return _model.AddVariable(key, out vid, possibleValues);
		}

		/// <summary>
		/// Adds a constant to the model. Constants are considered rows.
		/// </summary>
		/// <param name="value">The value of the constant to create.</param>
		/// <param name="vid">The vid of the new constant, or the vid of an existing constant with the same value.</param>
		/// <returns>True if a new constant was added. False if an existing constant was reused.</returns>
		public bool AddConstant(Rational value, out int vid)
		{
			if (_constantVidCache.TryGetValue(value, out vid))
			{
				return false;
			}
			bool result = _model.AddConstant(value, out vid);
			_constantVidCache.Add(value, vid);
			return result;
		}

		/// <summary>
		/// Tests if a vid is an operation (not a variable or constant).
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <returns></returns>
		public bool IsOperation(int vid)
		{
			return _model.IsOperation(vid);
		}

		/// <summary>
		/// Tests if a vid is a constant (not a variable or operation).
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <returns></returns>
		public bool IsConstant(int vid)
		{
			return _model.IsConstant(vid);
		}

		/// <summary>
		/// Gets the operation associated with a vid.
		/// </summary>
		/// <param name="vid">The vid of an operation.</param>
		/// <returns>The operation.</returns>
		public TermModelOperation GetOperation(int vid)
		{
			return _model.GetOperation(vid);
		}

		/// <summary>
		/// Gets the number of operands associated with a vid.
		/// </summary>
		/// <param name="vid">The vid of an operation.</param>
		/// <returns>The operand count.</returns>
		public int GetOperandCount(int vid)
		{
			return _model.GetOperandCount(vid);
		}

		/// <summary>
		/// Gets the operands associated with a vid.
		/// </summary>
		/// <param name="vid">The vid of an operation.</param>
		/// <returns>All the vids of the operands.</returns>
		public IEnumerable<int> GetOperands(int vid)
		{
			return _model.GetOperands(vid);
		}

		/// <summary>
		/// Gets an operand associated with a vid.
		/// </summary>
		/// <param name="vid">The vid of an operation.</param>
		/// <param name="index">The operand index.</param>
		/// <returns>The vid of the operand</returns>
		public int GetOperand(int vid, int index)
		{
			return _model.GetOperand(vid, index);
		}

		/// <summary>
		/// Maps the variable index from the key. If not found, KeyNotFoundException will be thrown 
		/// </summary>
		/// <param name="key"></param>
		/// <returns>variable index </returns>
		public int GetIndexFromKey(object key)
		{
			return _model.GetIndexFromKey(key);
		}

		/// <summary>
		/// Try to get the variable index based on the key
		/// </summary>
		/// <param name="key">the key value </param>
		/// <param name="vid">the variable index </param>
		/// <returns>true if the variable exists, otherwise false</returns>
		public bool TryGetIndexFromKey(object key, out int vid)
		{
			return _model.TryGetIndexFromKey(key, out vid);
		}

		/// <summary>
		/// Map from the variable index to the key. If not found, ArgumentException will be thrown
		/// </summary>
		/// <param name="vid">the variable index</param>
		/// <returns>the variable key</returns>
		/// <remarks>key might be null</remarks>
		public object GetKeyFromIndex(int vid)
		{
			return _model.GetKeyFromIndex(vid);
		}

		/// <summary> If the model already includes a row referenced by key, this sets vid to the row’s index and returns false. 
		/// Otherwise, if the model already includes a user variable referenced by key, this sets vid to -1 and returns false. 
		/// Otherwise, this adds a new row associated with key to the model, assigns the next available index to the new row, sets vid to this index, 
		/// and returns true.
		/// </summary>
		/// <param name="key">a key for the row</param>
		/// <param name="vid">a row variable index </param>
		/// <returns>true if added successfully, otherwise false</returns>
		public bool AddRow(object key, out int vid)
		{
			return _model.AddRow(key, out vid);
		}

		/// <summary>
		/// Validate if it is a row index and not a variable index.
		/// </summary>
		/// <param name="vid">row index</param>
		/// <returns>True if a row otherwise false.</returns>
		public bool IsRow(int vid)
		{
			return _model.IsRow(vid);
		}

		/// <summary>
		/// Adjusts whether the bounds of a vid should be respected or ignored during solving. 
		/// By default, bounds are respected.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <param name="fIgnore">whether to ignore the bounds</param>
		public void SetIgnoreBounds(int vid, bool fIgnore)
		{
			_model.SetIgnoreBounds(vid, fIgnore);
		}

		/// <summary>
		/// Get the flag whether is bound is ignored.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if bounds are ignored, otherwise false</returns>
		public bool GetIgnoreBounds(int vid)
		{
			return _model.GetIgnoreBounds(vid);
		}

		/// <summary>
		/// Logically, a vid may have an upper bound of Infinity and/or a lower bound of -Infinity. 
		/// Specifying any other non-finite values for bounds should be avoided. 
		/// If a vid has a lower bound that is greater than its upper bound, the model is automatically infeasible, 
		/// now the ArgumentException is thrown for this case.  
		/// </summary>
		/// <param name="vid">the variable index </param>
		/// <param name="numLo">lower bound</param>
		/// <param name="numHi">upper bound</param>
		public void SetBounds(int vid, Rational numLo, Rational numHi)
		{
			_model.SetBounds(vid, numLo, numHi);
		}

		/// <summary>
		/// set or adjust the lower bound of the variable 
		/// </summary>
		/// <param name="vid">the variable index </param>
		/// <param name="numLo">lower bound</param>
		public void SetLowerBound(int vid, Rational numLo)
		{
			_model.SetLowerBound(vid, numLo);
		}

		/// <summary>
		/// set or adjust the upper bound of the variable 
		/// </summary>
		/// <param name="vid">the variable index</param>
		/// <param name="numHi">the upper bound</param>
		public void SetUpperBound(int vid, Rational numHi)
		{
			_model.SetUpperBound(vid, numHi);
		}

		/// <summary>
		/// Return the bounds for the variable 
		/// </summary>
		/// <param name="vid">the variable index</param>
		/// <param name="numLo">the lower bound returned</param>
		/// <param name="numHi">the upper bound returned</param>
		public void GetBounds(int vid, out Rational numLo, out Rational numHi)
		{
			_model.GetBounds(vid, out numLo, out numHi);
		}

		/// <summary>
		/// The AddVariable method ensures that a user variable with the given key is in the model.
		/// If the model already includes a user variable referenced by key, this sets vid to the variable’s index 
		/// and returns false. Otherwise, if the model already includes a row referenced by key, this sets vid to -1 and returns false. 
		/// Otherwise, this adds a new user variable associated with key to the model, assigns the next available index to the new variable, 
		/// sets vid to this index, and returns true.
		/// </summary>
		/// <param name="key"> Variable key </param>
		/// <param name="vid">variable index </param>
		/// <returns>true if added successfully, otherwise false</returns>
		public bool AddVariable(object key, out int vid)
		{
			return _model.AddVariable(key, out vid);
		}

		/// <summary>
		/// Get the value associated with the variable index. This is typically used to fetch solver result 
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>the variable value</returns>
		public Rational GetValue(int vid)
		{
			return _model.GetValue(vid);
		}

		/// <summary>
		/// The default value for a vid is Indeterminate. An IRowVariableModel can be used to represent not just a model, 
		/// but also a current state for the model’s (user and row) variables. 
		/// The state associates with each vid a current value represented as a Rational. 
		/// This state may be used as a starting point when solving, and may be updated by a solve attempt. 
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <param name="num">the value for the variable</param>    
		public void SetValue(int vid, Rational num)
		{
			_model.SetValue(vid, num);
		}

		/// <summary>
		/// Mark a variable as an integer variable 
		/// </summary>
		/// <param name="vid">a variable index </param>
		/// <param name="fInteger">whether to be an integer variable</param>
		public void SetIntegrality(int vid, bool fInteger)
		{
			_model.SetIntegrality(vid, fInteger);
		}

		/// <summary>
		/// Check if a variable is an integer variable
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if this variable is an integer variable. Otherwise false.</returns>
		public bool GetIntegrality(int vid)
		{
			return _model.GetIntegrality(vid);
		}

		/// <summary>
		/// Mark a row as a goal row 
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <param name="pri">the priority of a goal</param>
		/// <param name="fMinimize">whether to minimize the goal row</param>
		/// <returns>the goal entry</returns>
		public IGoal AddGoal(int vid, int pri, bool fMinimize)
		{
			return _model.AddGoal(vid, pri, fMinimize);
		}

		/// <summary>
		/// Check if a row id is a goal row 
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <returns>true if this a goal row. otherwise false</returns>
		public bool IsGoal(int vid)
		{
			return _model.IsGoal(vid);
		}

		/// <summary>
		/// Check if a row id is a goal. If true, return the goal entry 
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <param name="goal">return the goal entry</param>
		/// <returns>true if a goal row. otherwise false</returns>
		public bool IsGoal(int vid, out IGoal goal)
		{
			return _model.IsGoal(vid, out goal);
		}

		/// <summary>
		/// Remove a goal row
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <returns>true if the goal is removed. otherwise false</returns>
		public bool RemoveGoal(int vid)
		{
			return _model.RemoveGoal(vid);
		}

		/// <summary>
		/// Clear all the goals 
		/// </summary>
		public void ClearGoals()
		{
			_model.ClearGoals();
		}

		/// <summary>
		/// Return a goal entry if the row id is a goal
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>A goal entry. Null if not a goal row</returns>
		public IGoal GetGoalFromIndex(int vid)
		{
			return _model.GetGoalFromIndex(vid);
		}
	}
}
