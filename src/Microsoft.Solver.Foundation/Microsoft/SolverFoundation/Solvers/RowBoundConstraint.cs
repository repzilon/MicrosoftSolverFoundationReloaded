using System.Diagnostics;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Represents a constraint on row bounds.
	/// </summary>
	[DebuggerDisplay("{_lowerBound} <= {_row} <= {_upperBound}")]
	internal class RowBoundConstraint : Constraint
	{
		private readonly int _row;

		private readonly Rational _lowerBound;

		private readonly Rational _upperBound;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="lowerBound"></param>
		/// <param name="upperBound"></param>
		public RowBoundConstraint(int row, Rational lowerBound, Rational upperBound)
		{
			_row = row;
			_lowerBound = lowerBound;
			_upperBound = upperBound;
		}

		/// <summary>
		/// Applies the constraint to the model.
		/// </summary>
		/// <param name="thread"></param>
		public override void ApplyConstraintCore(SimplexTask thread)
		{
			if (!thread.Model.IsRowEliminated(_row))
			{
				thread.BoundManager.SetRowBounds(_row, thread.Model.MapValueFromVidToVar(thread.Model.GetSlackVarForRow(_row), _lowerBound), thread.Model.MapValueFromVidToVar(thread.Model.GetSlackVarForRow(_row), _upperBound));
			}
		}

		/// <summary>
		/// Removes the constraint from the model.
		/// </summary>
		/// <param name="thread"></param>
		public override void ResetConstraintCore(SimplexTask thread)
		{
			if (!thread.Model.IsRowEliminated(_row))
			{
				thread.BoundManager.ResetLowerBound(thread.Model.GetSlackVarForRow(_row));
				thread.BoundManager.ResetUpperBound(thread.Model.GetSlackVarForRow(_row));
			}
		}
	}
}
