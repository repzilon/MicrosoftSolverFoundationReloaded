namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form bResult == (b0 != b1 != ...) where bN are Booleans
	/// </summary>
	internal sealed class BooleanUnequal : LogicFunction
	{
		private bool _resolved;

		internal override string Name => "Unequal";

		/// <summary> bResult == (b0 != b1 != ...)
		/// </summary>
		internal BooleanUnequal(ConstraintSystem solver, params CspSolverTerm[] inputs)
			: base(solver, inputs)
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
			int width = Width;
			if (2 < width)
			{
				return Force(choice: false, out conflict);
			}
			if (width < 2)
			{
				return Force(choice: true, out conflict);
			}
			CspSolverTerm cspSolverTerm = _inputs[0];
			if (cspSolverTerm.Count == 0)
			{
				conflict = cspSolverTerm;
				return false;
			}
			CspSolverTerm cspSolverTerm2 = _inputs[1];
			if (cspSolverTerm2.Count == 0)
			{
				conflict = cspSolverTerm2;
				return false;
			}
			_resolved = true;
			if (1 == base.Count)
			{
				if (1 == cspSolverTerm.Count)
				{
					return cspSolverTerm2.Force(cspSolverTerm.IsTrue != IsTrue, out conflict);
				}
				if (1 == cspSolverTerm2.Count)
				{
					return cspSolverTerm.Force(cspSolverTerm2.IsTrue != IsTrue, out conflict);
				}
			}
			if (1 == cspSolverTerm.Count && 1 == cspSolverTerm2.Count)
			{
				return Force(cspSolverTerm.IsTrue != cspSolverTerm2.IsTrue, out conflict);
			}
			_resolved = false;
			return false;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Unequal(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Unequal(inputs);
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			int value;
			switch (_inputs.Length)
			{
			case 0:
			case 1:
				value = BooleanFunction.Satisfied;
				break;
			case 2:
			{
				int num = ls[_inputs[0]];
				int num2 = ls[_inputs[1]];
				value = BooleanFunction.And(BooleanFunction.Or(BooleanFunction.Not(num), BooleanFunction.Not(num2)), BooleanFunction.Or(num2, num));
				break;
			}
			default:
				value = BooleanFunction.Violated;
				break;
			}
			ls[this] = value;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients v;
			switch (_inputs.Length)
			{
			case 0:
			case 1:
				v = BooleanFunction.Satisfied;
				break;
			case 2:
			{
				ValueWithGradients gradients = ls.GetGradients(_inputs[0]);
				ValueWithGradients gradients2 = ls.GetGradients(_inputs[1]);
				v = Gradients.Not(Gradients.Equivalent(gradients, gradients2));
				break;
			}
			default:
				v = BooleanFunction.Violated;
				break;
			}
			ls.SetGradients(this, v);
		}
	}
}
