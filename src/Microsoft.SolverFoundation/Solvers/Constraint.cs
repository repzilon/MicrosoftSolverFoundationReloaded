using System.Collections.Generic;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// The base class for constraints that can be added to a linear model.
	/// </summary>
	internal abstract class Constraint
	{
		private Constraint _parentConstraint;

		private int _count;

		private List<Constraint> _parentConstraints = new List<Constraint>();

		/// <summary>
		/// Gets and sets the parent constraint.
		/// </summary>
		public Constraint ParentConstraint => _parentConstraint;

		/// <summary>
		/// Gets and sets the number of individual constraints included in this constraint.
		/// </summary>
		public int Count => _count;

		internal void ExtendConstraint(Constraint parent)
		{
			_parentConstraint = parent;
			if (_parentConstraint != null)
			{
				_count = _parentConstraint.Count + 1;
			}
			else
			{
				_count = 1;
			}
		}

		/// <summary>
		/// Create a constraint instance
		/// </summary>
		public Constraint()
			: this(null)
		{
		}

		/// <summary>
		/// Construct a constraint and set the parent constraint.
		/// </summary>
		/// <param name="parentConstraint"></param>
		public Constraint(Constraint parentConstraint)
		{
			ExtendConstraint(parentConstraint);
		}

		/// <summary>
		/// Extends the current constraint with a new constraint.
		/// </summary>
		/// <param name="newConstraint"></param>
		public void SetParent(Constraint newConstraint)
		{
			DebugContracts.NonNull(newConstraint);
			_parentConstraint = newConstraint;
			_count = _parentConstraint.Count + 1;
		}

		/// <summary>
		/// Applies the constraint to the model. If there exists a parent constraint, will appy parent constraint first.
		/// </summary>
		/// <param name="thread"></param>    
		public void ApplyConstraint(SimplexTask thread)
		{
			_parentConstraints.Clear();
			for (Constraint parentConstraint = ParentConstraint; parentConstraint != null; parentConstraint = parentConstraint.ParentConstraint)
			{
				_parentConstraints.Add(parentConstraint);
			}
			for (int num = _parentConstraints.Count - 1; num >= 0; num--)
			{
				_parentConstraints[num].ApplyConstraintCore(thread);
			}
			ApplyConstraintCore(thread);
		}

		/// <summary>
		/// Removes the constraint to the model. If there exists a parent constraint, will remove parent constraint first.
		/// </summary>
		/// <param name="thread"></param>    
		public void ResetConstraint(SimplexTask thread)
		{
			_parentConstraints.Clear();
			for (Constraint parentConstraint = ParentConstraint; parentConstraint != null; parentConstraint = parentConstraint.ParentConstraint)
			{
				_parentConstraints.Add(parentConstraint);
			}
			for (int num = _parentConstraints.Count - 1; num >= 0; num--)
			{
				_parentConstraints[num].ResetConstraintCore(thread);
			}
			ResetConstraintCore(thread);
		}

		/// <summary>
		/// Applies the constraint to the model.
		/// </summary>
		/// <param name="thread"></param>
		public abstract void ApplyConstraintCore(SimplexTask thread);

		/// <summary>
		/// Removes the constraint from the model.
		/// </summary>
		/// <param name="thread"></param>
		public abstract void ResetConstraintCore(SimplexTask thread);
	}
}
