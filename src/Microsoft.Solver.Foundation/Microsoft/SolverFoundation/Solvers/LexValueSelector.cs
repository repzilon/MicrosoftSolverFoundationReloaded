namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   A value ordering that always returns the lowest
	///   available value, i.e. the values will be enumerated
	///   in increasing order
	/// </summary>
	internal class LexValueSelector : ValueSelector
	{
		public LexValueSelector(TreeSearchAlgorithm p)
			: base(p)
		{
		}

		public override long DecideValue(DiscreteVariable v)
		{
			return v.GetLowerBound();
		}
	}
}
