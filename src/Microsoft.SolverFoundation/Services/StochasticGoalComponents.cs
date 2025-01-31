using System;

namespace Microsoft.SolverFoundation.Services
{
	[Flags]
	internal enum StochasticGoalComponents
	{
		None = 0,
		Decision = 1,
		RandomParameter = 2,
		RandomParameterTimesDecision = 4
	}
}
