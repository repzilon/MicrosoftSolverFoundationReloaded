using System;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Lists the cuts the solver can generate.
	/// </summary>
	[Flags]
	public enum CutKind
	{
		/// <summary>
		/// No cuts.
		/// </summary>
		None = 0,
		/// <summary>
		/// Gomory cuts.
		/// </summary>
		GomoryFractional = 1,
		/// <summary>
		/// Cover cuts.
		/// </summary>
		Cover = 2,
		/// <summary>
		/// Mixed cover cuts.
		/// </summary>
		MixedCover = 4,
		/// <summary>
		/// Flow cover cuts.
		/// </summary>
		FlowCover = 8,
		/// <summary>
		/// Default set of cuts.
		/// </summary>
		Default = 1,
		/// <summary>
		/// All cuts.
		/// </summary>
		All = 0xF
	}
}
