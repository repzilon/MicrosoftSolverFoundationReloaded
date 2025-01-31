using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// An ILinearModel instance encapsulates a linear optimization problem consisting of decision variables, 
	/// constraints, and goals. The LinearModel class implements ILinearModel 
	/// and the SimplexSolver class derives from LinearModel.
	/// </summary>
	public interface ILinearModel
	{
		/// <summary>
		/// Used for row or variable key comparison 
		/// </summary>
		IEqualityComparer<object> KeyComparer { get; }

		/// <summary> the number of keys, inclusive of rows and variables.
		/// </summary>
		int KeyCount { get; }

		/// <summary> Return the variable and row key collection.
		/// Indices are guaranteed to &gt;= 0 and &lt; KeyCount.
		/// </summary>
		IEnumerable<object> Keys { get; }

		/// <summary> return the variable index collection, inclusive of rows
		/// </summary>
		IEnumerable<int> Indices { get; }

		/// <summary> return the number of integer variables 
		/// </summary>
		int IntegerIndexCount { get; }

		/// <summary> Is the model has a quadratic term on the objective function 
		/// </summary>
		bool IsQuadraticModel { get; }

		/// <summary> the number of rows in the model 
		/// </summary>
		int RowCount { get; }

		/// <summary> return the row key collection 
		/// </summary>
		IEnumerable<object> RowKeys { get; }

		/// <summary> return the row index collection 
		/// </summary>
		IEnumerable<int> RowIndices { get; }

		/// <summary>
		/// return the variable count 
		/// </summary>
		int VariableCount { get; }

		/// <summary>
		/// return the variable key collection 
		/// </summary>
		IEnumerable<object> VariableKeys { get; }

		/// <summary>
		/// return the variable index collection
		/// </summary>
		IEnumerable<int> VariableIndices { get; }

		/// <summary> Is the linear model SOS
		/// </summary>
		bool IsSpecialOrderedSet { get; }

		/// <summary>
		/// The number of non-zero coefficient 
		/// </summary>
		int CoefficientCount { get; }

		/// <summary>
		/// The number of goals in this linear model
		/// </summary>
		int GoalCount { get; }

		/// <summary>
		/// Return the goal collection of this linear model. 
		/// </summary>
		IEnumerable<ILinearGoal> Goals { get; }

		/// <summary> Does the variable participate in any quadratic row
		/// </summary>
		/// <param name="vidVar"> any valid vid </param>
		/// <returns></returns>
		bool IsQuadraticVariable(int vidVar);

		/// <summary> If the model already includes a row referenced by key, this sets vid to the row’s index and returns false. 
		/// Otherwise, if the model already includes a user variable referenced by key, this sets vid to -1 and returns false. 
		/// Otherwise, this adds a new row associated with key to the model, assigns the next available index to the new row, sets vid to this index, 
		/// and returns true.
		/// </summary>
		/// <param name="key">a key for the row</param>
		/// <param name="vid">a row variable index </param>
		/// <returns>true if added successfully, otherwise false</returns>
		bool AddRow(object key, out int vid);

		/// <summary> Add a reference row for a SOS set. Each SOS set has one reference row
		/// </summary>
		/// <param name="key">a SOS key</param>
		/// <param name="sos">type of SOS</param>
		/// <param name="vidRow">the vid of the reference row</param>
		/// <returns></returns>
		bool AddRow(object key, SpecialOrderedSetType sos, out int vidRow);

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
		bool AddVariable(object key, out int vid);

		/// <summary>
		/// validate if it is a row index 
		/// </summary>
		/// <param name="vid">row index</param>
		/// <returns>true if a row otherwise false</returns>
		bool IsRow(int vid);

		/// <summary>
		/// Try to get the variable index based on the key
		/// </summary>
		/// <param name="key">the key value</param>
		/// <param name="vid">the variable index</param>
		/// <returns>true if the variable exists, otherwise false</returns>
		bool TryGetIndexFromKey(object key, out int vid);

		/// <summary>
		/// Maps the variable index from the key. If not found, KeyNotFoundException will be thrown 
		/// </summary>
		/// <param name="key">the key value</param>
		/// <returns>variable index </returns>
		int GetIndexFromKey(object key);

		/// <summary>
		/// Map from the variable index to the key. If not found, ArgumentException will be thrown
		/// </summary>
		/// <param name="vid">the variable index</param>
		/// <returns>the variable key</returns>
		/// <remarks>key might be null</remarks>
		object GetKeyFromIndex(int vid);

		/// <summary>
		/// Logically, a vid may have an upper bound of Infinity and/or a lower bound of -Infinity. 
		/// Specifying any other non-finite values for bounds should be avoided. 
		/// If a vid has a lower bound that is greater than its upper bound, the model is automatically infeasible, 
		/// now the ArgumentException is thrown for this case.  
		/// </summary>
		/// <param name="vid">the variable index </param>
		/// <param name="numLo">lower bound</param>
		/// <param name="numHi">upper bound</param>
		void SetBounds(int vid, Rational numLo, Rational numHi);

		/// <summary>
		/// set or adjust the lower bound of the variable 
		/// </summary>
		/// <param name="vid">the variable index </param>
		/// <param name="numLo">lower bound</param>
		void SetLowerBound(int vid, Rational numLo);

		/// <summary>
		/// set or adjust the upper bound of the variable 
		/// </summary>
		/// <param name="vid">the variable index</param>
		/// <param name="numHi">the upper bound</param>
		void SetUpperBound(int vid, Rational numHi);

		/// <summary>
		/// Return the bounds for the variable 
		/// </summary>
		/// <param name="vid">the variable index</param>
		/// <param name="numLo">the lower bound returned</param>
		/// <param name="numHi">the upper bound returned</param>
		void GetBounds(int vid, out Rational numLo, out Rational numHi);

		/// <summary>
		/// The default value for a vid is Indeterminate. An ILinearModel can be used to represent not just a linear model, 
		/// but also a current state for the model’s (user and row) variables. 
		/// The state associates with each vid a current value, represented as a Rational, and a basis status, represented as a boolean. 
		/// This state may be used as a starting point when solving, and may be updated by a solve attempt. 
		/// In particular, invoking the Solve method of the SimplexSolver class updates the values and basis status appropriately.
		/// Some other solvers may ignore this initial state for rows and even for variables.
		/// </summary>
		/// <param name="vid">variable index</param>
		/// <param name="num">current value</param>    
		void SetValue(int vid, Rational num);

		/// <summary>
		/// Get the value associated with the variable index. This is typically used to fetch solver result 
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>the variable value</returns>
		Rational GetValue(int vid);

		/// <summary>
		/// Get the value state of this variable 
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>variable state</returns>
		LinearValueState GetValueState(int vid);

		/// <summary>
		/// Adjusts whether the bounds of a vid should be respected or ignored during solving. 
		/// By default, bounds are respected.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <param name="fIgnore">whether to ignore the bounds</param>
		void SetIgnoreBounds(int vid, bool fIgnore);

		/// <summary>
		/// Get the flag whether is bound is ignored
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if bounds are ignored, otherwise false</returns>
		bool GetIgnoreBounds(int vid);

		/// <summary>
		/// The SetBasic method sets the basis status for a variable. The default basis status for a variable is false. 
		/// The SimplexSolver class updates these flags after a solve attempt.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <param name="fBasic">whether set it to a basic variable</param>
		void SetBasic(int vid, bool fBasic);

		/// <summary>
		/// Get the basis status for this variable 
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if this variable is a basic variable. otherwise false</returns>
		bool GetBasic(int vid);

		/// <summary>
		/// Mark a variable as an integer variable 
		/// </summary>
		/// <param name="vid">a variable index </param>
		/// <param name="fInteger">whether to be an integer variable</param>
		void SetIntegrality(int vid, bool fInteger);

		/// <summary>
		/// Check if a variable is an integer variable
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if this variable is an integer variable. Otherwise false.</returns>
		bool GetIntegrality(int vid);

		/// <summary>
		/// Set the coefficient of the A matrix in the linear model. If num is zero, the entry is removed. 
		/// </summary>
		/// <param name="vidRow">a row id </param>
		/// <param name="vidVar">a column/variable id</param>
		/// <param name="num">a value</param>
		void SetCoefficient(int vidRow, int vidVar, Rational num);

		/// <summary>
		/// Set the coefficient of the Q matrix on the objective row. If num is zero, the entry is removed. 
		/// This is used for quadratic terms on the objective row.
		/// </summary>
		/// <param name="vidRow">a goal row</param>
		/// <param name="num">a value </param>
		/// <param name="vidVar1">a column/variable id</param>
		/// <param name="vidVar2">another column/variable id</param>
		void SetCoefficient(int vidRow, Rational num, int vidVar1, int vidVar2);

		/// <summary>
		/// Return the coefficient of the A matrix in the linear model.
		/// </summary>
		/// <param name="vidRow">a row id</param>
		/// <param name="vidVar">a column/variable id</param>
		/// <returns>a coefficient value</returns>
		Rational GetCoefficient(int vidRow, int vidVar);

		/// <summary>
		/// Return the coefficient of the Q matrix on the objective row.
		/// </summary>
		/// <param name="goalRow">a goal row</param>
		/// <param name="vidVar1">a column/variable id</param>
		/// <param name="vidVar2">another column/variable id</param>
		/// <returns>a coefficient value</returns>
		Rational GetCoefficient(int goalRow, int vidVar1, int vidVar2);

		/// <summary>
		/// Return the number of non-zero coefficients for the given row index
		/// </summary>
		/// <param name="vidRow">a row id</param>
		/// <returns>number of non-zero entries</returns>
		int GetRowEntryCount(int vidRow);

		/// <summary>
		/// Return a collection of non-zero variable entries
		/// </summary>
		/// <param name="vidRow"></param>
		/// <returns>the variable collection</returns>
		IEnumerable<LinearEntry> GetRowEntries(int vidRow);

		/// <summary>
		/// Return a collection of non-zero variable entries on the
		/// quadratic row
		/// </summary>
		/// <param name="vidRow"></param>
		/// <returns>the variable collection</returns>
		IEnumerable<QuadraticEntry> GetRowQuadraticEntries(int vidRow);

		/// <summary>
		/// Return the number of non-zero coefficients for the given variable/column index
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>number of non-zero entries</returns>
		int GetVariableEntryCount(int vid);

		/// <summary>
		/// Return a collection of non-zero column entries
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>number of non-zero entries</returns>
		IEnumerable<LinearEntry> GetVariableEntries(int vid);

		/// <summary> Return a list of SOS1 or SOS2 rows
		/// </summary>
		/// <param name="sosType"></param>
		/// <returns></returns>
		IEnumerable<int> GetSpecialOrderedSetTypeRowIndexes(SpecialOrderedSetType sosType);

		/// <summary>
		/// Mark a row as a goal row 
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <param name="pri">the priority of a goal</param>
		/// <param name="fMinimize">whether to minimize the goal row</param>
		/// <returns>the goal entry</returns>
		ILinearGoal AddGoal(int vid, int pri, bool fMinimize);

		/// <summary>
		/// Clear all the goals 
		/// </summary>
		void ClearGoals();

		/// <summary>
		/// Remove a goal row
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <returns>true if the goal is removed. otherwise false</returns>
		bool RemoveGoal(int vid);

		/// <summary>
		/// Check if a row id is a goal row 
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <returns>true if this a goal row. otherwise false</returns>
		bool IsGoal(int vid);

		/// <summary>
		/// Check if a row id is a goal. If true, return the goal entry 
		/// </summary>
		/// <param name="vid">a row id</param>
		/// <param name="goal">return the goal entry</param>
		/// <returns>true if a goal row. otherwise false</returns>
		bool IsGoal(int vid, out ILinearGoal goal);

		/// <summary>
		/// Return a goal entry if the row id is a goal
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>A goal entry. Null if not a goal row</returns>
		ILinearGoal GetGoalFromIndex(int vid);
	}
}
