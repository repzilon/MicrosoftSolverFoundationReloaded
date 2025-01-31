using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form bResult == (a -&gt; c) where a and c are Booleans
	/// </summary>
	internal sealed class BooleanImplies : LogicFunction
	{
		private bool _resolved;

		internal override string Name => "-:";

		/// <summary> A constraint of the form bResult == (a -&gt; c) where a and c are Booleans
		/// </summary>
		internal BooleanImplies(ConstraintSystem solver, CspSolverTerm antecedent, CspSolverTerm consequent)
			: base(solver, antecedent, consequent)
		{
			Reset();
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
			CspSolverTerm cspSolverTerm2 = _inputs[1];
			if (cspSolverTerm.Count == 0)
			{
				conflict = cspSolverTerm;
				return false;
			}
			if (cspSolverTerm2.Count == 0)
			{
				conflict = cspSolverTerm2;
				return false;
			}
			_resolved = true;
			if (1 == base.Count)
			{
				if (!IsTrue)
				{
					bool flag = false;
					flag |= cspSolverTerm.Force(choice: true, out conflict);
					if (conflict != null)
					{
						return flag;
					}
					return flag | cspSolverTerm2.Force(choice: false, out conflict);
				}
				if (1 == cspSolverTerm.Count)
				{
					if (cspSolverTerm.IsTrue)
					{
						return cspSolverTerm2.Force(choice: true, out conflict);
					}
				}
				else if (1 == cspSolverTerm2.Count && !cspSolverTerm2.IsTrue)
				{
					return cspSolverTerm.Force(choice: false, out conflict);
				}
			}
			else if (1 == cspSolverTerm.Count)
			{
				if (!cspSolverTerm.IsTrue)
				{
					return Force(choice: true, out conflict);
				}
				if (1 == cspSolverTerm2.Count)
				{
					return Force(!cspSolverTerm.IsTrue || cspSolverTerm2.IsTrue, out conflict);
				}
			}
			else if (1 == cspSolverTerm2.Count && cspSolverTerm2.IsTrue)
			{
				return Force(choice: true, out conflict);
			}
			_resolved = false;
			return false;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Implies(inputs[0], inputs[1]);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Implies(inputs[0], inputs[1]);
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			int violation = ls[_inputs[0]];
			int r = ls[_inputs[1]];
			ls[this] = BooleanFunction.Or(BooleanFunction.Not(violation), r);
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients gradients = ls.GetGradients(_inputs[0]);
			ValueWithGradients gradients2 = ls.GetGradients(_inputs[1]);
			ValueWithGradients v = Gradients.Implies(gradients, gradients2);
			ls.SetGradients(this, v);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			CspSolverTerm cspSolverTerm = _inputs[0];
			CspSolverTerm cspSolverTerm2 = _inputs[1];
			Random randomSource = ls.RandomSource;
			bool flag = target == 1;
			bool flag2 = flag ^ BooleanFunction.IsSatisfied(ls[cspSolverTerm]);
			bool flag3 = flag == BooleanFunction.IsSatisfied(ls[cspSolverTerm2]);
			CspSolverTerm cspSolverTerm3 = ((flag2 != flag3) ? (flag2 ? cspSolverTerm2 : cspSolverTerm) : _inputs[randomSource.Next(2)]);
			int val = (object.ReferenceEquals(cspSolverTerm3, cspSolverTerm) ? (1 - target) : target);
			return CspSolverTerm.CreateFlipSuggestion(cspSolverTerm3, val, randomSource);
		}
	}
}
