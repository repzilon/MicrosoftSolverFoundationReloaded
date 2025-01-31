namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> different kinds of variables supported by the Simplex solver 
	/// </summary>
	public enum SimplexVarValKind
	{
		/// <summary>
		/// basis 
		/// </summary>
		Basic,
		/// <summary> fixed variable 
		/// </summary>
		Fixed,
		/// <summary> variables fixed the lower bound 
		/// </summary>
		Lower,
		/// <summary> variables fixed at the upper bound 
		/// </summary>
		Upper,
		/// <summary> free, no bound variables 
		/// </summary>
		Zero
	}
}
