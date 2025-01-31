namespace Microsoft.SolverFoundation.Services
{
	/// <summary>Properties that can be retrieved by events raised by the Interior Point solver.
	/// </summary> 
	public static class InteriorPointProperties
	{
		/// <summary>Absolute duality gap as a Double.
		/// </summary>
		public static readonly string AbsoluteGap = "AbsoluteGap";

		/// <summary>The primal objective value as a Double.
		/// </summary>
		public static readonly string PrimalObjectiveValue = "PrimalObjectiveValue";

		/// <summary>The dual objective value as a Double.
		/// </summary>
		public static readonly string DualObjectiveValue = "DualObjectiveValue";
	}
}
