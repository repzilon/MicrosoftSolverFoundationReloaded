namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A value ordering that always returns the highest
	///   available value, i.e. the values will be enumerated
	///   in decreasing order
	/// </summary>
	internal class InvLexValueSelector : ValueSelector
	{
		public InvLexValueSelector(TreeSearchAlgorithm p)
			: base(p)
		{
		}

		public override long DecideValue(DiscreteVariable v)
		{
			return v.GetUpperBound();
		}
	}
}
