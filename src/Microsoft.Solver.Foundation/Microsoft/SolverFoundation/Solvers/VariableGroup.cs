using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Subset of variables. Used whenever we need to distinguish which
	///   particular variables are involved, for instance, in a deduction.
	///   In terms of "constraint graph" a VariableGroup will typically 
	///   represent a (hyper-)edge of the graph.
	/// </summary>
	/// <remarks>
	///   Acts like a readonly collection - purposely not modifiable; because
	///   meant to be multi-referenced
	/// </remarks>
	internal class VariableGroup
	{
		private readonly DiscreteVariable[] _variables;

		private static readonly VariableGroup _empty = new VariableGroup();

		/// <summary>
		///   Number of variables in the variable group
		/// </summary>
		public int Length => _variables.Length;

		/// <summary>
		///   Direct access to the variable of given index
		/// </summary>
		public DiscreteVariable this[int idx] => _variables[idx];

		/// <summary>
		///   Construction - beware: ownership of array is transferred;
		///   we don't copy
		/// </summary>
		public VariableGroup(params DiscreteVariable[] vars)
		{
			_variables = vars;
		}

		/// <summary>
		///   Enumerates all variables in the group
		/// </summary>
		public IEnumerable<DiscreteVariable> GetVariables()
		{
			int len = _variables.Length;
			for (int i = 0; i < len; i++)
			{
				yield return _variables[i];
			}
		}

		/// <summary>
		///   Pre-allocated Variable group representing the empty set
		/// </summary>
		public static VariableGroup EmptyGroup()
		{
			return _empty;
		}

		private VariableGroup()
		{
			_variables = new DiscreteVariable[0];
		}
	}
}
