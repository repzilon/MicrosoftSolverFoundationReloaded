namespace Microsoft.SolverFoundation.Services
{
	/// <summary>
	/// Heuristic to use for variable selection in CSP
	/// </summary>
	public enum TreeSearchVariableSelection
	{
		/// <summary>
		/// Use whatever heuristic the solver thinks is best
		/// </summary>
		Default,
		/// <summary>
		/// Enumeration that chooses a variable with smallest domain
		/// </summary> 
		MinimalDomainFirst,
		/// <summary>
		/// Enumeration following the declaration order of the variables
		/// </summary>
		DeclarationOrder,
		/// <summary>
		/// Weigh variables dynamically according to their dependents and current domain sizes
		/// </summary>
		DynamicWeighting,
		/// <summary>
		/// Enumeration based on conflict analysis following a variant 
		/// of the VSIDS heuristic used in SAT solvers
		/// </summary>
		ConflictDriven,
		/// <summary>
		/// Enumeration based on a forecast of the impact 
		/// of the decision
		/// </summary>
		ImpactPrediction,
		/// <summary>
		/// Enumeration based on the "domain over weighted degree"
		/// </summary> 
		DomainOverWeightedDegree
	}
}
