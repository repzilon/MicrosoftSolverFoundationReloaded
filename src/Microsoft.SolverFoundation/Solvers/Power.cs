using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form result == arg^power
	/// </summary>
	internal sealed class Power : ProductOrPower
	{
		private int _power;

		internal int Exponent => _power;

		internal override string Name => "Power";

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		/// <summary> A constraint of the form result == arg^power
		/// </summary>
		internal Power(ConstraintSystem solver, CspSolverTerm input, int power)
			: base(solver, input)
		{
			_power = power;
			InitPowerScales(_power);
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Power(inputs[0], _power);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Power(inputs[0], _power);
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			if (base.Count == 0)
			{
				conflict = this;
				return false;
			}
			CspSolverTerm cspSolverTerm = _inputs[0];
			int count = cspSolverTerm.Count;
			if (count == 0)
			{
				conflict = cspSolverTerm;
				return false;
			}
			conflict = null;
			if (_power == 0)
			{
				return Intersect(1, 1, out conflict);
			}
			if (1 == _power)
			{
				bool flag = cspSolverTerm.Intersect(base.FiniteValue, out conflict);
				if (conflict == null)
				{
					flag |= Intersect(cspSolverTerm.FiniteValue, out conflict);
				}
				return flag;
			}
			if (_power < 0)
			{
				bool flag = cspSolverTerm.Intersect(1, 1, out conflict);
				if (conflict == null)
				{
					flag |= Intersect(1, 1, out conflict);
				}
				return flag;
			}
			if (1 == count)
			{
				int first = cspSolverTerm.First;
				double num = Math.Pow(first, _power);
				if (num < (double)ConstraintSystem.MinFinite || (double)ConstraintSystem.MaxFinite < num)
				{
					return cspSolverTerm.Intersect(ConstraintSystem.DEmpty, out conflict);
				}
				int num2 = (int)num;
				return Intersect(num2, num2, out conflict);
			}
			return PropagatePower(cspSolverTerm, _power, 1, out conflict);
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			double x = ls.GetIntegerValue(_inputs[0]);
			double num = Math.Pow(x, _power);
			if (!CspSolverTerm.IsSafe(num))
			{
				ls.SignalOverflow(this);
			}
			ls[this] = (int)num;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients integerGradients = ls.GetIntegerGradients(_inputs[0]);
			ValueWithGradients v = Gradients.Power(integerGradients, _power);
			ls.SetGradients(this, v);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping,
		///           together with a suggestion of value for this subterm
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			double num = Math.Pow(target, 1.0 / (double)_power);
			return CspSolverTerm.CreateFlipSuggestion(_inputs[0], (int)num, ls.RandomSource);
		}
	}
}
