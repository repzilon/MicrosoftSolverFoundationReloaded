using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Data structure that specifies solution for standard LP model
	/// </summary>
	internal class StandardSolution
	{
		/// <summary> primal variables, A·x = b
		/// </summary>
		public double[] x;

		/// <summary> dual variables, A'·y + z = c
		/// </summary>
		public double[] y;

		/// <summary> dual slacks, A'·y + z = c
		/// </summary>
		public double[] z;

		/// <summary> costs, primal: minimize c·x, dual: A'·y + z = c
		/// </summary>
		public double cx;

		/// <summary> RHS, primal: A·x = b, dual: maximize b·y
		/// </summary>
		public double by;

		/// <summary> the result of the solve
		/// </summary>
		public LinearResult status;

		/// <summary> relative gap (cx-by)/(1+cx)
		/// </summary>
		public double relGap;

		/// <summary> relative primal feasibility ||A*x-b||/(1+||b||)
		/// </summary>
		public double relPrmlFeas;

		/// <summary> relative dual feasibility ||c-A'*y-z||/(1+||c||)
		/// </summary>
		public double relDualFeas;
	}
}
