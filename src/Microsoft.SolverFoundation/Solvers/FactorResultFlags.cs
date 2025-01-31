using System;

namespace Microsoft.SolverFoundation.Solvers
{
	[Flags]
	internal enum FactorResultFlags
	{
		None = 0,
		Completed = 1,
		Substituted = 2,
		Permuted = 4,
		Abort = 8
	}
}
