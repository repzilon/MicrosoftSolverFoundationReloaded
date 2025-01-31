namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> IPM algorithm solution metrics.
	/// </summary>
	public interface IInteriorPointStatistics
	{
		/// <summary> The number of rows in the solver model.
		/// </summary>
		int RowCount { get; }

		/// <summary> Total number of variables, user and slack.
		/// </summary>
		int VarCount { get; }

		/// <summary>Iteration count.
		/// </summary>
		int IterationCount { get; }

		/// <summary> The primal version of the objective.
		/// </summary>
		double Primal { get; }

		/// <summary> The dual version of the objective.
		/// </summary>
		double Dual { get; }

		/// <summary> The gap between primal and dual objective values.
		/// </summary>
		double Gap { get; }

		/// <summary> The kind of IPM algorithm used.
		/// </summary>
		InteriorPointAlgorithmKind Algorithm { get; }

		/// <summary> The form of KKT matrices used.
		/// </summary>
		InteriorPointKktForm KktForm { get; }
	}
}
