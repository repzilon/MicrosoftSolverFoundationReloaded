namespace Microsoft.SolverFoundation.Services
{
	/// <summary> A general MIP directive to provide guidance for MIP solvers
	/// </summary>
	public class MixedIntegerProgrammingDirective : SimplexDirective
	{
		/// <summary> Gets or sets the tolerance to declare an integer solution optimal.
		/// </summary>
		public double GapTolerance { get; set; }

		/// <summary> Whether or not to focus on getting quick feasibility
		/// </summary>
		public bool QuickFeasibility { get; set; }

		/// <summary> Enable/disable cutting plane 
		/// </summary>
		public bool CuttingPlaneGeneration { get; set; }

		/// <summary> Enable/disable local search in Mixed Integer Solvers
		/// </summary>
		public bool LocalSearch { get; set; }

		/// <summary>Default constructor with default values for the MIP solver.
		/// </summary>
		public MixedIntegerProgrammingDirective()
		{
			CuttingPlaneGeneration = true;
		}

		/// <summary>
		/// Returns a representation of the directive as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "MixedInteger(GapTolerance = " + GapTolerance + ", QuickFeasibility = " + QuickFeasibility + ", CuttingPlaneGeneration = " + CuttingPlaneGeneration + ", LocalSearch = " + LocalSearch + ", " + base.ToString() + ")";
		}
	}
}
