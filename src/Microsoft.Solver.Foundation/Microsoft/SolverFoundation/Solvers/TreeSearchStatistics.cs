namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Statistics on the work done by a tree-search algorithm
	/// </summary>
	internal class TreeSearchStatistics
	{
		/// <summary>
		///   Number of events generated during whole search
		///   (one event corresponds to several delegate calls)
		/// </summary>
		public long TotalNbEvents;

		/// <summary>
		///   Number of nodes explored in the search tree
		/// </summary>
		public int TotalNbNodes;

		/// <summary>
		///   Number of nodes explored until reaching 1st solution
		/// </summary>
		public int NbNodesFirstSolution;

		/// <summary>
		///   Number of failures, i.e. leafs of the search tree
		/// </summary>
		public int NbFails;

		/// <summary>
		///   Number of restarts, i.e. times a tree was thrown away
		/// </summary>
		public int NbRestarts;

		/// <summary>
		///   Time in Milliseconds to the first solution
		/// </summary>
		public double TimeToFirstSolution;

		/// <summary>
		///   Time in Milliseconds to the last solution
		/// </summary>
		public double TimeToLastSolution;

		/// <summary>
		///   Number of constraints in the compiled representation of the problem
		/// </summary>
		public int NbConstraints;

		/// <summary>
		///   Total number of Boolean variables 
		///   in the compiled representation of the problem
		/// </summary>
		public int NbBooleanVariables;

		/// <summary>
		///   Total number of integer variables 
		///   in the compiled representation of the problem
		/// </summary>
		public int NbIntegerVariables;

		/// <summary>
		///   Number of user-defined Boolean variables 
		///   in the compiled representation of the problem
		/// </summary>
		public int NbUserDefinedBooleanVariables;

		/// <summary>
		///   Number of user-defined integer variables 
		///   in the compiled representation of the problem
		/// </summary>
		public int NbUserDefinedIntegerVariables;
	}
}
