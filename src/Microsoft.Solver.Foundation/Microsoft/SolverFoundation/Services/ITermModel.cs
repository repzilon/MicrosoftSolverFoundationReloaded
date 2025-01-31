using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>ITermModel represents optimization models consisting of terms involving decision variables.
	/// ITermModel is capable of representing most type of optimization models.
	/// </summary>
	public interface ITermModel : IRowVariableModel, IGoalModel
	{
		/// <summary>
		/// Adds an operation row to the model. This row can be turned into a constraint by setting the bounds.
		/// Examples of 1-operand operations: identity, negation, not, sin, cos, tan, exp, log, abs
		/// Examples of 2-operand operations: plus, minus, times, quotient, pow, max, min, and, or, equality/inequalities
		/// Examples of 3-operand operations: if
		/// </summary>
		/// <param name="op">The operation.</param>
		/// <param name="vidNew">The vid of the new row, or the vid of an existing row with the same value as the new row.</param>
		/// <param name="vid1">Vid of the first input argument.</param>
		/// <returns>True if a new row was added. False if an existing row was reused.</returns>
		bool AddOperation(TermModelOperation op, out int vidNew, int vid1);

		/// <summary>Adds a binary operation to the model.
		/// </summary>
		/// <param name="op">The operation.</param>
		/// <param name="vidNew">The vid of the new row, or the vid of an existing row with the same value as the new row.</param>
		/// <param name="vid1">Vid of the first input argument.</param>
		/// <param name="vid2">Vid of the second input argument.</param>
		/// <returns>True if a new row was added. False if an existing row was reused.</returns>
		bool AddOperation(TermModelOperation op, out int vidNew, int vid1, int vid2);

		/// <summary>Adds an operation to the model.
		/// </summary>
		/// <param name="op">The operation.</param>
		/// <param name="vidNew">The vid of the new row, or the vid of an existing row with the same value as the new row.</param>
		/// <param name="vid1">Vid of the first input argument.</param>
		/// <param name="vid2">Vid of the second input argument.</param>
		/// <param name="vid3">Vid of the third input argument.</param>
		/// <returns>True if a new row was added. False if an existing row was reused.</returns>
		bool AddOperation(TermModelOperation op, out int vidNew, int vid1, int vid2, int vid3);

		/// <summary>Adds an operation to the model.
		/// </summary>
		/// <param name="op">The operation.</param>
		/// <param name="vidNew">The vid of the new row, or the vid of an existing row with the same value as the new row.</param>
		/// <param name="vids">The vids of the input arguments.</param>
		/// <returns>True if a new row was added. False if an existing row was reused.</returns>
		bool AddOperation(TermModelOperation op, out int vidNew, params int[] vids);

		/// <summary>
		/// Adds a variable to the model, with bounds and integrality given at creation time.
		/// </summary>
		/// <param name="key">The optional key of the new variable, or null.</param>
		/// <param name="vid">The vid of the new variable, or of an existing variable with the same key.</param>
		/// <param name="lower">The lower bound.</param>
		/// <param name="upper">The upper bound.</param>
		/// <param name="isInteger">True if the new variable should be restricted to only integer values.</param>
		/// <returns>True if a new variable was added. False if an existing variable had the same key.</returns>
		bool AddVariable(object key, out int vid, Rational lower, Rational upper, bool isInteger);

		/// <summary>
		/// Adds a variable to the model, with a fixed set of possible values.
		/// </summary>
		/// <param name="key">The optional key of the new variable, or null.</param>
		/// <param name="vid">The vid of the new variable, or of an existing variable with the same key.</param>
		/// <param name="possibleValues">An array of possible values of the new variable. The caller must not modify the array after passing it to this function.</param>
		/// <returns>True if a new variable was added. False if an existing variable had the same key.</returns>
		bool AddVariable(object key, out int vid, IEnumerable<Rational> possibleValues);

		/// <summary>
		/// Adds a constant to the model. Constants are considered rows.
		/// </summary>
		/// <param name="value">The value of the constant to create.</param>
		/// <param name="vid">The vid of the new constant, or the vid of an existing constant with the same value.</param>
		/// <returns>True if a new constant was added. False if an existing constant was reused.</returns>
		bool AddConstant(Rational value, out int vid);

		/// <summary>
		/// Tests if a vid is an operation (not a variable or constant).
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <returns></returns>
		bool IsOperation(int vid);

		/// <summary>
		/// Tests if a vid is a constant (not a variable or operation).
		/// </summary>
		/// <param name="vid"></param>
		/// <returns></returns>
		bool IsConstant(int vid);

		/// <summary>
		/// Gets the operation associated with a vid.
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <returns>The operation.</returns>
		TermModelOperation GetOperation(int vid);

		/// <summary>
		/// Gets the number of operands associated with a vid.
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <returns>The operand count.</returns>
		int GetOperandCount(int vid);

		/// <summary>
		/// Gets the operands associated with a vid.
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <returns>All the vids of the operands.</returns>
		IEnumerable<int> GetOperands(int vid);

		/// <summary>
		/// Gets an operand associated with a vid.
		/// </summary>
		/// <param name="vid">A vid.</param>
		/// <param name="index">An operand index.</param>
		/// <returns>The vid of the operand.</returns>
		int GetOperand(int vid, int index);
	}
}
