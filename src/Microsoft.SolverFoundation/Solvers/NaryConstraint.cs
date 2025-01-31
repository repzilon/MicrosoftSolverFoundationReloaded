namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Root class for Nary constraints, i.e. constraints over an arbitrary
	///   number of variables (these variables have to be of homogeneous type)
	/// </summary>
	/// <remarks>
	///   Main reason for this class is to have direct, typed access to the 
	///   N variables that are constrained (avoids casts or new virtual methods
	///   added to root class for Discrete Variable)
	/// </remarks>
	internal abstract class NaryConstraint<A> : DisolverConstraint where A : DiscreteVariable
	{
		protected A[] _args;

		public NaryConstraint(Problem p, params A[] args)
			: base(p, args)
		{
			_args = args;
		}
	}
}
