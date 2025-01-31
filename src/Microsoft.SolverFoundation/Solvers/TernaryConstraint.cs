namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Root class for ternary constraints, i.e. constraints over 3 variables.
	/// </summary>
	/// <remarks>
	///   Main reason for this class is to have direct, typed access to the 
	///   3 variables that are constrained (avoids casts or new virtual methods
	///   added to root class for Discrete Variable)
	/// </remarks>
	internal abstract class TernaryConstraint<A, B, C> : DisolverConstraint where A : DiscreteVariable where B : DiscreteVariable where C : DiscreteVariable
	{
		protected A _x;

		protected B _y;

		protected C _z;

		public TernaryConstraint(Problem p, A x, B y, C z)
			: base(p, x, y, z)
		{
			_x = x;
			_y = y;
			_z = z;
		}
	}
}
