using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// Represents a node in the branch and bound tree.
	/// </summary>
	internal struct Node
	{
		private Constraint _constraint;

		private readonly OptimalGoalValues _lowerBoundGoalValue;

		private readonly Rational _expectedGoalValue;

		private readonly int _parent;

		/// <summary>
		/// Gets the ID of the parent node.
		/// </summary>
		public int Parent => _parent;

		/// <summary>
		/// Gets the number of individual constraints applying to the node.
		/// </summary>
		public int ConstraintCount
		{
			get
			{
				if (_constraint == null)
				{
					return 0;
				}
				return _constraint.Count;
			}
		}

		/// <summary>
		/// Gets the estimated goal value for the node.
		/// </summary>
		public Rational ExpectedGoalValue => _expectedGoalValue;

		/// <summary>
		/// Gets a lower bound goal value for the node.
		/// </summary>
		/// <remarks>
		/// The known goal value is the goal value of the parent node.
		/// </remarks>
		public OptimalGoalValues LowerBoundGoalValue => _lowerBoundGoalValue;

		/// <summary>
		/// Gets the variable the node branches on.
		/// </summary>
		public int BranchingVariable => (_constraint as BoundConstraint).Variable;

		/// <summary>
		/// Gets the value of the branching variable.
		/// </summary>
		public Rational BranchingValue => (_constraint as BoundConstraint).Bound;

		/// <summary>
		/// Gets the latest constraint.
		/// </summary>
		public Constraint LatestConstraint => _constraint;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="lowerBoundGoalValue"></param>
		/// <param name="expectedGoalValue"></param>
		public Node(int parent, OptimalGoalValues lowerBoundGoalValue, Rational expectedGoalValue)
		{
			_lowerBoundGoalValue = lowerBoundGoalValue;
			_expectedGoalValue = expectedGoalValue;
			_parent = parent;
			_constraint = null;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="lowerBoundGoalValue"></param>
		/// <param name="expectedGoalValue"></param>
		/// <param name="constraint"></param>
		public Node(int parent, OptimalGoalValues lowerBoundGoalValue, Rational expectedGoalValue, Constraint constraint)
			: this(parent, lowerBoundGoalValue, expectedGoalValue)
		{
			DebugContracts.NonNull(constraint);
			_constraint = constraint;
		}

		/// <summary>
		/// Applies the constraint related to the node to the model.
		/// </summary>
		/// <param name="thread"></param>
		public void ApplyConstraints(SimplexTask thread)
		{
			if (_constraint != null)
			{
				_constraint.ApplyConstraint(thread);
			}
		}

		/// <summary>
		/// Removes the constraint related to the node from the model.
		/// </summary>
		/// <param name="thread"></param>
		public void ResetConstraints(SimplexTask thread)
		{
			if (_constraint != null)
			{
				_constraint.ResetConstraint(thread);
			}
		}

		internal void ExtendConstraint(Constraint constraint)
		{
			constraint.ExtendConstraint(LatestConstraint);
			_constraint = constraint;
		}

		internal CuttingPlanePool GetAncestorCutPool()
		{
			if (LatestConstraint != null)
			{
				return CuttingPlanePool.GetAncestorCutPool(LatestConstraint);
			}
			return null;
		}
	}
}
