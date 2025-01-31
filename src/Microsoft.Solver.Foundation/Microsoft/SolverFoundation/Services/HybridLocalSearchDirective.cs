namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Directive for the hybrid local search solver.
	/// </summary>
	public class HybridLocalSearchDirective : Directive
	{
		/// <summary>
		/// Specifies that the Solve() method should continuously keep searching
		/// until aborted. In this case improved solutions are returned by callbacks.
		/// False by default. 
		/// </summary>
		public bool RunUntilTimeout { get; set; }

		/// <summary>Presolve level. -1 means automatic, 0 means no presolve.
		/// </summary>
		public int PresolveLevel { get; set; }

		/// <summary>
		/// Tolerance for values to be different and still considered equal. Set negative for default.
		/// </summary>
		public double EqualityTolerance { get; set; }

		/// <summary>
		/// Directive for the local search solver
		/// </summary>
		public HybridLocalSearchDirective()
		{
			RunUntilTimeout = false;
			PresolveLevel = -1;
			EqualityTolerance = -1.0;
		}

		/// <summary>
		/// Returns a representation of the directive as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "HybridLocalSearch(RunUntilTimeout = " + RunUntilTimeout + ", PresolveLevel = " + PresolveLevel + ", EqualityTolerance = " + EqualityTolerance + ")";
		}
	}
}
