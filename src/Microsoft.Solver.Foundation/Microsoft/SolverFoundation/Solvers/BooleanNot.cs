using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form bResult == (b0 != b1 != ...) where bN are Booleans
	/// </summary>
	internal sealed class BooleanNot : LogicFunction
	{
		private bool _resolved;

		internal override string Name => "!";

		/// <summary> bResult == !b0
		/// </summary>
		internal BooleanNot(ConstraintSystem solver, CspSolverTerm input)
			: base(solver, input)
		{
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override void Reset()
		{
			_resolved = false;
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			if (base.Count == 0)
			{
				conflict = this;
				return false;
			}
			conflict = null;
			if (_resolved)
			{
				return false;
			}
			CspSolverTerm cspSolverTerm = _inputs[0];
			if (cspSolverTerm.Count == 0)
			{
				conflict = cspSolverTerm;
				return false;
			}
			_resolved = true;
			if (1 == base.Count)
			{
				return cspSolverTerm.Force(!IsTrue, out conflict);
			}
			if (1 == cspSolverTerm.Count)
			{
				return Force(!cspSolverTerm.IsTrue, out conflict);
			}
			_resolved = false;
			return false;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Not(inputs[0]);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Not(inputs[0]);
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			ls[this] = BooleanFunction.Not(ls[_inputs[0]]);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			return new KeyValuePair<CspSolverTerm, int>(_inputs[0], 1 - target);
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients gradients = ls.GetGradients(_inputs[0]);
			ValueWithGradients v = Gradients.Not(gradients);
			ls.SetGradients(this, v);
		}
	}
}
