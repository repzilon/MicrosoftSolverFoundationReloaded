using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// NonlinearModel is an implementation of INonlinearModel. It represents a non-linear optimization model which has rows, 
	/// variables and goals, and in addition has callback that define the values and possible the gradients of the rows.
	/// </summary>
	public class NonlinearModel : RowVariableGoalModel, INonlinearModel, IRowVariableModel, IGoalModel
	{
		/// <summary>
		/// This is a sparse matrix by rows. There is no value associated with entry (row + variable). 
		/// Implicitly the value is boolean. The entry either exists or not
		/// </summary>
		protected class SparseMatrix
		{
			/// <summary>
			/// This is used as the sparse pattern for Jacobian.
			/// rowVid -&gt; hashset of variable vids
			/// </summary>
			private readonly Dictionary<int, HashSet<int>> _sparseMatrix;

			/// <summary>
			/// Row count of sparse matrix
			/// </summary>
			/// <remarks>This is O(1) operation</remarks>
			public int RowCount => _sparseMatrix.Count;

			/// <summary>
			/// Creates SparseMatrix instance
			/// </summary>
			public SparseMatrix()
			{
				_sparseMatrix = new Dictionary<int, HashSet<int>>();
			}

			/// <summary>Specify variables that participate in the row. 
			/// </summary>
			/// <param name="rowVid">The row index.</param>
			/// <returns>Enumeration of active variable on rowVid.</returns>
			/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index.</exception>
			public IEnumerable<int> GetActiveVariables(int rowVid)
			{
				if (!_sparseMatrix.ContainsKey(rowVid))
				{
					yield break;
				}
				HashSet<int> activeVariables = _sparseMatrix[rowVid];
				foreach (int item in activeVariables)
				{
					yield return item;
				}
			}

			/// <summary>Is a specific variable active in a specific row.
			/// </summary>
			/// <param name="rowVid">The row index.</param>
			/// <param name="varVid">The variable index.</param>
			/// <returns>True if variable is active, otherwise false.</returns>
			/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index, 
			/// or varVid is not a legal variable index.</exception>
			public bool IsActiveVariable(int rowVid, int varVid)
			{
				if (_sparseMatrix.ContainsKey(rowVid))
				{
					return _sparseMatrix[rowVid].Contains(varVid);
				}
				return false;
			}

			/// <summary>Set all variables in a row to be active/inactive
			/// </summary>
			/// <param name="rowVid">The row index.</param>
			/// <param name="variableIndexes">All variables for dense row.</param>
			/// <param name="active">If true, all variables become active, 
			/// if false all variables become inactive.</param>
			/// <exception cref="T:System.ArgumentNullException">variableIndices cannot be null.</exception>
			public void SetActiveVariables(int rowVid, IEnumerable<int> variableIndexes, bool active)
			{
				if (variableIndexes == null)
				{
					throw new ArgumentNullException("variableIndexes");
				}
				if (_sparseMatrix.ContainsKey(rowVid))
				{
					if (active)
					{
						_sparseMatrix[rowVid].UnionWith(variableIndexes);
					}
					else
					{
						_sparseMatrix[rowVid].Clear();
					}
				}
				else if (active)
				{
					HashSet<int> value = new HashSet<int>(variableIndexes);
					_sparseMatrix[rowVid] = value;
				}
			}

			/// <summary>Set a specific variable in a row to be active/inactive
			/// </summary>
			/// <param name="rowVid">The row index.</param>
			/// <param name="varVid">The variable index.</param>
			/// <param name="active">If true, the variable becomes active, 
			/// if false it becomes inactive.</param>
			/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index, 
			/// or varVid is not a legal variable index.</exception>
			public void SetActiveVariable(int rowVid, int varVid, bool active)
			{
				if (_sparseMatrix.ContainsKey(rowVid))
				{
					HashSet<int> hashSet = _sparseMatrix[rowVid];
					if (active)
					{
						hashSet.Add(varVid);
					}
					else
					{
						hashSet.Remove(varVid);
					}
				}
				else if (active)
				{
					HashSet<int> hashSet2 = new HashSet<int>();
					hashSet2.Add(varVid);
					_sparseMatrix[rowVid] = hashSet2;
				}
			}
		}

		private readonly SparseMatrix _jacobianSparsityPattern;

		/// <summary>
		/// This is used as the sparse pattern for jacobian.
		/// </summary>
		/// <remarks>Usually when talking about Jacobian it does not include the goal. 
		/// This one does include the goal's row (i.e. Jacobian rows + goals' gradient rows</remarks>
		protected SparseMatrix JacobianSparsityPattern => _jacobianSparsityPattern;

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
		/// Creates an instance of NonlinearModel
		/// </summary>
		/// <param name="comparer">Comparer for keys.</param>
		public NonlinearModel(IEqualityComparer<object> comparer)
			: base(comparer)
		{
			_jacobianSparsityPattern = new SparseMatrix();
		}

		/// <summary>
		/// Creates an instance of NonlinearModel
		/// </summary>
		public NonlinearModel()
			: this(null)
		{
		}

		/// <summary>Specify variables that participate in the row. 
		/// </summary>
		/// <param name="rowVid">The row index.</param>
		/// <returns>Enumeration of active variables on rowVid.</returns>
		/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index.</exception>
		public IEnumerable<int> GetActiveVariables(int rowVid)
		{
			ValidateRowVid(rowVid);
			return JacobianSparsityPattern.GetActiveVariables(rowVid);
		}

		/// <summary>Is a specific variable active in a specific row.
		/// </summary>
		/// <param name="rowVid">The row index.</param>
		/// <param name="varVid">The variable index.</param>
		/// <returns>True if variable is active, otherwise false.</returns>
		/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index, 
		/// or varVid is not a legal variable index.</exception>
		public bool IsActiveVariable(int rowVid, int varVid)
		{
			ValidateRowVid(rowVid);
			ValidateVid(varVid);
			return JacobianSparsityPattern.IsActiveVariable(rowVid, varVid);
		}

		/// <summary>Set all variables in a row to be active/inactive.
		/// </summary>
		/// <param name="rowVid">The row index.</param>
		/// <param name="active">If true, all variables become active, 
		/// if false all variables become inactive.</param>
		/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index.</exception>
		public void SetActiveVariables(int rowVid, bool active)
		{
			ValidateRowVid(rowVid);
			JacobianSparsityPattern.SetActiveVariables(rowVid, VariableIndices, active);
		}

		/// <summary>Set a specific variable in a row to be active/inactive.
		/// </summary>
		/// <param name="rowVid">The row index.</param>
		/// <param name="varVid">The variable index.</param>
		/// <param name="active">If true, the variable becomes active, 
		/// if false it becomes inactive.</param>
		/// <exception cref="T:System.ArgumentException">rowVid is not a legal row index, 
		/// or varVid is not a legal variable index.</exception>
		public void SetActiveVariable(int rowVid, int varVid, bool active)
		{
			ValidateRowVid(rowVid);
			ValidateVid(varVid);
			JacobianSparsityPattern.SetActiveVariable(rowVid, varVid, active);
		}

		/// <summary>Set all variables in all rows (include goal rows) to be active/inactive.
		/// </summary>
		/// <param name="active">If true, all variables become active, 
		/// if false all variables become inactive.</param>
		public virtual void SetActiveVariables(bool active)
		{
			foreach (int rowIndex in RowIndices)
			{
				JacobianSparsityPattern.SetActiveVariables(rowIndex, VariableIndices, active);
			}
		}
	}
}
