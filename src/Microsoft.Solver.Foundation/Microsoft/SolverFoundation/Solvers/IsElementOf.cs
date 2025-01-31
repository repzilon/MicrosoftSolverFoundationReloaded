using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form bResult == (b0 != b1 != ...) where bN are Booleans
	/// </summary>
	internal sealed class IsElementOf : BooleanFunction
	{
		private CspSolverDomain _domain;

		internal override string Name => "IsElementOf";

		public CspSolverDomain Domain => _domain;

		/// <summary> bResult == !b0
		/// </summary>
		internal IsElementOf(ConstraintSystem solver, CspSolverTerm input, CspSolverDomain domain)
			: base(solver, input)
		{
			_domain = domain;
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(_domain);
			visitor.Visit(this);
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			if (base.Count == 0)
			{
				conflict = this;
				return false;
			}
			conflict = null;
			CspSolverTerm cspSolverTerm = _inputs[0];
			if (cspSolverTerm.Count == 0)
			{
				conflict = cspSolverTerm;
				return false;
			}
			if (1 == base.Count)
			{
				if (IsTrue)
				{
					return cspSolverTerm.Intersect(_domain, out conflict);
				}
				return cspSolverTerm.Exclude(_domain, out conflict);
			}
			if (cspSolverTerm.Count < 100)
			{
				CspSolverDomain finiteValue = cspSolverTerm.FiniteValue;
				bool flag = true;
				bool flag2 = true;
				foreach (int item in finiteValue.Forward())
				{
					if (_domain.Contains(item))
					{
						flag2 = false;
					}
					else
					{
						flag = false;
					}
				}
				if (flag)
				{
					return Force(choice: true, out conflict);
				}
				if (flag2)
				{
					return Force(choice: false, out conflict);
				}
			}
			return false;
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			CspDomain domain = ConstraintSystem.CloneDomain(newModel, _domain);
			return newModel.IsElementOf(inputs[0], domain);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			CspDomain domain = ConstraintSystem.CloneDomain(newModel, _domain);
			return newModel.IsElementOf(inputs[0], domain);
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			int num = ls[_inputs[0]];
			int num2;
			if (_domain is CspIntervalDomain cspIntervalDomain)
			{
				num2 = BooleanFunction.And(BooleanFunction.LessEqual(cspIntervalDomain.First, num), BooleanFunction.LessEqual(num, cspIntervalDomain.Last));
			}
			else if (_domain.Count == 0)
			{
				num2 = BooleanFunction.Violated;
			}
			else
			{
				num2 = int.MaxValue;
				foreach (int item in _domain.Forward())
				{
					num2 = BooleanFunction.Or(num2, BooleanFunction.Equal(num, item));
				}
			}
			ls[this] = num2;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients gradients = ls.GetGradients(_inputs[0]);
			ValueWithGradients valueWithGradients;
			if (_domain is CspIntervalDomain cspIntervalDomain)
			{
				valueWithGradients = Gradients.And(Gradients.LessEqual(cspIntervalDomain.First, gradients), Gradients.LessEqual(gradients, cspIntervalDomain.Last));
			}
			else if (_domain.Count == 0)
			{
				valueWithGradients = new ValueWithGradients(BooleanFunction.Violated);
			}
			else
			{
				valueWithGradients = int.MaxValue;
				foreach (int item in _domain.Forward())
				{
					valueWithGradients = Gradients.Or(valueWithGradients, Gradients.Equal(gradients, item));
				}
			}
			ls.SetGradients(this, valueWithGradients);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			bool flag = target == 1;
			CspSolverTerm cspSolverTerm = _inputs[0];
			Random randomSource = ls.RandomSource;
			CspSolverDomain finiteValue = cspSolverTerm.FiniteValue;
			int num = 0;
			if (flag)
			{
				_domain.Intersect(finiteValue, out var newD);
				num = ((newD.Count != 0) ? newD.Pick(randomSource) : finiteValue.Pick(randomSource));
			}
			else
			{
				for (int i = 0; i < 10; i++)
				{
					num = finiteValue.Pick(randomSource);
					if (!_domain.Contains(num))
					{
						break;
					}
				}
			}
			return new KeyValuePair<CspSolverTerm, int>(cspSolverTerm, num);
		}
	}
}
