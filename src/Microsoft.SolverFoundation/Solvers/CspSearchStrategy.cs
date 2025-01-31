namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   The description of the parameters used by the solver when constructing
	///   its strategy
	/// </summary>
	internal struct CspSearchStrategy
	{
		public SearchStrategyFlag Flag;

		public VariableEnumerationStrategy Variables;

		public ValueEnumerationStrategy Values;

		public bool UseRestarts;

		/// <summary>
		///   Construction of a search strategy based on a variable/value
		///   ordering
		/// </summary>
		/// <param name="var">type of variable ordering</param>
		/// <param name="val">type of value ordering</param>
		/// <param name="restarts">true if restarts should be used</param>
		public CspSearchStrategy(VariableEnumerationStrategy var, ValueEnumerationStrategy val, bool restarts)
		{
			Flag = SearchStrategyFlag.VariableValueHeuristic;
			Variables = var;
			Values = val;
			UseRestarts = false;
		}
	}
}
