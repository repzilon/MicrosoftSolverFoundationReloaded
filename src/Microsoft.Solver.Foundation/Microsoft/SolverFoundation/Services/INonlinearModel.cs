using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// INonlinearModel represents a non-linear optimization model. It has row, variables and goals, and in addition it has callback that 
	/// define the values and possible the gradients of the rows.
	/// </summary>
	public interface INonlinearModel : IRowVariableModel, IGoalModel
	{
		/// <summary>
		/// Function value callback. The callback will be invoked periodically by the solver in order to obtain
		/// function values for different variable values.
		/// </summary>
		/// <remarks>
		/// The callback has several arguments.
		/// The first argument is the INonlinearModel being solved.
		/// The second is a System.Int32 representing the row (goal or constraint) id to be evaluated.
		/// The third is a ValuesByIndex containing the current variable values accessible by id.
		/// Next is a System.Boolean that indicates whether this is first evaluator call with the current variable values.
		/// The return value is a System.Double which should be the value of the row for the current variable 
		/// values.
		/// This callback must be set before trying to solving the model.</remarks>
		Func<INonlinearModel, int, ValuesByIndex, bool, double> FunctionEvaluator { get; set; }

		/// <summary>
		/// Gradient callback. The callback will be invoked periodically by the solver in order to obtain
		/// gradient information for different variable values.
		/// </summary>
		/// <remarks>
		/// The callback has several arguments.
		/// The first argument is the INonlinearModel being solved.
		/// The second is a System.Int32 representing the row (goal or constraint) id to be evaluated.
		/// The third is a ValuesByIndex containing the current variable values accessible by id.
		/// Next is a System.Boolean that indicates whether this is first evaluator call with the current variable values.
		/// The last argument is a ValuesByIndex object which should contain the gradient values on return.
		/// The callback function should set the values for all variables that were previously declared as active
		/// using the SetActiveVariables method.
		/// </remarks>
		Action<INonlinearModel, int, ValuesByIndex, bool, ValuesByIndex> GradientEvaluator { get; set; }

		/// <summary>Specify variables that participate in the row. 
		/// </summary>
		/// <param name="rowVid">The row id.</param>
		/// <returns>Enumeration of active variables on rowVid</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if rowVid is not a legal row id.</exception>
		/// <remarks>In the case of a model which has explicit linear terms (implements ILinearModel),
		/// there is no need to specify the linear terms with this call. If no call to SetActiveVariables
		/// has been made for the row then this method will return an empty result.
		/// </remarks>
		IEnumerable<int> GetActiveVariables(int rowVid);

		/// <summary>Indicates whether a variable is active in the specified row.
		/// </summary>
		/// <param name="rowVid">The row id.</param>
		/// <param name="varVid">The variable id.</param>
		/// <returns>Returns true if variable is active, otherwise false.</returns>
		/// <exception cref="T:System.ArgumentException">Thrown if rowVid is not a legal row index, 
		/// or varVid is not a legal variable index.</exception>
		bool IsActiveVariable(int rowVid, int varVid);

		/// <summary>Set all variables in a row to be active (or inactive).
		/// </summary>
		/// <param name="rowVid">The row id.</param>
		/// <param name="active">If true, all variables become active, 
		/// if false all variables become inactive.</param>
		/// <exception cref="T:System.ArgumentException">Thrown if rowVid is not a legal row index.</exception>
		void SetActiveVariables(int rowVid, bool active);

		/// <summary>Sets a variable in the specified row to be active (or inactive).
		/// </summary>
		/// <param name="rowVid">The row id.</param>
		/// <param name="varVid">The variable id.</param>
		/// <param name="active">If true, the variable becomes active, 
		/// if false it becomes inactive.</param>
		/// <exception cref="T:System.ArgumentException">Thrown if rowVid is not a legal row index, 
		/// or varVid is not a legal variable index.</exception>
		void SetActiveVariable(int rowVid, int varVid, bool active);
	}
}
