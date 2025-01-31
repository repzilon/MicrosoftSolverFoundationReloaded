namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Different kinds of Simplex costing 
	/// </summary>
	public enum SimplexCosting
	{
		/// <summary> default 
		/// </summary>
		Default,
		/// <summary> let the solvers to pick. Usually Simplex will pick the steepest edge.
		/// </summary>
		Automatic,
		/// <summary> compute all reduced cost 
		/// </summary>
		BestReducedCost,
		/// <summary> steepest edge pricing 
		/// </summary>
		SteepestEdge,
		/// <summary> compute partial reduced cost  
		/// </summary>
		Partial,
		/// <summary> another partial pricing 
		/// </summary>
		NewPartial
	}
}
