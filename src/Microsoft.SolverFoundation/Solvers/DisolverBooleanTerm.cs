namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A Boolean Term, as manipulated by Disolver.
	///   (the main thing is that they can be assigned an initial truth value)
	/// </summary>
	internal abstract class DisolverBooleanTerm : DisolverTerm
	{
		private BooleanVariableState _initialValue;

		public override bool IsBoolean => true;

		internal BooleanVariableState InitialStatus => _initialValue;

		public override long InitialLowerBound => (_initialValue == BooleanVariableState.True) ? 1 : 0;

		public override long InitialUpperBound => (_initialValue != BooleanVariableState.False) ? 1 : 0;

		/// <summary>
		///   Construction of a Boolean Term
		/// </summary>
		internal DisolverBooleanTerm(IntegerSolver solver, DisolverTerm[] subterms)
			: base(solver, subterms)
		{
			_initialValue = BooleanVariableState.Unassigned;
			solver.AddBooleanTerm(this);
		}

		/// <summary>
		///   Use when the value of the Term is set, say, by constant
		///   propagation before resolution.
		/// </summary>
		public void SetInitialValue(bool b)
		{
			if (b)
			{
				_initialValue = BooleanVariableState.True;
			}
			else
			{
				_initialValue = BooleanVariableState.False;
			}
		}
	}
}
