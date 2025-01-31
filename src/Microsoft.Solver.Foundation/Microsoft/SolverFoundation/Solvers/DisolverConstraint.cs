using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	///   Root class for constraints.
	///   Provides minimal interface to allows browsing on the "constraint
	///   graph"; i.e. essentially we have access to the list of variables 
	///   that is constrained.
	/// </summary>
	internal abstract class DisolverConstraint : Indexed
	{
		protected const bool Success = true;

		protected Problem _problem;

		protected VariableGroup _signature;

		/// <summary>
		///   Access to all discrete variables that are connected by the
		///   constraint.
		/// </summary>
		/// <remarks>
		///   The variable group has a readonly interface so this object can
		///   be referenced multiple times by whoever needs to.
		/// </remarks>
		public VariableGroup Signature => _signature;

		/// <summary>
		///   If the constraint generates a deduction, Cause will represent
		///   the natural cause of the modification. Used in most cases;  
		///   except when a refined variable group can be specified.
		/// </summary>
		protected Cause Cause => new Cause(this, _signature);

		internal DisolverConstraint(Problem p, params DiscreteVariable[] vars)
			: base(p.Constraints)
		{
			_problem = p;
			_signature = new VariableGroup(vars);
			Type type = GetType();
			Dictionary<Type, int> constraintCountByType = _problem.ConstraintCountByType;
			if (constraintCountByType.ContainsKey(type))
			{
				constraintCountByType[type]++;
			}
			else
			{
				constraintCountByType.Add(type, 1);
			}
		}
	}
}
