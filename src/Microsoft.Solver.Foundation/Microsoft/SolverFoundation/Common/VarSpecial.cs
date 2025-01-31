namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Track special conditions for a variable
	/// </summary>
	internal enum VarSpecial : byte
	{
		/// <summary> Unbounded variable
		/// </summary>
		Unbounded = 1,
		/// <summary> Variable has quadratic off-diagonal term(s)
		/// </summary>
		NotSeparable = 2,
		/// <summary> Variable has a quadratic term on the diagonal
		/// </summary>
		QDiagonal = 4
	}
}
