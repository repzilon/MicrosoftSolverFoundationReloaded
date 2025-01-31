namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Root class for unary constraints, i.e. constraints over 1 variable.
	/// </summary>
	/// <remarks>
	///   Main reason for this class is to have direct, typed access to the 
	///   variable that is constrained (avoids casts or new virtual methods
	///   added to root class for Discrete Variable)
	/// </remarks>
	internal abstract class UnaryConstraint<A> : DisolverConstraint where A : DiscreteVariable
	{
		protected A _x;

		public UnaryConstraint(Problem p, A x)
			: base(p, x)
		{
			_x = x;
		}
	}
}
