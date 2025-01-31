using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A bound constraint that can be added to a linear model.
	/// </summary>
	internal abstract class BoundConstraint : Constraint
	{
		private readonly int _variable;

		private readonly Rational _bound;

		/// <summary>
		/// Gets the variable involved in the constraint.
		/// </summary>
		public int Variable => _variable;

		/// <summary>
		/// Gets the new bound of the variable.
		/// </summary>
		public Rational Bound => _bound;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="bound"></param>
		public BoundConstraint(int variable, Rational bound)
			: this(variable, bound, null)
		{
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="bound"></param>
		/// <param name="parentConstraint"></param>
		public BoundConstraint(int variable, Rational bound, Constraint parentConstraint)
			: base(parentConstraint)
		{
			_variable = variable;
			_bound = bound;
		}
	}
}
