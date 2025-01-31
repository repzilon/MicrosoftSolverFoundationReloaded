namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Part of the Visitor pattern for visiting all components of a ConstraintSystem.
	/// These are embedded in all of the lower level parts of the model where we wish to dispatch the Visitation. 
	/// The implementations are trivial in most cases (call Visit on the visitor with 'this' as an argument).  It will be different wherever there
	/// are deep substructures  (see ConstraintSystem.Accept() for a more complex version).  Calling accept on a Term will use polymorphism to 
	/// get to the deepest class hierarchy class where the pattern is implemented.
	/// </summary>
	internal interface IVisitable
	{
		void Accept(IVisitor visitor);
	}
}
