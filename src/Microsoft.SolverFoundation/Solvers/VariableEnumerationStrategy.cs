namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Types of Variable enumeration strategies
	/// </summary>
	internal enum VariableEnumerationStrategy
	{
		Lex,
		MinDom,
		Random,
		RoundRobin,
		Impact,
		Vsids,
		ConfLex,
		DomWdeg
	}
}
