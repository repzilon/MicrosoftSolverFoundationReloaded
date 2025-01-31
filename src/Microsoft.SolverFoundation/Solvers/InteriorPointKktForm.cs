namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Select the form of KKT matrix arithmetic to be used.
	/// </summary>
	public enum InteriorPointKktForm
	{
		/// <summary> Normal form is faster, but will be ignored for non-diagonal quadratics.
		/// </summary>
		Normal,
		/// <summary> Augmented form is more robust and general, but a little slower.
		/// </summary>
		Augmented,
		/// <summary> A blend of augmented matrix containing an embedded normal section.
		/// </summary>
		Blended
	}
}
