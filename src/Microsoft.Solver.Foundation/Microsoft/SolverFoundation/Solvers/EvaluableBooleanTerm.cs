namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Evaluable term representing a Boolean expression but where
	/// the value of the term is in fact a violation indicator, which
	/// is positive if the expression is false and negative if it is true.
	/// </summary>
	internal abstract class EvaluableBooleanTerm : EvaluableTerm
	{
		protected double _violation;

		/// <summary>
		/// The current violation of the term
		/// </summary>
		public double Violation => _violation;

		/// <summary>
		/// The Boolean value of the term
		/// </summary>
		public bool Value
		{
			get
			{
				return Violation < 0.0;
			}
			protected set
			{
				_violation = ((!value) ? 1 : (-1));
			}
		}

		public sealed override double ValueAsDouble
		{
			get
			{
				if (!(_violation < 0.0))
				{
					return 0.0;
				}
				return 1.0;
			}
		}

		internal sealed override double StoredValue => _violation;

		protected EvaluableBooleanTerm(int depth)
			: base(depth)
		{
		}
	}
}
