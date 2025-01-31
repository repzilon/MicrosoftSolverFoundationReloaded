using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	internal abstract class CspSolver
	{
		protected ConstraintSystem _model;

		internal CspSolver(ConstraintSystem model)
		{
			_model = model;
		}

		/// <summary> Runs the algorithm; every time a solution is found the method
		///           yield-returns and a solution can be accessed either value by
		///           value or by batch using a snapshot
		/// </summary>
		/// <param name="yieldSuboptimals">true if intermdiate solutions 
		///           found during the search should be returned, false
		///           if we should only return provably optimal solutions</param>
		/// <returns> A sequence of ints, each of which indicates the solution number
		/// </returns>
		internal abstract IEnumerable<int> Search(bool yieldSuboptimals);

		/// <summary> Gets the current value associated to a Term by this algorithm
		/// </summary>
		/// <param name="variable">A CspSolverTerm</param>
		internal abstract object GetValue(CspTerm variable);

		/// <summary> Creates a representation of the current solution
		///           where values are integers
		/// </summary>
		internal abstract Dictionary<CspTerm, int> SnapshotVariablesIntegerValues();

		/// <summary> Creates a representation of the current solution
		///           where values are objects
		/// </summary>
		internal abstract Dictionary<CspTerm, object> SnapshotVariablesValues();
	}
}
