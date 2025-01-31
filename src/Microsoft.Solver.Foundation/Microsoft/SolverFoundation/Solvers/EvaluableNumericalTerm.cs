namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Evaluable term with a numerical value. 
	/// This value is always a double for simplicity, although some
	/// terms might be constrained to discrete values
	/// </summary>
	internal abstract class EvaluableNumericalTerm : EvaluableTerm
	{
		protected double _value;

		/// <summary>
		/// The current value of the term
		/// </summary>
		public double Value => _value;

		public sealed override double ValueAsDouble => _value;

		internal sealed override double StoredValue => _value;

		protected EvaluableNumericalTerm(int depth)
			: base(depth)
		{
		}
	}
}
