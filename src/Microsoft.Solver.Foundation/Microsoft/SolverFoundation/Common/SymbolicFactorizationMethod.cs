namespace Microsoft.SolverFoundation.Common
{
	/// <summary>Symbolic factorization method.
	/// </summary>
	internal enum SymbolicFactorizationMethod
	{
		/// <summary> System chooses.
		/// </summary>
		Automatic = -1,
		/// <summary> Minimize local fill.
		/// </summary>
		LocalFill,
		/// <summary> Approximate minimum degree.
		/// </summary>
		AMD
	}
}
