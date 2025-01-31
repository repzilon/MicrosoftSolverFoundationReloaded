using System;

namespace Microsoft.SolverFoundation.Solvers
{
	[Flags]
	internal enum MIPVarFlags : byte
	{
		/// <summary>
		/// 0-1 variable type 
		/// </summary>
		Binary = 1,
		/// <summary>
		/// integer variable type 
		/// </summary>
		Integer = 2,
		/// <summary>
		/// real/double variable type
		/// </summary>
		Continuous = 4
	}
}
