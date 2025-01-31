namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Root class for binary constraints, i.e. constraints over 2 variables.
	/// </summary>
	/// <remarks>
	///   Main reason for this class is to have direct, typed access to the 
	///   2 variables that are constrained (avoids casts or new virtual methods
	///   added to root class for Discrete Variable)
	/// </remarks>
	internal abstract class BinaryConstraint<A, B> : DisolverConstraint where A : DiscreteVariable where B : DiscreteVariable
	{
		protected A _x;

		protected B _y;

		public BinaryConstraint(Problem p, A x, B y)
			: base(p, x, y)
		{
			_x = x;
			_y = y;
		}
	}
}
