using System.Diagnostics;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Represents an lower bound constraint.
	/// </summary>
	[DebuggerDisplay("{Variable} => {Bound}")]
	internal class LowerBoundConstraint : BoundConstraint
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="bound"></param>
		public LowerBoundConstraint(int variable, Rational bound)
			: base(variable, bound)
		{
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="bound"></param>
		/// <param name="parentConstraint"></param>
		public LowerBoundConstraint(int variable, Rational bound, Constraint parentConstraint)
			: base(variable, bound, parentConstraint)
		{
		}

		/// <summary>
		/// Applies the constraint to the model.
		/// </summary>
		/// <param name="thread"></param>
		public override void ApplyConstraintCore(SimplexTask thread)
		{
			int var = thread.Model.GetVar(base.Variable);
			if (var != -1)
			{
				if (thread.Model.IsVarInteger(var))
				{
					thread.BoundManager.SetLowerBound(var, thread.Model.MapValueFromVidToVar(var, base.Bound.GetCeiling()));
				}
				else
				{
					thread.BoundManager.SetLowerBound(var, thread.Model.MapValueFromVidToVar(var, base.Bound));
				}
			}
		}

		/// <summary>
		/// Removes the constraint from the model.
		/// </summary>
		/// <param name="thread"></param>
		public override void ResetConstraintCore(SimplexTask thread)
		{
			int var = thread.Model.GetVar(base.Variable);
			if (var != -1)
			{
				thread.BoundManager.ResetLowerBound(var);
			}
		}
	}
}
