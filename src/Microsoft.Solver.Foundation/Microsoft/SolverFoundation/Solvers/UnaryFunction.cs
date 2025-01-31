namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A function whose input is one parameter of a generic type 
	///   and that returns a result from another generic type.
	/// </summary>
	internal delegate B UnaryFunction<A, B>(A src);
}
