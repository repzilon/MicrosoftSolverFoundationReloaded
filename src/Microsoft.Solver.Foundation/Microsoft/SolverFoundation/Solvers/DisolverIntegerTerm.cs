namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   An integer-valued variable, as manipulated by Disolver.
	/// </summary>
	internal abstract class DisolverIntegerTerm : DisolverTerm
	{
		private readonly long _lowerBound;

		private readonly long _upperBound;

		public override long InitialLowerBound => _lowerBound;

		public override long InitialUpperBound => _upperBound;

		public virtual double DomainSize => Width;

		public long Width => InitialUpperBound - InitialLowerBound + 1;

		public override bool IsBoolean => false;

		/// <summary>
		///   Construction of an integer Term
		/// </summary>
		/// <param name="solver">solver to which the term is connected</param>
		/// <param name="l">initial lower bound</param>
		/// <param name="r">initial upper bound</param>
		/// <param name="subterms">list of subterms (possibly null)</param>
		internal DisolverIntegerTerm(IntegerSolver solver, long l, long r, DisolverTerm[] subterms)
			: base(solver, subterms)
		{
			_lowerBound = l;
			_upperBound = r;
			solver.AddIntegerTerm(this);
		}
	}
}
