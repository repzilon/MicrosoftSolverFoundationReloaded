namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> Constraint for bResult == (b0 == b1 == ...) between boolean args
	/// </summary>
	internal sealed class BooleanEqual : LogicFunction
	{
		private bool _resolved;

		internal override string Name => "Equal";

		/// <summary> bResult == (b0 == b1 == ...)
		/// </summary>
		internal BooleanEqual(ConstraintSystem solver, params CspSolverTerm[] inputs)
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
			if (width < 2)
			{
				return Force(choice: true, out conflict);
			}
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			for (int i = 0; i < width; i++)
			{
				CspSolverTerm cspSolverTerm = _inputs[i];
				if (cspSolverTerm.Count == 0)
				{
					conflict = cspSolverTerm;
					return false;
				}
				if (1 == cspSolverTerm.Count)
				{
					if (flag)
					{
						if (flag3 != cspSolverTerm.IsTrue)
						{
							conflict = cspSolverTerm;
							break;
						}
					}
					else
					{
						flag = true;
						flag3 = cspSolverTerm.IsTrue;
					}
				}
				else
				{
					flag2 = true;
				}
			}
			bool flag4 = false;
			if (conflict != null)
			{
				flag4 = Force(choice: false, out var conflict2);
				if (conflict2 == null)
				{
					conflict = null;
				}
				_resolved = true;
				return flag4;
			}
			if (!flag2)
			{
				_resolved = true;
				return Force(choice: true, out conflict);
			}
			if (flag && 1 == base.Count && IsTrue)
			{
				for (int j = 0; j < width; j++)
				{
					CspSolverTerm cspSolverTerm2 = _inputs[j];
					if (1 < cspSolverTerm2.Count)
					{
						flag4 |= cspSolverTerm2.Force(flag3, out conflict);
						if (conflict != null)
						{
							break;
						}
					}
				}
				_resolved = true;
			}
			return flag4;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Equal(inputs);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Equal(inputs);
		}

		/// <summary> Naive recomputation: Recompute the value of the term 
		///           from the value of all its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			int num = _inputs.Length;
			int num2;
			if (num < 2)
			{
				num2 = BooleanFunction.Satisfied;
			}
			else
			{
				num2 = int.MinValue;
				int num3 = ls[_inputs[0]];
				for (int i = 1; i < num; i++)
				{
					int num4 = num3;
					num3 = ls[_inputs[i]];
					num2 = BooleanFunction.And(num2, BooleanFunction.And(BooleanFunction.Or(BooleanFunction.Not(num4), num3), BooleanFunction.Or(BooleanFunction.Not(num3), num4)));
				}
			}
			ls[this] = num2;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			if (_inputs.Length < 2)
			{
				ls.SetGradients(this, BooleanFunction.Satisfied);
				return;
			}
			ValueWithGradients valueWithGradients = new ValueWithGradients(int.MinValue);
			ValueWithGradients gradients = ls.GetGradients(_inputs[0]);
			for (int i = 1; i < _inputs.Length; i++)
			{
				ValueWithGradients x = gradients;
				gradients = ls.GetGradients(_inputs[i]);
				valueWithGradients = Gradients.And(valueWithGradients, Gradients.Equivalent(x, gradients));
			}
			ls.SetGradients(this, valueWithGradients);
		}
	}
}
