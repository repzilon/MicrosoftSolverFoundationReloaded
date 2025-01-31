using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary> A constraint of the form result == -arg
	/// </summary>
	internal sealed class Negate : CspFunction
	{
		internal override string Name => "Negate";

		/// <summary> A constraint of the form result == -arg
		/// </summary>
		internal Negate(ConstraintSystem solver, CspSolverTerm input)
			: base(solver, input)
		{
			ExcludeSymbols(input);
			InitMaximalScales();
		}

		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		internal override CspTerm Clone(ConstraintSystem newModel, CspTerm[] inputs)
		{
			return newModel.Neg(inputs[0]);
		}

		internal override CspTerm Clone(IntegerSolver newModel, CspTerm[] inputs)
		{
			return newModel.Neg(inputs[0]);
		}

		internal override bool Propagate(out CspSolverTerm conflict)
		{
			int count = base.Count;
			if (count == 0)
			{
				conflict = this;
				return false;
			}
			conflict = null;
			CspSolverTerm cspSolverTerm = _inputs[0];
			int count2 = cspSolverTerm.Count;
			if (count2 == 0)
			{
				conflict = cspSolverTerm;
				return false;
			}
			if (1 == count2)
			{
				int num = -cspSolverTerm.First;
				return Intersect(num, num, out conflict);
			}
			if (1 == count)
			{
				int num2 = -First;
				return cspSolverTerm.Intersect(num2, num2, out conflict);
			}
			bool flag = false;
			if (First != -cspSolverTerm.Last || Last != -cspSolverTerm.First)
			{
				flag = cspSolverTerm.Intersect(-Last, -First, out conflict);
				if (conflict != null)
				{
					return flag;
				}
				flag = Intersect(-cspSolverTerm.Last, -cspSolverTerm.First, out conflict) || flag;
			}
			count = base.Count;
			count2 = cspSolverTerm.Count;
			if (count == count2 && base.FiniteValue is CspIntervalDomain)
			{
				return flag;
			}
			if (count < 1000)
			{
				int[] array = new int[count];
				int num3 = 0;
				foreach (int item in Backward())
				{
					array[num3++] = -item;
				}
				flag |= cspSolverTerm.Intersect(out conflict, array);
				if (conflict != null)
				{
					return flag;
				}
				count2 = cspSolverTerm.Count;
				if (count == count2)
				{
					return flag;
				}
			}
			if (count2 < 1000)
			{
				int[] array2 = new int[count2];
				int num4 = 0;
				foreach (int item2 in cspSolverTerm.Backward())
				{
					array2[num4++] = -item2;
				}
				flag |= Intersect(out conflict, array2);
			}
			return flag;
		}

		/// <summary> Recompute the value of the term from the value of its inputs
		/// </summary>
		internal override void RecomputeValue(LocalSearchSolver ls)
		{
			int integerValue = ls.GetIntegerValue(_inputs[0]);
			if (integerValue == int.MinValue)
			{
				ls.SignalOverflow(this);
				return;
			}
			int value = -integerValue;
			ls[this] = value;
		}

		/// <summary> Recomputation of the gradient information attached to the term
		///           from the gradients and values of all its inputs
		/// </summary>
		internal override void RecomputeGradients(LocalSearchSolver ls)
		{
			ValueWithGradients integerGradients = ls.GetIntegerGradients(_inputs[0]);
			ValueWithGradients v = Gradients.Minus(integerGradients);
			ls.SetGradients(this, v);
		}

		/// <summary> Suggests a sub-term that is likely to be worth flipping
		/// </summary>
		internal override KeyValuePair<CspSolverTerm, int> SelectSubtermToFlip(LocalSearchSolver ls, int target)
		{
			return CspSolverTerm.CreateFlipSuggestion(_inputs[0], -target, ls.RandomSource);
		}
	}
}
