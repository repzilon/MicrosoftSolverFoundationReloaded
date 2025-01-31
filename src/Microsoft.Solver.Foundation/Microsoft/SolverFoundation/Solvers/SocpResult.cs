namespace Microsoft.SolverFoundation.Solvers
{
	internal enum SocpResult
	{
		Optimal,
		InfeasiblePrimal,
		InfeasibleDual,
		InfeasiblePrimalAndDual,
		InfeasiblePrimalOrDual,
		IllPosed,
		NumericalDifficulty,
		Interrupted,
		Invalid
	}
}
