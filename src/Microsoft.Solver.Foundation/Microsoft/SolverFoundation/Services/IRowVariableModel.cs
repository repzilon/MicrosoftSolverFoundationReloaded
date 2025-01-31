using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// IRowVariableModel represents optimization models consisting of decision variables and rows.
	/// </summary>
	public interface IRowVariableModel
	{
		/// <summary>
		/// Used for row or variable key comparison 
		/// </summary>
		IEqualityComparer<object> KeyComparer { get; }

		/// <summary> return the variable index collection, inclusive of rows
		/// </summary>
		IEnumerable<int> Indices { get; }

		/// <summary> Return the variable and row key collection.
		/// Indices are guaranteed to &gt;= 0 and &lt; KeyCount.
		/// </summary>
		IEnumerable<object> Keys { get; }

		/// <summary> the number of keys, inclusive of rows and variables.
		/// </summary>
		int KeyCount { get; }

		/// <summary> Return the row index collection. 
		/// </summary>
		IEnumerable<int> RowIndices { get; }

		/// <summary> Return the row key collection. 
		/// </summary>
		IEnumerable<object> RowKeys { get; }

		/// <summary> The number of rows in the model. 
		/// </summary>
		int RowCount { get; }

		/// <summary>
		/// return the variable index collection
		/// </summary>
		IEnumerable<int> VariableIndices { get; }

		/// <summary>
		/// return the variable key collection 
		/// </summary>
		IEnumerable<object> VariableKeys { get; }

		/// <summary>
		/// return the variable count 
		/// </summary>
		int VariableCount { get; }

		/// <summary> return the number of integer variables 
		/// </summary>
		int IntegerIndexCount { get; }

		/// <summary>
		/// Maps the variable index from the key. If not found, KeyNotFoundException will be thrown 
		/// </summary>
		/// <param name="key"></param>
		/// <returns>variable index </returns>
		int GetIndexFromKey(object key);

		/// <summary>
		/// Try to get the variable index based on the key
		/// </summary>
		/// <param name="key">the key value </param>
		/// <param name="vid">the variable index </param>
		/// <returns>true if the variable exists, otherwise false</returns>
		bool TryGetIndexFromKey(object key, out int vid);

		/// <summary>
		/// Map from the variable index to the key. If not found, ArgumentException will be thrown
		/// </summary>
		/// <param name="vid">the variable index</param>
		/// <returns>the variable key</returns>
		/// <remarks>key might be null</remarks>
		object GetKeyFromIndex(int vid);

		/// <summary> If the model already includes a row referenced by key, this sets vid to the row’s index and returns false. 
		/// Otherwise, if the model already includes a user variable referenced by key, this sets vid to -1 and returns false. 
		/// Otherwise, this adds a new row associated with key to the model, assigns the next available index to the new row, sets vid to this index, 
		/// and returns true.
		/// </summary>
		/// <param name="key">a key for the row</param>
		/// <param name="vid">a row variable index </param>
		/// <returns>true if added successfully, otherwise false</returns>
		bool AddRow(object key, out int vid);

		/// <summary>
		/// Validate if it is a row index and not a variable index.
		/// </summary>
		/// <param name="vid">row index</param>
		/// <returns>True if a row otherwise false.</returns>
		bool IsRow(int vid);

		/// <summary>
		/// Adjusts whether the bounds of a vid should be respected or ignored during solving. 
		/// By default, bounds are respected.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <param name="ignore">whether to ignore the bounds</param>
		void SetIgnoreBounds(int vid, bool ignore);

		/// <summary>
		/// Get the flag whether is bound is ignored.
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if bounds are ignored, otherwise false</returns>
		bool GetIgnoreBounds(int vid);

		/// <summary>Set the bounds for a vid.</summary>
		/// <remarks>
		/// Logically, a vid may have an upper bound of Infinity and/or a lower bound of -Infinity. 
		/// Specifying any other non-finite values for bounds should be avoided. 
		/// If a vid has a lower bound that is greater than its upper bound, the model is automatically infeasible, 
		/// and ArgumentException is thrown.  
		/// </remarks>
		/// <param name="vid">A vid.</param>
		/// <param name="lower">The lower bound.</param>
		/// <param name="upper">The upper bound.</param>
		void SetBounds(int vid, Rational lower, Rational upper);

		/// <summary>Set or adjust the lower bound of the vid. 
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <param name="lower">The lower bound.</param>
		void SetLowerBound(int vid, Rational lower);

		/// <summary>Set or adjust the upper bound of the vid. 
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <param name="upper">The upper bound.</param>
		void SetUpperBound(int vid, Rational upper);

		/// <summary> Return the bounds for the vid.
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <param name="lower">The lower bound returned.</param>
		/// <param name="upper">The upper bound returned.</param>
		void GetBounds(int vid, out Rational lower, out Rational upper);

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
		/// Get the value associated with the variable index. This is typically used to fetch solver result 
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>the variable value</returns>
		Rational GetValue(int vid);

		/// <summary>Sets the default value for a vid.
		/// </summary>
		/// <remarks>
		/// The default value for a vid is Indeterminate. An IRowVariableModel can be used to represent not just a model, 
		/// but also a current state for the model’s (user and row) variables. 
		/// The state associates with each vid a current value represented as a Rational. 
		/// This state may be used as a starting point when solving, and may be updated by a solve attempt. 
		/// Some solvers may ignore this initial state for rows and even for variables.
		/// </remarks>
		/// <param name="vid">A vid.</param>
		/// <param name="value">The default value for the variable.</param>    
		void SetValue(int vid, Rational value);

		/// <summary>
		/// Mark a variable as an integer variable 
		/// </summary>
		/// <param name="vid">a variable index </param>
		/// <param name="integer">whether to be an integer variable</param>
		void SetIntegrality(int vid, bool integer);

		/// <summary>
		/// Check if a variable is an integer variable
		/// </summary>
		/// <param name="vid">a variable index</param>
		/// <returns>true if this variable is an integer variable. Otherwise false.</returns>
		bool GetIntegrality(int vid);
	}
}
